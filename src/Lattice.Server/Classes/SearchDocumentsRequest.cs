namespace Lattice.Server.Classes
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;
    using Lattice.Core.Search;

    /// <summary>
    /// Request model for searching documents.
    /// </summary>
    public class SearchDocumentsRequest
    {
        #region Public-Members

        /// <summary>
        /// SQL-like expression for searching.
        /// </summary>
        [JsonPropertyName("sqlExpression")]
        public string? SqlExpression { get; set; }

        /// <summary>
        /// Structured search filters.
        /// </summary>
        [JsonPropertyName("filters")]
        public List<SearchFilterRequest>? Filters { get; set; }

        /// <summary>
        /// Label filters.
        /// </summary>
        [JsonPropertyName("labels")]
        public List<string>? Labels { get; set; }

        /// <summary>
        /// Tag filters.
        /// </summary>
        [JsonPropertyName("tags")]
        public Dictionary<string, string>? Tags { get; set; }

        /// <summary>
        /// Maximum number of results to return.
        /// </summary>
        [JsonPropertyName("maxResults")]
        public int? MaxResults { get; set; }

        /// <summary>
        /// Number of results to skip for pagination.
        /// </summary>
        [JsonPropertyName("skip")]
        public int? Skip { get; set; }

        /// <summary>
        /// Result ordering.
        /// </summary>
        [JsonPropertyName("ordering")]
        public EnumerationOrderEnum? Ordering { get; set; }

        /// <summary>
        /// Whether to include document content in results.
        /// </summary>
        [JsonPropertyName("includeContent")]
        public bool? IncludeContent { get; set; }

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public SearchDocumentsRequest()
        {
        }

        #endregion
    }
}
