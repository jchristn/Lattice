namespace Lattice.Server.API.REST
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using WatsonWebserver.Core;
    using WatsonWebserver.Core.Routing;
    using Lattice.Core.Helpers;
    using Lattice.Core.Models;
    using Lattice.Core.Security;
    using Lattice.Server.Classes;
    using Lattice.Server.Security;
    using Lattice.Server.Telemetry;

    /// <summary>
    /// Authentication, identity management, RBAC, and audit routes.
    /// </summary>
    public partial class RestServiceHandler
    {
        #region Auth-Helpers

        private async Task<CallerContext> AuthenticateAsync(HttpContextBase ctx)
        {
            if (_AuthN == null) return null;
            string bearer = ExtractBearer(ctx);
            if (String.IsNullOrEmpty(bearer)) return null;
            return await _AuthN.AuthenticateBearerAsync(bearer).ConfigureAwait(false);
        }

        private static string ExtractBearer(HttpContextBase ctx)
        {
            string auth = ctx.Request.Headers?.Get("Authorization");
            if (!String.IsNullOrEmpty(auth))
            {
                if (auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return auth.Substring(7).Trim();
                return auth.Trim();
            }
            string xtoken = ctx.Request.Headers?.Get("x-token");
            return String.IsNullOrEmpty(xtoken) ? null : xtoken.Trim();
        }

        private static string ResolveResourceId(RequestContext reqCtx, ResourceType resourceType)
        {
            switch (resourceType)
            {
                case ResourceType.Collection: return reqCtx.CollectionId;
                case ResourceType.Document: return reqCtx.DocumentId;
                case ResourceType.Schema: return reqCtx.SchemaId;
                default: return null;
            }
        }

        // The tenant a management request operates on: the caller's tenant, unless a system admin named
        // another tenant explicitly.
        private static string EffectiveTenant(RequestContext reqCtx, string bodyTenantId)
        {
            if (reqCtx.Caller != null && reqCtx.Caller.IsAdmin && !String.IsNullOrEmpty(bodyTenantId)) return bodyTenantId;
            return reqCtx.TenantId;
        }

        #endregion

        #region Route-Registration

        private void InitializeAuthRoutes()
        {
            WebserverRoutes routes = _Webserver!.Routes;

            routes.PreAuthentication.Static.Add(HttpMethod.POST, "/v1.0/token", PostTokenRoute, ExceptionRoute);
            routes.PreAuthentication.Static.Add(HttpMethod.GET, "/v1.0/token", GetWhoAmIRoute, ExceptionRoute);
            routes.PreAuthentication.Static.Add(HttpMethod.GET, "/v1.0/token/details", GetWhoAmIRoute, ExceptionRoute);
            routes.PreAuthentication.Static.Add(HttpMethod.DELETE, "/v1.0/token", DeleteTokenRoute, ExceptionRoute);
            routes.PreAuthentication.Static.Add(HttpMethod.GET, "/v1.0/whoami", GetWhoAmIRoute, ExceptionRoute);

            routes.PreAuthentication.Static.Add(HttpMethod.GET, "/v1.0/tenants", GetTenantsRoute, ExceptionRoute);
            routes.PreAuthentication.Static.Add(HttpMethod.PUT, "/v1.0/tenants", PutTenantRoute, ExceptionRoute);
            routes.PreAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantId}", GetTenantRoute, ExceptionRoute);
            routes.PreAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantId}", DeleteTenantRoute, ExceptionRoute);

            routes.PreAuthentication.Static.Add(HttpMethod.GET, "/v1.0/users", GetUsersRoute, ExceptionRoute);
            routes.PreAuthentication.Static.Add(HttpMethod.PUT, "/v1.0/users", PutUserRoute, ExceptionRoute);
            routes.PreAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/users/{userId}", GetUserRoute, ExceptionRoute);
            routes.PreAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/users/{userId}", DeleteUserRoute, ExceptionRoute);

            routes.PreAuthentication.Static.Add(HttpMethod.GET, "/v1.0/credentials", GetCredentialsRoute, ExceptionRoute);
            routes.PreAuthentication.Static.Add(HttpMethod.PUT, "/v1.0/credentials", PutCredentialRoute, ExceptionRoute);
            routes.PreAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/credentials/{credentialId}", GetCredentialRoute, ExceptionRoute);
            routes.PreAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/credentials/{credentialId}", DeleteCredentialRoute, ExceptionRoute);

            routes.PreAuthentication.Static.Add(HttpMethod.GET, "/v1.0/roles", GetRolesRoute, ExceptionRoute);

            routes.PreAuthentication.Static.Add(HttpMethod.GET, "/v1.0/assignments", GetAssignmentsRoute, ExceptionRoute);
            routes.PreAuthentication.Static.Add(HttpMethod.PUT, "/v1.0/assignments", PutAssignmentRoute, ExceptionRoute);
            routes.PreAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/assignments/{assignmentId}", DeleteAssignmentRoute, ExceptionRoute);

            routes.PreAuthentication.Static.Add(HttpMethod.GET, "/v1.0/audit", GetAuditRoute, ExceptionRoute);
            routes.PreAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/audit/{auditId}", GetAuditEntryRoute, ExceptionRoute);
            routes.PreAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/audit/{auditId}", DeleteAuditEntryRoute, ExceptionRoute);
        }

        #endregion

        #region Token-Routes

        private async Task PostTokenRoute(HttpContextBase ctx)
        {
            await WrappedRequestHandler(ctx, RequestTypeEnum.HealthCheck, async (reqCtx) =>
            {
                LoginRequest request = Deserialize<LoginRequest>(reqCtx.RequestBody);
                if (request == null || String.IsNullOrWhiteSpace(request.Email) || request.Password == null)
                    return new ResponseContext(false, 400, "email and password are required");

                // The tenant is inferred from the credentials when not supplied. If they match users in
                // several tenants, ask the caller to choose one; a wrong password matches nothing.
                string effectiveTenantId = request.TenantId;
                if (String.IsNullOrWhiteSpace(effectiveTenantId))
                {
                    List<LoginTenantOption> candidates = await _AuthN.ResolveTenantsForLoginAsync(request.Email, request.Password).ConfigureAwait(false);
                    if (candidates.Count == 0)
                    {
                        ServerTelemetry.RecordAuthRequest("session", false);
                        return new ResponseContext(false, 401, "Invalid credentials");
                    }
                    if (candidates.Count > 1)
                    {
                        AuthTokenResponse selection = new AuthTokenResponse { TenantSelectionRequired = true };
                        List<TenantOption> options = new List<TenantOption>();
                        foreach (LoginTenantOption candidate in candidates)
                            options.Add(new TenantOption { TenantId = candidate.TenantId, TenantName = candidate.TenantName });
                        selection.Tenants = options;
                        return new ResponseContext { Success = true, StatusCode = 200, Data = selection };
                    }
                    effectiveTenantId = candidates[0].TenantId;
                }

                LoginResult login = await _AuthN.LoginAsync(effectiveTenantId, request.Email, request.Password, reqCtx.IpAddress, null).ConfigureAwait(false);
                if (login == null)
                {
                    ServerTelemetry.RecordAuthRequest("session", false);
                    return new ResponseContext(false, 401, "Invalid credentials");
                }

                ServerTelemetry.RecordAuthRequest("session", true);
                ServerTelemetry.RecordSessionEvent("created");

                AuthTokenResponse response = new AuthTokenResponse
                {
                    Token = login.Token,
                    ExpiresUtc = login.ExpiresUtc,
                    TenantId = login.Caller.TenantId,
                    UserId = login.Caller.UserId,
                    Email = login.Caller.Email,
                    IsAdmin = login.Caller.IsAdmin,
                    IsTenantAdmin = login.Caller.IsTenantAdmin
                };
                return new ResponseContext { Success = true, StatusCode = 200, Data = response };
            }).ConfigureAwait(false);
        }

        private async Task GetWhoAmIRoute(HttpContextBase ctx)
        {
            await WrappedRequestHandler(ctx, RequestTypeEnum.HealthCheck, (reqCtx) =>
            {
                CallerContext caller = reqCtx.Caller;
                WhoAmIResponse response = new WhoAmIResponse
                {
                    IsAuthenticated = caller != null && caller.IsAuthenticated,
                    PrincipalType = caller?.PrincipalType.ToString(),
                    TenantId = caller?.TenantId,
                    UserId = caller?.UserId,
                    CredentialId = caller?.CredentialId,
                    Email = caller?.Email,
                    IsAdmin = caller != null && caller.IsAdmin,
                    IsTenantAdmin = caller != null && caller.IsTenantAdmin
                };
                return Task.FromResult(new ResponseContext { Success = true, StatusCode = 200, Data = response });
            }).ConfigureAwait(false);
        }

        private async Task DeleteTokenRoute(HttpContextBase ctx)
        {
            await WrappedRequestHandler(ctx, RequestTypeEnum.HealthCheck, async (reqCtx) =>
            {
                CallerContext caller = reqCtx.Caller;
                if (caller != null && !String.IsNullOrEmpty(caller.SessionId))
                {
                    AuthSession session = await _Client.Sessions.ReadById(caller.SessionId, CancellationToken.None).ConfigureAwait(false);
                    if (session != null)
                    {
                        session.RevokedUtc = DateTime.UtcNow;
                        session.RevocationReason = "logout";
                        session.Active = false;
                        await _Client.Sessions.Update(session, CancellationToken.None).ConfigureAwait(false);
                        ServerTelemetry.RecordSessionEvent("revoked");
                    }
                }
                return new ResponseContext { Success = true, StatusCode = 200, Data = null };
            }).ConfigureAwait(false);
        }

        #endregion

        #region Tenant-Routes

        private async Task GetTenantsRoute(HttpContextBase ctx)
        {
            await WrappedRequestHandler(ctx, RequestTypeEnum.Collection, async (reqCtx) =>
            {
                List<Tenant> tenants = await _Client.Tenants.ReadAll(CancellationToken.None).ConfigureAwait(false);
                return new ResponseContext { Success = true, StatusCode = 200, Data = BuildEnumerationResult(tenants, reqCtx) };
            }).ConfigureAwait(false);
        }

        private async Task PutTenantRoute(HttpContextBase ctx)
        {
            await WrappedRequestHandler(ctx, RequestTypeEnum.Collection, async (reqCtx) =>
            {
                Tenant request = Deserialize<Tenant>(reqCtx.RequestBody);
                if (request == null || String.IsNullOrWhiteSpace(request.Name)) return new ResponseContext(false, 400, "name is required");

                request.Id = IdGenerator.NewTenantId();
                request.CreatedUtc = DateTime.UtcNow;
                request.LastUpdateUtc = DateTime.UtcNow;
                Tenant created = await _Client.Tenants.Create(request, CancellationToken.None).ConfigureAwait(false);
                ServerTelemetry.RecordRbacMutation("tenant", "create");
                return new ResponseContext { Success = true, StatusCode = 201, Data = created };
            }).ConfigureAwait(false);
        }

        private async Task GetTenantRoute(HttpContextBase ctx)
        {
            await WrappedRequestHandler(ctx, RequestTypeEnum.Collection, async (reqCtx) =>
            {
                string tenantId = ctx.Request.Url.Parameters["tenantId"];
                Tenant tenant = await _Client.Tenants.ReadById(tenantId, CancellationToken.None).ConfigureAwait(false);
                if (tenant == null) return new ResponseContext(false, 404, "Tenant not found");
                return new ResponseContext { Success = true, StatusCode = 200, Data = tenant };
            }).ConfigureAwait(false);
        }

        private async Task DeleteTenantRoute(HttpContextBase ctx)
        {
            await WrappedRequestHandler(ctx, RequestTypeEnum.Collection, async (reqCtx) =>
            {
                string tenantId = ctx.Request.Url.Parameters["tenantId"];
                Tenant tenant = await _Client.Tenants.ReadById(tenantId, CancellationToken.None).ConfigureAwait(false);
                if (tenant == null) return new ResponseContext(false, 404, "Tenant not found");
                if (tenant.IsProtected) return new ResponseContext(false, 409, "Tenant is protected");
                await _Client.Tenants.Delete(tenantId, CancellationToken.None).ConfigureAwait(false);
                ServerTelemetry.RecordRbacMutation("tenant", "delete");
                return new ResponseContext { Success = true, StatusCode = 200, Data = null };
            }).ConfigureAwait(false);
        }

        #endregion

        #region User-Routes

        private async Task GetUsersRoute(HttpContextBase ctx)
        {
            await WrappedRequestHandler(ctx, RequestTypeEnum.Collection, async (reqCtx) =>
            {
                string tenantId = EffectiveTenant(reqCtx, ctx.Request.Query.Elements["tenantId"]);
                List<User> users = await _Client.Users.ReadByTenant(tenantId, CancellationToken.None).ConfigureAwait(false);
                foreach (User u in users) u.PasswordSha256 = null;
                return new ResponseContext { Success = true, StatusCode = 200, Data = BuildEnumerationResult(users, reqCtx) };
            }).ConfigureAwait(false);
        }

        private async Task PutUserRoute(HttpContextBase ctx)
        {
            await WrappedRequestHandler(ctx, RequestTypeEnum.Collection, async (reqCtx) =>
            {
                CreateUserRequest request = Deserialize<CreateUserRequest>(reqCtx.RequestBody);
                if (request == null || String.IsNullOrWhiteSpace(request.Email) || String.IsNullOrWhiteSpace(request.Password))
                    return new ResponseContext(false, 400, "email and password are required");

                string tenantId = EffectiveTenant(reqCtx, request.TenantId);
                User user = new User
                {
                    Id = IdGenerator.NewUserId(),
                    TenantId = tenantId,
                    Email = request.Email,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    PasswordSha256 = PasswordHasher.Sha256Hex(request.Password),
                    IsAdmin = reqCtx.Caller != null && reqCtx.Caller.IsAdmin && request.IsAdmin,
                    IsTenantAdmin = request.IsTenantAdmin,
                    CreatedUtc = DateTime.UtcNow,
                    LastUpdateUtc = DateTime.UtcNow
                };
                User created = await _Client.Users.Create(user, CancellationToken.None).ConfigureAwait(false);
                if (created != null) created.PasswordSha256 = null;
                ServerTelemetry.RecordRbacMutation("user", "create");
                return new ResponseContext { Success = true, StatusCode = 201, Data = created };
            }).ConfigureAwait(false);
        }

        private async Task GetUserRoute(HttpContextBase ctx)
        {
            await WrappedRequestHandler(ctx, RequestTypeEnum.Collection, async (reqCtx) =>
            {
                string userId = ctx.Request.Url.Parameters["userId"];
                User user = await _Client.Users.ReadById(userId, CancellationToken.None).ConfigureAwait(false);
                if (user == null || !TenantVisible(reqCtx, user.TenantId)) return new ResponseContext(false, 404, "User not found");
                user.PasswordSha256 = null;
                return new ResponseContext { Success = true, StatusCode = 200, Data = user };
            }).ConfigureAwait(false);
        }

        private async Task DeleteUserRoute(HttpContextBase ctx)
        {
            await WrappedRequestHandler(ctx, RequestTypeEnum.Collection, async (reqCtx) =>
            {
                string userId = ctx.Request.Url.Parameters["userId"];
                User user = await _Client.Users.ReadById(userId, CancellationToken.None).ConfigureAwait(false);
                if (user == null || !TenantVisible(reqCtx, user.TenantId)) return new ResponseContext(false, 404, "User not found");
                if (user.IsProtected) return new ResponseContext(false, 409, "User is protected");
                await _Client.Users.Delete(userId, CancellationToken.None).ConfigureAwait(false);
                ServerTelemetry.RecordRbacMutation("user", "delete");
                return new ResponseContext { Success = true, StatusCode = 200, Data = null };
            }).ConfigureAwait(false);
        }

        #endregion

        #region Credential-Routes

        private async Task GetCredentialsRoute(HttpContextBase ctx)
        {
            await WrappedRequestHandler(ctx, RequestTypeEnum.Collection, async (reqCtx) =>
            {
                string tenantId = EffectiveTenant(reqCtx, ctx.Request.Query.Elements["tenantId"]);
                List<Credential> creds = await _Client.Credentials.ReadByTenant(tenantId, CancellationToken.None).ConfigureAwait(false);
                foreach (Credential c in creds) c.AccessKeySha256 = null;
                return new ResponseContext { Success = true, StatusCode = 200, Data = BuildEnumerationResult(creds, reqCtx) };
            }).ConfigureAwait(false);
        }

        private async Task PutCredentialRoute(HttpContextBase ctx)
        {
            await WrappedRequestHandler(ctx, RequestTypeEnum.Collection, async (reqCtx) =>
            {
                CreateCredentialRequest request = Deserialize<CreateCredentialRequest>(reqCtx.RequestBody) ?? new CreateCredentialRequest();
                string tenantId = EffectiveTenant(reqCtx, request.TenantId);
                string userId = String.IsNullOrEmpty(request.UserId) ? reqCtx.Caller?.UserId : request.UserId;
                if (String.IsNullOrEmpty(userId)) return new ResponseContext(false, 400, "userId is required");

                string rawAccessKey = AccessKeyGenerator.NewAccessKey();
                Credential credential = new Credential
                {
                    Id = IdGenerator.NewCredentialId(),
                    TenantId = tenantId,
                    UserId = userId,
                    Name = request.Name,
                    AccessKeySha256 = PasswordHasher.Sha256Hex(rawAccessKey),
                    AccessKeyLast4 = rawAccessKey.Substring(rawAccessKey.Length - 4),
                    CreatedUtc = DateTime.UtcNow,
                    LastUpdateUtc = DateTime.UtcNow
                };
                Credential created = await _Client.Credentials.Create(credential, CancellationToken.None).ConfigureAwait(false);
                if (created != null)
                {
                    created.AccessKey = rawAccessKey; // shown once
                    created.AccessKeySha256 = null;   // never expose the stored hash
                }
                ServerTelemetry.RecordRbacMutation("credential", "create");
                return new ResponseContext { Success = true, StatusCode = 201, Data = created };
            }).ConfigureAwait(false);
        }

        private async Task GetCredentialRoute(HttpContextBase ctx)
        {
            await WrappedRequestHandler(ctx, RequestTypeEnum.Collection, async (reqCtx) =>
            {
                string credentialId = ctx.Request.Url.Parameters["credentialId"];
                Credential credential = await _Client.Credentials.ReadById(credentialId, CancellationToken.None).ConfigureAwait(false);
                if (credential == null || !TenantVisible(reqCtx, credential.TenantId)) return new ResponseContext(false, 404, "Credential not found");
                credential.AccessKeySha256 = null;
                return new ResponseContext { Success = true, StatusCode = 200, Data = credential };
            }).ConfigureAwait(false);
        }

        private async Task DeleteCredentialRoute(HttpContextBase ctx)
        {
            await WrappedRequestHandler(ctx, RequestTypeEnum.Collection, async (reqCtx) =>
            {
                string credentialId = ctx.Request.Url.Parameters["credentialId"];
                Credential credential = await _Client.Credentials.ReadById(credentialId, CancellationToken.None).ConfigureAwait(false);
                if (credential == null || !TenantVisible(reqCtx, credential.TenantId)) return new ResponseContext(false, 404, "Credential not found");
                if (credential.IsProtected) return new ResponseContext(false, 409, "Credential is protected");
                await _Client.Credentials.Delete(credentialId, CancellationToken.None).ConfigureAwait(false);
                ServerTelemetry.RecordRbacMutation("credential", "delete");
                return new ResponseContext { Success = true, StatusCode = 200, Data = null };
            }).ConfigureAwait(false);
        }

        #endregion

        #region Role-And-Assignment-Routes

        private async Task GetRolesRoute(HttpContextBase ctx)
        {
            await WrappedRequestHandler(ctx, RequestTypeEnum.Collection, async (reqCtx) =>
            {
                List<UserRole> roles = await _Client.Roles.ReadRoles(reqCtx.TenantId, CancellationToken.None).ConfigureAwait(false);
                return new ResponseContext { Success = true, StatusCode = 200, Data = BuildEnumerationResult(roles, reqCtx) };
            }).ConfigureAwait(false);
        }

        private async Task GetAssignmentsRoute(HttpContextBase ctx)
        {
            await WrappedRequestHandler(ctx, RequestTypeEnum.Collection, async (reqCtx) =>
            {
                string tenantId = EffectiveTenant(reqCtx, ctx.Request.Query.Elements["tenantId"]);
                List<UserRoleAssignment> assignments = await _Client.Roles.ReadAllUserRoleAssignments(tenantId, CancellationToken.None).ConfigureAwait(false);
                return new ResponseContext { Success = true, StatusCode = 200, Data = BuildEnumerationResult(assignments, reqCtx) };
            }).ConfigureAwait(false);
        }

        private async Task PutAssignmentRoute(HttpContextBase ctx)
        {
            await WrappedRequestHandler(ctx, RequestTypeEnum.Collection, async (reqCtx) =>
            {
                UserRoleAssignment request = Deserialize<UserRoleAssignment>(reqCtx.RequestBody);
                if (request == null || String.IsNullOrWhiteSpace(request.UserId) || (String.IsNullOrWhiteSpace(request.RoleId) && String.IsNullOrWhiteSpace(request.RoleName)))
                    return new ResponseContext(false, 400, "userId and a roleId or roleName are required");

                request.Id = IdGenerator.NewUserRoleAssignmentId();
                request.TenantId = EffectiveTenant(reqCtx, request.TenantId);
                request.CreatedUtc = DateTime.UtcNow;
                request.LastUpdateUtc = DateTime.UtcNow;
                UserRoleAssignment created = await _Client.Roles.CreateUserRoleAssignment(request, CancellationToken.None).ConfigureAwait(false);
                ServerTelemetry.RecordRbacMutation("assignment", "create");
                return new ResponseContext { Success = true, StatusCode = 201, Data = created };
            }).ConfigureAwait(false);
        }

        private async Task DeleteAssignmentRoute(HttpContextBase ctx)
        {
            await WrappedRequestHandler(ctx, RequestTypeEnum.Collection, async (reqCtx) =>
            {
                string assignmentId = ctx.Request.Url.Parameters["assignmentId"];
                UserRoleAssignment assignment = await _Client.Roles.ReadUserRoleAssignmentById(assignmentId, CancellationToken.None).ConfigureAwait(false);
                if (assignment == null || !TenantVisible(reqCtx, assignment.TenantId)) return new ResponseContext(false, 404, "Assignment not found");
                await _Client.Roles.DeleteUserRoleAssignment(assignmentId, CancellationToken.None).ConfigureAwait(false);
                ServerTelemetry.RecordRbacMutation("assignment", "delete");
                return new ResponseContext { Success = true, StatusCode = 200, Data = null };
            }).ConfigureAwait(false);
        }

        #endregion

        #region Audit-Routes

        private async Task GetAuditRoute(HttpContextBase ctx)
        {
            await WrappedRequestHandler(ctx, RequestTypeEnum.Search, async (reqCtx) =>
            {
                string tenantId = (reqCtx.Caller != null && reqCtx.Caller.IsAdmin) ? ctx.Request.Query.Elements["tenantId"] : reqCtx.TenantId;
                string eventType = ctx.Request.Query.Elements["eventType"];
                int skip = ParseQueryInt(reqCtx, "skip", 0, 0, Int32.MaxValue);
                int maxResults = ParseQueryInt(reqCtx, "maxResults", 100, 1, 1000);

                List<AuditEntry> entries = await _Client.Audit.Search(tenantId, eventType, null, null, skip, maxResults, CancellationToken.None).ConfigureAwait(false);
                long total = await _Client.Audit.Count(tenantId, eventType, null, null, CancellationToken.None).ConfigureAwait(false);
                return new ResponseContext { Success = true, StatusCode = 200, Data = BuildEnumerationResult(entries, skip, maxResults, total) };
            }).ConfigureAwait(false);
        }

        private async Task GetAuditEntryRoute(HttpContextBase ctx)
        {
            await WrappedRequestHandler(ctx, RequestTypeEnum.Search, async (reqCtx) =>
            {
                string auditId = ctx.Request.Url.Parameters["auditId"];
                AuditEntry entry = await _Client.Audit.ReadById(auditId, CancellationToken.None).ConfigureAwait(false);
                if (entry == null || !TenantVisible(reqCtx, entry.TenantId)) return new ResponseContext(false, 404, "Audit entry not found");
                return new ResponseContext { Success = true, StatusCode = 200, Data = entry };
            }).ConfigureAwait(false);
        }

        private async Task DeleteAuditEntryRoute(HttpContextBase ctx)
        {
            await WrappedRequestHandler(ctx, RequestTypeEnum.Search, async (reqCtx) =>
            {
                string auditId = ctx.Request.Url.Parameters["auditId"];
                AuditEntry entry = await _Client.Audit.ReadById(auditId, CancellationToken.None).ConfigureAwait(false);
                if (entry == null || !TenantVisible(reqCtx, entry.TenantId)) return new ResponseContext(false, 404, "Audit entry not found");
                await _Client.Audit.Delete(auditId, CancellationToken.None).ConfigureAwait(false);
                return new ResponseContext { Success = true, StatusCode = 200, Data = null };
            }).ConfigureAwait(false);
        }

        #endregion

        #region Private-Helpers

        private static bool TenantVisible(RequestContext reqCtx, string recordTenantId)
        {
            if (reqCtx.Caller != null && reqCtx.Caller.IsAdmin) return true;
            return String.Equals(reqCtx.TenantId, recordTenantId, StringComparison.Ordinal);
        }

        // True when the caller may access the given collection: auth disabled, a system administrator, a
        // shared collection with no owning tenant (created before tenancy or by the first-run seeder), or a
        // collection owned by the caller's tenant.
        private bool CollectionVisible(RequestContext reqCtx, Collection collection)
        {
            if (!_AuthEnabled) return true;
            if (collection == null) return false;
            if (reqCtx.Caller != null && reqCtx.Caller.IsAdmin) return true;
            if (String.IsNullOrEmpty(collection.TenantId)) return true;
            return String.Equals(collection.TenantId, reqCtx.TenantId, StringComparison.Ordinal);
        }

        // Load a collection by id and report whether the caller may access it. Returns false when the
        // collection does not exist, so callers can respond 404 without leaking cross-tenant existence.
        private async Task<bool> CollectionAccessibleAsync(RequestContext reqCtx, string collectionId)
        {
            if (!_AuthEnabled) return true;
            if (String.IsNullOrEmpty(collectionId)) return false;
            Collection collection = await _Client.Collection.ReadById(collectionId, CancellationToken.None).ConfigureAwait(false);
            return CollectionVisible(reqCtx, collection);
        }

        // Filter a collection list to those the caller may see (all of them for a system administrator).
        private List<Collection> VisibleCollections(RequestContext reqCtx, List<Collection> collections)
        {
            if (!_AuthEnabled || (reqCtx.Caller != null && reqCtx.Caller.IsAdmin)) return collections;
            List<Collection> visible = new List<Collection>();
            foreach (Collection collection in collections)
            {
                if (CollectionVisible(reqCtx, collection)) visible.Add(collection);
            }
            return visible;
        }

        // Append a security audit entry for an authentication or authorization event. Audit writes must
        // never break request handling, so failures are logged and swallowed.
        private async Task WriteSecurityAuditAsync(RequestContext reqCtx, RequiredPermission required, string eventType, string authResult, string authzResult, string denialReason, int responseCode)
        {
            if (!_AuthEnabled || _Client?.Audit == null) return;

            try
            {
                CallerContext caller = reqCtx?.Caller;
                AuditEntry entry = new AuditEntry
                {
                    Id = IdGenerator.NewAuditId(),
                    TenantId = caller?.TenantId,
                    EventType = eventType,
                    RequestId = reqCtx?.Guid,
                    PrincipalType = caller?.PrincipalType,
                    PrincipalId = caller?.PrincipalId,
                    UserId = caller?.UserId,
                    CredentialId = caller?.CredentialId,
                    ResourceType = required?.ResourceType,
                    ResourceId = required != null ? ResolveResourceId(reqCtx, required.ResourceType) : null,
                    RequestType = reqCtx?.RequestType.ToString(),
                    Method = reqCtx?.Method,
                    Path = reqCtx?.Path,
                    SourceIp = reqCtx?.IpAddress,
                    AuthResult = authResult,
                    AuthzResult = authzResult,
                    DenialReason = denialReason,
                    RequiredPermission = required != null ? (required.ResourceType + ":" + required.Operation) : null,
                    ResponseCode = responseCode,
                    CreatedUtc = DateTime.UtcNow
                };
                await _Client.Audit.Create(entry, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _Logging?.Warn(_Header + "audit write failed: " + e.Message);
            }
        }

        private static T Deserialize<T>(string body) where T : class
        {
            if (String.IsNullOrWhiteSpace(body)) return null;
            try
            {
                return JsonSerializer.Deserialize<T>(body, _JsonOptions);
            }
            catch
            {
                return null;
            }
        }

        #endregion
    }
}
