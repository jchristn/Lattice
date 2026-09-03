namespace Lattice.Server.API.REST
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading;
    using System.Threading.Tasks;
    using SyslogLogging;
    using WatsonWebserver;
    using WatsonWebserver.Core;
    using WatsonWebserver.Core.OpenApi;
    using Lattice.Core;
    using Lattice.Core.Exceptions;
    using Lattice.Core.Models;
    using Lattice.Core.Search;
    using Lattice.Core.Security;
    using Lattice.Core.Validation;
    using Lattice.Server.Classes;
    using Lattice.Server.Security;
    using Lattice.Server.Services;
    using Lattice.Server.Telemetry;

    /// <summary>
    /// REST service handler for Lattice API.
    /// </summary>
    public partial class RestServiceHandler
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private static readonly JsonSerializerOptions _JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };

        private Settings _Settings;
        private LatticeClient _Client;
        private LoggingModule? _Logging;
        private Webserver? _Webserver;
        private RequestHistoryService _RequestHistory;
        private readonly bool _AuthEnabled;
        private readonly SessionTokenCodec? _Codec;
        private readonly AuthenticationService? _AuthN;
        private readonly AuthorizationService? _AuthZ;
        private readonly string _Header = "[RestServiceHandler] ";

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the REST service handler.
        /// </summary>
        /// <param name="settings">Application settings.</param>
        /// <param name="client">Lattice client.</param>
        /// <param name="logging">Logging module.</param>
        /// <exception cref="ArgumentNullException">Thrown when required parameter is null.</exception>
        public RestServiceHandler(Settings settings, LatticeClient client, LoggingModule logging)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Client = client ?? throw new ArgumentNullException(nameof(client));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _RequestHistory = new RequestHistoryService(_Client, _Settings.RequestHistory, _Logging);

            _AuthEnabled = _Settings.Auth.Enable;
            if (_AuthEnabled)
            {
                _Codec = new SessionTokenCodec(_Settings.Auth.TokenSecret);
                _AuthN = new AuthenticationService(
                    _Client.Tenants, _Client.Users, _Client.Credentials, _Client.Sessions,
                    _Codec, _Settings.Auth.SessionTtlMinutes);
                _AuthZ = new AuthorizationService(_Client.Roles);
            }

            InitializeWebserver();
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Start the webserver.
        /// </summary>
        public void Start()
        {
            _Webserver?.Start();
            string protocol = _Settings.Rest.Ssl ? "https" : "http";
            _Logging?.Info(_Header + "started on " + protocol + "://" + _Settings.Rest.Hostname + ":" + _Settings.Rest.Port);
        }

        /// <summary>
        /// Stop the webserver and asynchronously dispose background services.
        /// </summary>
        /// <returns>A task that completes once the webserver has stopped and background services are disposed.</returns>
        public async Task StopAsync()
        {
            _Webserver?.Stop();
            if (_RequestHistory != null) await _RequestHistory.DisposeAsync().ConfigureAwait(false);
            _Logging?.Info(_Header + "stopped");
        }

        #endregion

        #region Private-Methods-Infrastructure

        private void InitializeWebserver()
        {
            WebserverSettings webserverSettings = new WebserverSettings
            {
                Hostname = _Settings.Rest.Hostname,
                Port = _Settings.Rest.Port
            };

            // Configure SSL/TLS settings
            if (_Settings.Rest.Ssl)
            {
                webserverSettings.Ssl.Enable = true;

                if (!String.IsNullOrEmpty(_Settings.Rest.SslCertificateFile))
                {
                    webserverSettings.Ssl.PfxCertificateFile = _Settings.Rest.SslCertificateFile;
                }

                if (!String.IsNullOrEmpty(_Settings.Rest.SslCertificatePassword))
                {
                    webserverSettings.Ssl.PfxCertificatePassword = _Settings.Rest.SslCertificatePassword;
                }
            }

            _Webserver = new Webserver(webserverSettings, DefaultRoute);

            // Configure OpenAPI/Swagger
            _Webserver.UseOpenApi(openApi =>
            {
                openApi.EnableOpenApi = _Settings.Rest.EnableOpenApi;
                openApi.EnableSwaggerUi = _Settings.Rest.EnableSwaggerUi;

                openApi.Info.Title = "Lattice API";
                openApi.Info.Version = "1.0.0";
                openApi.Info.Description = "Lattice is a JSON document store with schema validation, full-text indexing, and flexible search capabilities.";
                openApi.Info.Contact = new OpenApiContact
                {
                    Name = "Lattice Support",
                    Url = "https://github.com/jchristn/Lattice"
                };
                openApi.Info.License = new OpenApiLicense
                {
                    Name = "MIT",
                    Url = "https://opensource.org/licenses/MIT"
                };

                // Define tags for grouping endpoints
                openApi.Tags.Add(new OpenApiTag { Name = "Health", Description = "Health check endpoints" });
                openApi.Tags.Add(new OpenApiTag { Name = "Collections", Description = "Collection management operations" });
                openApi.Tags.Add(new OpenApiTag { Name = "Documents", Description = "Document CRUD operations" });
                openApi.Tags.Add(new OpenApiTag { Name = "Search", Description = "Document search operations" });
                openApi.Tags.Add(new OpenApiTag { Name = "Schemas", Description = "Schema management operations" });
                openApi.Tags.Add(new OpenApiTag { Name = "Index Tables", Description = "Index table operations" });
                openApi.Tags.Add(new OpenApiTag { Name = "Request History", Description = "HTTP request history and diagnostics" });
            });

            InitializeRoutes();
        }

        private void InitializeRoutes()
        {
            // General routes
            _Webserver!.Routes.Preflight = PreflightRoute;
            _Webserver.Routes.PreRouting = PreRoutingRoute;
            _Webserver.Routes.PostRouting = PostRoutingRoute;

            // Authentication, identity, RBAC, and audit routes (only when auth is enabled).
            if (_AuthEnabled) InitializeAuthRoutes();

            // Model Context Protocol (MCP) JSON-RPC endpoint (only when enabled).
            if (_Settings.Mcp.Enable) InitializeMcpRoutes();

            // Health check routes
            _Webserver.Routes.PreAuthentication.Static.Add(
                HttpMethod.GET, "/", GetHealthRoute, ExceptionRoute,
                openApiMetadata: OpenApiRouteMetadata.Create("Health check", "Health")
                    .WithDescription("Returns the health status of the Lattice server")
                    .WithResponse(200, OpenApiResponseMetadata.Json("Server is healthy", new OpenApiSchemaMetadata
                    {
                        Type = "object",
                        Properties = new Dictionary<string, OpenApiSchemaMetadata>
                        {
                            ["success"] = OpenApiSchemaMetadata.Boolean(),
                            ["statusCode"] = OpenApiSchemaMetadata.Integer(),
                            ["data"] = new OpenApiSchemaMetadata
                            {
                                Type = "object",
                                Properties = new Dictionary<string, OpenApiSchemaMetadata>
                                {
                                    ["status"] = OpenApiSchemaMetadata.String(),
                                    ["version"] = OpenApiSchemaMetadata.String(),
                                    ["timestamp"] = OpenApiSchemaMetadata.String("date-time")
                                }
                            }
                        }
                    })));

            _Webserver.Routes.PreAuthentication.Static.Add(
                HttpMethod.GET, "/v1.0/health", GetHealthRoute, ExceptionRoute,
                openApiMetadata: OpenApiRouteMetadata.Create("Health check (versioned)", "Health")
                    .WithDescription("Returns the health status of the Lattice server")
                    .WithResponse(200, OpenApiResponseMetadata.Json("Server is healthy", new OpenApiSchemaMetadata
                    {
                        Type = "object",
                        Properties = new Dictionary<string, OpenApiSchemaMetadata>
                        {
                            ["success"] = OpenApiSchemaMetadata.Boolean(),
                            ["statusCode"] = OpenApiSchemaMetadata.Integer(),
                            ["data"] = new OpenApiSchemaMetadata
                            {
                                Type = "object",
                                Properties = new Dictionary<string, OpenApiSchemaMetadata>
                                {
                                    ["status"] = OpenApiSchemaMetadata.String(),
                                    ["version"] = OpenApiSchemaMetadata.String(),
                                    ["timestamp"] = OpenApiSchemaMetadata.String("date-time")
                                }
                            }
                        }
                    })));

            // Request history routes
            _Webserver.Routes.PreAuthentication.Static.Add(
                HttpMethod.GET, "/v1.0/requesthistory", GetRequestHistoryRoute, ExceptionRoute,
                openApiMetadata: OpenApiRouteMetadata.Create("Search request history", "Request History")
                    .WithDescription("Retrieves paged request history entries using optional filters")
                    .WithParameter(OpenApiParameterMetadata.Query("page", "1-based page number", false, OpenApiSchemaMetadata.Integer()))
                    .WithParameter(OpenApiParameterMetadata.Query("pageSize", "Results per page", false, OpenApiSchemaMetadata.Integer()))
                    .WithParameter(OpenApiParameterMetadata.Query("requestType", "Filter by Lattice request type", false, OpenApiSchemaMetadata.String()))
                    .WithParameter(OpenApiParameterMetadata.Query("method", "Filter by HTTP method", false, OpenApiSchemaMetadata.String()))
                    .WithParameter(OpenApiParameterMetadata.Query("pathContains", "Substring match for the request path", false, OpenApiSchemaMetadata.String()))
                    .WithParameter(OpenApiParameterMetadata.Query("collectionId", "Filter by collection identifier", false, OpenApiSchemaMetadata.String()))
                    .WithParameter(OpenApiParameterMetadata.Query("documentId", "Filter by document identifier", false, OpenApiSchemaMetadata.String()))
                    .WithParameter(OpenApiParameterMetadata.Query("schemaId", "Filter by schema identifier", false, OpenApiSchemaMetadata.String()))
                    .WithParameter(OpenApiParameterMetadata.Query("tableName", "Filter by table name", false, OpenApiSchemaMetadata.String()))
                    .WithParameter(OpenApiParameterMetadata.Query("sourceIp", "Filter by source IP", false, OpenApiSchemaMetadata.String()))
                    .WithParameter(OpenApiParameterMetadata.Query("statusCode", "Filter by HTTP status code", false, OpenApiSchemaMetadata.Integer()))
                    .WithParameter(OpenApiParameterMetadata.Query("success", "Filter by request success", false, OpenApiSchemaMetadata.Boolean()))
                    .WithParameter(OpenApiParameterMetadata.Query("startUtc", "Inclusive UTC start timestamp", false, OpenApiSchemaMetadata.String("date-time")))
                    .WithParameter(OpenApiParameterMetadata.Query("endUtc", "Inclusive UTC end timestamp", false, OpenApiSchemaMetadata.String("date-time")))
                    .WithResponse(200, OpenApiResponseMetadata.Create("Request history entries retrieved successfully")));

            _Webserver.Routes.PreAuthentication.Static.Add(
                HttpMethod.GET, "/v1.0/requesthistory/summary", GetRequestHistorySummaryRoute, ExceptionRoute,
                openApiMetadata: OpenApiRouteMetadata.Create("Summarize request history", "Request History")
                    .WithDescription("Aggregates request history entries into summary buckets for charts and dashboards")
                    .WithParameter(OpenApiParameterMetadata.Query("interval", "Bucket interval: minute, 15minute, hour, 6hour, or day", false, OpenApiSchemaMetadata.String()))
                    .WithParameter(OpenApiParameterMetadata.Query("requestType", "Filter by Lattice request type", false, OpenApiSchemaMetadata.String()))
                    .WithParameter(OpenApiParameterMetadata.Query("method", "Filter by HTTP method", false, OpenApiSchemaMetadata.String()))
                    .WithParameter(OpenApiParameterMetadata.Query("pathContains", "Substring match for the request path", false, OpenApiSchemaMetadata.String()))
                    .WithParameter(OpenApiParameterMetadata.Query("collectionId", "Filter by collection identifier", false, OpenApiSchemaMetadata.String()))
                    .WithParameter(OpenApiParameterMetadata.Query("documentId", "Filter by document identifier", false, OpenApiSchemaMetadata.String()))
                    .WithParameter(OpenApiParameterMetadata.Query("schemaId", "Filter by schema identifier", false, OpenApiSchemaMetadata.String()))
                    .WithParameter(OpenApiParameterMetadata.Query("tableName", "Filter by table name", false, OpenApiSchemaMetadata.String()))
                    .WithParameter(OpenApiParameterMetadata.Query("sourceIp", "Filter by source IP", false, OpenApiSchemaMetadata.String()))
                    .WithParameter(OpenApiParameterMetadata.Query("statusCode", "Filter by HTTP status code", false, OpenApiSchemaMetadata.Integer()))
                    .WithParameter(OpenApiParameterMetadata.Query("success", "Filter by request success", false, OpenApiSchemaMetadata.Boolean()))
                    .WithParameter(OpenApiParameterMetadata.Query("startUtc", "Inclusive UTC start timestamp", false, OpenApiSchemaMetadata.String("date-time")))
                    .WithParameter(OpenApiParameterMetadata.Query("endUtc", "Inclusive UTC end timestamp", false, OpenApiSchemaMetadata.String("date-time")))
                    .WithResponse(200, OpenApiResponseMetadata.Create("Request history summary retrieved successfully")));

            _Webserver.Routes.PreAuthentication.Static.Add(
                HttpMethod.DELETE, "/v1.0/requesthistory/bulk", DeleteRequestHistoryBulkRoute, ExceptionRoute,
                openApiMetadata: OpenApiRouteMetadata.Create("Bulk delete request history", "Request History")
                    .WithDescription("Deletes all request history entries that match the supplied filter")
                    .WithRequestBody(OpenApiRequestBodyMetadata.Json(
                        new OpenApiSchemaMetadata
                        {
                            Type = "object",
                            Properties = new Dictionary<string, OpenApiSchemaMetadata>
                            {
                                ["requestType"] = OpenApiSchemaMetadata.String(),
                                ["method"] = OpenApiSchemaMetadata.String(),
                                ["pathContains"] = OpenApiSchemaMetadata.String(),
                                ["collectionId"] = OpenApiSchemaMetadata.String(),
                                ["documentId"] = OpenApiSchemaMetadata.String(),
                                ["schemaId"] = OpenApiSchemaMetadata.String(),
                                ["tableName"] = OpenApiSchemaMetadata.String(),
                                ["sourceIp"] = OpenApiSchemaMetadata.String(),
                                ["statusCode"] = OpenApiSchemaMetadata.Integer(),
                                ["success"] = OpenApiSchemaMetadata.Boolean(),
                                ["startUtc"] = OpenApiSchemaMetadata.String("date-time"),
                                ["endUtc"] = OpenApiSchemaMetadata.String("date-time")
                            }
                        }, "Bulk delete filter", false))
                    .WithResponse(200, OpenApiResponseMetadata.Create("Matching request history entries deleted successfully")));

            _Webserver.Routes.PreAuthentication.Parameter.Add(
                HttpMethod.GET, "/v1.0/requesthistory/{requestId}", GetRequestHistoryEntryRoute, ExceptionRoute,
                openApiMetadata: OpenApiRouteMetadata.Create("Get request history entry", "Request History")
                    .WithDescription("Retrieves metadata for a single request history entry")
                    .WithParameter(OpenApiParameterMetadata.Path("requestId", "The unique request history identifier", OpenApiSchemaMetadata.String()))
                    .WithResponse(200, OpenApiResponseMetadata.Create("Request history entry retrieved successfully"))
                    .WithResponse(404, OpenApiResponseMetadata.NotFound()));

            _Webserver.Routes.PreAuthentication.Parameter.Add(
                HttpMethod.GET, "/v1.0/requesthistory/{requestId}/detail", GetRequestHistoryDetailRoute, ExceptionRoute,
                openApiMetadata: OpenApiRouteMetadata.Create("Get request history detail", "Request History")
                    .WithDescription("Retrieves full request and response detail for a single history entry")
                    .WithParameter(OpenApiParameterMetadata.Path("requestId", "The unique request history identifier", OpenApiSchemaMetadata.String()))
                    .WithResponse(200, OpenApiResponseMetadata.Create("Request history detail retrieved successfully"))
                    .WithResponse(404, OpenApiResponseMetadata.NotFound()));

            _Webserver.Routes.PreAuthentication.Parameter.Add(
                HttpMethod.DELETE, "/v1.0/requesthistory/{requestId}", DeleteRequestHistoryEntryRoute, ExceptionRoute,
                openApiMetadata: OpenApiRouteMetadata.Create("Delete request history entry", "Request History")
                    .WithDescription("Deletes a single request history entry")
                    .WithParameter(OpenApiParameterMetadata.Path("requestId", "The unique request history identifier", OpenApiSchemaMetadata.String()))
                    .WithResponse(200, OpenApiResponseMetadata.Create("Request history entry deleted successfully"))
                    .WithResponse(404, OpenApiResponseMetadata.NotFound()));

            // Collection routes
            _Webserver.Routes.PreAuthentication.Static.Add(
                HttpMethod.GET, "/v1.0/collections", GetCollectionsRoute, ExceptionRoute,
                openApiMetadata: OpenApiRouteMetadata.Create("List all collections", "Collections")
                    .WithDescription("Retrieves a list of all collections in the database")
                    .WithResponse(200, OpenApiResponseMetadata.Create("List of collections retrieved successfully")));

            _Webserver.Routes.PreAuthentication.Static.Add(
                HttpMethod.PUT, "/v1.0/collections", PutCollectionRoute, ExceptionRoute,
                openApiMetadata: OpenApiRouteMetadata.Create("Create a collection", "Collections")
                    .WithDescription("Creates a new collection with optional schema constraints and indexing configuration")
                    .WithRequestBody(OpenApiRequestBodyMetadata.Json(
                        new OpenApiSchemaMetadata
                        {
                            Type = "object",
                            Required = new List<string> { "name" },
                            Properties = new Dictionary<string, OpenApiSchemaMetadata>
                            {
                                ["name"] = new OpenApiSchemaMetadata { Type = "string", Description = "Name of the collection" },
                                ["description"] = new OpenApiSchemaMetadata { Type = "string", Description = "Optional description" },
                                ["documentsDirectory"] = new OpenApiSchemaMetadata { Type = "string", Description = "Custom storage directory for documents" },
                                ["labels"] = new OpenApiSchemaMetadata { Type = "array", Items = OpenApiSchemaMetadata.String(), Description = "Labels for categorization" },
                                ["tags"] = new OpenApiSchemaMetadata { Type = "object", Description = "Key-value metadata tags" },
                                ["schemaEnforcementMode"] = new OpenApiSchemaMetadata { Type = "string", Enum = new List<object> { "none", "strict", "warn" }, Description = "Schema validation mode" },
                                ["fieldConstraints"] = new OpenApiSchemaMetadata { Type = "array", Description = "Field-level validation rules" },
                                ["indexingMode"] = new OpenApiSchemaMetadata { Type = "string", Enum = new List<object> { "all", "selective" }, Description = "Indexing mode" },
                                ["indexedFields"] = new OpenApiSchemaMetadata { Type = "array", Items = OpenApiSchemaMetadata.String(), Description = "Fields to index (if selective mode)" }
                            }
                        }, "Collection creation request", true))
                    .WithResponse(201, OpenApiResponseMetadata.Create("Collection created successfully"))
                    .WithResponse(400, OpenApiResponseMetadata.BadRequest()));

            _Webserver.Routes.PreAuthentication.Parameter.Add(
                HttpMethod.GET, "/v1.0/collections/{collectionId}", GetCollectionRoute, ExceptionRoute,
                openApiMetadata: OpenApiRouteMetadata.Create("Get a collection", "Collections")
                    .WithDescription("Retrieves a specific collection by its ID")
                    .WithParameter(OpenApiParameterMetadata.Path("collectionId", "The unique identifier of the collection", OpenApiSchemaMetadata.String()))
                    .WithResponse(200, OpenApiResponseMetadata.Create("Collection retrieved successfully"))
                    .WithResponse(404, OpenApiResponseMetadata.NotFound()));

            _Webserver.Routes.PreAuthentication.Parameter.Add(
                HttpMethod.HEAD, "/v1.0/collections/{collectionId}", HeadCollectionRoute, ExceptionRoute,
                openApiMetadata: OpenApiRouteMetadata.Create("Check if collection exists", "Collections")
                    .WithDescription("Checks if a collection exists without returning the full collection data")
                    .WithParameter(OpenApiParameterMetadata.Path("collectionId", "The unique identifier of the collection", OpenApiSchemaMetadata.String()))
                    .WithResponse(200, OpenApiResponseMetadata.Create("Collection exists"))
                    .WithResponse(404, OpenApiResponseMetadata.NotFound()));

            _Webserver.Routes.PreAuthentication.Parameter.Add(
                HttpMethod.DELETE, "/v1.0/collections/{collectionId}", DeleteCollectionRoute, ExceptionRoute,
                openApiMetadata: OpenApiRouteMetadata.Create("Delete a collection", "Collections")
                    .WithDescription("Deletes a collection and all its documents")
                    .WithParameter(OpenApiParameterMetadata.Path("collectionId", "The unique identifier of the collection", OpenApiSchemaMetadata.String()))
                    .WithResponse(200, OpenApiResponseMetadata.Create("Collection deleted successfully"))
                    .WithResponse(404, OpenApiResponseMetadata.NotFound()));

            // Collection constraints and indexing routes
            _Webserver.Routes.PreAuthentication.Parameter.Add(
                HttpMethod.GET, "/v1.0/collections/{collectionId}/constraints", GetConstraintsRoute, ExceptionRoute,
                openApiMetadata: OpenApiRouteMetadata.Create("Get collection constraints", "Collections")
                    .WithDescription("Retrieves the schema enforcement mode and field constraints for a collection")
                    .WithParameter(OpenApiParameterMetadata.Path("collectionId", "The unique identifier of the collection", OpenApiSchemaMetadata.String()))
                    .WithResponse(200, OpenApiResponseMetadata.Create("Constraints retrieved successfully"))
                    .WithResponse(404, OpenApiResponseMetadata.NotFound()));

            _Webserver.Routes.PreAuthentication.Parameter.Add(
                HttpMethod.PUT, "/v1.0/collections/{collectionId}/constraints", PutConstraintsRoute, ExceptionRoute,
                openApiMetadata: OpenApiRouteMetadata.Create("Update collection constraints", "Collections")
                    .WithDescription("Updates the schema enforcement mode and field constraints for a collection")
                    .WithParameter(OpenApiParameterMetadata.Path("collectionId", "The unique identifier of the collection", OpenApiSchemaMetadata.String()))
                    .WithRequestBody(OpenApiRequestBodyMetadata.Json(
                        new OpenApiSchemaMetadata
                        {
                            Type = "object",
                            Properties = new Dictionary<string, OpenApiSchemaMetadata>
                            {
                                ["schemaEnforcementMode"] = new OpenApiSchemaMetadata { Type = "string", Enum = new List<object> { "none", "strict", "warn" }, Description = "Schema validation mode" },
                                ["fieldConstraints"] = new OpenApiSchemaMetadata { Type = "array", Description = "Field-level validation rules" }
                            }
                        }, "Constraints update request", true))
                    .WithResponse(200, OpenApiResponseMetadata.Create("Constraints updated successfully"))
                    .WithResponse(404, OpenApiResponseMetadata.NotFound()));

            _Webserver.Routes.PreAuthentication.Parameter.Add(
                HttpMethod.GET, "/v1.0/collections/{collectionId}/indexing", GetIndexingRoute, ExceptionRoute,
                openApiMetadata: OpenApiRouteMetadata.Create("Get indexing configuration", "Collections")
                    .WithDescription("Retrieves the indexing mode and indexed fields for a collection")
                    .WithParameter(OpenApiParameterMetadata.Path("collectionId", "The unique identifier of the collection", OpenApiSchemaMetadata.String()))
                    .WithResponse(200, OpenApiResponseMetadata.Create("Indexing configuration retrieved successfully"))
                    .WithResponse(404, OpenApiResponseMetadata.NotFound()));

            _Webserver.Routes.PreAuthentication.Parameter.Add(
                HttpMethod.PUT, "/v1.0/collections/{collectionId}/indexing", PutIndexingRoute, ExceptionRoute,
                openApiMetadata: OpenApiRouteMetadata.Create("Update indexing configuration", "Collections")
                    .WithDescription("Updates the indexing mode and indexed fields for a collection")
                    .WithParameter(OpenApiParameterMetadata.Path("collectionId", "The unique identifier of the collection", OpenApiSchemaMetadata.String()))
                    .WithRequestBody(OpenApiRequestBodyMetadata.Json(
                        new OpenApiSchemaMetadata
                        {
                            Type = "object",
                            Properties = new Dictionary<string, OpenApiSchemaMetadata>
                            {
                                ["indexingMode"] = new OpenApiSchemaMetadata { Type = "string", Enum = new List<object> { "all", "selective" }, Description = "Indexing mode" },
                                ["indexedFields"] = new OpenApiSchemaMetadata { Type = "array", Items = OpenApiSchemaMetadata.String(), Description = "Fields to index" },
                                ["rebuildIndexes"] = new OpenApiSchemaMetadata { Type = "boolean", Description = "Whether to rebuild indexes immediately" }
                            }
                        }, "Indexing update request", true))
                    .WithResponse(200, OpenApiResponseMetadata.Create("Indexing configuration updated successfully"))
                    .WithResponse(404, OpenApiResponseMetadata.NotFound()));

            _Webserver.Routes.PreAuthentication.Parameter.Add(
                HttpMethod.POST, "/v1.0/collections/{collectionId}/indexes/rebuild", PostRebuildIndexesRoute, ExceptionRoute,
                openApiMetadata: OpenApiRouteMetadata.Create("Rebuild collection indexes", "Collections")
                    .WithDescription("Triggers a rebuild of all indexes for a collection")
                    .WithParameter(OpenApiParameterMetadata.Path("collectionId", "The unique identifier of the collection", OpenApiSchemaMetadata.String()))
                    .WithRequestBody(OpenApiRequestBodyMetadata.Json(
                        new OpenApiSchemaMetadata
                        {
                            Type = "object",
                            Properties = new Dictionary<string, OpenApiSchemaMetadata>
                            {
                                ["dropUnusedIndexes"] = new OpenApiSchemaMetadata { Type = "boolean", Description = "Whether to drop indexes that are no longer needed" }
                            }
                        }, "Rebuild options", false))
                    .WithResponse(200, OpenApiResponseMetadata.Create("Index rebuild completed"))
                    .WithResponse(404, OpenApiResponseMetadata.NotFound()));

            // Document routes
            _Webserver.Routes.PreAuthentication.Parameter.Add(
                HttpMethod.GET, "/v1.0/collections/{collectionId}/documents", GetDocumentsRoute, ExceptionRoute,
                openApiMetadata: OpenApiRouteMetadata.Create("List documents in collection", "Documents")
                    .WithDescription("Retrieves all documents in a collection")
                    .WithParameter(OpenApiParameterMetadata.Path("collectionId", "The unique identifier of the collection", OpenApiSchemaMetadata.String()))
                    .WithResponse(200, OpenApiResponseMetadata.Create("List of documents retrieved successfully"))
                    .WithResponse(404, OpenApiResponseMetadata.NotFound()));

            _Webserver.Routes.PreAuthentication.Parameter.Add(
                HttpMethod.PUT, "/v1.0/collections/{collectionId}/documents", PutDocumentRoute, ExceptionRoute,
                openApiMetadata: OpenApiRouteMetadata.Create("Create a document", "Documents")
                    .WithDescription("Creates a new document in the specified collection")
                    .WithParameter(OpenApiParameterMetadata.Path("collectionId", "The unique identifier of the collection", OpenApiSchemaMetadata.String()))
                    .WithRequestBody(OpenApiRequestBodyMetadata.Json(
                        new OpenApiSchemaMetadata
                        {
                            Type = "object",
                            Required = new List<string> { "content" },
                            Properties = new Dictionary<string, OpenApiSchemaMetadata>
                            {
                                ["content"] = new OpenApiSchemaMetadata { Type = "object", Description = "The JSON document content" },
                                ["name"] = new OpenApiSchemaMetadata { Type = "string", Description = "Optional document name" },
                                ["labels"] = new OpenApiSchemaMetadata { Type = "array", Items = OpenApiSchemaMetadata.String(), Description = "Labels for categorization" },
                                ["tags"] = new OpenApiSchemaMetadata { Type = "object", Description = "Key-value metadata tags" }
                            }
                        }, "Document creation request", true))
                    .WithResponse(201, OpenApiResponseMetadata.Create("Document created successfully"))
                    .WithResponse(400, OpenApiResponseMetadata.BadRequest())
                    .WithResponse(404, OpenApiResponseMetadata.NotFound()));

            _Webserver.Routes.PreAuthentication.Parameter.Add(
                HttpMethod.PUT, "/v1.0/collections/{collectionId}/documents/batch", PutDocumentBatchRoute, ExceptionRoute,
                openApiMetadata: OpenApiRouteMetadata.Create("Batch ingest documents", "Documents")
                    .WithDescription("Ingests multiple documents into a collection in a single batch operation")
                    .WithParameter(OpenApiParameterMetadata.Path("collectionId", "The unique identifier of the collection", OpenApiSchemaMetadata.String()))
                    .WithRequestBody(OpenApiRequestBodyMetadata.Json(
                        new OpenApiSchemaMetadata
                        {
                            Type = "object",
                            Required = new List<string> { "documents" },
                            Properties = new Dictionary<string, OpenApiSchemaMetadata>
                            {
                                ["documents"] = new OpenApiSchemaMetadata
                                {
                                    Type = "array",
                                    Description = "Array of documents to ingest",
                                    Items = new OpenApiSchemaMetadata
                                    {
                                        Type = "object",
                                        Required = new List<string> { "content" },
                                        Properties = new Dictionary<string, OpenApiSchemaMetadata>
                                        {
                                            ["content"] = new OpenApiSchemaMetadata { Type = "object", Description = "The JSON document content" },
                                            ["name"] = new OpenApiSchemaMetadata { Type = "string", Description = "Optional document name" },
                                            ["labels"] = new OpenApiSchemaMetadata { Type = "array", Items = OpenApiSchemaMetadata.String(), Description = "Labels for categorization" },
                                            ["tags"] = new OpenApiSchemaMetadata { Type = "object", Description = "Key-value metadata tags" }
                                        }
                                    }
                                }
                            }
                        }, "Batch document ingestion request", true))
                    .WithResponse(201, OpenApiResponseMetadata.Create("Documents created successfully"))
                    .WithResponse(400, OpenApiResponseMetadata.BadRequest())
                    .WithResponse(404, OpenApiResponseMetadata.NotFound()));

            _Webserver.Routes.PreAuthentication.Parameter.Add(
                HttpMethod.GET, "/v1.0/collections/{collectionId}/documents/{documentId}", GetDocumentRoute, ExceptionRoute,
                openApiMetadata: OpenApiRouteMetadata.Create("Get a document", "Documents")
                    .WithDescription("Retrieves a specific document by its ID. Use includeContent=true to get raw JSON content.")
                    .WithParameter(OpenApiParameterMetadata.Path("collectionId", "The unique identifier of the collection", OpenApiSchemaMetadata.String()))
                    .WithParameter(OpenApiParameterMetadata.Path("documentId", "The unique identifier of the document", OpenApiSchemaMetadata.String()))
                    .WithParameter(OpenApiParameterMetadata.Query("includeContent", "If true, returns raw JSON content instead of document metadata", false, OpenApiSchemaMetadata.Boolean()))
                    .WithResponse(200, OpenApiResponseMetadata.Create("Document retrieved successfully"))
                    .WithResponse(404, OpenApiResponseMetadata.NotFound()));

            _Webserver.Routes.PreAuthentication.Parameter.Add(
                HttpMethod.HEAD, "/v1.0/collections/{collectionId}/documents/{documentId}", HeadDocumentRoute, ExceptionRoute,
                openApiMetadata: OpenApiRouteMetadata.Create("Check if document exists", "Documents")
                    .WithDescription("Checks if a document exists without returning the document data")
                    .WithParameter(OpenApiParameterMetadata.Path("collectionId", "The unique identifier of the collection", OpenApiSchemaMetadata.String()))
                    .WithParameter(OpenApiParameterMetadata.Path("documentId", "The unique identifier of the document", OpenApiSchemaMetadata.String()))
                    .WithResponse(200, OpenApiResponseMetadata.Create("Document exists"))
                    .WithResponse(404, OpenApiResponseMetadata.NotFound()));

            _Webserver.Routes.PreAuthentication.Parameter.Add(
                HttpMethod.DELETE, "/v1.0/collections/{collectionId}/documents/{documentId}", DeleteDocumentRoute, ExceptionRoute,
                openApiMetadata: OpenApiRouteMetadata.Create("Delete a document", "Documents")
                    .WithDescription("Deletes a document from the collection")
                    .WithParameter(OpenApiParameterMetadata.Path("collectionId", "The unique identifier of the collection", OpenApiSchemaMetadata.String()))
                    .WithParameter(OpenApiParameterMetadata.Path("documentId", "The unique identifier of the document", OpenApiSchemaMetadata.String()))
                    .WithResponse(200, OpenApiResponseMetadata.Create("Document deleted successfully"))
                    .WithResponse(404, OpenApiResponseMetadata.NotFound()));

            // Search route
            _Webserver.Routes.PreAuthentication.Parameter.Add(
                HttpMethod.POST, "/v1.0/collections/{collectionId}/documents/search", PostSearchRoute, ExceptionRoute,
                openApiMetadata: OpenApiRouteMetadata.Create("Search documents", "Search")
                    .WithDescription("Searches for documents in a collection using SQL expressions or structured filters")
                    .WithParameter(OpenApiParameterMetadata.Path("collectionId", "The unique identifier of the collection", OpenApiSchemaMetadata.String()))
                    .WithRequestBody(OpenApiRequestBodyMetadata.Json(
                        new OpenApiSchemaMetadata
                        {
                            Type = "object",
                            Properties = new Dictionary<string, OpenApiSchemaMetadata>
                            {
                                ["sqlExpression"] = new OpenApiSchemaMetadata { Type = "string", Description = "SQL-like query expression" },
                                ["filters"] = new OpenApiSchemaMetadata
                                {
                                    Type = "array",
                                    Description = "List of field filters",
                                    Items = new OpenApiSchemaMetadata
                                    {
                                        Type = "object",
                                        Properties = new Dictionary<string, OpenApiSchemaMetadata>
                                        {
                                            ["field"] = OpenApiSchemaMetadata.String(),
                                            ["condition"] = new OpenApiSchemaMetadata { Type = "string", Enum = new List<object> { "equals", "notEquals", "contains", "startsWith", "endsWith", "greaterThan", "lessThan" } },
                                            ["value"] = OpenApiSchemaMetadata.String()
                                        }
                                    }
                                },
                                ["labels"] = new OpenApiSchemaMetadata { Type = "array", Items = OpenApiSchemaMetadata.String(), Description = "Filter by labels" },
                                ["tags"] = new OpenApiSchemaMetadata { Type = "object", Description = "Filter by tags" },
                                ["maxResults"] = new OpenApiSchemaMetadata { Type = "integer", Description = "Maximum number of results to return" },
                                ["skip"] = new OpenApiSchemaMetadata { Type = "integer", Description = "Number of results to skip" },
                                ["ordering"] = new OpenApiSchemaMetadata { Type = "string", Enum = new List<object> { "createdAscending", "createdDescending", "updatedAscending", "updatedDescending" }, Description = "Sort order" },
                                ["includeContent"] = new OpenApiSchemaMetadata { Type = "boolean", Description = "Whether to include document content in results" }
                            }
                        }, "Search request", true))
                    .WithResponse(200, OpenApiResponseMetadata.Create("Search results"))
                    .WithResponse(400, OpenApiResponseMetadata.BadRequest())
                    .WithResponse(404, OpenApiResponseMetadata.NotFound()));

            // Schema routes
            _Webserver.Routes.PreAuthentication.Static.Add(
                HttpMethod.GET, "/v1.0/schemas", GetSchemasRoute, ExceptionRoute,
                openApiMetadata: OpenApiRouteMetadata.Create("List all schemas", "Schemas")
                    .WithDescription("Retrieves a list of all discovered schemas from ingested documents")
                    .WithResponse(200, OpenApiResponseMetadata.Create("List of schemas retrieved successfully")));

            _Webserver.Routes.PreAuthentication.Parameter.Add(
                HttpMethod.GET, "/v1.0/schemas/{schemaId}", GetSchemaRoute, ExceptionRoute,
                openApiMetadata: OpenApiRouteMetadata.Create("Get a schema", "Schemas")
                    .WithDescription("Retrieves a specific schema by its ID")
                    .WithParameter(OpenApiParameterMetadata.Path("schemaId", "The unique identifier of the schema", OpenApiSchemaMetadata.String()))
                    .WithResponse(200, OpenApiResponseMetadata.Create("Schema retrieved successfully"))
                    .WithResponse(404, OpenApiResponseMetadata.NotFound()));

            _Webserver.Routes.PreAuthentication.Parameter.Add(
                HttpMethod.GET, "/v1.0/schemas/{schemaId}/elements", GetSchemaElementsRoute, ExceptionRoute,
                openApiMetadata: OpenApiRouteMetadata.Create("Get schema elements", "Schemas")
                    .WithDescription("Retrieves the elements (fields) defined in a schema")
                    .WithParameter(OpenApiParameterMetadata.Path("schemaId", "The unique identifier of the schema", OpenApiSchemaMetadata.String()))
                    .WithResponse(200, OpenApiResponseMetadata.Create("Schema elements retrieved successfully"))
                    .WithResponse(404, OpenApiResponseMetadata.NotFound()));

            // Index table routes
            _Webserver.Routes.PreAuthentication.Static.Add(
                HttpMethod.GET, "/v1.0/tables", GetIndexTablesRoute, ExceptionRoute,
                openApiMetadata: OpenApiRouteMetadata.Create("List index tables", "Index Tables")
                    .WithDescription("Retrieves a list of all index table mappings")
                    .WithResponse(200, OpenApiResponseMetadata.Create("List of index tables retrieved successfully")));

            _Webserver.Routes.PreAuthentication.Parameter.Add(
                HttpMethod.GET, "/v1.0/tables/{tableName}/entries", GetTableEntriesRoute, ExceptionRoute,
                openApiMetadata: OpenApiRouteMetadata.Create("Get index table entries", "Index Tables")
                    .WithDescription("Retrieves entries from a specific index table with pagination")
                    .WithParameter(OpenApiParameterMetadata.Path("tableName", "The name of the index table", OpenApiSchemaMetadata.String()))
                    .WithParameter(OpenApiParameterMetadata.Query("skip", "Number of entries to skip", false, OpenApiSchemaMetadata.Integer()))
                    .WithParameter(OpenApiParameterMetadata.Query("limit", "Maximum number of entries to return (1-1000)", false, OpenApiSchemaMetadata.Integer()))
                    .WithResponse(200, OpenApiResponseMetadata.Create("Index table entries retrieved successfully"))
                    .WithResponse(404, OpenApiResponseMetadata.NotFound()));
        }

        // Watson invokes this exclusively for OPTIONS (CORS preflight) requests.
        private async Task PreflightRoute(HttpContextBase ctx)
        {
            ApplyCorsHeaders(ctx.Response, ctx.Request);
            ctx.Response.StatusCode = 204;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.Send().ConfigureAwait(false);
        }

        private async Task PreRoutingRoute(HttpContextBase ctx)
        {
            // Stamp CORS headers on every (non-OPTIONS) response before it is handled.
            ApplyCorsHeaders(ctx.Response, ctx.Request);

            // Increment in-flight request gauge. Decremented symmetrically in PostRoutingRoute.
            try
            {
                string method = ctx.Request.Method.ToString();
                if (!String.Equals(method, "OPTIONS", StringComparison.OrdinalIgnoreCase))
                {
                    ServerTelemetry.RequestStarted(method);
                }
            }
            catch
            {
                // Telemetry must never break request handling.
            }

            await Task.CompletedTask.ConfigureAwait(false);
        }

        private async Task PostRoutingRoute(HttpContextBase ctx)
        {
            ctx.Request.Timestamp.End = DateTime.UtcNow;

            // Record HTTP server metrics for every request (health, swagger, 404s, and functional API).
            try
            {
                string method = ctx.Request.Method.ToString();
                if (!String.Equals(method, "OPTIONS", StringComparison.OrdinalIgnoreCase))
                {
                    string rawPath = ctx.Request.Url.RawWithQuery ?? "/";
                    int queryIndex = rawPath.IndexOf('?');
                    string path = queryIndex >= 0 ? rawPath.Substring(0, queryIndex) : rawPath;

                    string requestType = ServerTelemetry.ClassifyRequestType(method, path);
                    double seconds = (ctx.Request.Timestamp.TotalMs ?? 0.0) / 1000.0;
                    long requestBytes = ctx.Request.ContentLength;
                    long responseBytes = GetResponseContentLength(ctx);

                    ServerTelemetry.RequestCompleted(
                        method,
                        ctx.Response.StatusCode,
                        requestType,
                        seconds,
                        requestBytes,
                        responseBytes);
                }
            }
            catch
            {
                // Telemetry must never break request handling.
            }

            _Logging?.Debug(
                _Header
                + ctx.Request.Method + " " + ctx.Request.Url.RawWithQuery + " "
                + ctx.Response.StatusCode + " "
                + "(" + ctx.Request.Timestamp.TotalMs?.ToString("F2") + "ms)");
        }

        private static long GetResponseContentLength(HttpContextBase ctx)
        {
            try
            {
                return ctx.Response.ContentLength;
            }
            catch
            {
                return -1;
            }
        }

        private async Task DefaultRoute(HttpContextBase ctx)
        {
            ResponseContext response = new ResponseContext(false, 404, "Not found");
            await SendResponse(ctx, response).ConfigureAwait(false);
        }

        private async Task ExceptionRoute(HttpContextBase ctx, Exception e)
        {
            if (e is JsonException)
            {
                _Logging?.Warn(_Header + "JSON parsing error: " + e.Message);
                ResponseContext jsonResponse = new ResponseContext(false, 400, "Invalid JSON: " + e.Message);
                await SendResponse(ctx, jsonResponse).ConfigureAwait(false);
                return;
            }

            if (e is SchemaValidationException sve)
            {
                _Logging?.Warn(_Header + "Schema validation error: " + sve.Message);
                ResponseContext validationResponse = new ResponseContext(false, 400, "Schema validation failed")
                {
                    Data = new { Errors = sve.Errors }
                };
                await SendResponse(ctx, validationResponse).ConfigureAwait(false);
                return;
            }

            if (e is ArgumentException)
            {
                _Logging?.Warn(_Header + "Argument error: " + e.Message);
                ResponseContext argResponse = new ResponseContext(false, 400, e.Message);
                await SendResponse(ctx, argResponse).ConfigureAwait(false);
                return;
            }

            _Logging?.Error(_Header + "Exception: " + e.Message);
            ResponseContext response = new ResponseContext(false, 500, e.Message);
            await SendResponse(ctx, response).ConfigureAwait(false);
        }

        private async Task WrappedRequestHandler(HttpContextBase ctx, RequestTypeEnum requestType, Func<RequestContext, Task<ResponseContext>> handler)
        {
            RequestContext requestContext = await BuildRequestContext(ctx, requestType).ConfigureAwait(false);
            DateTime startTime = requestContext.CreatedUtc;
            ResponseContext responseContext;

            // Authentication and authorization gate.
            if (_AuthEnabled)
            {
                RequiredPermission required = RequestPermissionMap.Resolve(requestContext.Method, requestContext.Path);
                CallerContext caller = await AuthenticateAsync(ctx).ConfigureAwait(false);

                if (caller != null && caller.IsAuthenticated)
                {
                    requestContext.Caller = caller;
                    requestContext.TenantId = caller.TenantId;
                }

                if (!required.Public)
                {
                    if (caller == null || !caller.IsAuthenticated)
                    {
                        DateTime unauthUtc = DateTime.UtcNow;
                        ResponseContext unauth = new ResponseContext(false, 401, "Authentication required") { Guid = requestContext.Guid };
                        string unauthBody = await SendResponse(ctx, unauth).ConfigureAwait(false);
                        await RecordRequestHistoryAsync(requestContext, unauth, unauthBody, unauthUtc).ConfigureAwait(false);
                        return;
                    }

                    if (!required.AnyAuthenticated)
                    {
                        string resourceId = ResolveResourceId(requestContext, required.ResourceType);
                        AuthorizationVerdict verdict = await _AuthZ.AuthorizeAsync(caller, required.ResourceType, required.Operation, resourceId).ConfigureAwait(false);
                        if (verdict != AuthorizationVerdict.Permitted)
                        {
                            DateTime deniedUtc = DateTime.UtcNow;
                            ResponseContext forbidden = new ResponseContext(false, 403,
                                "Authorization denied: requires " + required.ResourceType + ":" + required.Operation) { Guid = requestContext.Guid };
                            string forbiddenBody = await SendResponse(ctx, forbidden).ConfigureAwait(false);
                            await RecordRequestHistoryAsync(requestContext, forbidden, forbiddenBody, deniedUtc).ConfigureAwait(false);
                            return;
                        }
                    }
                }
            }

            // Start an HTTP server span. Core-engine spans started during handler execution nest under
            // it (same async context). Disposed at method end via the using declaration.
            using Activity activity = ServerTelemetry.StartServerSpan(
                requestContext.Method,
                requestContext.Path,
                ServerTelemetry.ClassifyRequestType(requestContext.Method, requestContext.Path),
                requestContext.CollectionId,
                requestContext.DocumentId);

            try
            {
                responseContext = await handler(requestContext).ConfigureAwait(false);
            }
            catch (JsonException e)
            {
                _Logging?.Warn(_Header + "JSON parsing error in " + requestType + ": " + e.Message);
                responseContext = new ResponseContext(false, 400, "Invalid JSON: " + e.Message);
            }
            catch (SchemaValidationException sve)
            {
                _Logging?.Warn(_Header + "Schema validation error in " + requestType + ": " + sve.Message);
                responseContext = new ResponseContext(false, 400, "Schema validation failed")
                {
                    Data = new { Errors = sve.Errors }
                };
            }
            catch (DocumentLockedException dle)
            {
                _Logging?.Warn(_Header + "Document locked in " + requestType + ": " + dle.Message);
                responseContext = new ResponseContext(false, 409, "Document is locked")
                {
                    Data = new
                    {
                        CollectionId = dle.CollectionId,
                        DocumentName = dle.DocumentName,
                        LockedByHostname = dle.LockedByHostname,
                        LockCreatedUtc = dle.LockCreatedUtc
                    }
                };
            }
            catch (ArgumentException e)
            {
                _Logging?.Warn(_Header + "Argument error in " + requestType + ": " + e.Message);
                responseContext = new ResponseContext(false, 400, e.Message);
            }
            catch (Exception e)
            {
                _Logging?.Error(_Header + "Exception in " + requestType + ": " + e.Message);
                responseContext = new ResponseContext(false, 500, e.Message);
            }

            DateTime completedUtc = DateTime.UtcNow;
            responseContext.Guid = requestContext.Guid;
            responseContext.ProcessingTimeMs = completedUtc.Subtract(startTime).TotalMilliseconds;

            if (activity != null)
            {
                activity.SetTag("http.response.status_code", responseContext.StatusCode);
                activity.SetTag("lattice.request_id", requestContext.Guid);
                if (responseContext.Success && responseContext.StatusCode < 500)
                    activity.SetStatus(ActivityStatusCode.Ok);
                else
                    activity.SetStatus(ActivityStatusCode.Error);
            }

            string responseBody = await SendResponse(ctx, responseContext).ConfigureAwait(false);
            await RecordRequestHistoryAsync(requestContext, responseContext, responseBody, completedUtc).ConfigureAwait(false);
        }

        private async Task<RequestContext> BuildRequestContext(HttpContextBase ctx, RequestTypeEnum requestType)
        {
            string rawPath = ctx.Request.Url.RawWithQuery ?? "/";
            int queryIndex = rawPath.IndexOf('?');
            string path = queryIndex >= 0 ? rawPath.Substring(0, queryIndex) : rawPath;

            RequestContext requestContext = new RequestContext
            {
                CreatedUtc = DateTime.UtcNow,
                RequestType = requestType,
                Method = ctx.Request.Method.ToString(),
                Url = ctx.Request.Url.Full,
                Path = String.IsNullOrWhiteSpace(path) ? "/" : path,
                IpAddress = ctx.Request.Source.IpAddress,
                QueryParams = CloneNameValueCollection(ctx.Request.Query?.Elements),
                Headers = ToDictionary(ctx.Request.Headers),
                RequestBody = await GetRequestBody(ctx).ConfigureAwait(false),
                CollectionId = ctx.Request.Url.Parameters["collectionId"],
                DocumentId = ctx.Request.Url.Parameters["documentId"],
                SchemaId = ctx.Request.Url.Parameters["schemaId"],
                TableName = ctx.Request.Url.Parameters["tableName"]
            };

            return requestContext;
        }

        private async Task<string> GetRequestBody(HttpContextBase ctx)
        {
            if (ctx.Request.Data != null && ctx.Request.ContentLength > 0)
            {
                using (StreamReader reader = new StreamReader(ctx.Request.Data, Encoding.UTF8))
                {
                    return await reader.ReadToEndAsync().ConfigureAwait(false);
                }
            }

            return string.Empty;
        }

        private async Task<string> SendResponse(HttpContextBase ctx, ResponseContext response)
        {
            ctx.Response.StatusCode = response.StatusCode;
            ctx.Response.ContentType = "application/json";
            EnsureStandardResponseHeaders(response.Headers, response.Guid, "application/json");

            if (response.ProcessingTimeMs > 0)
            {
                response.Headers["X-Lattice-Processing-Time-Ms"] =
                    response.ProcessingTimeMs.ToString("F4", CultureInfo.InvariantCulture);
            }

            // The response body is the payload directly (no envelope). Errors return { error, detail? }.
            string json;
            if (response.Success)
            {
                json = response.Data != null ? JsonSerializer.Serialize(response.Data, _JsonOptions) : String.Empty;
            }
            else
            {
                ErrorResponse error = new ErrorResponse
                {
                    Error = String.IsNullOrEmpty(response.ErrorMessage) ? "An error occurred." : response.ErrorMessage,
                    Detail = response.Data
                };
                json = JsonSerializer.Serialize(error, _JsonOptions);
            }

            ApplyHeaders(ctx, response.Headers);
            await ctx.Response.Send(json).ConfigureAwait(false);
            return json;
        }

        private async Task SendRawResponse(HttpContextBase ctx, RequestContext requestContext, int statusCode, string responseBody, DateTime startTime, bool success, string? errorMessage = null, string contentType = "application/json")
        {
            DateTime completedUtc = DateTime.UtcNow;
            Dictionary<string, string> responseHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            EnsureStandardResponseHeaders(responseHeaders, requestContext.Guid, contentType);

            ctx.Response.StatusCode = statusCode;
            ctx.Response.ContentType = contentType;
            ApplyHeaders(ctx, responseHeaders);
            await ctx.Response.Send(responseBody ?? String.Empty).ConfigureAwait(false);

            ResponseContext responseContext = new ResponseContext(success, statusCode, errorMessage)
            {
                Guid = requestContext.Guid,
                Headers = responseHeaders,
                ProcessingTimeMs = completedUtc.Subtract(startTime).TotalMilliseconds
            };

            await RecordRequestHistoryAsync(requestContext, responseContext, responseBody, completedUtc).ConfigureAwait(false);
        }

        private void ApplyHeaders(HttpContextBase ctx, Dictionary<string, string> headers)
        {
            foreach (KeyValuePair<string, string> header in headers)
            {
                ctx.Response.Headers[header.Key] = header.Value;
            }
        }

        private static string SerializeDocumentContent(object content)
        {
            // Content is arbitrary user JSON deserialized into a JSON value; re-serialize it verbatim.
            // Naming policies do not apply to an already-parsed JSON value, so field casing is preserved.
            return content == null ? null : JsonSerializer.Serialize(content);
        }

        private static int ParseQueryInt(RequestContext requestContext, string name, int defaultValue, int minValue, int maxValue)
        {
            string raw = requestContext?.QueryParams?[name];
            if (String.IsNullOrEmpty(raw)) return defaultValue;
            if (!Int32.TryParse(raw, out int parsed)) return defaultValue;
            if (parsed < minValue) return minValue;
            if (parsed > maxValue) return maxValue;
            return parsed;
        }

        private static EnumerationResult<T> BuildEnumerationResult<T>(List<T> allItems, RequestContext requestContext)
        {
            int maxResults = ParseQueryInt(requestContext, "maxResults", 100, 1, 1000);
            int skip = ParseQueryInt(requestContext, "skip", 0, 0, Int32.MaxValue);
            return BuildEnumerationResult(allItems, skip, maxResults);
        }

        private static EnumerationResult<T> BuildEnumerationResult<T>(List<T> allItems, int skip, int maxResults, long? totalRecords = null)
        {
            List<T> source = allItems ?? new List<T>();
            long total = totalRecords ?? source.Count;

            // When a caller supplies totalRecords, source is already the requested page; otherwise page here.
            List<T> page = totalRecords.HasValue ? source : source.Skip(skip).Take(maxResults).ToList();

            EnumerationResult<T> result = new EnumerationResult<T>();
            result.Success = true;
            result.MaxResults = maxResults;
            result.Skip = skip;
            result.TotalRecords = total;
            result.Objects = page;

            long remaining = total - skip - page.Count;
            result.RecordsRemaining = remaining < 0 ? 0 : remaining;
            result.EndOfResults = result.RecordsRemaining == 0;
            result.Timestamp.End = DateTime.UtcNow;
            return result;
        }

        private static void EnsureStandardResponseHeaders(Dictionary<string, string> headers, string? requestId, string contentType)
        {
            if (headers == null) return;

            // CORS headers are stamped centrally in PreRoutingRoute / PreflightRoute via ApplyCorsHeaders.
            headers["Content-Type"] = contentType;
            headers["X-Lattice-Request-Id"] = String.IsNullOrWhiteSpace(requestId)
                ? System.Guid.NewGuid().ToString("N")
                : requestId;
        }

        // Stamp configurable CORS headers on a response, echoing/allowing the request Origin per settings.
        private void ApplyCorsHeaders(HttpResponseBase response, HttpRequestBase request)
        {
            CorsSettings cors = _Settings?.Rest?.Cors;
            if (cors == null || !cors.Enable) return;

            string origin = request.Headers?.Get("Origin");
            if (String.IsNullOrEmpty(origin)) return;

            // Watson's OpenAPI/Swagger middleware stamps its own CORS headers on these paths; skip them
            // here so responses do not carry a duplicate Access-Control-Allow-Origin (which browsers reject).
            string rawPath = request.Url?.RawWithQuery ?? String.Empty;
            if (rawPath.StartsWith("/openapi", StringComparison.OrdinalIgnoreCase)
                || rawPath.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            bool originAllowed = false;
            string allowedOriginValue = null;

            if (cors.AllowOrigins.Contains("*"))
            {
                originAllowed = true;
                allowedOriginValue = cors.AllowCredentials ? origin : "*";
            }
            else
            {
                foreach (string allowedOrigin in cors.AllowOrigins)
                {
                    if (String.Equals(allowedOrigin, origin, StringComparison.OrdinalIgnoreCase))
                    {
                        originAllowed = true;
                        allowedOriginValue = origin;
                        break;
                    }
                }
            }

            if (!originAllowed) return;

            response.Headers["Access-Control-Allow-Origin"] = allowedOriginValue;

            // When the allowed origin is echoed rather than "*", advertise that responses vary by origin.
            if (!String.Equals(allowedOriginValue, "*", StringComparison.Ordinal))
                response.Headers["Vary"] = "Origin";

            if (cors.AllowMethods != null && cors.AllowMethods.Count > 0)
                response.Headers["Access-Control-Allow-Methods"] = String.Join(", ", cors.AllowMethods);

            if (cors.AllowHeaders != null && cors.AllowHeaders.Count > 0)
                response.Headers["Access-Control-Allow-Headers"] = String.Join(", ", cors.AllowHeaders);

            if (cors.ExposeHeaders != null && cors.ExposeHeaders.Count > 0)
                response.Headers["Access-Control-Expose-Headers"] = String.Join(", ", cors.ExposeHeaders);

            if (cors.AllowCredentials)
                response.Headers["Access-Control-Allow-Credentials"] = "true";

            if (request.Method == HttpMethod.OPTIONS && cors.MaxAgeSeconds > 0)
                response.Headers["Access-Control-Max-Age"] = cors.MaxAgeSeconds.ToString();
        }

        private static NameValueCollection CloneNameValueCollection(NameValueCollection? source)
        {
            NameValueCollection ret = new NameValueCollection(StringComparer.OrdinalIgnoreCase);
            if (source == null) return ret;

            foreach (string? key in source.AllKeys)
            {
                if (String.IsNullOrWhiteSpace(key)) continue;
                ret[key] = source[key];
            }

            return ret;
        }

        private static Dictionary<string, string> ToDictionary(NameValueCollection? source)
        {
            Dictionary<string, string> ret = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (source == null) return ret;

            foreach (string? key in source.AllKeys)
            {
                if (String.IsNullOrWhiteSpace(key)) continue;
                ret[key] = source[key] ?? String.Empty;
            }

            return ret;
        }

        private async Task RecordRequestHistoryAsync(RequestContext requestContext, ResponseContext responseContext, string? responseBody, DateTime completedUtc)
        {
            if (!_RequestHistory.Enabled) return;
            if (!ShouldRecordRequestHistory(requestContext.Path)) return;

            RequestHistoryDetail detail = new RequestHistoryDetail
            {
                Id = requestContext.Guid,
                CreatedUtc = requestContext.CreatedUtc,
                CompletedUtc = completedUtc,
                RequestType = JsonNamingPolicy.CamelCase.ConvertName(requestContext.RequestType.ToString()),
                Method = requestContext.Method ?? "GET",
                Path = requestContext.Path ?? "/",
                Url = requestContext.Url ?? requestContext.Path ?? "/",
                SourceIp = requestContext.IpAddress ?? "unknown",
                CollectionId = requestContext.CollectionId,
                DocumentId = requestContext.DocumentId,
                SchemaId = requestContext.SchemaId,
                TableName = requestContext.TableName,
                StatusCode = responseContext.StatusCode,
                Success = responseContext.Success,
                ProcessingTimeMs = responseContext.ProcessingTimeMs,
                RequestContentType = GetHeaderValue(requestContext.Headers, "Content-Type"),
                ResponseContentType = GetHeaderValue(responseContext.Headers, "Content-Type"),
                RequestHeaders = new Dictionary<string, string>(requestContext.Headers, StringComparer.OrdinalIgnoreCase),
                RequestBody = requestContext.RequestBody,
                ResponseHeaders = new Dictionary<string, string>(responseContext.Headers, StringComparer.OrdinalIgnoreCase),
                ResponseBody = responseBody
            };

            await _RequestHistory.RecordAsync(detail, CancellationToken.None).ConfigureAwait(false);
        }

        private static string? GetHeaderValue(Dictionary<string, string>? headers, string name)
        {
            if (headers == null || String.IsNullOrWhiteSpace(name)) return null;
            return headers.TryGetValue(name, out string? value) ? value : null;
        }

        private static bool ShouldRecordRequestHistory(string? path)
        {
            if (String.IsNullOrWhiteSpace(path)) return false;

            string normalized = path.Trim().ToLowerInvariant();
            if (normalized == "/") return false;
            if (normalized == "/v1.0/health") return false;
            if (normalized.StartsWith("/v1.0/requesthistory")) return false;
            if (normalized.StartsWith("/openapi")) return false;
            if (normalized.StartsWith("/swagger")) return false;
            if (normalized == "/favicon.ico") return false;

            return true;
        }

        private RequestHistorySearchFilter BuildRequestHistoryFilterFromQuery(HttpContextBase ctx, bool includePaging)
        {
            RequestHistorySearchFilter filter = new RequestHistorySearchFilter();

            filter.RequestType = GetQueryValue(ctx, "requestType");
            filter.Method = GetQueryValue(ctx, "method");
            filter.PathContains = GetQueryValue(ctx, "pathContains");
            filter.CollectionId = GetQueryValue(ctx, "collectionId");
            filter.DocumentId = GetQueryValue(ctx, "documentId");
            filter.SchemaId = GetQueryValue(ctx, "schemaId");
            filter.TableName = GetQueryValue(ctx, "tableName");
            filter.SourceIp = GetQueryValue(ctx, "sourceIp");

            if (TryGetIntQueryValue(ctx, "statusCode", out int statusCode))
            {
                filter.StatusCode = statusCode;
            }

            if (TryGetBoolQueryValue(ctx, "success", out bool success))
            {
                filter.Success = success;
            }

            if (TryGetDateTimeQueryValue(ctx, "startUtc", out DateTime startUtc))
            {
                filter.StartUtc = startUtc;
            }

            if (TryGetDateTimeQueryValue(ctx, "endUtc", out DateTime endUtc))
            {
                filter.EndUtc = endUtc;
            }

            if (includePaging)
            {
                if (TryGetIntQueryValue(ctx, "page", out int page))
                {
                    filter.Page = page;
                }

                if (TryGetIntQueryValue(ctx, "pageSize", out int pageSize))
                {
                    filter.PageSize = pageSize;
                }
            }

            return filter;
        }

        private static string? GetQueryValue(HttpContextBase ctx, string name)
        {
            string? value = ctx.Request.Query.Elements[name];
            return String.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static bool TryGetIntQueryValue(HttpContextBase ctx, string name, out int value)
        {
            return Int32.TryParse(GetQueryValue(ctx, name), out value);
        }

        private static bool TryGetBoolQueryValue(HttpContextBase ctx, string name, out bool value)
        {
            return Boolean.TryParse(GetQueryValue(ctx, name), out value);
        }

        private static bool TryGetDateTimeQueryValue(HttpContextBase ctx, string name, out DateTime value)
        {
            return DateTime.TryParse(
                GetQueryValue(ctx, name),
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out value);
        }

        private static bool TryGetEnumQueryValue<T>(HttpContextBase ctx, string name, out T value) where T : struct
        {
            return Enum.TryParse(GetQueryValue(ctx, name), true, out value);
        }

        #endregion

        #region Private-Methods-Health

        private async Task GetHealthRoute(HttpContextBase ctx)
        {
            await WrappedRequestHandler(ctx, RequestTypeEnum.HealthCheck, (reqCtx) =>
            {
                return Task.FromResult(new ResponseContext
                {
                    Success = true,
                    StatusCode = 200,
                    Data = new
                    {
                        Status = "Healthy",
                        Version = "1.0.0",
                        Timestamp = DateTime.UtcNow
                    }
                });
            }).ConfigureAwait(false);
        }

        #endregion

        #region Private-Methods-Collections

        private async Task GetCollectionsRoute(HttpContextBase ctx)
        {
            await WrappedRequestHandler(ctx, RequestTypeEnum.Collection, async (reqCtx) =>
            {
                List<Collection> collections = await _Client.Collection.ReadAll(CancellationToken.None).ConfigureAwait(false);
                return new ResponseContext
                {
                    Success = true,
                    StatusCode = 200,
                    Data = BuildEnumerationResult(collections, reqCtx)
                };
            }).ConfigureAwait(false);
        }

        private async Task PutCollectionRoute(HttpContextBase ctx)
        {
            await WrappedRequestHandler(ctx, RequestTypeEnum.Collection, async (reqCtx) =>
            {
                string body = reqCtx.RequestBody ?? String.Empty;
                if (String.IsNullOrEmpty(body))
                {
                    return new ResponseContext(false, 400, "Request body is required");
                }

                CreateCollectionRequest? request = JsonSerializer.Deserialize<CreateCollectionRequest>(body, _JsonOptions);
                if (request == null)
                {
                    return new ResponseContext(false, 400, "Invalid JSON in request body");
                }

                if (!request.Validate(out string errorMessage))
                {
                    return new ResponseContext(false, 400, errorMessage);
                }

                Collection collection = await _Client.Collection.Create(
                    request.Name,
                    request.Description,
                    request.DocumentsDirectory,
                    request.Labels,
                    request.Tags,
                    request.SchemaEnforcementMode,
                    request.FieldConstraints,
                    request.IndexingMode,
                    request.IndexedFields,
                    CancellationToken.None).ConfigureAwait(false);

                return new ResponseContext
                {
                    Success = true,
                    StatusCode = 201,
                    Data = collection
                };
            }).ConfigureAwait(false);
        }

        private async Task GetCollectionRoute(HttpContextBase ctx)
        {
            await WrappedRequestHandler(ctx, RequestTypeEnum.Collection, async (reqCtx) =>
            {
                string? collectionId = ctx.Request.Url.Parameters["collectionId"];
                if (String.IsNullOrEmpty(collectionId))
                {
                    return new ResponseContext(false, 400, "Collection ID is required");
                }

                Collection? collection = await _Client.Collection.ReadById(collectionId, CancellationToken.None).ConfigureAwait(false);
                if (collection == null)
                {
                    return new ResponseContext(false, 404, "Collection not found");
                }

                return new ResponseContext
                {
                    Success = true,
                    StatusCode = 200,
                    Data = collection
                };
            }).ConfigureAwait(false);
        }

        private async Task HeadCollectionRoute(HttpContextBase ctx)
        {
            await WrappedRequestHandler(ctx, RequestTypeEnum.Collection, async (reqCtx) =>
            {
                string? collectionId = ctx.Request.Url.Parameters["collectionId"];
                if (String.IsNullOrEmpty(collectionId))
                {
                    return new ResponseContext(false, 400, "Collection ID is required");
                }

                bool exists = await _Client.Collection.Exists(collectionId, CancellationToken.None).ConfigureAwait(false);
                if (!exists)
                {
                    return new ResponseContext(false, 404, "Collection not found");
                }

                return new ResponseContext
                {
                    Success = true,
                    StatusCode = 200
                };
            }).ConfigureAwait(false);
        }

        private async Task DeleteCollectionRoute(HttpContextBase ctx)
        {
            await WrappedRequestHandler(ctx, RequestTypeEnum.Collection, async (reqCtx) =>
            {
                string? collectionId = ctx.Request.Url.Parameters["collectionId"];
                if (String.IsNullOrEmpty(collectionId))
                {
                    return new ResponseContext(false, 400, "Collection ID is required");
                }

                bool exists = await _Client.Collection.Exists(collectionId, CancellationToken.None).ConfigureAwait(false);
                if (!exists)
                {
                    return new ResponseContext(false, 404, "Collection not found");
                }

                await _Client.Collection.Delete(collectionId, CancellationToken.None).ConfigureAwait(false);

                return new ResponseContext
                {
                    Success = true,
                    StatusCode = 200,
                    Data = new { Message = "Collection deleted successfully", CollectionId = collectionId }
                };
            }).ConfigureAwait(false);
        }

        private async Task GetConstraintsRoute(HttpContextBase ctx)
        {
            await WrappedRequestHandler(ctx, RequestTypeEnum.Collection, async (reqCtx) =>
            {
                string? collectionId = ctx.Request.Url.Parameters["collectionId"];
                if (String.IsNullOrEmpty(collectionId))
                {
                    return new ResponseContext(false, 400, "Collection ID is required");
                }

                Collection? collection = await _Client.Collection.ReadById(collectionId, CancellationToken.None).ConfigureAwait(false);
                if (collection == null)
                {
                    return new ResponseContext(false, 404, "Collection not found");
                }

                List<FieldConstraint> constraints = await _Client.Collection.GetConstraints(collectionId, CancellationToken.None).ConfigureAwait(false);
                return new ResponseContext
                {
                    Success = true,
                    StatusCode = 200,
                    Data = new
                    {
                        CollectionId = collectionId,
                        SchemaEnforcementMode = collection.SchemaEnforcementMode,
                        FieldConstraints = constraints
                    }
                };
            }).ConfigureAwait(false);
        }

        private async Task PutConstraintsRoute(HttpContextBase ctx)
        {
            await WrappedRequestHandler(ctx, RequestTypeEnum.Collection, async (reqCtx) =>
            {
                string? collectionId = ctx.Request.Url.Parameters["collectionId"];
                if (String.IsNullOrEmpty(collectionId))
                {
                    return new ResponseContext(false, 400, "Collection ID is required");
                }

                bool exists = await _Client.Collection.Exists(collectionId, CancellationToken.None).ConfigureAwait(false);
                if (!exists)
                {
                    return new ResponseContext(false, 404, "Collection not found");
                }

                string body = reqCtx.RequestBody ?? String.Empty;
                if (String.IsNullOrEmpty(body))
                {
                    return new ResponseContext(false, 400, "Request body is required");
                }

                UpdateConstraintsRequest? request = JsonSerializer.Deserialize<UpdateConstraintsRequest>(body, _JsonOptions);
                if (request == null)
                {
                    return new ResponseContext(false, 400, "Invalid JSON in request body");
                }

                Collection updated = await _Client.Collection.UpdateConstraints(
                    collectionId,
                    request.SchemaEnforcementMode,
                    request.FieldConstraints,
                    CancellationToken.None).ConfigureAwait(false);

                List<FieldConstraint> constraints = await _Client.Collection.GetConstraints(collectionId, CancellationToken.None).ConfigureAwait(false);

                return new ResponseContext
                {
                    Success = true,
                    StatusCode = 200,
                    Data = new
                    {
                        CollectionId = collectionId,
                        SchemaEnforcementMode = updated.SchemaEnforcementMode,
                        FieldConstraints = constraints
                    }
                };
            }).ConfigureAwait(false);
        }

        private async Task GetIndexingRoute(HttpContextBase ctx)
        {
            await WrappedRequestHandler(ctx, RequestTypeEnum.Collection, async (reqCtx) =>
            {
                string? collectionId = ctx.Request.Url.Parameters["collectionId"];
                if (String.IsNullOrEmpty(collectionId))
                {
                    return new ResponseContext(false, 400, "Collection ID is required");
                }

                Collection? collection = await _Client.Collection.ReadById(collectionId, CancellationToken.None).ConfigureAwait(false);
                if (collection == null)
                {
                    return new ResponseContext(false, 404, "Collection not found");
                }

                List<IndexedField> indexedFields = await _Client.Collection.GetIndexedFields(collectionId, CancellationToken.None).ConfigureAwait(false);
                return new ResponseContext
                {
                    Success = true,
                    StatusCode = 200,
                    Data = new
                    {
                        CollectionId = collectionId,
                        IndexingMode = collection.IndexingMode,
                        IndexedFields = indexedFields.Select(f => f.FieldPath).ToList()
                    }
                };
            }).ConfigureAwait(false);
        }

        private async Task PutIndexingRoute(HttpContextBase ctx)
        {
            await WrappedRequestHandler(ctx, RequestTypeEnum.Collection, async (reqCtx) =>
            {
                string? collectionId = ctx.Request.Url.Parameters["collectionId"];
                if (String.IsNullOrEmpty(collectionId))
                {
                    return new ResponseContext(false, 400, "Collection ID is required");
                }

                bool exists = await _Client.Collection.Exists(collectionId, CancellationToken.None).ConfigureAwait(false);
                if (!exists)
                {
                    return new ResponseContext(false, 404, "Collection not found");
                }

                string body = reqCtx.RequestBody ?? String.Empty;
                if (String.IsNullOrEmpty(body))
                {
                    return new ResponseContext(false, 400, "Request body is required");
                }

                UpdateIndexingRequest? request = JsonSerializer.Deserialize<UpdateIndexingRequest>(body, _JsonOptions);
                if (request == null)
                {
                    return new ResponseContext(false, 400, "Invalid JSON in request body");
                }

                Collection updated = await _Client.Collection.UpdateIndexing(
                    collectionId,
                    request.IndexingMode,
                    request.IndexedFields,
                    CancellationToken.None).ConfigureAwait(false);

                // Optionally rebuild indexes
                IndexRebuildResult? rebuildResult = null;
                if (request.RebuildIndexes)
                {
                    rebuildResult = await _Client.Collection.RebuildIndexes(collectionId, true, null, CancellationToken.None).ConfigureAwait(false);
                }

                List<IndexedField> indexedFields = await _Client.Collection.GetIndexedFields(collectionId, CancellationToken.None).ConfigureAwait(false);

                return new ResponseContext
                {
                    Success = true,
                    StatusCode = 200,
                    Data = new
                    {
                        CollectionId = collectionId,
                        IndexingMode = updated.IndexingMode,
                        IndexedFields = indexedFields.Select(f => f.FieldPath).ToList(),
                        RebuildResult = rebuildResult
                    }
                };
            }).ConfigureAwait(false);
        }

        private async Task PostRebuildIndexesRoute(HttpContextBase ctx)
        {
            await WrappedRequestHandler(ctx, RequestTypeEnum.Collection, async (reqCtx) =>
            {
                string? collectionId = ctx.Request.Url.Parameters["collectionId"];
                if (String.IsNullOrEmpty(collectionId))
                {
                    return new ResponseContext(false, 400, "Collection ID is required");
                }

                bool exists = await _Client.Collection.Exists(collectionId, CancellationToken.None).ConfigureAwait(false);
                if (!exists)
                {
                    return new ResponseContext(false, 404, "Collection not found");
                }

                bool dropUnusedIndexes = true;
                string body = reqCtx.RequestBody ?? String.Empty;
                if (!String.IsNullOrEmpty(body))
                {
                    RebuildIndexesRequest? request = JsonSerializer.Deserialize<RebuildIndexesRequest>(body, _JsonOptions);
                    if (request != null)
                    {
                        dropUnusedIndexes = request.DropUnusedIndexes;
                    }
                }

                IndexRebuildResult result = await _Client.Collection.RebuildIndexes(collectionId, dropUnusedIndexes, null, CancellationToken.None).ConfigureAwait(false);

                return new ResponseContext
                {
                    Success = result.Success,
                    StatusCode = result.Success ? 200 : 500,
                    Data = result
                };
            }).ConfigureAwait(false);
        }

        #endregion

        #region Private-Methods-Documents

        private async Task GetDocumentsRoute(HttpContextBase ctx)
        {
            await WrappedRequestHandler(ctx, RequestTypeEnum.Document, async (reqCtx) =>
            {
                string? collectionId = ctx.Request.Url.Parameters["collectionId"];
                if (String.IsNullOrEmpty(collectionId))
                {
                    return new ResponseContext(false, 400, "Collection ID is required");
                }

                bool collectionExists = await _Client.Collection.Exists(collectionId, CancellationToken.None).ConfigureAwait(false);
                if (!collectionExists)
                {
                    return new ResponseContext(false, 404, "Collection not found");
                }

                List<Document> documents = await _Client.Document.ReadAllInCollection(collectionId, token: CancellationToken.None).ConfigureAwait(false);

                return new ResponseContext
                {
                    Success = true,
                    StatusCode = 200,
                    Data = BuildEnumerationResult(documents, reqCtx)
                };
            }).ConfigureAwait(false);
        }

        private async Task PutDocumentRoute(HttpContextBase ctx)
        {
            await WrappedRequestHandler(ctx, RequestTypeEnum.Document, async (reqCtx) =>
            {
                string? collectionId = ctx.Request.Url.Parameters["collectionId"];
                if (String.IsNullOrEmpty(collectionId))
                {
                    return new ResponseContext(false, 400, "Collection ID is required");
                }

                bool collectionExists = await _Client.Collection.Exists(collectionId, CancellationToken.None).ConfigureAwait(false);
                if (!collectionExists)
                {
                    return new ResponseContext(false, 404, "Collection not found");
                }

                string body = reqCtx.RequestBody ?? String.Empty;
                if (String.IsNullOrEmpty(body))
                {
                    return new ResponseContext(false, 400, "Request body is required");
                }

                CreateDocumentRequest? request = JsonSerializer.Deserialize<CreateDocumentRequest>(body, _JsonOptions);
                if (request == null)
                {
                    return new ResponseContext(false, 400, "Invalid JSON in request body");
                }

                if (!request.Validate(out string errorMessage))
                {
                    return new ResponseContext(false, 400, errorMessage);
                }

                // Serialize the content to its raw JSON string, preserving field casing.
                string jsonContent = SerializeDocumentContent(request.Content);

                Document document = await _Client.Document.Ingest(
                    collectionId,
                    jsonContent,
                    request.Name,
                    request.Labels,
                    request.Tags,
                    CancellationToken.None).ConfigureAwait(false);

                return new ResponseContext
                {
                    Success = true,
                    StatusCode = 201,
                    Data = document
                };
            }).ConfigureAwait(false);
        }

        private async Task PutDocumentBatchRoute(HttpContextBase ctx)
        {
            await WrappedRequestHandler(ctx, RequestTypeEnum.Document, async (reqCtx) =>
            {
                string? collectionId = ctx.Request.Url.Parameters["collectionId"];
                if (String.IsNullOrEmpty(collectionId))
                {
                    return new ResponseContext(false, 400, "Collection ID is required");
                }

                bool collectionExists = await _Client.Collection.Exists(collectionId, CancellationToken.None).ConfigureAwait(false);
                if (!collectionExists)
                {
                    return new ResponseContext(false, 404, "Collection not found");
                }

                string body = reqCtx.RequestBody ?? String.Empty;
                if (String.IsNullOrEmpty(body))
                {
                    return new ResponseContext(false, 400, "Request body is required");
                }

                BatchIngestRequest? request = JsonSerializer.Deserialize<BatchIngestRequest>(body, _JsonOptions);
                if (request == null)
                {
                    return new ResponseContext(false, 400, "Invalid JSON in request body");
                }

                if (!request.Validate(out string errorMessage))
                {
                    return new ResponseContext(false, 400, errorMessage);
                }

                List<Lattice.Core.Models.BatchDocument> batchDocs = new List<Lattice.Core.Models.BatchDocument>();
                foreach (BatchIngestDocumentEntry entry in request.Documents)
                {
                    string jsonContent = SerializeDocumentContent(entry.Content);

                    batchDocs.Add(new Lattice.Core.Models.BatchDocument(
                        jsonContent,
                        entry.Name,
                        entry.Labels,
                        entry.Tags));
                }

                List<Document> documents = await _Client.Document.IngestBatch(
                    collectionId,
                    batchDocs,
                    CancellationToken.None).ConfigureAwait(false);

                return new ResponseContext
                {
                    Success = true,
                    StatusCode = 201,
                    Data = documents
                };
            }).ConfigureAwait(false);
        }

        private async Task GetDocumentRoute(HttpContextBase ctx)
        {
            // Check for includeContent query parameter
            bool includeContent = false;
            string? includeContentParam = ctx.Request.Query.Elements["includeContent"];
            if (!String.IsNullOrEmpty(includeContentParam))
            {
                Boolean.TryParse(includeContentParam, out includeContent);
            }

            // If includeContent=true, return raw JSON content directly (not wrapped)
            if (includeContent)
            {
                await GetDocumentContentRaw(ctx).ConfigureAwait(false);
                return;
            }

            // Otherwise return document metadata in standard response wrapper
            await WrappedRequestHandler(ctx, RequestTypeEnum.Document, async (reqCtx) =>
            {
                string? collectionId = ctx.Request.Url.Parameters["collectionId"];
                string? documentId = ctx.Request.Url.Parameters["documentId"];

                if (String.IsNullOrEmpty(collectionId))
                {
                    return new ResponseContext(false, 400, "Collection ID is required");
                }

                if (String.IsNullOrEmpty(documentId))
                {
                    return new ResponseContext(false, 400, "Document ID is required");
                }

                Document? document = await _Client.Document.ReadById(documentId, includeContent: false, token: CancellationToken.None).ConfigureAwait(false);
                if (document == null || document.CollectionId != collectionId)
                {
                    return new ResponseContext(false, 404, "Document not found");
                }

                return new ResponseContext
                {
                    Success = true,
                    StatusCode = 200,
                    Data = document
                };
            }).ConfigureAwait(false);
        }

        private async Task GetDocumentContentRaw(HttpContextBase ctx)
        {
            RequestContext requestContext = await BuildRequestContext(ctx, RequestTypeEnum.Document).ConfigureAwait(false);
            DateTime startTime = requestContext.CreatedUtc;
            int statusCode = 200;
            bool success = true;
            string responseBody = "{}";
            string? errorMessage = null;

            try
            {
                string? collectionId = requestContext.CollectionId;
                string? documentId = requestContext.DocumentId;

                if (String.IsNullOrEmpty(collectionId))
                {
                    statusCode = 400;
                    success = false;
                    errorMessage = "Collection ID is required";
                    responseBody = JsonSerializer.Serialize(new ErrorResponse { Error = errorMessage }, _JsonOptions);
                    await SendRawResponse(ctx, requestContext, statusCode, responseBody, startTime, success, errorMessage).ConfigureAwait(false);
                    return;
                }

                if (String.IsNullOrEmpty(documentId))
                {
                    statusCode = 400;
                    success = false;
                    errorMessage = "Document ID is required";
                    responseBody = JsonSerializer.Serialize(new ErrorResponse { Error = errorMessage }, _JsonOptions);
                    await SendRawResponse(ctx, requestContext, statusCode, responseBody, startTime, success, errorMessage).ConfigureAwait(false);
                    return;
                }

                Document? document = await _Client.Document.ReadById(documentId, includeContent: true, token: CancellationToken.None).ConfigureAwait(false);
                if (document == null || document.CollectionId != collectionId)
                {
                    statusCode = 404;
                    success = false;
                    errorMessage = "Document not found";
                    responseBody = JsonSerializer.Serialize(new ErrorResponse { Error = errorMessage }, _JsonOptions);
                    await SendRawResponse(ctx, requestContext, statusCode, responseBody, startTime, success, errorMessage).ConfigureAwait(false);
                    return;
                }

                responseBody = document.Content ?? "{}";
                await SendRawResponse(ctx, requestContext, statusCode, responseBody, startTime, success).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _Logging?.Error(_Header + "Exception in GetDocumentContentRaw: " + e.Message);
                statusCode = 500;
                success = false;
                errorMessage = e.Message;
                responseBody = JsonSerializer.Serialize(new ErrorResponse { Error = e.Message }, _JsonOptions);
                await SendRawResponse(ctx, requestContext, statusCode, responseBody, startTime, success, errorMessage).ConfigureAwait(false);
            }
        }

        private async Task HeadDocumentRoute(HttpContextBase ctx)
        {
            await WrappedRequestHandler(ctx, RequestTypeEnum.Document, async (reqCtx) =>
            {
                string? collectionId = ctx.Request.Url.Parameters["collectionId"];
                string? documentId = ctx.Request.Url.Parameters["documentId"];

                if (String.IsNullOrEmpty(collectionId))
                {
                    return new ResponseContext(false, 400, "Collection ID is required");
                }

                if (String.IsNullOrEmpty(documentId))
                {
                    return new ResponseContext(false, 400, "Document ID is required");
                }

                Document? document = await _Client.Document.ReadById(documentId, token: CancellationToken.None).ConfigureAwait(false);
                if (document == null || document.CollectionId != collectionId)
                {
                    return new ResponseContext(false, 404, "Document not found");
                }

                return new ResponseContext
                {
                    Success = true,
                    StatusCode = 200
                };
            }).ConfigureAwait(false);
        }

        private async Task DeleteDocumentRoute(HttpContextBase ctx)
        {
            await WrappedRequestHandler(ctx, RequestTypeEnum.Document, async (reqCtx) =>
            {
                string? collectionId = ctx.Request.Url.Parameters["collectionId"];
                string? documentId = ctx.Request.Url.Parameters["documentId"];

                if (String.IsNullOrEmpty(collectionId))
                {
                    return new ResponseContext(false, 400, "Collection ID is required");
                }

                if (String.IsNullOrEmpty(documentId))
                {
                    return new ResponseContext(false, 400, "Document ID is required");
                }

                Document? document = await _Client.Document.ReadById(documentId, token: CancellationToken.None).ConfigureAwait(false);
                if (document == null || document.CollectionId != collectionId)
                {
                    return new ResponseContext(false, 404, "Document not found");
                }

                await _Client.Document.Delete(documentId, CancellationToken.None).ConfigureAwait(false);

                return new ResponseContext
                {
                    Success = true,
                    StatusCode = 200,
                    Data = new { Message = "Document deleted successfully", DocumentId = documentId }
                };
            }).ConfigureAwait(false);
        }

        #endregion

        #region Private-Methods-Search

        private async Task PostSearchRoute(HttpContextBase ctx)
        {
            await WrappedRequestHandler(ctx, RequestTypeEnum.Search, async (reqCtx) =>
            {
                string? collectionId = ctx.Request.Url.Parameters["collectionId"];
                if (String.IsNullOrEmpty(collectionId))
                {
                    return new ResponseContext(false, 400, "Collection ID is required");
                }

                bool collectionExists = await _Client.Collection.Exists(collectionId, CancellationToken.None).ConfigureAwait(false);
                if (!collectionExists)
                {
                    return new ResponseContext(false, 404, "Collection not found");
                }

                string body = reqCtx.RequestBody ?? String.Empty;
                if (String.IsNullOrEmpty(body))
                {
                    return new ResponseContext(false, 400, "Request body is required");
                }

                SearchDocumentsRequest? request = JsonSerializer.Deserialize<SearchDocumentsRequest>(body, _JsonOptions);
                if (request == null)
                {
                    return new ResponseContext(false, 400, "Invalid JSON in request body");
                }

                // Use SQL expression if provided
                if (!String.IsNullOrWhiteSpace(request.SqlExpression))
                {
                    SearchResult result = await _Client.Search.SearchBySql(
                        collectionId,
                        request.SqlExpression,
                        request.Labels ?? new List<string>(),
                        request.Tags ?? new Dictionary<string, string>(),
                        CancellationToken.None).ConfigureAwait(false);
                    return new ResponseContext
                    {
                        Success = true,
                        StatusCode = 200,
                        Data = result
                    };
                }

                // Otherwise use structured query
                SearchQuery query = new SearchQuery
                {
                    CollectionId = collectionId,
                    Filters = request.Filters?.Select(f => new SearchFilter(f.Field, f.Condition, f.Value)).ToList() ?? new List<SearchFilter>(),
                    Labels = request.Labels ?? new List<string>(),
                    Tags = request.Tags ?? new Dictionary<string, string>(),
                    MaxResults = request.MaxResults ?? 100,
                    Skip = request.Skip ?? 0,
                    Ordering = request.Ordering ?? EnumerationOrderEnum.CreatedDescending,
                    IncludeContent = request.IncludeContent ?? false
                };

                SearchResult searchResult = await _Client.Search.Search(query, CancellationToken.None).ConfigureAwait(false);
                return new ResponseContext
                {
                    Success = true,
                    StatusCode = 200,
                    Data = searchResult
                };
            }).ConfigureAwait(false);
        }

        #endregion

        #region Private-Methods-RequestHistory

        private async Task GetRequestHistoryRoute(HttpContextBase ctx)
        {
            await WrappedRequestHandler(ctx, RequestTypeEnum.Search, async (reqCtx) =>
            {
                if (!_RequestHistory.Enabled)
                {
                    return new ResponseContext(false, 503, "Request history is disabled");
                }

                RequestHistorySearchFilter filter = BuildRequestHistoryFilterFromQuery(ctx, true);
                RequestHistorySearchResult result = await _RequestHistory.SearchAsync(filter, CancellationToken.None).ConfigureAwait(false);
                return new ResponseContext
                {
                    Success = true,
                    StatusCode = 200,
                    Data = result
                };
            }).ConfigureAwait(false);
        }

        private async Task GetRequestHistoryEntryRoute(HttpContextBase ctx)
        {
            await WrappedRequestHandler(ctx, RequestTypeEnum.Search, async (reqCtx) =>
            {
                if (!_RequestHistory.Enabled)
                {
                    return new ResponseContext(false, 503, "Request history is disabled");
                }

                string? requestId = ctx.Request.Url.Parameters["requestId"];
                if (String.IsNullOrWhiteSpace(requestId))
                {
                    return new ResponseContext(false, 400, "Request ID is required");
                }

                RequestHistoryEntry? entry = await _RequestHistory.GetEntryAsync(requestId, CancellationToken.None).ConfigureAwait(false);
                if (entry == null)
                {
                    return new ResponseContext(false, 404, "Request history entry not found");
                }

                return new ResponseContext
                {
                    Success = true,
                    StatusCode = 200,
                    Data = entry
                };
            }).ConfigureAwait(false);
        }

        private async Task GetRequestHistoryDetailRoute(HttpContextBase ctx)
        {
            await WrappedRequestHandler(ctx, RequestTypeEnum.Search, async (reqCtx) =>
            {
                if (!_RequestHistory.Enabled)
                {
                    return new ResponseContext(false, 503, "Request history is disabled");
                }

                string? requestId = ctx.Request.Url.Parameters["requestId"];
                if (String.IsNullOrWhiteSpace(requestId))
                {
                    return new ResponseContext(false, 400, "Request ID is required");
                }

                RequestHistoryDetail? detail = await _RequestHistory.GetDetailAsync(requestId, CancellationToken.None).ConfigureAwait(false);
                if (detail == null)
                {
                    return new ResponseContext(false, 404, "Request history entry not found");
                }

                return new ResponseContext
                {
                    Success = true,
                    StatusCode = 200,
                    Data = detail
                };
            }).ConfigureAwait(false);
        }

        private async Task GetRequestHistorySummaryRoute(HttpContextBase ctx)
        {
            await WrappedRequestHandler(ctx, RequestTypeEnum.Search, async (reqCtx) =>
            {
                if (!_RequestHistory.Enabled)
                {
                    return new ResponseContext(false, 503, "Request history is disabled");
                }

                RequestHistorySearchFilter filter = BuildRequestHistoryFilterFromQuery(ctx, false);
                string? interval = GetQueryValue(ctx, "interval");
                RequestHistorySummaryResult result = await _RequestHistory.GetSummaryAsync(filter, interval, CancellationToken.None).ConfigureAwait(false);
                return new ResponseContext
                {
                    Success = true,
                    StatusCode = 200,
                    Data = result
                };
            }).ConfigureAwait(false);
        }

        private async Task DeleteRequestHistoryEntryRoute(HttpContextBase ctx)
        {
            await WrappedRequestHandler(ctx, RequestTypeEnum.Search, async (reqCtx) =>
            {
                if (!_RequestHistory.Enabled)
                {
                    return new ResponseContext(false, 503, "Request history is disabled");
                }

                string? requestId = ctx.Request.Url.Parameters["requestId"];
                if (String.IsNullOrWhiteSpace(requestId))
                {
                    return new ResponseContext(false, 400, "Request ID is required");
                }

                bool deleted = await _RequestHistory.DeleteAsync(requestId, CancellationToken.None).ConfigureAwait(false);
                if (!deleted)
                {
                    return new ResponseContext(false, 404, "Request history entry not found");
                }

                return new ResponseContext
                {
                    Success = true,
                    StatusCode = 200,
                    Data = new { Deleted = true, RequestId = requestId }
                };
            }).ConfigureAwait(false);
        }

        private async Task DeleteRequestHistoryBulkRoute(HttpContextBase ctx)
        {
            await WrappedRequestHandler(ctx, RequestTypeEnum.Search, async (reqCtx) =>
            {
                if (!_RequestHistory.Enabled)
                {
                    return new ResponseContext(false, 503, "Request history is disabled");
                }

                RequestHistorySearchFilter filter = new RequestHistorySearchFilter();

                if (!String.IsNullOrWhiteSpace(reqCtx.RequestBody))
                {
                    RequestHistorySearchFilter? request = JsonSerializer.Deserialize<RequestHistorySearchFilter>(reqCtx.RequestBody, _JsonOptions);
                    if (request == null)
                    {
                        return new ResponseContext(false, 400, "Invalid JSON in request body");
                    }

                    filter = request;
                }

                long deletedCount = await _RequestHistory.DeleteBulkAsync(filter, CancellationToken.None).ConfigureAwait(false);
                return new ResponseContext
                {
                    Success = true,
                    StatusCode = 200,
                    Data = new { DeletedCount = deletedCount }
                };
            }).ConfigureAwait(false);
        }

        #endregion

        #region Private-Methods-Schemas

        private async Task GetSchemasRoute(HttpContextBase ctx)
        {
            await WrappedRequestHandler(ctx, RequestTypeEnum.Collection, async (reqCtx) =>
            {
                List<Lattice.Core.Models.Schema> schemas = await _Client.Schema.ReadAll(CancellationToken.None).ConfigureAwait(false);
                return new ResponseContext
                {
                    Success = true,
                    StatusCode = 200,
                    Data = BuildEnumerationResult(schemas, reqCtx)
                };
            }).ConfigureAwait(false);
        }

        private async Task GetSchemaRoute(HttpContextBase ctx)
        {
            await WrappedRequestHandler(ctx, RequestTypeEnum.Collection, async (reqCtx) =>
            {
                string? schemaId = ctx.Request.Url.Parameters["schemaId"];
                if (String.IsNullOrEmpty(schemaId))
                {
                    return new ResponseContext(false, 400, "Schema ID is required");
                }

                Lattice.Core.Models.Schema? schema = await _Client.Schema.ReadById(schemaId, CancellationToken.None).ConfigureAwait(false);
                if (schema == null)
                {
                    return new ResponseContext(false, 404, "Schema not found");
                }

                return new ResponseContext
                {
                    Success = true,
                    StatusCode = 200,
                    Data = schema
                };
            }).ConfigureAwait(false);
        }

        private async Task GetSchemaElementsRoute(HttpContextBase ctx)
        {
            await WrappedRequestHandler(ctx, RequestTypeEnum.Collection, async (reqCtx) =>
            {
                string? schemaId = ctx.Request.Url.Parameters["schemaId"];
                if (String.IsNullOrEmpty(schemaId))
                {
                    return new ResponseContext(false, 400, "Schema ID is required");
                }

                Lattice.Core.Models.Schema? schema = await _Client.Schema.ReadById(schemaId, CancellationToken.None).ConfigureAwait(false);
                if (schema == null)
                {
                    return new ResponseContext(false, 404, "Schema not found");
                }

                List<SchemaElement> elements = await _Client.Schema.GetElements(schemaId, CancellationToken.None).ConfigureAwait(false);
                return new ResponseContext
                {
                    Success = true,
                    StatusCode = 200,
                    Data = BuildEnumerationResult(elements, reqCtx)
                };
            }).ConfigureAwait(false);
        }

        #endregion

        #region Private-Methods-IndexTables

        private async Task GetIndexTablesRoute(HttpContextBase ctx)
        {
            await WrappedRequestHandler(ctx, RequestTypeEnum.Collection, async (reqCtx) =>
            {
                List<IndexTableMapping> mappings = await _Client.Index.GetMappings(CancellationToken.None).ConfigureAwait(false);
                return new ResponseContext
                {
                    Success = true,
                    StatusCode = 200,
                    Data = BuildEnumerationResult(mappings, reqCtx)
                };
            }).ConfigureAwait(false);
        }

        private async Task GetTableEntriesRoute(HttpContextBase ctx)
        {
            await WrappedRequestHandler(ctx, RequestTypeEnum.Collection, async (reqCtx) =>
            {
                string? tableName = ctx.Request.Url.Parameters["tableName"];
                if (String.IsNullOrEmpty(tableName))
                {
                    return new ResponseContext(false, 400, "Table name is required");
                }

                // Parse enumeration parameters (accept legacy "limit" as an alias for "maxResults")
                int skip = ParseQueryInt(reqCtx, "skip", 0, 0, Int32.MaxValue);
                int maxResults = ParseQueryInt(reqCtx, "maxResults", ParseQueryInt(reqCtx, "limit", 100, 1, 1000), 1, 1000);

                // Verify table exists
                IndexTableMapping? mapping = await _Client.Index.GetMappingByKey(tableName, CancellationToken.None).ConfigureAwait(false);
                if (mapping == null)
                {
                    // Try by table name directly
                    List<IndexTableMapping> allMappings = await _Client.Index.GetMappings(CancellationToken.None).ConfigureAwait(false);
                    mapping = allMappings.Find(m => m.TableName == tableName);
                }

                if (mapping == null)
                {
                    return new ResponseContext(false, 404, "Index table not found");
                }

                List<IndexTableEntry> entries = await _Client.Index.GetTableEntries(mapping.TableName, skip, maxResults, CancellationToken.None).ConfigureAwait(false);
                long totalCount = await _Client.Index.GetTableEntryCount(mapping.TableName, CancellationToken.None).ConfigureAwait(false);

                return new ResponseContext
                {
                    Success = true,
                    StatusCode = 200,
                    Data = BuildEnumerationResult(entries, skip, maxResults, totalCount)
                };
            }).ConfigureAwait(false);
        }

        #endregion
    }
}
