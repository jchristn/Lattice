namespace Lattice.Server.API.REST
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using WatsonWebserver.Core;
    using WatsonWebserver.Core.OpenApi;
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

            // Authentication / session.
            routes.PreAuthentication.Static.Add(HttpMethod.POST, "/v1.0/token", PostTokenRoute, ExceptionRoute,
                openApiMetadata: OpenApiRouteMetadata.Create("Log in", "Authentication")
                    .WithDescription("Exchange an email and password for a session token. The tenant is optional and inferred from the credentials; when they match multiple tenants the response lists them to choose from. This route is public.")
                    .WithRequestBody(OpenApiRequestBodyMetadata.Json(new OpenApiSchemaMetadata
                    {
                        Type = "object",
                        Required = new List<string> { "email", "password" },
                        Properties = new Dictionary<string, OpenApiSchemaMetadata>
                        {
                            ["email"] = new OpenApiSchemaMetadata { Type = "string", Description = "User email." },
                            ["password"] = new OpenApiSchemaMetadata { Type = "string", Description = "User password." },
                            ["tenantId"] = new OpenApiSchemaMetadata { Type = "string", Description = "Tenant to authenticate against; omit to infer from the credentials." }
                        }
                    }, "Login credentials", true))
                    .WithResponse(200, OpenApiResponseMetadata.Create("A session token, or a tenant selection to complete."))
                    .WithResponse(401, OpenApiResponseMetadata.Create("Invalid credentials.")));
            routes.PreAuthentication.Static.Add(HttpMethod.GET, "/v1.0/token", GetWhoAmIRoute, ExceptionRoute,
                openApiMetadata: OpenApiRouteMetadata.Create("Describe the current principal", "Authentication")
                    .WithDescription("Return the resolved principal (tenant, user/credential, admin status) for the presented bearer.")
                    .WithResponse(200, OpenApiResponseMetadata.Create("The resolved principal.")));
            routes.PreAuthentication.Static.Add(HttpMethod.GET, "/v1.0/token/details", GetWhoAmIRoute, ExceptionRoute,
                openApiMetadata: OpenApiRouteMetadata.Create("Describe the current principal (alias)", "Authentication")
                    .WithDescription("Alias of GET /v1.0/whoami.")
                    .WithResponse(200, OpenApiResponseMetadata.Create("The resolved principal.")));
            routes.PreAuthentication.Static.Add(HttpMethod.DELETE, "/v1.0/token", DeleteTokenRoute, ExceptionRoute,
                openApiMetadata: OpenApiRouteMetadata.Create("Log out", "Authentication")
                    .WithDescription("Revoke the current session token.")
                    .WithResponse(200, OpenApiResponseMetadata.Create("Session revoked.")));
            routes.PreAuthentication.Static.Add(HttpMethod.GET, "/v1.0/whoami", GetWhoAmIRoute, ExceptionRoute,
                openApiMetadata: OpenApiRouteMetadata.Create("Describe the current principal", "Authentication")
                    .WithDescription("Return the resolved principal for the presented bearer.")
                    .WithResponse(200, OpenApiResponseMetadata.Create("The resolved principal.")));

            // Tenants.
            routes.PreAuthentication.Static.Add(HttpMethod.GET, "/v1.0/tenants", GetTenantsRoute, ExceptionRoute,
                openApiMetadata: OpenApiRouteMetadata.Create("List tenants", "Tenants")
                    .WithDescription("Enumerate tenants (system administrator).")
                    .WithResponse(200, OpenApiResponseMetadata.Create("Tenants retrieved.")));
            routes.PreAuthentication.Static.Add(HttpMethod.PUT, "/v1.0/tenants", PutTenantRoute, ExceptionRoute,
                openApiMetadata: OpenApiRouteMetadata.Create("Create a tenant", "Tenants")
                    .WithRequestBody(OpenApiRequestBodyMetadata.Json(new OpenApiSchemaMetadata
                    {
                        Type = "object",
                        Required = new List<string> { "name" },
                        Properties = new Dictionary<string, OpenApiSchemaMetadata> { ["name"] = new OpenApiSchemaMetadata { Type = "string", Description = "Tenant name." } }
                    }, "Tenant to create", true))
                    .WithResponse(201, OpenApiResponseMetadata.Create("Tenant created.")));
            routes.PreAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantId}", GetTenantRoute, ExceptionRoute,
                openApiMetadata: OpenApiRouteMetadata.Create("Get a tenant", "Tenants")
                    .WithParameter(OpenApiParameterMetadata.Path("tenantId", "Tenant identifier", OpenApiSchemaMetadata.String()))
                    .WithResponse(200, OpenApiResponseMetadata.Create("Tenant retrieved."))
                    .WithResponse(404, OpenApiResponseMetadata.NotFound()));
            routes.PreAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantId}", UpdateTenantRoute, ExceptionRoute,
                openApiMetadata: OpenApiRouteMetadata.Create("Update a tenant", "Tenants")
                    .WithDescription("Update a tenant's name and/or active flag. Only supplied fields change.")
                    .WithParameter(OpenApiParameterMetadata.Path("tenantId", "Tenant identifier", OpenApiSchemaMetadata.String()))
                    .WithResponse(200, OpenApiResponseMetadata.Create("Tenant updated."))
                    .WithResponse(404, OpenApiResponseMetadata.NotFound()));
            routes.PreAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantId}", DeleteTenantRoute, ExceptionRoute,
                openApiMetadata: OpenApiRouteMetadata.Create("Delete a tenant", "Tenants")
                    .WithParameter(OpenApiParameterMetadata.Path("tenantId", "Tenant identifier", OpenApiSchemaMetadata.String()))
                    .WithResponse(200, OpenApiResponseMetadata.Create("Tenant deleted."))
                    .WithResponse(404, OpenApiResponseMetadata.NotFound()));

            // Users.
            routes.PreAuthentication.Static.Add(HttpMethod.GET, "/v1.0/users", GetUsersRoute, ExceptionRoute,
                openApiMetadata: OpenApiRouteMetadata.Create("List users", "Users")
                    .WithDescription("Enumerate users in the caller's tenant. System administrators may pass tenantId to target another tenant.")
                    .WithParameter(OpenApiParameterMetadata.Query("tenantId", "Tenant to list (system administrators only)", false, OpenApiSchemaMetadata.String()))
                    .WithResponse(200, OpenApiResponseMetadata.Create("Users retrieved.")));
            routes.PreAuthentication.Static.Add(HttpMethod.PUT, "/v1.0/users", PutUserRoute, ExceptionRoute,
                openApiMetadata: OpenApiRouteMetadata.Create("Create a user", "Users")
                    .WithRequestBody(OpenApiRequestBodyMetadata.Json(new OpenApiSchemaMetadata
                    {
                        Type = "object",
                        Required = new List<string> { "email", "password" },
                        Properties = new Dictionary<string, OpenApiSchemaMetadata>
                        {
                            ["email"] = new OpenApiSchemaMetadata { Type = "string", Description = "User email (unique within the tenant)." },
                            ["password"] = new OpenApiSchemaMetadata { Type = "string", Description = "Initial password." },
                            ["firstName"] = new OpenApiSchemaMetadata { Type = "string", Description = "Given name." },
                            ["lastName"] = new OpenApiSchemaMetadata { Type = "string", Description = "Family name." },
                            ["isAdmin"] = new OpenApiSchemaMetadata { Type = "boolean", Description = "System administrator (only honored for admin callers)." },
                            ["isTenantAdmin"] = new OpenApiSchemaMetadata { Type = "boolean", Description = "Tenant administrator." },
                            ["tenantId"] = new OpenApiSchemaMetadata { Type = "string", Description = "Target tenant (system administrators only)." }
                        }
                    }, "User to create", true))
                    .WithResponse(201, OpenApiResponseMetadata.Create("User created.")));
            routes.PreAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/users/{userId}", GetUserRoute, ExceptionRoute,
                openApiMetadata: OpenApiRouteMetadata.Create("Get a user", "Users")
                    .WithParameter(OpenApiParameterMetadata.Path("userId", "User identifier", OpenApiSchemaMetadata.String()))
                    .WithResponse(200, OpenApiResponseMetadata.Create("User retrieved."))
                    .WithResponse(404, OpenApiResponseMetadata.NotFound()));
            routes.PreAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/users/{userId}", UpdateUserRoute, ExceptionRoute,
                openApiMetadata: OpenApiRouteMetadata.Create("Update a user", "Users")
                    .WithDescription("Update a user's name, password, tenant-admin/admin flags, or active state. Only supplied fields change.")
                    .WithParameter(OpenApiParameterMetadata.Path("userId", "User identifier", OpenApiSchemaMetadata.String()))
                    .WithResponse(200, OpenApiResponseMetadata.Create("User updated."))
                    .WithResponse(404, OpenApiResponseMetadata.NotFound()));
            routes.PreAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/users/{userId}", DeleteUserRoute, ExceptionRoute,
                openApiMetadata: OpenApiRouteMetadata.Create("Delete a user", "Users")
                    .WithParameter(OpenApiParameterMetadata.Path("userId", "User identifier", OpenApiSchemaMetadata.String()))
                    .WithResponse(200, OpenApiResponseMetadata.Create("User deleted."))
                    .WithResponse(404, OpenApiResponseMetadata.NotFound()));

            // Credentials.
            routes.PreAuthentication.Static.Add(HttpMethod.GET, "/v1.0/credentials", GetCredentialsRoute, ExceptionRoute,
                openApiMetadata: OpenApiRouteMetadata.Create("List credentials", "Credentials")
                    .WithDescription("Enumerate access-key credentials in the caller's tenant.")
                    .WithParameter(OpenApiParameterMetadata.Query("tenantId", "Tenant to list (system administrators only)", false, OpenApiSchemaMetadata.String()))
                    .WithResponse(200, OpenApiResponseMetadata.Create("Credentials retrieved.")));
            routes.PreAuthentication.Static.Add(HttpMethod.PUT, "/v1.0/credentials", PutCredentialRoute, ExceptionRoute,
                openApiMetadata: OpenApiRouteMetadata.Create("Create a credential", "Credentials")
                    .WithDescription("Create an access-key credential. The raw access key is returned once and cannot be retrieved again.")
                    .WithRequestBody(OpenApiRequestBodyMetadata.Json(new OpenApiSchemaMetadata
                    {
                        Type = "object",
                        Properties = new Dictionary<string, OpenApiSchemaMetadata>
                        {
                            ["name"] = new OpenApiSchemaMetadata { Type = "string", Description = "Credential name." },
                            ["userId"] = new OpenApiSchemaMetadata { Type = "string", Description = "Owning user; defaults to the caller." },
                            ["tenantId"] = new OpenApiSchemaMetadata { Type = "string", Description = "Target tenant (system administrators only)." }
                        }
                    }, "Credential to create", false))
                    .WithResponse(201, OpenApiResponseMetadata.Create("Credential created (access key shown once).")));
            routes.PreAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/credentials/{credentialId}", GetCredentialRoute, ExceptionRoute,
                openApiMetadata: OpenApiRouteMetadata.Create("Get a credential", "Credentials")
                    .WithParameter(OpenApiParameterMetadata.Path("credentialId", "Credential identifier", OpenApiSchemaMetadata.String()))
                    .WithResponse(200, OpenApiResponseMetadata.Create("Credential retrieved."))
                    .WithResponse(404, OpenApiResponseMetadata.NotFound()));
            routes.PreAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/credentials/{credentialId}", UpdateCredentialRoute, ExceptionRoute,
                openApiMetadata: OpenApiRouteMetadata.Create("Update a credential", "Credentials")
                    .WithDescription("Update a credential's name and/or active flag. Only supplied fields change.")
                    .WithParameter(OpenApiParameterMetadata.Path("credentialId", "Credential identifier", OpenApiSchemaMetadata.String()))
                    .WithResponse(200, OpenApiResponseMetadata.Create("Credential updated."))
                    .WithResponse(404, OpenApiResponseMetadata.NotFound()));
            routes.PreAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/credentials/{credentialId}", DeleteCredentialRoute, ExceptionRoute,
                openApiMetadata: OpenApiRouteMetadata.Create("Delete a credential", "Credentials")
                    .WithParameter(OpenApiParameterMetadata.Path("credentialId", "Credential identifier", OpenApiSchemaMetadata.String()))
                    .WithResponse(200, OpenApiResponseMetadata.Create("Credential deleted."))
                    .WithResponse(404, OpenApiResponseMetadata.NotFound()));

            // Roles.
            routes.PreAuthentication.Static.Add(HttpMethod.GET, "/v1.0/roles", GetRolesRoute, ExceptionRoute,
                openApiMetadata: OpenApiRouteMetadata.Create("List roles", "Roles")
                    .WithDescription("Enumerate the roles visible to the caller's tenant (its own plus global built-ins).")
                    .WithResponse(200, OpenApiResponseMetadata.Create("Roles retrieved.")));
            routes.PreAuthentication.Static.Add(HttpMethod.PUT, "/v1.0/roles", PutRoleRoute, ExceptionRoute,
                openApiMetadata: OpenApiRouteMetadata.Create("Create a role", "Roles")
                    .WithDescription("Create a tenant role and the grants it confers (built-in roles are global and read-only).")
                    .WithRequestBody(OpenApiRequestBodyMetadata.Json(new OpenApiSchemaMetadata
                    {
                        Type = "object",
                        Required = new List<string> { "name" },
                        Properties = new Dictionary<string, OpenApiSchemaMetadata>
                        {
                            ["name"] = new OpenApiSchemaMetadata { Type = "string", Description = "Role name (unique within the tenant)." },
                            ["permissions"] = new OpenApiSchemaMetadata
                            {
                                Type = "array",
                                Description = "The grants: each has permissionType (permit/deny), resourceTypes[], and operationTypes[].",
                                Items = new OpenApiSchemaMetadata { Type = "object" }
                            }
                        }
                    }, "Role to create", true))
                    .WithResponse(201, OpenApiResponseMetadata.Create("Role created."))
                    .WithResponse(409, OpenApiResponseMetadata.Create("A role with that name already exists.")));
            routes.PreAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/roles/{roleId}", GetRoleRoute, ExceptionRoute,
                openApiMetadata: OpenApiRouteMetadata.Create("Get a role", "Roles")
                    .WithParameter(OpenApiParameterMetadata.Path("roleId", "Role identifier", OpenApiSchemaMetadata.String()))
                    .WithResponse(200, OpenApiResponseMetadata.Create("Role retrieved with its grants."))
                    .WithResponse(404, OpenApiResponseMetadata.NotFound()));
            routes.PreAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/roles/{roleId}", UpdateRoleRoute, ExceptionRoute,
                openApiMetadata: OpenApiRouteMetadata.Create("Update a role", "Roles")
                    .WithDescription("Rename a tenant role and/or replace its grants. Built-in roles cannot be modified.")
                    .WithParameter(OpenApiParameterMetadata.Path("roleId", "Role identifier", OpenApiSchemaMetadata.String()))
                    .WithResponse(200, OpenApiResponseMetadata.Create("Role updated."))
                    .WithResponse(409, OpenApiResponseMetadata.Create("Built-in roles cannot be modified.")));
            routes.PreAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/roles/{roleId}", DeleteRoleRoute, ExceptionRoute,
                openApiMetadata: OpenApiRouteMetadata.Create("Delete a role", "Roles")
                    .WithParameter(OpenApiParameterMetadata.Path("roleId", "Role identifier", OpenApiSchemaMetadata.String()))
                    .WithResponse(200, OpenApiResponseMetadata.Create("Role deleted."))
                    .WithResponse(409, OpenApiResponseMetadata.Create("Built-in roles cannot be deleted.")));

            // Assignments (authorization scope).
            routes.PreAuthentication.Static.Add(HttpMethod.GET, "/v1.0/assignments", GetAssignmentsRoute, ExceptionRoute,
                openApiMetadata: OpenApiRouteMetadata.Create("List role assignments", "Assignments")
                    .WithDescription("Enumerate the user/role assignments in a tenant.")
                    .WithParameter(OpenApiParameterMetadata.Query("tenantId", "Tenant to list (system administrators only)", false, OpenApiSchemaMetadata.String()))
                    .WithResponse(200, OpenApiResponseMetadata.Create("Assignments retrieved.")));
            routes.PreAuthentication.Static.Add(HttpMethod.PUT, "/v1.0/assignments", PutAssignmentRoute, ExceptionRoute,
                openApiMetadata: OpenApiRouteMetadata.Create("Create a role assignment", "Assignments")
                    .WithDescription("Assign a role (by id or name) to a user, optionally scoped to a specific resource.")
                    .WithRequestBody(OpenApiRequestBodyMetadata.Json(new OpenApiSchemaMetadata
                    {
                        Type = "object",
                        Required = new List<string> { "userId" },
                        Properties = new Dictionary<string, OpenApiSchemaMetadata>
                        {
                            ["userId"] = new OpenApiSchemaMetadata { Type = "string", Description = "User to grant the role to." },
                            ["roleId"] = new OpenApiSchemaMetadata { Type = "string", Description = "Role id (or supply roleName)." },
                            ["roleName"] = new OpenApiSchemaMetadata { Type = "string", Description = "Role name (or supply roleId)." },
                            ["resourceScope"] = new OpenApiSchemaMetadata { Type = "string", Description = "Scope: Tenant or Resource." },
                            ["resourceId"] = new OpenApiSchemaMetadata { Type = "string", Description = "Specific resource id, for a Resource-scoped grant." },
                            ["tenantId"] = new OpenApiSchemaMetadata { Type = "string", Description = "Target tenant (system administrators only)." }
                        }
                    }, "Assignment to create", true))
                    .WithResponse(201, OpenApiResponseMetadata.Create("Assignment created.")));
            routes.PreAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/assignments/{assignmentId}", DeleteAssignmentRoute, ExceptionRoute,
                openApiMetadata: OpenApiRouteMetadata.Create("Delete a role assignment", "Assignments")
                    .WithParameter(OpenApiParameterMetadata.Path("assignmentId", "Assignment identifier", OpenApiSchemaMetadata.String()))
                    .WithResponse(200, OpenApiResponseMetadata.Create("Assignment deleted."))
                    .WithResponse(404, OpenApiResponseMetadata.NotFound()));

            // Audit.
            routes.PreAuthentication.Static.Add(HttpMethod.GET, "/v1.0/audit", GetAuditRoute, ExceptionRoute,
                openApiMetadata: OpenApiRouteMetadata.Create("Search the audit log", "Audit")
                    .WithDescription("Enumerate security audit entries for the caller's tenant, filtered optionally by event type.")
                    .WithParameter(OpenApiParameterMetadata.Query("eventType", "Filter by event type (e.g. AuthFailure, AuthzDenied)", false, OpenApiSchemaMetadata.String()))
                    .WithParameter(OpenApiParameterMetadata.Query("tenantId", "Tenant to scope to (system administrators only)", false, OpenApiSchemaMetadata.String()))
                    .WithParameter(OpenApiParameterMetadata.Query("maxResults", "Page size (1-1000)", false, OpenApiSchemaMetadata.Integer()))
                    .WithParameter(OpenApiParameterMetadata.Query("skip", "Records to skip", false, OpenApiSchemaMetadata.Integer()))
                    .WithResponse(200, OpenApiResponseMetadata.Create("Audit entries retrieved.")));
            routes.PreAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/audit/{auditId}", GetAuditEntryRoute, ExceptionRoute,
                openApiMetadata: OpenApiRouteMetadata.Create("Get an audit entry", "Audit")
                    .WithParameter(OpenApiParameterMetadata.Path("auditId", "Audit entry identifier", OpenApiSchemaMetadata.String()))
                    .WithResponse(200, OpenApiResponseMetadata.Create("Audit entry retrieved."))
                    .WithResponse(404, OpenApiResponseMetadata.NotFound()));
            routes.PreAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/audit/{auditId}", DeleteAuditEntryRoute, ExceptionRoute,
                openApiMetadata: OpenApiRouteMetadata.Create("Delete an audit entry", "Audit")
                    .WithParameter(OpenApiParameterMetadata.Path("auditId", "Audit entry identifier", OpenApiSchemaMetadata.String()))
                    .WithResponse(200, OpenApiResponseMetadata.Create("Audit entry deleted."))
                    .WithResponse(404, OpenApiResponseMetadata.NotFound()));
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

                reqCtx.Caller = login.Caller;
                reqCtx.TenantId = login.Caller?.TenantId;
                await WriteAuditEventAsync(reqCtx, "AuthSuccess", ResourceType.Session, login.Caller?.SessionId, 200).ConfigureAwait(false);

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
                        await WriteAuditEventAsync(reqCtx, "SessionRevoked", ResourceType.Session, caller.SessionId, 200).ConfigureAwait(false);
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
                await WriteAuditEventAsync(reqCtx, "TenantCreated", ResourceType.Tenant, created?.Id, 201).ConfigureAwait(false);
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

        private async Task UpdateTenantRoute(HttpContextBase ctx)
        {
            await WrappedRequestHandler(ctx, RequestTypeEnum.Collection, async (reqCtx) =>
            {
                string tenantId = ctx.Request.Url.Parameters["tenantId"];
                Tenant tenant = await _Client.Tenants.ReadById(tenantId, CancellationToken.None).ConfigureAwait(false);
                if (tenant == null || !TenantVisible(reqCtx, tenant.Id)) return new ResponseContext(false, 404, "Tenant not found");

                UpdateTenantRequest request = Deserialize<UpdateTenantRequest>(reqCtx.RequestBody);
                if (request == null) return new ResponseContext(false, 400, "A tenant body is required");

                if (!String.IsNullOrWhiteSpace(request.Name)) tenant.Name = request.Name;
                if (request.Active.HasValue) tenant.Active = request.Active.Value;
                tenant.LastUpdateUtc = DateTime.UtcNow;
                Tenant updated = await _Client.Tenants.Update(tenant, CancellationToken.None).ConfigureAwait(false);

                ServerTelemetry.RecordRbacMutation("tenant", "update");
                await WriteAuditEventAsync(reqCtx, "TenantUpdated", ResourceType.Tenant, tenant.Id, 200).ConfigureAwait(false);
                return new ResponseContext { Success = true, StatusCode = 200, Data = updated };
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
                await WriteAuditEventAsync(reqCtx, "TenantDeleted", ResourceType.Tenant, tenantId, 200).ConfigureAwait(false);
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
                if (created != null)
                {
                    await AssignDefaultRoleAsync(created).ConfigureAwait(false);
                    await WriteAuditEventAsync(reqCtx, "UserCreated", ResourceType.User, created.Id, 201).ConfigureAwait(false);
                }
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

        private async Task UpdateUserRoute(HttpContextBase ctx)
        {
            await WrappedRequestHandler(ctx, RequestTypeEnum.Collection, async (reqCtx) =>
            {
                string userId = ctx.Request.Url.Parameters["userId"];
                User user = await _Client.Users.ReadById(userId, CancellationToken.None).ConfigureAwait(false);
                if (user == null || !TenantVisible(reqCtx, user.TenantId)) return new ResponseContext(false, 404, "User not found");

                UpdateUserRequest request = Deserialize<UpdateUserRequest>(reqCtx.RequestBody);
                if (request == null) return new ResponseContext(false, 400, "A user body is required");

                if (request.FirstName != null) user.FirstName = request.FirstName;
                if (request.LastName != null) user.LastName = request.LastName;
                if (!String.IsNullOrEmpty(request.Password)) user.PasswordSha256 = PasswordHasher.Sha256Hex(request.Password);
                if (request.IsTenantAdmin.HasValue) user.IsTenantAdmin = request.IsTenantAdmin.Value;
                if (request.IsAdmin.HasValue && reqCtx.Caller != null && reqCtx.Caller.IsAdmin) user.IsAdmin = request.IsAdmin.Value;
                if (request.Active.HasValue) user.Active = request.Active.Value;
                user.LastUpdateUtc = DateTime.UtcNow;
                User updated = await _Client.Users.Update(user, CancellationToken.None).ConfigureAwait(false);
                if (updated != null) updated.PasswordSha256 = null;

                ServerTelemetry.RecordRbacMutation("user", "update");
                await WriteAuditEventAsync(reqCtx, "UserUpdated", ResourceType.User, user.Id, 200).ConfigureAwait(false);
                return new ResponseContext { Success = true, StatusCode = 200, Data = updated };
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
                await WriteAuditEventAsync(reqCtx, "UserDeleted", ResourceType.User, userId, 200).ConfigureAwait(false);
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
                    AccessKey = rawAccessKey,
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
                await WriteAuditEventAsync(reqCtx, "CredentialCreated", ResourceType.Credential, created?.Id, 201).ConfigureAwait(false);
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

        private async Task UpdateCredentialRoute(HttpContextBase ctx)
        {
            await WrappedRequestHandler(ctx, RequestTypeEnum.Collection, async (reqCtx) =>
            {
                string credentialId = ctx.Request.Url.Parameters["credentialId"];
                Credential credential = await _Client.Credentials.ReadById(credentialId, CancellationToken.None).ConfigureAwait(false);
                if (credential == null || !TenantVisible(reqCtx, credential.TenantId)) return new ResponseContext(false, 404, "Credential not found");

                UpdateCredentialRequest request = Deserialize<UpdateCredentialRequest>(reqCtx.RequestBody);
                if (request == null) return new ResponseContext(false, 400, "A credential body is required");

                if (request.Name != null) credential.Name = request.Name;
                if (!String.IsNullOrEmpty(request.AccessKey))
                {
                    // The access key is editable; recompute the hash and last-four so the new key authenticates.
                    credential.AccessKey = request.AccessKey;
                    credential.AccessKeySha256 = PasswordHasher.Sha256Hex(request.AccessKey);
                    credential.AccessKeyLast4 = request.AccessKey.Length >= 4 ? request.AccessKey.Substring(request.AccessKey.Length - 4) : request.AccessKey;
                }
                if (request.Active.HasValue) credential.Active = request.Active.Value;
                credential.LastUpdateUtc = DateTime.UtcNow;
                Credential updated = await _Client.Credentials.Update(credential, CancellationToken.None).ConfigureAwait(false);
                if (updated != null) updated.AccessKeySha256 = null; // hide the stored hash; the raw access key is returned

                ServerTelemetry.RecordRbacMutation("credential", "update");
                await WriteAuditEventAsync(reqCtx, "CredentialUpdated", ResourceType.Credential, credential.Id, 200).ConfigureAwait(false);
                return new ResponseContext { Success = true, StatusCode = 200, Data = updated };
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
                await WriteAuditEventAsync(reqCtx, "CredentialDeleted", ResourceType.Credential, credentialId, 200).ConfigureAwait(false);
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

        private async Task PutRoleRoute(HttpContextBase ctx)
        {
            await WrappedRequestHandler(ctx, RequestTypeEnum.Collection, async (reqCtx) =>
            {
                CreateRoleRequest request = Deserialize<CreateRoleRequest>(reqCtx.RequestBody);
                if (request == null || String.IsNullOrWhiteSpace(request.Name)) return new ResponseContext(false, 400, "name is required");

                UserRole existing = await _Client.Roles.ReadRoleByName(reqCtx.TenantId, request.Name, CancellationToken.None).ConfigureAwait(false);
                if (existing != null) return new ResponseContext(false, 409, "A role with that name already exists");

                DateTime now = DateTime.UtcNow;
                UserRole role = new UserRole
                {
                    Id = IdGenerator.NewUserRoleId(),
                    TenantId = reqCtx.TenantId,
                    Name = request.Name,
                    IsBuiltIn = false,
                    Active = true,
                    IsProtected = false,
                    CreatedUtc = now,
                    LastUpdateUtc = now
                };
                await _Client.Roles.CreateRole(role, CancellationToken.None).ConfigureAwait(false);
                await CreateRoleGrantsAsync(role, request.Permissions).ConfigureAwait(false);

                ServerTelemetry.RecordRbacMutation("role", "create");
                await WriteAuditEventAsync(reqCtx, "RoleCreated", ResourceType.Role, role.Id, 201).ConfigureAwait(false);
                return new ResponseContext { Success = true, StatusCode = 201, Data = await BuildRoleDetailAsync(role).ConfigureAwait(false) };
            }).ConfigureAwait(false);
        }

        private async Task GetRoleRoute(HttpContextBase ctx)
        {
            await WrappedRequestHandler(ctx, RequestTypeEnum.Collection, async (reqCtx) =>
            {
                string roleId = ctx.Request.Url.Parameters["roleId"];
                UserRole role = await _Client.Roles.ReadRoleById(roleId, CancellationToken.None).ConfigureAwait(false);
                if (role == null || !RoleVisible(reqCtx, role)) return new ResponseContext(false, 404, "Role not found");
                return new ResponseContext { Success = true, StatusCode = 200, Data = await BuildRoleDetailAsync(role).ConfigureAwait(false) };
            }).ConfigureAwait(false);
        }

        private async Task UpdateRoleRoute(HttpContextBase ctx)
        {
            await WrappedRequestHandler(ctx, RequestTypeEnum.Collection, async (reqCtx) =>
            {
                string roleId = ctx.Request.Url.Parameters["roleId"];
                UserRole role = await _Client.Roles.ReadRoleById(roleId, CancellationToken.None).ConfigureAwait(false);
                if (role == null || !RoleVisible(reqCtx, role)) return new ResponseContext(false, 404, "Role not found");
                if (role.IsBuiltIn || role.IsProtected) return new ResponseContext(false, 409, "Built-in roles cannot be modified");
                if (!RoleEditable(reqCtx, role)) return new ResponseContext(false, 403, "Role belongs to another tenant");

                CreateRoleRequest request = Deserialize<CreateRoleRequest>(reqCtx.RequestBody);
                if (request == null) return new ResponseContext(false, 400, "A role body is required");

                if (!String.IsNullOrWhiteSpace(request.Name) && !String.Equals(request.Name, role.Name, StringComparison.Ordinal))
                {
                    role.Name = request.Name;
                    role.LastUpdateUtc = DateTime.UtcNow;
                    await _Client.Roles.UpdateRole(role, CancellationToken.None).ConfigureAwait(false);
                }

                // Replace the role's grants when a permission set was supplied.
                if (request.Permissions != null)
                {
                    await ClearRoleGrantsAsync(role.Id).ConfigureAwait(false);
                    await CreateRoleGrantsAsync(role, request.Permissions).ConfigureAwait(false);
                }

                ServerTelemetry.RecordRbacMutation("role", "update");
                await WriteAuditEventAsync(reqCtx, "RoleUpdated", ResourceType.Role, role.Id, 200).ConfigureAwait(false);
                return new ResponseContext { Success = true, StatusCode = 200, Data = await BuildRoleDetailAsync(role).ConfigureAwait(false) };
            }).ConfigureAwait(false);
        }

        private async Task DeleteRoleRoute(HttpContextBase ctx)
        {
            await WrappedRequestHandler(ctx, RequestTypeEnum.Collection, async (reqCtx) =>
            {
                string roleId = ctx.Request.Url.Parameters["roleId"];
                UserRole role = await _Client.Roles.ReadRoleById(roleId, CancellationToken.None).ConfigureAwait(false);
                if (role == null || !RoleVisible(reqCtx, role)) return new ResponseContext(false, 404, "Role not found");
                if (role.IsBuiltIn || role.IsProtected) return new ResponseContext(false, 409, "Built-in roles cannot be deleted");
                if (!RoleEditable(reqCtx, role)) return new ResponseContext(false, 403, "Role belongs to another tenant");

                await ClearRoleGrantsAsync(role.Id).ConfigureAwait(false);
                await _Client.Roles.DeleteRole(role.Id, CancellationToken.None).ConfigureAwait(false);

                ServerTelemetry.RecordRbacMutation("role", "delete");
                await WriteAuditEventAsync(reqCtx, "RoleDeleted", ResourceType.Role, role.Id, 200).ConfigureAwait(false);
                return new ResponseContext { Success = true, StatusCode = 200, Data = null };
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
                await WriteAuditEventAsync(reqCtx, "AssignmentCreated", ResourceType.Assignment, created?.Id, 201).ConfigureAwait(false);
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
                await WriteAuditEventAsync(reqCtx, "AssignmentDeleted", ResourceType.Assignment, assignmentId, 200).ConfigureAwait(false);
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

        // A role is visible when it is a global built-in, owned by the caller's tenant, or the caller is a
        // system administrator.
        private static bool RoleVisible(RequestContext reqCtx, UserRole role)
        {
            if (role == null) return false;
            if (reqCtx.Caller != null && reqCtx.Caller.IsAdmin) return true;
            if (String.IsNullOrEmpty(role.TenantId)) return true;
            return String.Equals(reqCtx.TenantId, role.TenantId, StringComparison.Ordinal);
        }

        // A role is editable only when it is a tenant-owned (non-global) role in the caller's tenant.
        private static bool RoleEditable(RequestContext reqCtx, UserRole role)
        {
            if (role == null || String.IsNullOrEmpty(role.TenantId)) return false;
            if (reqCtx.Caller != null && reqCtx.Caller.IsAdmin) return true;
            return String.Equals(reqCtx.TenantId, role.TenantId, StringComparison.Ordinal);
        }

        // Build the role + its grants for the role detail responses.
        private async Task<RoleDetailResponse> BuildRoleDetailAsync(UserRole role)
        {
            RoleDetailResponse detail = new RoleDetailResponse
            {
                Id = role.Id,
                TenantId = role.TenantId,
                Name = role.Name,
                IsBuiltIn = role.IsBuiltIn,
                Active = role.Active,
                IsProtected = role.IsProtected,
                CreatedUtc = role.CreatedUtc,
                LastUpdateUtc = role.LastUpdateUtc
            };

            List<Permission> permissions = await _Client.Roles.ReadPermissionsForRole(role.Id, CancellationToken.None).ConfigureAwait(false);
            if (permissions != null)
            {
                foreach (Permission permission in permissions)
                {
                    detail.Permissions.Add(new RolePermissionSpec
                    {
                        PermissionType = permission.PermissionType,
                        ResourceTypes = new List<ResourceType>(permission.ResourceTypes ?? new List<ResourceType>()),
                        OperationTypes = new List<OperationType>(permission.OperationTypes ?? new List<OperationType>())
                    });
                }
            }

            return detail;
        }

        // Create the permission records and role/permission maps for a role's grant specs.
        private async Task CreateRoleGrantsAsync(UserRole role, List<RolePermissionSpec> specs)
        {
            if (specs == null) return;
            DateTime now = DateTime.UtcNow;

            foreach (RolePermissionSpec spec in specs)
            {
                if (spec == null) continue;
                if (spec.ResourceTypes == null || spec.ResourceTypes.Count == 0) continue;
                if (spec.OperationTypes == null || spec.OperationTypes.Count == 0) continue;

                Permission permission = new Permission
                {
                    Id = IdGenerator.NewPermissionId(),
                    TenantId = role.TenantId,
                    Name = role.Name + " grant",
                    PermissionType = spec.PermissionType,
                    ResourceTypes = new List<ResourceType>(spec.ResourceTypes),
                    OperationTypes = new List<OperationType>(spec.OperationTypes),
                    Active = true,
                    IsProtected = false,
                    CreatedUtc = now,
                    LastUpdateUtc = now
                };
                await _Client.Roles.CreatePermission(permission, CancellationToken.None).ConfigureAwait(false);

                RolePermissionMap map = new RolePermissionMap
                {
                    Id = IdGenerator.NewRolePermissionMapId(),
                    TenantId = role.TenantId,
                    RoleId = role.Id,
                    PermissionId = permission.Id,
                    Active = true,
                    CreatedUtc = now,
                    LastUpdateUtc = now
                };
                await _Client.Roles.CreateRolePermissionMap(map, CancellationToken.None).ConfigureAwait(false);
            }
        }

        // Remove all grants (permissions + maps) for a role, used when editing or deleting it.
        private async Task ClearRoleGrantsAsync(string roleId)
        {
            List<Permission> permissions = await _Client.Roles.ReadPermissionsForRole(roleId, CancellationToken.None).ConfigureAwait(false);
            if (permissions != null)
            {
                foreach (Permission permission in permissions)
                {
                    await _Client.Roles.DeletePermission(permission.Id, CancellationToken.None).ConfigureAwait(false);
                }
            }
            await _Client.Roles.DeleteRolePermissionMapsByRole(roleId, CancellationToken.None).ConfigureAwait(false);
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

        // Give a newly created user a baseline TenantMember role assignment so the tenant has visible,
        // meaningful role assignments as users are added. Best-effort; failures are logged, not fatal.
        private async Task AssignDefaultRoleAsync(User user)
        {
            if (user == null) return;

            try
            {
                UserRole role = await _Client.Roles.ReadRoleByName(null, "TenantMember", CancellationToken.None).ConfigureAwait(false);
                if (role == null) return;

                UserRoleAssignment assignment = new UserRoleAssignment
                {
                    Id = IdGenerator.NewUserRoleAssignmentId(),
                    TenantId = user.TenantId,
                    UserId = user.Id,
                    RoleId = role.Id,
                    RoleName = role.Name,
                    ResourceScope = ResourceScope.Tenant,
                    InheritsToChildren = true,
                    Active = true,
                    CreatedUtc = DateTime.UtcNow,
                    LastUpdateUtc = DateTime.UtcNow
                };
                await _Client.Roles.CreateUserRoleAssignment(assignment, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _Logging?.Warn(_Header + "default role assignment failed: " + e.Message);
            }
        }

        // Append an audit entry for a successful, security-relevant event (a login, a logout, or an
        // identity/RBAC mutation). Best-effort; never breaks request handling.
        private async Task WriteAuditEventAsync(RequestContext reqCtx, string eventType, ResourceType? resourceType, string resourceId, int responseCode)
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
                    ResourceType = resourceType,
                    ResourceId = resourceId,
                    RequestType = reqCtx?.RequestType.ToString(),
                    Method = reqCtx?.Method,
                    Path = reqCtx?.Path,
                    SourceIp = reqCtx?.IpAddress,
                    AuthResult = "Success",
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
