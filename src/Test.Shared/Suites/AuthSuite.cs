namespace Test.Shared.Suites
{
    using System;
    using System.Collections.Generic;
    using Lattice.Core;
    using Lattice.Core.Helpers;
    using Lattice.Core.Models;
    using Lattice.Core.Security;
    using Touchstone.Core;

    /// <summary>
    /// Authentication, authorization, RBAC, and multi-tenancy suite. Exercises the security engine
    /// (password hashing, session-token codec, permission evaluation) and the auth data layer end to end
    /// against whichever provider the runner is configured for, so tenant isolation and cross-tenant
    /// rejection are validated on SQLite, MySQL, PostgreSQL, and SQL Server alike.
    /// </summary>
    public static class AuthSuite
    {
        #region Public-Methods

        /// <summary>
        /// Build the auth suite descriptor.
        /// </summary>
        /// <returns>The suite descriptor.</returns>
        public static TestSuiteDescriptor Build()
        {
            SuiteBuilder builder = new SuiteBuilder("auth", "Authentication, Authorization & Multi-tenancy");

            builder.Add("SHA-256 hashing is deterministic and compared in constant time", _ =>
            {
                string a = PasswordHasher.Sha256Hex("password");
                string b = PasswordHasher.Sha256Hex("password");
                TestAssert.Equal(a, b, "Same input must hash identically.");
                TestAssert.Equal(64, a.Length, "SHA-256 hex is 64 characters.");
                TestAssert.NotEqual(a, PasswordHasher.Sha256Hex("Password"), "Different input must hash differently.");
                TestAssert.True(PasswordHasher.ConstantTimeEquals(a, b), "Equal hashes must compare equal.");
                TestAssert.False(PasswordHasher.ConstantTimeEquals(a, "nope"), "Unequal hashes must compare unequal.");
                TestAssert.False(PasswordHasher.ConstantTimeEquals(null, a), "Null must never compare equal.");
                return System.Threading.Tasks.Task.CompletedTask;
            });

            builder.Add("Access keys are prefixed, high-entropy, and unique", _ =>
            {
                string k1 = AccessKeyGenerator.NewAccessKey();
                string k2 = AccessKeyGenerator.NewAccessKey();
                TestAssert.StartsWith("access_", k1, "Access keys carry the access_ prefix.");
                TestAssert.True(k1.Length >= 39, "Access keys carry high entropy.");
                TestAssert.NotEqual(k1, k2, "Access keys must be unique.");
                return System.Threading.Tasks.Task.CompletedTask;
            });

            builder.Add("Session token codec round-trips a payload", _ =>
            {
                SessionTokenCodec codec = new SessionTokenCodec("unit-test-secret");
                TokenPayload payload = new TokenPayload
                {
                    SessionId = "ses_abc",
                    TokenId = "tok_xyz",
                    UserId = "usr_1",
                    TenantId = "ten_1",
                    Nonce = "nonce",
                    IssuedUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    ExpiresUtc = new DateTime(2026, 1, 1, 1, 0, 0, DateTimeKind.Utc)
                };
                string token = codec.Encode(payload);
                TestAssert.NotNullOrEmpty(token, "Encoding must produce a token.");
                TokenPayload decoded = codec.Decode(token);
                TestAssert.NotNull(decoded, "A valid token must decode.");
                TestAssert.Equal(payload.SessionId, decoded.SessionId);
                TestAssert.Equal(payload.TokenId, decoded.TokenId);
                TestAssert.Equal(payload.UserId, decoded.UserId);
                TestAssert.Equal(payload.TenantId, decoded.TenantId);
                TestAssert.Null(codec.Decode("not-a-real-token"), "A malformed token must decode to null.");
                return System.Threading.Tasks.Task.CompletedTask;
            });

            builder.Add("Permission evaluator permits a matching grant and denies otherwise", _ =>
            {
                List<EffectiveGrant> grants = new List<EffectiveGrant>
                {
                    Grant(PermissionType.Permit, ResourceType.Collection, OperationType.Read)
                };
                TestAssert.Equal(AuthorizationVerdict.Permitted,
                    PermissionEvaluator.Evaluate(grants, ResourceType.Collection, OperationType.Read, null),
                    "A matching permit must be permitted.");
                TestAssert.Equal(AuthorizationVerdict.DeniedImplicit,
                    PermissionEvaluator.Evaluate(grants, ResourceType.Collection, OperationType.Delete, null),
                    "An unmatched operation must be implicitly denied.");
                TestAssert.Equal(AuthorizationVerdict.DeniedImplicit,
                    PermissionEvaluator.Evaluate(new List<EffectiveGrant>(), ResourceType.Collection, OperationType.Read, null),
                    "No grants must be implicitly denied.");
                return System.Threading.Tasks.Task.CompletedTask;
            });

            builder.Add("Permission evaluator applies deny-over-permit", _ =>
            {
                List<EffectiveGrant> grants = new List<EffectiveGrant>
                {
                    Grant(PermissionType.Permit, ResourceType.Collection, OperationType.Read),
                    Grant(PermissionType.Deny, ResourceType.Collection, OperationType.Read)
                };
                TestAssert.Equal(AuthorizationVerdict.DeniedExplicit,
                    PermissionEvaluator.Evaluate(grants, ResourceType.Collection, OperationType.Read, null),
                    "An explicit deny must win over a permit.");
                return System.Threading.Tasks.Task.CompletedTask;
            });

            builder.Add("Permission evaluator expands Write to Create/Update/Delete", _ =>
            {
                List<EffectiveGrant> grants = new List<EffectiveGrant>
                {
                    Grant(PermissionType.Permit, ResourceType.Document, OperationType.Write)
                };
                TestAssert.Equal(AuthorizationVerdict.Permitted,
                    PermissionEvaluator.Evaluate(grants, ResourceType.Document, OperationType.Create, null),
                    "Write must satisfy Create.");
                TestAssert.Equal(AuthorizationVerdict.Permitted,
                    PermissionEvaluator.Evaluate(grants, ResourceType.Document, OperationType.Delete, null),
                    "Write must satisfy Delete.");
                TestAssert.Equal(AuthorizationVerdict.DeniedImplicit,
                    PermissionEvaluator.Evaluate(grants, ResourceType.Document, OperationType.Read, null),
                    "Write must not satisfy Read.");
                return System.Threading.Tasks.Task.CompletedTask;
            });

            builder.Add("Tenant, user, and credential persist and read back", async client =>
            {
                Tenant tenant = await CreateTenantAsync(client, "acme");
                Tenant readTenant = await client.Tenants.ReadById(tenant.Id);
                TestAssert.NotNull(readTenant, "Tenant must read back.");
                TestAssert.Equal("acme", readTenant.Name);

                User user = await CreateUserAsync(client, tenant.Id, "user@acme", "secret", false, false);
                User byEmail = await client.Users.ReadByEmail(tenant.Id, "user@acme");
                TestAssert.NotNull(byEmail, "User must read back by email within its tenant.");
                TestAssert.Equal(user.Id, byEmail.Id);
                TestAssert.Null(await client.Users.ReadByEmail("ten_other", "user@acme"), "User must not resolve in another tenant.");

                string rawKey = AccessKeyGenerator.NewAccessKey();
                await CreateCredentialAsync(client, tenant.Id, user.Id, rawKey);
                Credential byHash = await client.Credentials.ReadByAccessKeyHash(PasswordHasher.Sha256Hex(rawKey));
                TestAssert.NotNull(byHash, "Credential must resolve by access-key hash.");
                TestAssert.Equal(user.Id, byHash.UserId);
            });

            builder.Add("First-boot seeding creates defaults once and is idempotent", async client =>
            {
                SeedResult first = await FirstBootSeeder.SeedAsync(client, "Default Tenant", "admin@lattice", "password");
                TestAssert.True(first.CreatedDefaults, "First seeding must create defaults.");
                TestAssert.NotNullOrEmpty(first.TenantId, "Seeding must report the tenant id.");
                TestAssert.NotNullOrEmpty(first.DefaultAccessKey, "Seeding must report the default access key.");

                SeedResult second = await FirstBootSeeder.SeedAsync(client, "Default Tenant", "admin@lattice", "password");
                TestAssert.False(second.CreatedDefaults, "Re-seeding must not create a second tenant.");
                TestAssert.Equal(1, (await client.Tenants.ReadAll()).Count, "Exactly one tenant must exist after re-seeding.");
            });

            builder.Add("Login issues a token whose bearer resolves the user", async client =>
            {
                Tenant tenant = await CreateTenantAsync(client, "login-co");
                User user = await CreateUserAsync(client, tenant.Id, "u@login-co", "hunter2", false, false);
                AuthenticationService authn = NewAuthN(client);

                LoginResult login = await authn.LoginAsync(tenant.Id, "u@login-co", "hunter2", "127.0.0.1", null);
                TestAssert.NotNull(login, "Valid credentials must log in.");
                TestAssert.NotNullOrEmpty(login.Token, "Login must issue a token.");

                CallerContext caller = await authn.AuthenticateBearerAsync(login.Token);
                TestAssert.NotNull(caller, "The session token must resolve a principal.");
                TestAssert.True(caller.IsAuthenticated, "The resolved principal must be authenticated.");
                TestAssert.Equal(PrincipalType.User, caller.PrincipalType);
                TestAssert.Equal(user.Id, caller.UserId);
                TestAssert.Equal(tenant.Id, caller.TenantId);
            });

            builder.Add("Login rejects a wrong password and a wrong tenant", async client =>
            {
                Tenant tenant = await CreateTenantAsync(client, "reject-co");
                await CreateUserAsync(client, tenant.Id, "u@reject-co", "correct", false, false);
                AuthenticationService authn = NewAuthN(client);

                TestAssert.Null(await authn.LoginAsync(tenant.Id, "u@reject-co", "wrong", null, null), "A wrong password must be rejected.");
                TestAssert.Null(await authn.LoginAsync("ten_other", "u@reject-co", "correct", null, null), "A wrong tenant must be rejected (cross-tenant isolation).");
                TestAssert.Null(await authn.AuthenticateBearerAsync("garbage-bearer"), "A garbage bearer must not authenticate.");
            });

            builder.Add("Login resolves the tenant from credentials and lists candidates when ambiguous", async client =>
            {
                Tenant a = await CreateTenantAsync(client, "resolve-a");
                Tenant b = await CreateTenantAsync(client, "resolve-b");
                await CreateUserAsync(client, a.Id, "solo@x", "pw", false, false);
                await CreateUserAsync(client, a.Id, "dup@x", "pw", false, false);
                await CreateUserAsync(client, b.Id, "dup@x", "pw", false, false);
                AuthenticationService authn = NewAuthN(client);

                List<LoginTenantOption> solo = await authn.ResolveTenantsForLoginAsync("solo@x", "pw");
                TestAssert.Equal(1, solo.Count, "A unique email resolves to exactly one tenant.");
                TestAssert.Equal(a.Id, solo[0].TenantId, "The single match must be the owning tenant.");

                List<LoginTenantOption> dup = await authn.ResolveTenantsForLoginAsync("dup@x", "pw");
                TestAssert.Equal(2, dup.Count, "A shared email with a matching password resolves to both tenants.");

                List<LoginTenantOption> wrong = await authn.ResolveTenantsForLoginAsync("dup@x", "nope");
                TestAssert.Equal(0, wrong.Count, "A wrong password resolves to no tenants (no enumeration).");

                List<LoginTenantOption> unknown = await authn.ResolveTenantsForLoginAsync("ghost@x", "pw");
                TestAssert.Equal(0, unknown.Count, "An unknown email resolves to no tenants.");
            });

            builder.Add("Access-key bearer resolves the credential principal", async client =>
            {
                Tenant tenant = await CreateTenantAsync(client, "key-co");
                User user = await CreateUserAsync(client, tenant.Id, "svc@key-co", "pw", false, false);
                string rawKey = AccessKeyGenerator.NewAccessKey();
                Credential credential = await CreateCredentialAsync(client, tenant.Id, user.Id, rawKey);
                AuthenticationService authn = NewAuthN(client);

                CallerContext caller = await authn.AuthenticateBearerAsync(rawKey);
                TestAssert.NotNull(caller, "The access key must resolve a principal.");
                TestAssert.Equal(PrincipalType.Credential, caller.PrincipalType);
                TestAssert.Equal(credential.Id, caller.CredentialId);
                TestAssert.Equal(tenant.Id, caller.TenantId);
                TestAssert.Null(await authn.AuthenticateBearerAsync("access_wrongkeywrongkeywrongkeywrongkey00"), "An unknown access key must not authenticate.");
            });

            builder.Add("Authorization: admins bypass; a plain user is denied by default", async client =>
            {
                AuthorizationService authz = new AuthorizationService(client.Roles);

                CallerContext admin = new CallerContext { IsAuthenticated = true, PrincipalType = PrincipalType.User, IsAdmin = true, TenantId = "ten_a", UserId = "usr_a" };
                TestAssert.Equal(AuthorizationVerdict.Permitted,
                    await authz.AuthorizeAsync(admin, ResourceType.Tenant, OperationType.Delete, null),
                    "System admins must bypass evaluation.");

                CallerContext tenantAdmin = new CallerContext { IsAuthenticated = true, PrincipalType = PrincipalType.User, IsTenantAdmin = true, TenantId = "ten_a", UserId = "usr_b" };
                TestAssert.Equal(AuthorizationVerdict.Permitted,
                    await authz.AuthorizeAsync(tenantAdmin, ResourceType.Collection, OperationType.Create, null),
                    "Tenant admins must bypass evaluation within their tenant.");

                CallerContext plain = new CallerContext { IsAuthenticated = true, PrincipalType = PrincipalType.User, TenantId = "ten_a", UserId = "usr_c" };
                TestAssert.NotEqual(AuthorizationVerdict.Permitted,
                    await authz.AuthorizeAsync(plain, ResourceType.Collection, OperationType.Read, null),
                    "A user with no assignments must be denied.");
            });

            builder.Add("Authorization resolves a granted role end to end", async client =>
            {
                Tenant tenant = await CreateTenantAsync(client, "rbac-co");
                User user = await CreateUserAsync(client, tenant.Id, "reader@rbac-co", "pw", false, false);

                Permission permission = new Permission
                {
                    Id = IdGenerator.NewPermissionId(),
                    TenantId = tenant.Id,
                    Name = "collections-read",
                    PermissionType = PermissionType.Permit,
                    Active = true,
                    CreatedUtc = DateTime.UtcNow,
                    LastUpdateUtc = DateTime.UtcNow
                };
                permission.ResourceTypes = new List<ResourceType> { ResourceType.Collection };
                permission.OperationTypes = new List<OperationType> { OperationType.Read };
                await client.Roles.CreatePermission(permission);

                UserRole role = new UserRole
                {
                    Id = IdGenerator.NewUserRoleId(),
                    TenantId = tenant.Id,
                    Name = "collection-reader",
                    Active = true,
                    CreatedUtc = DateTime.UtcNow,
                    LastUpdateUtc = DateTime.UtcNow
                };
                await client.Roles.CreateRole(role);

                await client.Roles.CreateRolePermissionMap(new RolePermissionMap
                {
                    Id = IdGenerator.NewRolePermissionMapId(),
                    TenantId = tenant.Id,
                    RoleId = role.Id,
                    PermissionId = permission.Id,
                    Active = true,
                    CreatedUtc = DateTime.UtcNow,
                    LastUpdateUtc = DateTime.UtcNow
                });

                await client.Roles.CreateUserRoleAssignment(new UserRoleAssignment
                {
                    Id = IdGenerator.NewUserRoleAssignmentId(),
                    TenantId = tenant.Id,
                    UserId = user.Id,
                    RoleId = role.Id,
                    ResourceScope = ResourceScope.Tenant,
                    InheritsToChildren = true,
                    Active = true,
                    CreatedUtc = DateTime.UtcNow,
                    LastUpdateUtc = DateTime.UtcNow
                });

                AuthorizationService authz = new AuthorizationService(client.Roles);
                CallerContext caller = new CallerContext
                {
                    IsAuthenticated = true,
                    PrincipalType = PrincipalType.User,
                    TenantId = tenant.Id,
                    UserId = user.Id
                };
                TestAssert.Equal(AuthorizationVerdict.Permitted,
                    await authz.AuthorizeAsync(caller, ResourceType.Collection, OperationType.Read, null),
                    "The granted role must permit Collection:Read.");
                TestAssert.NotEqual(AuthorizationVerdict.Permitted,
                    await authz.AuthorizeAsync(caller, ResourceType.Collection, OperationType.Delete, null),
                    "The granted role must not permit Collection:Delete.");
            });

            builder.Add("Collections persist and read back their owning tenant", async client =>
            {
                Collection owned = await client.Collection.Create("tenant-owned", tenantId: "ten_owner");
                TestAssert.NotNull(owned, "Collection must be created.");
                Collection read = await client.Collection.ReadById(owned.Id);
                TestAssert.NotNull(read, "Collection must read back.");
                TestAssert.Equal("ten_owner", read.TenantId, "Collection must persist its owning tenant.");

                Collection shared = await client.Collection.Create("shared");
                Collection readShared = await client.Collection.ReadById(shared.Id);
                TestAssert.Null(readShared.TenantId, "A collection created without a tenant must have a null owner.");
            });

            builder.Add("Audit entries persist, search, and count within a tenant", async client =>
            {
                Tenant tenant = await CreateTenantAsync(client, "audit-co");
                AuditEntry entry = new AuditEntry
                {
                    Id = IdGenerator.NewAuditId(),
                    TenantId = tenant.Id,
                    EventType = "AuthzDenied",
                    AuthzResult = "DeniedImplicit",
                    Method = "GET",
                    Path = "/v1.0/collections",
                    ResponseCode = 403,
                    CreatedUtc = DateTime.UtcNow
                };
                await client.Audit.Create(entry);

                List<AuditEntry> found = await client.Audit.Search(tenant.Id, "AuthzDenied", null, null, 0, 100);
                TestAssert.Equal(1, found.Count, "The audit entry must be searchable within its tenant.");
                TestAssert.Equal(1L, await client.Audit.Count(tenant.Id, "AuthzDenied", null, null), "Count must match the search.");
                TestAssert.Equal(0, (await client.Audit.Search("ten_other", null, null, null, 0, 100)).Count, "Audit entries must not leak across tenants.");
            });

            return builder.Build();
        }

        #endregion

        #region Private-Methods

        private static EffectiveGrant Grant(PermissionType type, ResourceType resource, OperationType operation)
        {
            return new EffectiveGrant
            {
                PermissionType = type,
                ResourceTypes = new List<ResourceType> { resource },
                OperationTypes = new List<OperationType> { operation },
                ResourceScope = ResourceScope.Tenant,
                InheritsToChildren = true
            };
        }

        private static AuthenticationService NewAuthN(LatticeClient client)
        {
            return new AuthenticationService(
                client.Tenants, client.Users, client.Credentials, client.Sessions,
                new SessionTokenCodec("unit-test-secret"), 60);
        }

        private static async System.Threading.Tasks.Task<Tenant> CreateTenantAsync(LatticeClient client, string name)
        {
            Tenant tenant = new Tenant
            {
                Id = IdGenerator.NewTenantId(),
                Name = name,
                Active = true,
                CreatedUtc = DateTime.UtcNow,
                LastUpdateUtc = DateTime.UtcNow
            };
            return await client.Tenants.Create(tenant).ConfigureAwait(false);
        }

        private static async System.Threading.Tasks.Task<User> CreateUserAsync(LatticeClient client, string tenantId, string email, string password, bool isAdmin, bool isTenantAdmin)
        {
            User user = new User
            {
                Id = IdGenerator.NewUserId(),
                TenantId = tenantId,
                Email = email,
                PasswordSha256 = PasswordHasher.Sha256Hex(password),
                IsAdmin = isAdmin,
                IsTenantAdmin = isTenantAdmin,
                Active = true,
                CreatedUtc = DateTime.UtcNow,
                LastUpdateUtc = DateTime.UtcNow
            };
            return await client.Users.Create(user).ConfigureAwait(false);
        }

        private static async System.Threading.Tasks.Task<Credential> CreateCredentialAsync(LatticeClient client, string tenantId, string userId, string rawKey)
        {
            Credential credential = new Credential
            {
                Id = IdGenerator.NewCredentialId(),
                TenantId = tenantId,
                UserId = userId,
                AccessKeySha256 = PasswordHasher.Sha256Hex(rawKey),
                AccessKeyLast4 = rawKey.Substring(rawKey.Length - 4),
                Active = true,
                CreatedUtc = DateTime.UtcNow,
                LastUpdateUtc = DateTime.UtcNow
            };
            return await client.Credentials.Create(credential).ConfigureAwait(false);
        }

        #endregion
    }
}
