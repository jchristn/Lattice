namespace Lattice.Core.Models
{
    using System;

    /// <summary>
    /// Searchable request history entry metadata.
    /// </summary>
    public class RequestHistoryEntry
    {
        /// <summary>
        /// Unique identifier for the request history entry.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        /// <summary>
        /// When the request was received.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When the request completed.
        /// </summary>
        public DateTime CompletedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Request classification.
        /// </summary>
        public string RequestType { get; set; } = "unknown";

        /// <summary>
        /// HTTP method.
        /// </summary>
        public string Method { get; set; } = "GET";

        /// <summary>
        /// Request path without query string.
        /// </summary>
        public string Path { get; set; } = "/";

        /// <summary>
        /// Full request URL including query string.
        /// </summary>
        public string Url { get; set; } = "/";

        /// <summary>
        /// Source IP address.
        /// </summary>
        public string SourceIp { get; set; } = "unknown";

        /// <summary>
        /// Associated collection identifier when available.
        /// </summary>
        public string CollectionId { get; set; } = null;

        /// <summary>
        /// Associated document identifier when available.
        /// </summary>
        public string DocumentId { get; set; } = null;

        /// <summary>
        /// Associated schema identifier when available.
        /// </summary>
        public string SchemaId { get; set; } = null;

        /// <summary>
        /// Associated table name when available.
        /// </summary>
        public string TableName { get; set; } = null;

        /// <summary>
        /// HTTP status code returned to the client.
        /// </summary>
        public int StatusCode { get; set; } = 200;

        /// <summary>
        /// Whether the request completed successfully.
        /// </summary>
        public bool Success { get; set; } = true;

        /// <summary>
        /// Processing time in milliseconds.
        /// </summary>
        public double ProcessingTimeMs { get; set; } = 0;

        /// <summary>
        /// Request body length in bytes.
        /// </summary>
        public long RequestBodyLength { get; set; } = 0;

        /// <summary>
        /// Response body length in bytes.
        /// </summary>
        public long ResponseBodyLength { get; set; } = 0;

        /// <summary>
        /// Whether the request body was truncated.
        /// </summary>
        public bool RequestBodyTruncated { get; set; } = false;

        /// <summary>
        /// Whether the response body was truncated.
        /// </summary>
        public bool ResponseBodyTruncated { get; set; } = false;

        /// <summary>
        /// Request content type when present.
        /// </summary>
        public string RequestContentType { get; set; } = null;

        /// <summary>
        /// Response content type when present.
        /// </summary>
        public string ResponseContentType { get; set; } = null;
    }
}
