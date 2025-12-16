using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lattice.Sdk.Exceptions;
using Lattice.Sdk.Models;
using Lattice.Sdk.Methods;

namespace Lattice.Sdk
{
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
        /// Check if the Lattice server is healthy.
        /// </summary>
        public async Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                ResponseContext response = await RequestAsync("GET", "/v1.0/health", cancellationToken: cancellationToken);
                return response.Success;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Make an HTTP request to the Lattice API.
        /// </summary>
        internal async Task<ResponseContext> RequestAsync(
            string method,
            string path,
            object? data = null,
            Dictionary<string, string>? queryParams = null,
            CancellationToken cancellationToken = default)
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

                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);

                // For HEAD requests, we don't have a body
                if (method == "HEAD")
                {
                    return new ResponseContext
                    {
                        Success = response.IsSuccessStatusCode,
                        StatusCode = (int)response.StatusCode
                    };
                }

                string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!string.IsNullOrEmpty(responseContent))
                {
                    try
                    {
                        ResponseContext? context = JsonSerializer.Deserialize<ResponseContext>(responseContent, _jsonOptions);
                        if (context != null)
                        {
                            return context;
                        }
                    }
                    catch
                    {
                        // If we can't parse as ResponseContext, return a basic response
                    }
                }

                return new ResponseContext
                {
                    Success = response.IsSuccessStatusCode,
                    StatusCode = (int)response.StatusCode
                };
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
        /// Get JSON serializer options.
        /// </summary>
        internal JsonSerializerOptions JsonOptions => _jsonOptions;

        /// <summary>
        /// Make an HTTP request that returns raw JSON content (not wrapped in ResponseContext).
        /// </summary>
        internal async Task<JsonElement?> RequestRawContentAsync(
            string method,
            string url,
            CancellationToken cancellationToken = default)
        {
            string fullUrl = _baseUrl + url;

            try
            {
                HttpRequestMessage request = new HttpRequestMessage(new HttpMethod(method), fullUrl);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!string.IsNullOrEmpty(responseContent))
                {
                    using JsonDocument doc = JsonDocument.Parse(responseContent);
                    return doc.RootElement.Clone();
                }

                return null;
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
