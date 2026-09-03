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
    using Lattice.Core.Search;
    using Lattice.Core.Security;
    using Lattice.Server.API.MCP;
    using Lattice.Server.API.MCP.Json;
    using Lattice.Server.Classes;
    using Lattice.Server.Telemetry;

    /// <summary>
    /// Model Context Protocol (MCP) endpoint, served in-process over JSON-RPC 2.0 at the configured path
    /// (default <c>POST /v1.0/mcp</c>). Every tool call authenticates with the same bearer credentials and
    /// is authorized against the same <see cref="AuthorizationService"/> as the REST API. This partial owns
    /// transport, method routing, and tool execution; tool metadata lives in <see cref="McpToolCatalog"/>
    /// and authorization in <see cref="McpToolAuthorization"/>.
    /// </summary>
    public partial class RestServiceHandler
    {
        #region Mcp-Route-Registration

        private void InitializeMcpRoutes()
        {
            WebserverRoutes routes = _Webserver!.Routes;
            string path = String.IsNullOrEmpty(_Settings.Mcp.Path) ? "/v1.0/mcp" : _Settings.Mcp.Path;
            routes.PreAuthentication.Static.Add(HttpMethod.POST, path, PostMcpRoute, ExceptionRoute);
        }

        #endregion

        #region Mcp-Transport

        private async Task PostMcpRoute(HttpContextBase ctx)
        {
            CallerContext caller = _AuthEnabled ? await AuthenticateAsync(ctx).ConfigureAwait(false) : null;

            string body = await GetRequestBody(ctx).ConfigureAwait(false);
            McpRequest request;
            try
            {
                request = String.IsNullOrWhiteSpace(body)
                    ? null
                    : JsonSerializer.Deserialize<McpRequest>(body, _JsonOptions);
            }
            catch (JsonException)
            {
                await SendMcpErrorAsync(ctx, null, -32700, "Parse error.").ConfigureAwait(false);
                return;
            }

            if (request == null || String.IsNullOrEmpty(request.Method))
            {
                await SendMcpErrorAsync(ctx, request?.Id, -32600, "Invalid request: missing method.").ConfigureAwait(false);
                return;
            }

            bool authenticated = !_AuthEnabled || (caller != null && caller.IsAuthenticated);

            switch (request.Method)
            {
                case "initialize":
                    await SendMcpResultAsync(ctx, request.Id, McpToolCatalog.BuildInitializeResult(_Settings.Mcp.ServerName)).ConfigureAwait(false);
                    return;
                case "ping":
                    await SendMcpResultAsync(ctx, request.Id, new { }).ConfigureAwait(false);
                    return;
                case "notifications/initialized":
                    ctx.Response.StatusCode = 202;
                    await ctx.Response.Send().ConfigureAwait(false);
                    return;
                case "tools/list":
                    if (!authenticated) { await SendMcpErrorAsync(ctx, request.Id, -32000, "Authentication required.").ConfigureAwait(false); return; }
                    await SendMcpResultAsync(ctx, request.Id, new { tools = McpToolCatalog.BuildToolList() }).ConfigureAwait(false);
                    return;
                case "tools/call":
                    if (!authenticated) { await SendMcpErrorAsync(ctx, request.Id, -32000, "Authentication required.").ConfigureAwait(false); return; }
                    await HandleToolCallAsync(ctx, caller, request.Id, request.Params).ConfigureAwait(false);
                    return;
                default:
                    await SendMcpErrorAsync(ctx, request.Id, -32601, "Method not found: " + request.Method).ConfigureAwait(false);
                    return;
            }
        }

        private async Task HandleToolCallAsync(HttpContextBase ctx, CallerContext caller, McpId id, McpRequestParams parameters)
        {
            string toolName = parameters?.Name;
            McpToolArguments args = parameters?.Arguments ?? new McpToolArguments();

            if (String.IsNullOrEmpty(toolName))
            {
                await SendMcpErrorAsync(ctx, id, -32602, "Invalid params: a tool name is required.").ConfigureAwait(false);
                return;
            }

            if (_AuthEnabled && _AuthZ != null)
            {
                bool permitted = await McpToolAuthorization.AuthorizeAsync(_AuthZ, caller, toolName, CancellationToken.None).ConfigureAwait(false);
                if (!permitted)
                {
                    await SendMcpErrorAsync(ctx, id, -32000, "Not permitted.").ConfigureAwait(false);
                    return;
                }
            }

            object toolResult;
            switch (toolName)
            {
                case "lattice_capabilities":
                    toolResult = McpToolCatalog.BuildCapabilities();
                    break;

                case "lattice_whoami":
                    toolResult = BuildWhoAmI(caller);
                    break;

                case "lattice_list_collections":
                {
                    List<Collection> collections = await _Client.Collection.ReadAll(CancellationToken.None).ConfigureAwait(false);
                    toolResult = BuildEnumerationResult(collections, McpSkip(args), McpMax(args));
                    break;
                }

                case "lattice_get_collection":
                {
                    if (String.IsNullOrEmpty(args.CollectionId)) { await SendMcpErrorAsync(ctx, id, -32602, "collectionId is required.").ConfigureAwait(false); return; }
                    Collection collection = await _Client.Collection.ReadById(args.CollectionId, CancellationToken.None).ConfigureAwait(false);
                    if (collection == null) { await SendMcpErrorAsync(ctx, id, -32000, "Collection not found.").ConfigureAwait(false); return; }
                    toolResult = collection;
                    break;
                }

                case "lattice_create_collection":
                {
                    if (String.IsNullOrWhiteSpace(args.Name)) { await SendMcpErrorAsync(ctx, id, -32602, "name is required.").ConfigureAwait(false); return; }
                    Collection created = await _Client.Collection.Create(args.Name, args.Description, labels: args.Labels, tags: args.Tags, token: CancellationToken.None).ConfigureAwait(false);
                    toolResult = created;
                    break;
                }

                case "lattice_delete_collection":
                {
                    if (String.IsNullOrEmpty(args.CollectionId)) { await SendMcpErrorAsync(ctx, id, -32602, "collectionId is required.").ConfigureAwait(false); return; }
                    if (!await _Client.Collection.Exists(args.CollectionId, CancellationToken.None).ConfigureAwait(false)) { await SendMcpErrorAsync(ctx, id, -32000, "Collection not found.").ConfigureAwait(false); return; }
                    await _Client.Collection.Delete(args.CollectionId, CancellationToken.None).ConfigureAwait(false);
                    toolResult = new { deleted = true, collectionId = args.CollectionId };
                    break;
                }

                case "lattice_list_documents":
                {
                    if (String.IsNullOrEmpty(args.CollectionId)) { await SendMcpErrorAsync(ctx, id, -32602, "collectionId is required.").ConfigureAwait(false); return; }
                    List<Document> documents = await _Client.Document.ReadAllInCollection(args.CollectionId, token: CancellationToken.None).ConfigureAwait(false);
                    toolResult = BuildEnumerationResult(documents, McpSkip(args), McpMax(args));
                    break;
                }

                case "lattice_get_document":
                {
                    if (String.IsNullOrEmpty(args.CollectionId) || String.IsNullOrEmpty(args.DocumentId)) { await SendMcpErrorAsync(ctx, id, -32602, "collectionId and documentId are required.").ConfigureAwait(false); return; }
                    Document document = await _Client.Document.ReadById(args.DocumentId, includeContent: args.IncludeContent ?? false, token: CancellationToken.None).ConfigureAwait(false);
                    if (document == null || document.CollectionId != args.CollectionId) { await SendMcpErrorAsync(ctx, id, -32000, "Document not found.").ConfigureAwait(false); return; }
                    toolResult = document;
                    break;
                }

                case "lattice_create_document":
                {
                    if (String.IsNullOrEmpty(args.CollectionId)) { await SendMcpErrorAsync(ctx, id, -32602, "collectionId is required.").ConfigureAwait(false); return; }
                    if (args.Content == null) { await SendMcpErrorAsync(ctx, id, -32602, "content is required.").ConfigureAwait(false); return; }
                    if (!await _Client.Collection.Exists(args.CollectionId, CancellationToken.None).ConfigureAwait(false)) { await SendMcpErrorAsync(ctx, id, -32000, "Collection not found.").ConfigureAwait(false); return; }
                    string json = SerializeDocumentContent(args.Content);
                    Document ingested = await _Client.Document.Ingest(args.CollectionId, json, args.Name, args.Labels, args.Tags, CancellationToken.None).ConfigureAwait(false);
                    toolResult = ingested;
                    break;
                }

                case "lattice_delete_document":
                {
                    if (String.IsNullOrEmpty(args.DocumentId)) { await SendMcpErrorAsync(ctx, id, -32602, "documentId is required.").ConfigureAwait(false); return; }
                    if (!await _Client.Document.Exists(args.DocumentId, CancellationToken.None).ConfigureAwait(false)) { await SendMcpErrorAsync(ctx, id, -32000, "Document not found.").ConfigureAwait(false); return; }
                    await _Client.Document.Delete(args.DocumentId, CancellationToken.None).ConfigureAwait(false);
                    toolResult = new { deleted = true, documentId = args.DocumentId };
                    break;
                }

                case "lattice_search_documents":
                {
                    if (String.IsNullOrEmpty(args.CollectionId)) { await SendMcpErrorAsync(ctx, id, -32602, "collectionId is required.").ConfigureAwait(false); return; }
                    SearchQuery query = new SearchQuery
                    {
                        CollectionId = args.CollectionId,
                        SqlExpression = args.SqlExpression,
                        MaxResults = McpMax(args),
                        Skip = McpSkip(args),
                        IncludeContent = args.IncludeContent ?? false
                    };
                    SearchResult result = await _Client.Search.Search(query, CancellationToken.None).ConfigureAwait(false);
                    toolResult = result;
                    break;
                }

                case "lattice_list_schemas":
                {
                    List<Schema> schemas = await _Client.Schema.ReadAll(CancellationToken.None).ConfigureAwait(false);
                    toolResult = BuildEnumerationResult(schemas, McpSkip(args), McpMax(args));
                    break;
                }

                case "lattice_get_schema":
                {
                    if (String.IsNullOrEmpty(args.SchemaId)) { await SendMcpErrorAsync(ctx, id, -32602, "schemaId is required.").ConfigureAwait(false); return; }
                    Schema schema = await _Client.Schema.ReadById(args.SchemaId, CancellationToken.None).ConfigureAwait(false);
                    if (schema == null) { await SendMcpErrorAsync(ctx, id, -32000, "Schema not found.").ConfigureAwait(false); return; }
                    toolResult = schema;
                    break;
                }

                case "lattice_get_schema_elements":
                {
                    if (String.IsNullOrEmpty(args.SchemaId)) { await SendMcpErrorAsync(ctx, id, -32602, "schemaId is required.").ConfigureAwait(false); return; }
                    List<SchemaElement> elements = await _Client.Schema.GetElements(args.SchemaId, CancellationToken.None).ConfigureAwait(false);
                    toolResult = BuildEnumerationResult(elements, McpSkip(args), McpMax(args));
                    break;
                }

                case "lattice_list_tables":
                {
                    List<IndexTableMapping> tables = await _Client.Index.GetMappings(CancellationToken.None).ConfigureAwait(false);
                    toolResult = BuildEnumerationResult(tables, McpSkip(args), McpMax(args));
                    break;
                }

                case "lattice_get_table_entries":
                {
                    if (String.IsNullOrEmpty(args.TableName)) { await SendMcpErrorAsync(ctx, id, -32602, "tableName is required.").ConfigureAwait(false); return; }
                    int skip = McpSkip(args);
                    int limit = McpClamp(args.Limit ?? args.MaxResults ?? 100, 1, 1000);
                    List<IndexTableEntry> entries = await _Client.Index.GetTableEntries(args.TableName, skip, limit, CancellationToken.None).ConfigureAwait(false);
                    long total = await _Client.Index.GetTableEntryCount(args.TableName, CancellationToken.None).ConfigureAwait(false);
                    toolResult = BuildEnumerationResult(entries, skip, limit, total);
                    break;
                }

                case "lattice_list_tenants":
                {
                    List<Tenant> tenants = await _Client.Tenants.ReadAll(CancellationToken.None).ConfigureAwait(false);
                    toolResult = BuildEnumerationResult(tenants, McpSkip(args), McpMax(args));
                    break;
                }

                case "lattice_list_users":
                {
                    string tenantId = McpEffectiveTenant(caller, args.TenantId);
                    List<User> users = await _Client.Users.ReadByTenant(tenantId, CancellationToken.None).ConfigureAwait(false);
                    foreach (User u in users) u.PasswordSha256 = null;
                    toolResult = BuildEnumerationResult(users, McpSkip(args), McpMax(args));
                    break;
                }

                case "lattice_list_credentials":
                {
                    string tenantId = McpEffectiveTenant(caller, args.TenantId);
                    List<Credential> creds = await _Client.Credentials.ReadByTenant(tenantId, CancellationToken.None).ConfigureAwait(false);
                    toolResult = BuildEnumerationResult(creds, McpSkip(args), McpMax(args));
                    break;
                }

                case "lattice_create_credential":
                {
                    string tenantId = McpEffectiveTenant(caller, args.TenantId);
                    string userId = String.IsNullOrEmpty(args.UserId) ? caller?.UserId : args.UserId;
                    if (String.IsNullOrEmpty(userId)) { await SendMcpErrorAsync(ctx, id, -32602, "userId is required.").ConfigureAwait(false); return; }

                    string rawAccessKey = AccessKeyGenerator.NewAccessKey();
                    Credential credential = new Credential
                    {
                        Id = IdGenerator.NewCredentialId(),
                        TenantId = tenantId,
                        UserId = userId,
                        Name = args.Name,
                        AccessKeySha256 = PasswordHasher.Sha256Hex(rawAccessKey),
                        AccessKeyLast4 = rawAccessKey.Substring(rawAccessKey.Length - 4),
                        CreatedUtc = DateTime.UtcNow,
                        LastUpdateUtc = DateTime.UtcNow
                    };
                    Credential created = await _Client.Credentials.Create(credential, CancellationToken.None).ConfigureAwait(false);
                    if (created != null) created.AccessKey = rawAccessKey; // shown once
                    ServerTelemetry.RecordRbacMutation("credential", "create");
                    toolResult = created;
                    break;
                }

                case "lattice_list_roles":
                {
                    List<UserRole> roles = await _Client.Roles.ReadRoles(caller?.TenantId, CancellationToken.None).ConfigureAwait(false);
                    toolResult = BuildEnumerationResult(roles, McpSkip(args), McpMax(args));
                    break;
                }

                case "lattice_list_audit":
                {
                    string tenantId = McpEffectiveTenant(caller, args.TenantId);
                    int skip = McpSkip(args);
                    int max = McpMax(args);
                    List<AuditEntry> entries = await _Client.Audit.Search(tenantId, args.EventType, null, null, skip, max, CancellationToken.None).ConfigureAwait(false);
                    long total = await _Client.Audit.Count(tenantId, args.EventType, null, null, CancellationToken.None).ConfigureAwait(false);
                    toolResult = BuildEnumerationResult(entries, skip, max, total);
                    break;
                }

                default:
                    await SendMcpErrorAsync(ctx, id, -32601, "Unknown tool: " + toolName).ConfigureAwait(false);
                    return;
            }

            object callResult = new
            {
                content = new[] { new { type = "text", text = JsonSerializer.Serialize(toolResult, _JsonOptions) } },
                isError = false
            };
            await SendMcpResultAsync(ctx, id, callResult).ConfigureAwait(false);
        }

        #endregion

        #region Mcp-Helpers

        private WhoAmIResponse BuildWhoAmI(CallerContext caller)
        {
            return new WhoAmIResponse
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
        }

        // The tenant a management tool operates on: the caller's tenant, unless a system admin named another.
        private static string McpEffectiveTenant(CallerContext caller, string bodyTenantId)
        {
            if (caller != null && caller.IsAdmin && !String.IsNullOrEmpty(bodyTenantId)) return bodyTenantId;
            return caller?.TenantId;
        }

        private static int McpMax(McpToolArguments args)
        {
            return McpClamp(args.MaxResults ?? 100, 1, 1000);
        }

        private static int McpSkip(McpToolArguments args)
        {
            int skip = args.Skip ?? 0;
            return skip < 0 ? 0 : skip;
        }

        private static int McpClamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private async Task SendMcpResultAsync(HttpContextBase ctx, McpId id, object result)
        {
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = "application/json";
            McpSuccessResponse response = new McpSuccessResponse(id, result);
            await ctx.Response.Send(JsonSerializer.Serialize(response, _JsonOptions)).ConfigureAwait(false);
        }

        private async Task SendMcpErrorAsync(HttpContextBase ctx, McpId id, int code, string message)
        {
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = "application/json";
            McpErrorResponse response = new McpErrorResponse(id, code, message);
            await ctx.Response.Send(JsonSerializer.Serialize(response, _JsonOptions)).ConfigureAwait(false);
        }

        #endregion
    }
}
