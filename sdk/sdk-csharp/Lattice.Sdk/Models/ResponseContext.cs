using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lattice.Sdk.Models
{
    /// <summary>
    /// Represents the standard API response wrapper.
    /// </summary>
    public class ResponseContext
    {
        /// <summary>
        /// Gets or sets whether the API request was successful.
        /// </summary>
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the HTTP status code of the response.
        /// </summary>
        [JsonPropertyName("statusCode")]
        public int StatusCode { get; set; }

        /// <summary>
        /// Gets or sets the error message, if any.
        /// </summary>
        [JsonPropertyName("errorMessage")]
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the response data as a JSON element.
        /// </summary>
        [JsonPropertyName("data")]
        public JsonElement? Data { get; set; }

        /// <summary>
        /// Gets or sets the HTTP headers from the response.
        /// </summary>
        [JsonPropertyName("headers")]
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Gets or sets the server processing time in milliseconds.
        /// </summary>
        [JsonPropertyName("processingTimeMs")]
        public double ProcessingTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier for this request.
        /// </summary>
        [JsonPropertyName("guid")]
        public string? Guid { get; set; }

        /// <summary>
        /// Gets or sets the UTC timestamp of the response.
        /// </summary>
        [JsonPropertyName("timestampUtc")]
        public DateTime? TimestampUtc { get; set; }
    }

    /// <summary>
    /// Represents an index table mapping.
    /// </summary>
    public class IndexTableMapping
    {
        /// <summary>
        /// Gets or sets the key (field path) for this mapping.
        /// </summary>
        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the database table name for this index.
        /// </summary>
        [JsonPropertyName("tableName")]
        public string TableName { get; set; } = string.Empty;
    }

}
