namespace Lattice.LoadGenerator
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Lattice.Core;
    using Lattice.Core.Helpers;
    using Lattice.Core.Models;
    using Lattice.Core.Security;

    /// <summary>
    /// Seeds a Lattice database with realistic synthetic activity so the dashboard renders a fully
    /// hydrated system. All synthetic entities are marked (collections with the label <c>synthetic</c> and
    /// tag <c>generator=loadgen</c>, users under the <c>@loadgen.synthetic</c> email domain, custom roles
    /// with the <c>LG-</c> prefix) so <see cref="WipeAsync"/> can find and remove them.
    /// </summary>
    public class Seeder
    {
        #region Public-Members

        /// <summary>Label applied to every synthetic collection.</summary>
        public const string SyntheticLabel = "synthetic";

        /// <summary>Tag key applied to every synthetic collection.</summary>
        public const string GeneratorTagKey = "generator";

        /// <summary>Tag value applied to every synthetic collection.</summary>
        public const string GeneratorTagValue = "loadgen";

        /// <summary>Email domain used for synthetic users.</summary>
        public const string SyntheticEmailDomain = "loadgen.synthetic";

        /// <summary>Name prefix used for synthetic custom roles.</summary>
        public const string RolePrefix = "LG-";

        #endregion

        #region Private-Members

        private readonly LatticeClient _Client;
        private readonly LoadGeneratorSettings _Settings;
        private readonly Random _Random;
        private readonly ContentFactory _Content;
        private readonly ActivityClock _Clock;
        private readonly DateTime _WindowStartUtc;
        private readonly DateTime _WindowEndUtc;

        #endregion

        #region Constructors-and-Factories

        /// <summary>Instantiate.</summary>
        /// <param name="client">Lattice client.</param>
        /// <param name="settings">Load-generator settings.</param>
        /// <param name="random">Random number generator.</param>
        /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
        public Seeder(LatticeClient client, LoadGeneratorSettings settings, Random random)
        {
            _Client = client ?? throw new ArgumentNullException(nameof(client));
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Random = random ?? throw new ArgumentNullException(nameof(random));
            _Content = new ContentFactory(random);
            _WindowEndUtc = DateTime.UtcNow;
            _WindowStartUtc = _WindowEndUtc.AddDays(-1.0 * settings.Days);
            _Clock = new ActivityClock(random, _WindowStartUtc, _WindowEndUtc);
        }

        #endregion

        #region Public-Methods

        /// <summary>Delete previously generated synthetic entities from the target tenant.</summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task WipeAsync(CancellationToken token = default)
        {
            Tenant tenant = await ResolveTenantAsync(false, token).ConfigureAwait(false);
            if (tenant == null)
            {
                Console.WriteLine("Wipe: no matching tenant; nothing to remove.");
                return;
            }

            int collections = 0;
            List<Collection> allCollections = await _Client.Collection.ReadAll(token).ConfigureAwait(false);
            foreach (Collection collection in allCollections)
            {
                if (collection.TenantId != tenant.Id) continue;
                if (!IsSynthetic(collection)) continue;
                await _Client.Collection.Delete(collection.Id, token).ConfigureAwait(false);
                collections++;
            }

            int credentials = 0;
            int users = 0;
            List<User> tenantUsers = await _Client.Users.ReadByTenant(tenant.Id, token).ConfigureAwait(false);
            foreach (User user in tenantUsers)
            {
                if (String.IsNullOrEmpty(user.Email) || !user.Email.EndsWith("@" + SyntheticEmailDomain, StringComparison.OrdinalIgnoreCase)) continue;

                List<Credential> userCredentials = await _Client.Credentials.ReadByUser(user.Id, token).ConfigureAwait(false);
                foreach (Credential credential in userCredentials)
                {
                    if (credential.IsProtected) continue;
                    await _Client.Credentials.Delete(credential.Id, token).ConfigureAwait(false);
                    credentials++;
                }

                await _Client.Users.Delete(user.Id, token).ConfigureAwait(false);
                users++;
            }

            int assignments = 0;
            List<UserRoleAssignment> allAssignments = await _Client.Roles.ReadAllUserRoleAssignments(tenant.Id, token).ConfigureAwait(false);
            foreach (UserRoleAssignment assignment in allAssignments)
            {
                if (assignment.RoleName == null || !assignment.RoleName.StartsWith(RolePrefix, StringComparison.Ordinal)) continue;
                await _Client.Roles.DeleteUserRoleAssignment(assignment.Id, token).ConfigureAwait(false);
                assignments++;
            }

            int roles = 0;
            List<UserRole> tenantRoles = await _Client.Roles.ReadRoles(tenant.Id, token).ConfigureAwait(false);
            foreach (UserRole role in tenantRoles)
            {
                if (role.IsBuiltIn || role.Name == null || !role.Name.StartsWith(RolePrefix, StringComparison.Ordinal)) continue;
                await _Client.Roles.DeleteRolePermissionMapsByRole(role.Id, token).ConfigureAwait(false);
                await _Client.Roles.DeleteRole(role.Id, token).ConfigureAwait(false);
                roles++;
            }

            Console.WriteLine("Wiped: " + collections + " collections, " + users + " users, " + credentials + " credentials, " + roles + " roles, " + assignments + " assignments.");
            Console.WriteLine("(Request-history and audit entries accumulate and are pruned by retention; they are not wiped.)");
        }

        /// <summary>Generate synthetic activity according to the settings.</summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Summary of what was created.</returns>
        public async Task<SeedSummary> SeedAsync(CancellationToken token = default)
        {
            SeedSummary summary = new SeedSummary();
            Tenant tenant = await ResolveTenantAsync(true, token).ConfigureAwait(false);

            List<User> users = new List<User>();
            if (_Settings.IsEnabled(OperationKind.Users))
            {
                users = await SeedUsersAsync(tenant, summary, token).ConfigureAwait(false);
            }

            if (_Settings.IsEnabled(OperationKind.Credentials))
            {
                await SeedCredentialsAsync(tenant, users, summary, token).ConfigureAwait(false);
            }

            if (_Settings.IsEnabled(OperationKind.Roles))
            {
                await SeedRolesAsync(tenant, users, summary, token).ConfigureAwait(false);
            }

            List<Collection> collections = new List<Collection>();
            if (_Settings.IsEnabled(OperationKind.Collections))
            {
                collections = await SeedCollectionsAsync(tenant, summary, token).ConfigureAwait(false);
            }
            else
            {
                collections = await TenantCollectionsAsync(tenant, token).ConfigureAwait(false);
            }

            if (_Settings.IsEnabled(OperationKind.Documents) && collections.Count > 0)
            {
                await SeedDocumentsAsync(collections, summary, token).ConfigureAwait(false);
            }

            if (_Settings.IsEnabled(OperationKind.Requests))
            {
                await SeedRequestHistoryAsync(collections, summary, token).ConfigureAwait(false);
            }

            if (_Settings.IsEnabled(OperationKind.Audit))
            {
                await SeedAuditAsync(tenant, users, summary, token).ConfigureAwait(false);
            }

            if (!String.IsNullOrEmpty(_Settings.ServerUrl) && !String.IsNullOrEmpty(_Settings.AccessKey) && _Settings.LiveRequestCount > 0)
            {
                summary.LiveRequests = await LiveTrafficAsync(collections, token).ConfigureAwait(false);
            }

            return summary;
        }

        #endregion

        #region Private-Methods

        private async Task<Tenant> ResolveTenantAsync(bool createIfMissing, CancellationToken token)
        {
            if (!String.IsNullOrEmpty(_Settings.TenantName))
            {
                Tenant named = await _Client.Tenants.ReadByName(_Settings.TenantName, token).ConfigureAwait(false);
                if (named != null) return named;
                if (!createIfMissing) return null;

                Tenant created = new Tenant
                {
                    Id = IdGenerator.NewTenantId(),
                    Name = _Settings.TenantName,
                    Active = true,
                    CreatedUtc = _WindowStartUtc,
                    LastUpdateUtc = _WindowStartUtc
                };
                return await _Client.Tenants.Create(created, token).ConfigureAwait(false);
            }

            List<Tenant> tenants = await _Client.Tenants.ReadAll(token).ConfigureAwait(false);
            if (tenants != null && tenants.Count > 0) return tenants[0];
            if (!createIfMissing) return null;

            // Empty database (no first-boot seeding has run) — create a default tenant to own the data.
            Tenant fallback = new Tenant
            {
                Id = IdGenerator.NewTenantId(),
                Name = "Default",
                Active = true,
                CreatedUtc = _WindowStartUtc,
                LastUpdateUtc = _WindowStartUtc
            };
            return await _Client.Tenants.Create(fallback, token).ConfigureAwait(false);
        }

        private async Task<List<User>> SeedUsersAsync(Tenant tenant, SeedSummary summary, CancellationToken token)
        {
            int count = _Settings.EffectiveUserCount();
            List<DateTime> created = _Clock.GenerateTimestamps(count);
            List<User> result = new List<User>();

            for (int i = 0; i < count; i++)
            {
                string first = _Content.FirstName();
                string last = _Content.LastName();
                string email = (first + "." + last + "." + _Random.Next(100, 999)).ToLowerInvariant() + "@" + SyntheticEmailDomain;

                User user = new User
                {
                    Id = IdGenerator.NewUserId(),
                    TenantId = tenant.Id,
                    FirstName = first,
                    LastName = last,
                    Email = email,
                    PasswordSha256 = PasswordHasher.Sha256Hex("password"),
                    IsTenantAdmin = _Random.NextDouble() < 0.15,
                    Active = _Random.NextDouble() < 0.9,
                    CreatedUtc = created[i],
                    LastUpdateUtc = created[i]
                };

                User createdUser = await _Client.Users.Create(user, token).ConfigureAwait(false);
                result.Add(createdUser ?? user);
                summary.Users++;
            }

            return result;
        }

        private async Task SeedCredentialsAsync(Tenant tenant, List<User> users, SeedSummary summary, CancellationToken token)
        {
            List<User> pool = users != null && users.Count > 0 ? users : await _Client.Users.ReadByTenant(tenant.Id, token).ConfigureAwait(false);
            if (pool == null || pool.Count == 0) return;

            int count = _Settings.EffectiveCredentialCount();
            List<DateTime> created = _Clock.GenerateTimestamps(count);

            for (int i = 0; i < count; i++)
            {
                User owner = pool[_Random.Next(pool.Count)];
                string rawKey = AccessKeyGenerator.NewAccessKey();

                Credential credential = new Credential
                {
                    Id = IdGenerator.NewCredentialId(),
                    TenantId = tenant.Id,
                    UserId = owner.Id,
                    Name = _Content.Pick(new[] { "CI pipeline", "backup job", "analytics", "integration", "mobile app", "ingest worker" }) + " key",
                    AccessKey = rawKey,
                    AccessKeySha256 = PasswordHasher.Sha256Hex(rawKey),
                    AccessKeyLast4 = rawKey.Substring(rawKey.Length - 4),
                    Active = _Random.NextDouble() < 0.85,
                    CreatedUtc = created[i],
                    LastUpdateUtc = created[i]
                };

                await _Client.Credentials.Create(credential, token).ConfigureAwait(false);
                summary.Credentials++;
            }
        }

        private async Task SeedRolesAsync(Tenant tenant, List<User> users, SeedSummary summary, CancellationToken token)
        {
            int count = _Settings.EffectiveRoleCount();
            List<UserRole> created = new List<UserRole>();
            string[] roleWords = { "Analyst", "Operator", "Auditor", "Integrator", "Reviewer", "Support", "Billing", "Automation" };

            for (int i = 0; i < count; i++)
            {
                string name = RolePrefix + roleWords[i % roleWords.Length] + (i >= roleWords.Length ? "-" + i : String.Empty);

                UserRole role = new UserRole
                {
                    Id = IdGenerator.NewUserRoleId(),
                    TenantId = tenant.Id,
                    Name = name,
                    IsBuiltIn = false,
                    Active = true,
                    CreatedUtc = _WindowStartUtc,
                    LastUpdateUtc = _WindowStartUtc
                };
                UserRole createdRole = await _Client.Roles.CreateRole(role, token).ConfigureAwait(false);
                UserRole effective = createdRole ?? role;

                Permission permission = new Permission
                {
                    Id = IdGenerator.NewPermissionId(),
                    TenantId = tenant.Id,
                    Name = name + " grant",
                    PermissionType = PermissionType.Permit,
                    ResourceTypes = new List<ResourceType> { ResourceType.Collection, ResourceType.Document },
                    OperationTypes = new List<OperationType> { OperationType.Read },
                    CreatedUtc = _WindowStartUtc,
                    LastUpdateUtc = _WindowStartUtc
                };
                Permission createdPermission = await _Client.Roles.CreatePermission(permission, token).ConfigureAwait(false);

                RolePermissionMap map = new RolePermissionMap
                {
                    Id = IdGenerator.NewRolePermissionMapId(),
                    TenantId = tenant.Id,
                    RoleId = effective.Id,
                    PermissionId = (createdPermission ?? permission).Id,
                    CreatedUtc = _WindowStartUtc,
                    LastUpdateUtc = _WindowStartUtc
                };
                await _Client.Roles.CreateRolePermissionMap(map, token).ConfigureAwait(false);

                created.Add(effective);
                summary.Roles++;
            }

            if (users == null || users.Count == 0 || created.Count == 0) return;

            List<DateTime> assignedAt = _Clock.GenerateTimestamps(users.Count);
            for (int i = 0; i < users.Count; i++)
            {
                UserRole role = created[_Random.Next(created.Count)];
                UserRoleAssignment assignment = new UserRoleAssignment
                {
                    Id = IdGenerator.NewUserRoleAssignmentId(),
                    TenantId = tenant.Id,
                    UserId = users[i].Id,
                    RoleId = role.Id,
                    RoleName = role.Name,
                    ResourceScope = ResourceScope.Tenant,
                    InheritsToChildren = true,
                    Active = true,
                    CreatedUtc = assignedAt[i],
                    LastUpdateUtc = assignedAt[i]
                };
                await _Client.Roles.CreateUserRoleAssignment(assignment, token).ConfigureAwait(false);
                summary.Assignments++;
            }
        }

        private async Task<List<Collection>> SeedCollectionsAsync(Tenant tenant, SeedSummary summary, CancellationToken token)
        {
            List<CollectionTheme> themes = _Content.Themes();
            int count = _Settings.EffectiveCollectionCount();
            List<Collection> result = new List<Collection>();

            for (int i = 0; i < count; i++)
            {
                CollectionTheme theme = themes[i % themes.Count];
                string name = i < themes.Count ? theme.Name : theme.Name + "-" + (i / themes.Count + 1);

                Collection collection = await _Client.Collection.Create(
                    name,
                    theme.Description,
                    null,
                    new List<string> { SyntheticLabel },
                    new Dictionary<string, string> { [GeneratorTagKey] = GeneratorTagValue },
                    SchemaEnforcementMode.None,
                    null,
                    IndexingMode.All,
                    null,
                    tenant.Id,
                    token).ConfigureAwait(false);

                if (collection != null)
                {
                    result.Add(collection);
                    summary.Collections++;
                }
            }

            return result;
        }

        private async Task<List<Collection>> TenantCollectionsAsync(Tenant tenant, CancellationToken token)
        {
            List<Collection> all = await _Client.Collection.ReadAll(token).ConfigureAwait(false);
            List<Collection> result = new List<Collection>();
            foreach (Collection collection in all)
            {
                if (collection.TenantId == tenant.Id) result.Add(collection);
            }
            return result;
        }

        private async Task SeedDocumentsAsync(List<Collection> collections, SeedSummary summary, CancellationToken token)
        {
            List<CollectionTheme> themes = _Content.Themes();
            int perCollection = _Settings.EffectiveDocumentsPerCollection();

            foreach (Collection collection in collections)
            {
                CollectionTheme theme = ThemeForName(themes, collection.Name);
                List<BatchDocument> batch = new List<BatchDocument>();

                for (int i = 0; i < perCollection; i++)
                {
                    batch.Add(new BatchDocument
                    {
                        Json = theme.DocumentFactory(),
                        Labels = new List<string> { SyntheticLabel },
                        Tags = new Dictionary<string, string> { [GeneratorTagKey] = GeneratorTagValue }
                    });

                    if (batch.Count >= 200)
                    {
                        await _Client.Document.IngestBatch(collection.Id, batch, token).ConfigureAwait(false);
                        summary.Documents += batch.Count;
                        batch = new List<BatchDocument>();
                    }
                }

                if (batch.Count > 0)
                {
                    await _Client.Document.IngestBatch(collection.Id, batch, token).ConfigureAwait(false);
                    summary.Documents += batch.Count;
                }
            }
        }

        private async Task SeedRequestHistoryAsync(List<Collection> collections, SeedSummary summary, CancellationToken token)
        {
            int count = _Settings.EffectiveRequestCount();
            List<DateTime> timestamps = _Clock.GenerateTimestamps(count);

            for (int i = 0; i < count; i++)
            {
                DateTime start = timestamps[i];
                RequestShape shape = NextRequestShape(collections);
                int status = NextStatusCode();
                double latency = NextLatencyMs(shape.RequestType);

                RequestHistoryDetail detail = new RequestHistoryDetail
                {
                    Id = Guid.NewGuid().ToString("N"),
                    CreatedUtc = start,
                    CompletedUtc = start.AddMilliseconds(latency),
                    RequestType = shape.RequestType,
                    Method = shape.Method,
                    Path = shape.Path,
                    Url = shape.Path,
                    SourceIp = _Content.SourceIp(),
                    CollectionId = shape.CollectionId,
                    StatusCode = status,
                    Success = status < 400,
                    ProcessingTimeMs = Math.Round(latency, 2),
                    RequestBodyLength = shape.Method == "GET" || shape.Method == "HEAD" ? 0 : _Random.Next(40, 2048),
                    ResponseBodyLength = _Random.Next(20, 8192),
                    RequestContentType = shape.Method == "GET" || shape.Method == "HEAD" ? null : "application/json",
                    ResponseContentType = "application/json"
                };

                await _Client.RequestHistory.Create(detail, token).ConfigureAwait(false);
                summary.RequestHistory++;
            }
        }

        private async Task SeedAuditAsync(Tenant tenant, List<User> users, SeedSummary summary, CancellationToken token)
        {
            int count = _Settings.EffectiveAuditCount();
            List<DateTime> timestamps = _Clock.GenerateTimestamps(count);

            for (int i = 0; i < count; i++)
            {
                AuditShape shape = NextAuditShape();
                string userId = users != null && users.Count > 0 ? users[_Random.Next(users.Count)].Id : null;

                AuditEntry entry = new AuditEntry
                {
                    Id = IdGenerator.NewAuditId(),
                    TenantId = tenant.Id,
                    EventType = shape.EventType,
                    PrincipalType = PrincipalType.User,
                    UserId = userId,
                    Method = shape.Method,
                    Path = shape.Path,
                    AuthResult = shape.ResponseCode == 401 ? "Failed" : "Success",
                    AuthzResult = shape.ResponseCode == 403 ? "Denied" : "Permitted",
                    DenialReason = shape.ResponseCode == 403 ? "Missing required permission" : null,
                    ResponseCode = shape.ResponseCode,
                    SourceIp = _Content.SourceIp(),
                    CreatedUtc = timestamps[i]
                };

                await _Client.Audit.Create(entry, token).ConfigureAwait(false);
                summary.AuditEntries++;
            }
        }

        private async Task<int> LiveTrafficAsync(List<Collection> collections, CancellationToken token)
        {
            int fired = 0;
            using (System.Net.Http.HttpClient http = new System.Net.Http.HttpClient())
            {
                http.Timeout = TimeSpan.FromSeconds(10);
                http.DefaultRequestHeaders.Add("Authorization", "Bearer " + _Settings.AccessKey);
                string baseUrl = _Settings.ServerUrl.TrimEnd('/');

                for (int i = 0; i < _Settings.LiveRequestCount; i++)
                {
                    string path = "/v1.0/health";
                    double roll = _Random.NextDouble();
                    if (roll < 0.5) path = "/v1.0/collections";
                    else if (roll < 0.7 && collections.Count > 0) path = "/v1.0/collections/" + collections[_Random.Next(collections.Count)].Id + "/documents?maxResults=10";

                    try
                    {
                        using (System.Net.Http.HttpResponseMessage response = await http.GetAsync(baseUrl + path, token).ConfigureAwait(false))
                        {
                            fired++;
                        }
                    }
                    catch (Exception)
                    {
                        // Ignore transient live-traffic failures; the burst is best-effort.
                    }
                }
            }
            return fired;
        }

        private RequestShape NextRequestShape(List<Collection> collections)
        {
            string collectionId = collections != null && collections.Count > 0 ? collections[_Random.Next(collections.Count)].Id : "col_example";
            double roll = _Random.NextDouble();

            if (roll < 0.20) return new RequestShape("healthCheck", "GET", "/v1.0/health", null);
            if (roll < 0.42) return new RequestShape("collection", "GET", "/v1.0/collections", null);
            if (roll < 0.50) return new RequestShape("collection", "GET", "/v1.0/collections/" + collectionId, collectionId);
            if (roll < 0.70) return new RequestShape("document", "GET", "/v1.0/collections/" + collectionId + "/documents", collectionId);
            if (roll < 0.82) return new RequestShape("document", "PUT", "/v1.0/collections/" + collectionId + "/documents", collectionId);
            if (roll < 0.90) return new RequestShape("search", "POST", "/v1.0/collections/" + collectionId + "/documents/search", collectionId);
            if (roll < 0.96) return new RequestShape("document", "DELETE", "/v1.0/collections/" + collectionId + "/documents/doc_example", collectionId);
            return new RequestShape("collection", "PUT", "/v1.0/collections", null);
        }

        private int NextStatusCode()
        {
            double roll = _Random.NextDouble();
            if (roll < 0.78) return 200;
            if (roll < 0.90) return 201;
            if (roll < 0.92) return 204;
            if (roll < 0.95) return 400;
            if (roll < 0.97) return 401;
            if (roll < 0.99) return 404;
            return 500;
        }

        private double NextLatencyMs(string requestType)
        {
            double baseMs = requestType == "search" ? 45.0 : (requestType == "healthCheck" ? 2.0 : 12.0);
            double gaussian = Math.Abs(NextGaussian());
            return Math.Max(0.4, baseMs * (0.5 + gaussian));
        }

        private AuditShape NextAuditShape()
        {
            double roll = _Random.NextDouble();
            if (roll < 0.45) return new AuditShape("AuthSuccess", "POST", "/v1.0/token", 200);
            if (roll < 0.55) return new AuditShape("AuthFailure", "POST", "/v1.0/token", 401);
            if (roll < 0.62) return new AuditShape("AuthorizationDenied", "DELETE", "/v1.0/collections", 403);
            if (roll < 0.72) return new AuditShape("CollectionCreated", "PUT", "/v1.0/collections", 201);
            if (roll < 0.80) return new AuditShape("UserCreated", "PUT", "/v1.0/users", 201);
            if (roll < 0.87) return new AuditShape("CredentialCreated", "PUT", "/v1.0/credentials", 201);
            if (roll < 0.93) return new AuditShape("RoleAssigned", "PUT", "/v1.0/assignments", 200);
            if (roll < 0.97) return new AuditShape("TenantUpdated", "PUT", "/v1.0/tenants", 200);
            return new AuditShape("AuditViewed", "GET", "/v1.0/audit", 200);
        }

        private double NextGaussian()
        {
            double u1 = 1.0 - _Random.NextDouble();
            double u2 = _Random.NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        }

        private static bool IsSynthetic(Collection collection)
        {
            if (collection.Labels != null && collection.Labels.Contains(SyntheticLabel)) return true;
            if (collection.Tags != null && collection.Tags.TryGetValue(GeneratorTagKey, out string value) && value == GeneratorTagValue) return true;
            return false;
        }

        private static CollectionTheme ThemeForName(List<CollectionTheme> themes, string name)
        {
            foreach (CollectionTheme theme in themes)
            {
                if (name != null && name.StartsWith(theme.Name, StringComparison.OrdinalIgnoreCase)) return theme;
            }
            return themes[0];
        }

        #endregion
    }
}
