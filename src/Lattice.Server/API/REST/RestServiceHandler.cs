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
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
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
            _Logging?.Info(_Header + "started on http://" + _Settings.Rest.Hostname + ":" + _Settings.Rest.Port);
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

            _Webserver = new Webserver(webserverSettings, DefaultRoute);
            InitializeRoutes();
        }

        private void InitializeRoutes()
        {
            // General routes
            _Webserver!.Routes.Preflight = PreflightRoute;
            _Webserver.Routes.PostRouting = PostRoutingRoute;

            // Health check routes
            _Webserver.Routes.PreAuthentication.Static.Add(HttpMethod.GET, "/", GetHealthRoute, ExceptionRoute);
            _Webserver.Routes.PreAuthentication.Static.Add(HttpMethod.GET, "/v1.0/health", GetHealthRoute, ExceptionRoute);

            // Collection routes
            _Webserver.Routes.PreAuthentication.Static.Add(HttpMethod.GET, "/v1.0/collections", GetCollectionsRoute, ExceptionRoute);
            _Webserver.Routes.PreAuthentication.Static.Add(HttpMethod.PUT, "/v1.0/collections", PutCollectionRoute, ExceptionRoute);
            _Webserver.Routes.PreAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/collections/{collectionId}", GetCollectionRoute, ExceptionRoute);
            _Webserver.Routes.PreAuthentication.Parameter.Add(HttpMethod.HEAD, "/v1.0/collections/{collectionId}", HeadCollectionRoute, ExceptionRoute);
            _Webserver.Routes.PreAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/collections/{collectionId}", DeleteCollectionRoute, ExceptionRoute);

            // Collection constraints and indexing routes
            _Webserver.Routes.PreAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/collections/{collectionId}/constraints", GetConstraintsRoute, ExceptionRoute);
            _Webserver.Routes.PreAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/collections/{collectionId}/constraints", PutConstraintsRoute, ExceptionRoute);
            _Webserver.Routes.PreAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/collections/{collectionId}/indexing", GetIndexingRoute, ExceptionRoute);
            _Webserver.Routes.PreAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/collections/{collectionId}/indexing", PutIndexingRoute, ExceptionRoute);
            _Webserver.Routes.PreAuthentication.Parameter.Add(HttpMethod.POST, "/v1.0/collections/{collectionId}/indexes/rebuild", PostRebuildIndexesRoute, ExceptionRoute);

            // Document routes
            _Webserver.Routes.PreAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/collections/{collectionId}/documents", GetDocumentsRoute, ExceptionRoute);
            _Webserver.Routes.PreAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/collections/{collectionId}/documents", PutDocumentRoute, ExceptionRoute);
            _Webserver.Routes.PreAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/collections/{collectionId}/documents/{documentId}", GetDocumentRoute, ExceptionRoute);
            _Webserver.Routes.PreAuthentication.Parameter.Add(HttpMethod.HEAD, "/v1.0/collections/{collectionId}/documents/{documentId}", HeadDocumentRoute, ExceptionRoute);
            _Webserver.Routes.PreAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/collections/{collectionId}/documents/{documentId}", DeleteDocumentRoute, ExceptionRoute);

            // Search route
            _Webserver.Routes.PreAuthentication.Parameter.Add(HttpMethod.POST, "/v1.0/collections/{collectionId}/documents/search", PostSearchRoute, ExceptionRoute);

            // Schema routes
            _Webserver.Routes.PreAuthentication.Static.Add(HttpMethod.GET, "/v1.0/schemas", GetSchemasRoute, ExceptionRoute);
            _Webserver.Routes.PreAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/schemas/{schemaId}", GetSchemaRoute, ExceptionRoute);
            _Webserver.Routes.PreAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/schemas/{schemaId}/elements", GetSchemaElementsRoute, ExceptionRoute);

            // Index table routes
            _Webserver.Routes.PreAuthentication.Static.Add(HttpMethod.GET, "/v1.0/tables", GetIndexTablesRoute, ExceptionRoute);
            _Webserver.Routes.PreAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tables/{tableName}/entries", GetTableEntriesRoute, ExceptionRoute);
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
                    SearchResult result = await _Client.Search.SearchBySql(collectionId, request.SqlExpression, CancellationToken.None);
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
