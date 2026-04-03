namespace Lattice.Core.Models
{
    using System;

    /// <summary>
    /// Filter criteria for request history search.
    /// </summary>
    public class RequestHistorySearchFilter
    {
        /// <summary>
        /// Request type filter.
        /// </summary>
        public string RequestType { get; set; } = null;

        /// <summary>
        /// HTTP method filter.
        /// </summary>
        public string Method { get; set; } = null;

        /// <summary>
        /// Path substring filter.
        /// </summary>
        public string PathContains { get; set; } = null;

        /// <summary>
        /// Collection identifier filter.
        /// </summary>
        public string CollectionId { get; set; } = null;

        /// <summary>
        /// Document identifier filter.
        /// </summary>
        public string DocumentId { get; set; } = null;

        /// <summary>
        /// Schema identifier filter.
        /// </summary>
        public string SchemaId { get; set; } = null;

        /// <summary>
        /// Table name filter.
        /// </summary>
        public string TableName { get; set; } = null;

        /// <summary>
        /// Source IP filter.
        /// </summary>
        public string SourceIp { get; set; } = null;

        /// <summary>
        /// HTTP status code filter.
        /// </summary>
        public int? StatusCode { get; set; } = null;

        /// <summary>
        /// Success filter.
        /// </summary>
        public bool? Success { get; set; } = null;

        /// <summary>
        /// Inclusive start timestamp filter.
        /// </summary>
        public DateTime? StartUtc { get; set; } = null;

        /// <summary>
        /// Inclusive end timestamp filter.
        /// </summary>
        public DateTime? EndUtc { get; set; } = null;

        /// <summary>
        /// 1-based page number.
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// Page size.
        /// </summary>
        public int PageSize { get; set; } = 25;
    }
}
