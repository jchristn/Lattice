namespace Lattice.Server.Classes
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;

    /// <summary>
    /// Request context containing metadata about the incoming request.
    /// </summary>
    public class RequestContext
    {
        #region Public-Members

        /// <summary>
        /// Request creation time in UTC.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Unique identifier for this request.
        /// </summary>
        public string Guid { get; set; } = System.Guid.NewGuid().ToString();

        /// <summary>
        /// API version.
        /// </summary>
        public string ApiVersion { get; set; } = "1.0";

        /// <summary>
        /// Request type.
        /// </summary>
        public RequestTypeEnum RequestType { get; set; } = RequestTypeEnum.Unknown;

        /// <summary>
        /// Client IP address.
        /// </summary>
        public string? IpAddress { get; set; } = null;

        /// <summary>
        /// HTTP method.
        /// </summary>
        public string? Method { get; set; } = null;

        /// <summary>
        /// Full URL of the request.
        /// </summary>
        public string? Url { get; set; } = null;

        /// <summary>
        /// Request path without the query string.
        /// </summary>
        public string? Path { get; set; } = null;

        /// <summary>
        /// Query parameters.
        /// </summary>
        public NameValueCollection QueryParams { get; set; } = new NameValueCollection();

        /// <summary>
        /// Request headers.
        /// </summary>
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Request body text when present.
        /// </summary>
        public string? RequestBody { get; set; } = null;

        /// <summary>
        /// Collection identifier when present in the route.
        /// </summary>
        public string? CollectionId { get; set; } = null;

        /// <summary>
        /// Document identifier when present in the route.
        /// </summary>
        public string? DocumentId { get; set; } = null;

        /// <summary>
        /// Schema identifier when present in the route.
        /// </summary>
        public string? SchemaId { get; set; } = null;

        /// <summary>
        /// Table name when present in the route.
        /// </summary>
        public string? TableName { get; set; } = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public RequestContext()
        {
        }

        #endregion
    }
}
