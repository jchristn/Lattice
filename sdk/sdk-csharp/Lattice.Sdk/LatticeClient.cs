namespace Lattice.Sdk
{
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using Lattice.Sdk.Exceptions;
    using Lattice.Sdk.Methods;
    using Lattice.Sdk.Models;

    /// <summary>
    /// Client for interacting with the Lattice REST API.
    /// </summary>
    public class LatticeClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly JsonSerializerOptions _jsonOptions;
        private bool _disposed;

        /// <summary>
        /// Collection management methods.
        /// </summary>
        public ICollectionMethods Collection { get; }

        /// <summary>
        /// Document management methods.
        /// </summary>
        public IDocumentMethods Document { get; }

        /// <summary>
        /// Search methods.
        /// </summary>
        public ISearchMethods Search { get; }

        /// <summary>
        /// Schema management methods.
        /// </summary>
        public ISchemaMethods Schema { get; }

        /// <summary>
        /// Index management methods.
        /// </summary>
        public IIndexMethods Index { get; }

        /// <summary>
        /// Initialize the Lattice client.
        /// </summary>
        /// <param name="baseUrl">The base URL of the Lattice server (e.g., "http://localhost:8000")</param>
        /// <param name="timeout">Request timeout (default: 30 seconds)</param>
        public LatticeClient(string baseUrl, TimeSpan? timeout = null)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _httpClient = new HttpClient
            {
                Timeout = timeout ?? TimeSpan.FromSeconds(30)
            };

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                PropertyNameCaseInsensitive = true
            };

            Collection = new CollectionMethods(this);
            Document = new DocumentMethods(this);
            Search = new SearchMethods(this);
            Schema = new SchemaMethods(this);
            Index = new IndexMethods(this);
        }

        /// <summary>
        /// The response header carrying the request/correlation id (replaces the old envelope "guid").
        /// </summary>
        internal const string RequestIdHeader = "X-Lattice-Request-Id";

        /// <summary>
        /// Check if the Lattice server is healthy.
        /// </summary>
        public async Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await RequestStatusAsync("GET", "/v1.0/health", throwOnError: false, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Send an HTTP request and deserialize the raw 2xx response body directly into <typeparamref name="T"/>.
        /// </summary>
        /// <remarks>
        /// On a 2xx response the body IS the payload (no envelope). An empty body maps to <c>default(T)</c>.
        /// On a non-2xx response the body <c>{ "error": "...", "detail"?: ... }</c> is read and thrown as a
        /// <see cref="LatticeApiException"/>. When <paramref name="nullOnNotFound"/> is set, a 404 returns
        /// <c>default(T)</c> instead of throwing (used by "read by id" style lookups).
        /// </remarks>
        internal async Task<T?> RequestJsonAsync<T>(
            string method,
            string path,
            object? data = null,
            Dictionary<string, string>? queryParams = null,
            bool nullOnNotFound = false,
            CancellationToken cancellationToken = default)
        {
            using HttpResponseMessage response = await SendCoreAsync(method, path, data, queryParams, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                if (nullOnNotFound && response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return default;
                }

                await ThrowApiExceptionAsync(response, cancellationToken).ConfigureAwait(false);
            }

            string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(body))
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(body, _jsonOptions);
        }

        /// <summary>
        /// Send an HTTP request that has no payload of interest and report whether it succeeded.
        /// </summary>
        /// <remarks>
        /// Returns <c>true</c> for a 2xx response. For a non-2xx response it throws a
        /// <see cref="LatticeApiException"/> when <paramref name="throwOnError"/> is <c>true</c> (the default),
        /// otherwise it returns <c>false</c> (used by HEAD existence checks and the health probe).
        /// </remarks>
        internal async Task<bool> RequestStatusAsync(
            string method,
            string path,
            object? data = null,
            Dictionary<string, string>? queryParams = null,
            bool throwOnError = true,
            CancellationToken cancellationToken = default)
        {
            using HttpResponseMessage response = await SendCoreAsync(method, path, data, queryParams, cancellationToken).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            if (throwOnError)
            {
                await ThrowApiExceptionAsync(response, cancellationToken).ConfigureAwait(false);
            }

            return false;
        }

        /// <summary>
        /// Build and send the HTTP request, translating transport failures into <see cref="LatticeConnectionException"/>.
        /// </summary>
        private async Task<HttpResponseMessage> SendCoreAsync(
            string method,
            string path,
            object? data,
            Dictionary<string, string>? queryParams,
            CancellationToken cancellationToken)
        {
            string url = _baseUrl + path;

            if (queryParams != null && queryParams.Count > 0)
            {
                string queryString = string.Join("&", queryParams.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
                url += "?" + queryString;
            }

            try
            {
                HttpRequestMessage request = new HttpRequestMessage(new HttpMethod(method), url);

                if (data != null && (method == "PUT" || method == "POST"))
                {
                    string jsonContent = JsonSerializer.Serialize(data, _jsonOptions);
                    request.Content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
                }

                return await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                throw new LatticeConnectionException($"Failed to connect to {url}", ex);
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                throw new LatticeConnectionException($"Request to {url} timed out", ex);
            }
        }

        /// <summary>
        /// Read the <c>{ error, detail? }</c> error body from a non-2xx response and throw a <see cref="LatticeApiException"/>.
        /// Falls back to the HTTP reason phrase when the body is missing or not the expected JSON shape.
        /// </summary>
        private async Task ThrowApiExceptionAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            int statusCode = (int)response.StatusCode;
            string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            string? errorMessage = null;
            string? detail = null;

            if (!string.IsNullOrWhiteSpace(body))
            {
                try
                {
                    ApiErrorResponse? parsed = JsonSerializer.Deserialize<ApiErrorResponse>(body, _jsonOptions);
                    if (parsed != null)
                    {
                        errorMessage = parsed.Error;
                        if (parsed.Detail != null)
                        {
                            detail = JsonSerializer.Serialize(parsed.Detail, _jsonOptions);
                        }
                    }
                }
                catch (JsonException)
                {
                    // Body was not the expected JSON shape; fall back to the status reason phrase below.
                }
            }

            if (string.IsNullOrEmpty(errorMessage))
            {
                errorMessage = response.ReasonPhrase ?? $"HTTP {statusCode}";
            }

            string message = string.IsNullOrEmpty(detail) || detail == "null"
                ? errorMessage!
                : $"{errorMessage} (detail: {detail})";

            throw new LatticeApiException(message, statusCode, message);
        }

        /// <summary>
        /// Get JSON serializer options.
        /// </summary>
        internal JsonSerializerOptions JsonOptions => _jsonOptions;

        /// <summary>
        /// Make an HTTP request and return the raw JSON body as a string (null on empty body or non-2xx).
        /// </summary>
        internal async Task<string?> RequestRawContentAsync(
            string method,
            string url,
            CancellationToken cancellationToken = default)
        {
            string fullUrl = _baseUrl + url;

            try
            {
                HttpRequestMessage request = new HttpRequestMessage(new HttpMethod(method), fullUrl);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                string responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

                return string.IsNullOrEmpty(responseContent) ? null : responseContent;
            }
            catch (HttpRequestException ex)
            {
                throw new LatticeConnectionException($"Failed to connect to {fullUrl}", ex);
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                throw new LatticeConnectionException($"Request to {fullUrl} timed out", ex);
            }
        }

        /// <summary>
        /// Dispose of the client resources.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _httpClient.Dispose();
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }
}
