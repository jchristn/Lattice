namespace Lattice.Server.API.REST
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
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
    using Lattice.Core.Validation;
    using Lattice.Server.Classes;

    /// <summary>
    /// REST service handler for Lattice API.
    /// </summary>
    public class RestServiceHandler
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
        /// Stop the webserver.
        /// </summary>
        public void Stop()
        {
            _Webserver?.Stop();
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
            });

            InitializeRoutes();
        }

        private void InitializeRoutes()
        {
            // General routes
            _Webserver!.Routes.Preflight = PreflightRoute;
            _Webserver.Routes.PostRouting = PostRoutingRoute;

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

        private async Task PreflightRoute(HttpContextBase ctx)
        {
            NameValueCollection responseHeaders = new NameValueCollection(StringComparer.InvariantCultureIgnoreCase);

            string[]? requestedHeaders = null;
            string headers = "";

            if (ctx.Request.Headers != null)
            {
                for (int i = 0; i < ctx.Request.Headers.Count; i++)
                {
                    string? key = ctx.Request.Headers.GetKey(i);
                    string? value = ctx.Request.Headers.Get(i);
                    if (String.IsNullOrEmpty(key)) continue;
                    if (String.IsNullOrEmpty(value)) continue;
                    if (String.Compare(key.ToLower(), "access-control-request-headers") == 0)
                    {
                        requestedHeaders = value.Split(',');
                        break;
                    }
                }
            }

            if (requestedHeaders != null)
            {
                foreach (string curr in requestedHeaders)
                {
                    headers += ", " + curr;
                }
            }

            responseHeaders.Add("Access-Control-Allow-Methods", "OPTIONS, HEAD, GET, PUT, POST, DELETE");
            responseHeaders.Add("Access-Control-Allow-Headers", "*, Content-Type, X-Requested-With" + headers);
            responseHeaders.Add("Access-Control-Expose-Headers", "Content-Type, X-Requested-With" + headers);
            responseHeaders.Add("Access-Control-Allow-Origin", "*");
            responseHeaders.Add("Accept", "*/*");
            responseHeaders.Add("Accept-Language", "en-US, en");
            responseHeaders.Add("Accept-Charset", "ISO-8859-1, utf-8");
            responseHeaders.Add("Connection", "keep-alive");

            ctx.Response.StatusCode = 200;
            ctx.Response.Headers = responseHeaders;
            await ctx.Response.Send().ConfigureAwait(false);
        }

        private async Task PostRoutingRoute(HttpContextBase ctx)
        {
            ctx.Request.Timestamp.End = DateTime.UtcNow;

            _Logging?.Debug(
                _Header
                + ctx.Request.Method + " " + ctx.Request.Url.RawWithQuery + " "
                + ctx.Response.StatusCode + " "
                + "(" + ctx.Request.Timestamp.TotalMs?.ToString("F2") + "ms)");
        }

        private async Task DefaultRoute(HttpContextBase ctx)
        {
            ResponseContext response = new ResponseContext(false, 404, "Not found");
            await SendResponse(ctx, response);
        }

        private async Task ExceptionRoute(HttpContextBase ctx, Exception e)
        {
            if (e is JsonException)
            {
                _Logging?.Warn(_Header + "JSON parsing error: " + e.Message);
                ResponseContext jsonResponse = new ResponseContext(false, 400, "Invalid JSON: " + e.Message);
                await SendResponse(ctx, jsonResponse);
                return;
            }

            if (e is SchemaValidationException sve)
            {
                _Logging?.Warn(_Header + "Schema validation error: " + sve.Message);
                ResponseContext validationResponse = new ResponseContext(false, 400, "Schema validation failed")
                {
                    Data = new { Errors = sve.Errors }
                };
                await SendResponse(ctx, validationResponse);
                return;
            }

            if (e is ArgumentException)
            {
                _Logging?.Warn(_Header + "Argument error: " + e.Message);
                ResponseContext argResponse = new ResponseContext(false, 400, e.Message);
                await SendResponse(ctx, argResponse);
                return;
            }

            _Logging?.Error(_Header + "Exception: " + e.Message);
            ResponseContext response = new ResponseContext(false, 500, e.Message);
            await SendResponse(ctx, response);
        }

        private async Task WrappedRequestHandler(HttpContextBase ctx, RequestTypeEnum requestType, Func<RequestContext, Task<ResponseContext>> handler)
        {
            DateTime startTime = DateTime.UtcNow;
            RequestContext requestContext = BuildRequestContext(ctx, requestType);
            ResponseContext responseContext;

            try
            {
                responseContext = await handler(requestContext);
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

            responseContext.ProcessingTimeMs = DateTime.UtcNow.Subtract(startTime).TotalMilliseconds;
            await SendResponse(ctx, responseContext);
        }

        private RequestContext BuildRequestContext(HttpContextBase ctx, RequestTypeEnum requestType)
        {
            RequestContext requestContext = new RequestContext
            {
                RequestType = requestType,
                Method = ctx.Request.Method.ToString(),
                Url = ctx.Request.Url.Full,
                IpAddress = ctx.Request.Source.IpAddress
            };

            return requestContext;
        }

        private async Task<string> GetRequestBody(HttpContextBase ctx)
        {
            if (ctx.Request.Data != null && ctx.Request.ContentLength > 0)
            {
                using (StreamReader reader = new StreamReader(ctx.Request.Data, Encoding.UTF8))
                {
                    return await reader.ReadToEndAsync();
                }
            }

            return string.Empty;
        }

        private async Task SendResponse(HttpContextBase ctx, ResponseContext response)
        {
            ctx.Response.StatusCode = response.StatusCode;
            ctx.Response.ContentType = "application/json";

            foreach (KeyValuePair<string, string> header in response.Headers)
            {
                ctx.Response.Headers.Add(header.Key, header.Value);
            }

            string json = JsonSerializer.Serialize(response, _JsonOptions);
            await ctx.Response.Send(json);
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
            });
        }

        #endregion

        #region Private-Methods-Collections

        private async Task GetCollectionsRoute(HttpContextBase ctx)
        {
            await WrappedRequestHandler(ctx, RequestTypeEnum.Collection, async (reqCtx) =>
            {
                List<Collection> collections = await _Client.Collection.ReadAll(CancellationToken.None);
                return new ResponseContext
                {
                    Success = true,
                    StatusCode = 200,
                    Data = collections
                };
            });
        }

        private async Task PutCollectionRoute(HttpContextBase ctx)
        {
            await WrappedRequestHandler(ctx, RequestTypeEnum.Collection, async (reqCtx) =>
            {
                string body = await GetRequestBody(ctx);
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
                    CancellationToken.None);

                return new ResponseContext
                {
                    Success = true,
                    StatusCode = 201,
                    Data = collection
                };
            });
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

                Collection? collection = await _Client.Collection.ReadById(collectionId, CancellationToken.None);
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
            });
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

                bool exists = await _Client.Collection.Exists(collectionId, CancellationToken.None);
                if (!exists)
                {
                    return new ResponseContext(false, 404, "Collection not found");
                }

                return new ResponseContext
                {
                    Success = true,
                    StatusCode = 200
                };
            });
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

                bool exists = await _Client.Collection.Exists(collectionId, CancellationToken.None);
                if (!exists)
                {
                    return new ResponseContext(false, 404, "Collection not found");
                }

                await _Client.Collection.Delete(collectionId, CancellationToken.None);

                return new ResponseContext
                {
                    Success = true,
                    StatusCode = 200,
                    Data = new { Message = "Collection deleted successfully", CollectionId = collectionId }
                };
            });
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

                Collection? collection = await _Client.Collection.ReadById(collectionId, CancellationToken.None);
                if (collection == null)
                {
                    return new ResponseContext(false, 404, "Collection not found");
                }

                List<FieldConstraint> constraints = await _Client.Collection.GetConstraints(collectionId, CancellationToken.None);
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
            });
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

                bool exists = await _Client.Collection.Exists(collectionId, CancellationToken.None);
                if (!exists)
                {
                    return new ResponseContext(false, 404, "Collection not found");
                }

                string body = await GetRequestBody(ctx);
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
                    CancellationToken.None);

                List<FieldConstraint> constraints = await _Client.Collection.GetConstraints(collectionId, CancellationToken.None);

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
            });
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

                Collection? collection = await _Client.Collection.ReadById(collectionId, CancellationToken.None);
                if (collection == null)
                {
                    return new ResponseContext(false, 404, "Collection not found");
                }

                List<IndexedField> indexedFields = await _Client.Collection.GetIndexedFields(collectionId, CancellationToken.None);
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
            });
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

                bool exists = await _Client.Collection.Exists(collectionId, CancellationToken.None);
                if (!exists)
                {
                    return new ResponseContext(false, 404, "Collection not found");
                }

                string body = await GetRequestBody(ctx);
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
                    CancellationToken.None);

                // Optionally rebuild indexes
                IndexRebuildResult? rebuildResult = null;
                if (request.RebuildIndexes)
                {
                    rebuildResult = await _Client.Collection.RebuildIndexes(collectionId, true, null, CancellationToken.None);
                }

                List<IndexedField> indexedFields = await _Client.Collection.GetIndexedFields(collectionId, CancellationToken.None);

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
            });
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

                bool exists = await _Client.Collection.Exists(collectionId, CancellationToken.None);
                if (!exists)
                {
                    return new ResponseContext(false, 404, "Collection not found");
                }

                bool dropUnusedIndexes = true;
                string body = await GetRequestBody(ctx);
                if (!String.IsNullOrEmpty(body))
                {
                    RebuildIndexesRequest? request = JsonSerializer.Deserialize<RebuildIndexesRequest>(body, _JsonOptions);
                    if (request != null)
                    {
                        dropUnusedIndexes = request.DropUnusedIndexes;
                    }
                }

                IndexRebuildResult result = await _Client.Collection.RebuildIndexes(collectionId, dropUnusedIndexes, null, CancellationToken.None);

                return new ResponseContext
                {
                    Success = result.Success,
                    StatusCode = result.Success ? 200 : 500,
                    Data = result
                };
            });
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

                bool collectionExists = await _Client.Collection.Exists(collectionId, CancellationToken.None);
                if (!collectionExists)
                {
                    return new ResponseContext(false, 404, "Collection not found");
                }

                List<Document> documents = await _Client.Document.ReadAllInCollection(collectionId, token: CancellationToken.None);

                return new ResponseContext
                {
                    Success = true,
                    StatusCode = 200,
                    Data = documents
                };
            });
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

                bool collectionExists = await _Client.Collection.Exists(collectionId, CancellationToken.None);
                if (!collectionExists)
                {
                    return new ResponseContext(false, 404, "Collection not found");
                }

                string body = await GetRequestBody(ctx);
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

                // Serialize the content to JSON
                string jsonContent;
                if (request.Content is JsonElement element)
                {
                    jsonContent = element.GetRawText();
                }
                else
                {
                    jsonContent = JsonSerializer.Serialize(request.Content);
                }

                Document document = await _Client.Document.Ingest(
                    collectionId,
                    jsonContent,
                    request.Name,
                    request.Labels,
                    request.Tags,
                    CancellationToken.None);

                return new ResponseContext
                {
                    Success = true,
                    StatusCode = 201,
                    Data = document
                };
            });
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
                await GetDocumentContentRaw(ctx);
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

                Document? document = await _Client.Document.ReadById(documentId, includeContent: false, token: CancellationToken.None);
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
            });
        }

        private async Task GetDocumentContentRaw(HttpContextBase ctx)
        {
            DateTime startTime = DateTime.UtcNow;

            try
            {
                string? collectionId = ctx.Request.Url.Parameters["collectionId"];
                string? documentId = ctx.Request.Url.Parameters["documentId"];

                if (String.IsNullOrEmpty(collectionId))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send("{\"error\": \"Collection ID is required\"}");
                    return;
                }

                if (String.IsNullOrEmpty(documentId))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send("{\"error\": \"Document ID is required\"}");
                    return;
                }

                Document? document = await _Client.Document.ReadById(documentId, includeContent: true, token: CancellationToken.None);
                if (document == null || document.CollectionId != collectionId)
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.Send("{\"error\": \"Document not found\"}");
                    return;
                }

                // Return raw JSON content directly
                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send(document.Content ?? "{}");
            }
            catch (Exception e)
            {
                _Logging?.Error(_Header + "Exception in GetDocumentContentRaw: " + e.Message);
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.Send("{\"error\": \"" + e.Message.Replace("\"", "\\\"") + "\"}");
            }
            finally
            {
                double processingTimeMs = DateTime.UtcNow.Subtract(startTime).TotalMilliseconds;
                _Logging?.Debug(
                    _Header
                    + ctx.Request.Method + " " + ctx.Request.Url.RawWithQuery + " "
                    + ctx.Response.StatusCode + " "
                    + "(" + processingTimeMs.ToString("F2") + "ms)");
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

                Document? document = await _Client.Document.ReadById(documentId, token: CancellationToken.None);
                if (document == null || document.CollectionId != collectionId)
                {
                    return new ResponseContext(false, 404, "Document not found");
                }

                return new ResponseContext
                {
                    Success = true,
                    StatusCode = 200
                };
            });
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

                Document? document = await _Client.Document.ReadById(documentId, token: CancellationToken.None);
                if (document == null || document.CollectionId != collectionId)
                {
                    return new ResponseContext(false, 404, "Document not found");
                }

                await _Client.Document.Delete(documentId, CancellationToken.None);

                return new ResponseContext
                {
                    Success = true,
                    StatusCode = 200,
                    Data = new { Message = "Document deleted successfully", DocumentId = documentId }
                };
            });
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

                bool collectionExists = await _Client.Collection.Exists(collectionId, CancellationToken.None);
                if (!collectionExists)
                {
                    return new ResponseContext(false, 404, "Collection not found");
                }

                string body = await GetRequestBody(ctx);
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
                        CancellationToken.None);
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

                SearchResult searchResult = await _Client.Search.Search(query, CancellationToken.None);
                return new ResponseContext
                {
                    Success = true,
                    StatusCode = 200,
                    Data = searchResult
                };
            });
        }

        #endregion

        #region Private-Methods-Schemas

        private async Task GetSchemasRoute(HttpContextBase ctx)
        {
            await WrappedRequestHandler(ctx, RequestTypeEnum.Collection, async (reqCtx) =>
            {
                List<Lattice.Core.Models.Schema> schemas = await _Client.Schema.ReadAll(CancellationToken.None);
                return new ResponseContext
                {
                    Success = true,
                    StatusCode = 200,
                    Data = schemas
                };
            });
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

                Lattice.Core.Models.Schema? schema = await _Client.Schema.ReadById(schemaId, CancellationToken.None);
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
            });
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

                Lattice.Core.Models.Schema? schema = await _Client.Schema.ReadById(schemaId, CancellationToken.None);
                if (schema == null)
                {
                    return new ResponseContext(false, 404, "Schema not found");
                }

                List<SchemaElement> elements = await _Client.Schema.GetElements(schemaId, CancellationToken.None);
                return new ResponseContext
                {
                    Success = true,
                    StatusCode = 200,
                    Data = elements
                };
            });
        }

        #endregion

        #region Private-Methods-IndexTables

        private async Task GetIndexTablesRoute(HttpContextBase ctx)
        {
            await WrappedRequestHandler(ctx, RequestTypeEnum.Collection, async (reqCtx) =>
            {
                List<IndexTableMapping> mappings = await _Client.Index.GetMappings(CancellationToken.None);
                return new ResponseContext
                {
                    Success = true,
                    StatusCode = 200,
                    Data = mappings
                };
            });
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

                // Parse pagination parameters
                int skip = 0;
                int limit = 100;

                string? skipParam = ctx.Request.Query.Elements["skip"];
                if (!String.IsNullOrEmpty(skipParam) && Int32.TryParse(skipParam, out int parsedSkip))
                {
                    skip = Math.Max(0, parsedSkip);
                }

                string? limitParam = ctx.Request.Query.Elements["limit"];
                if (!String.IsNullOrEmpty(limitParam) && Int32.TryParse(limitParam, out int parsedLimit))
                {
                    limit = Math.Clamp(parsedLimit, 1, 1000);
                }

                // Verify table exists
                IndexTableMapping? mapping = await _Client.Index.GetMappingByKey(tableName, CancellationToken.None);
                if (mapping == null)
                {
                    // Try by table name directly
                    List<IndexTableMapping> allMappings = await _Client.Index.GetMappings(CancellationToken.None);
                    mapping = allMappings.Find(m => m.TableName == tableName);
                }

                if (mapping == null)
                {
                    return new ResponseContext(false, 404, "Index table not found");
                }

                List<IndexTableEntry> entries = await _Client.Index.GetTableEntries(mapping.TableName, skip, limit, CancellationToken.None);
                long totalCount = await _Client.Index.GetTableEntryCount(mapping.TableName, CancellationToken.None);

                return new ResponseContext
                {
                    Success = true,
                    StatusCode = 200,
                    Data = new
                    {
                        TableName = mapping.TableName,
                        FieldKey = mapping.Key,
                        Entries = entries,
                        TotalCount = totalCount,
                        Skip = skip,
                        Limit = limit
                    }
                };
            });
        }

        #endregion
    }
}
