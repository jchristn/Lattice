namespace Lattice.Server.Classes
{
    using System.Collections.Generic;
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
        public string? SqlExpression { get; set; }

        /// <summary>
        /// Structured search filters.
        /// </summary>
        public List<SearchFilterRequest>? Filters { get; set; }

        /// <summary>
        /// Label filters.
        /// </summary>
        public List<string>? Labels { get; set; }

        /// <summary>
        /// Tag filters.
        /// </summary>
        public Dictionary<string, string>? Tags { get; set; }

        /// <summary>
        /// Maximum number of results to return.
        /// </summary>
        public int? MaxResults { get; set; }

        /// <summary>
        /// Number of results to skip for pagination.
        /// </summary>
        public int? Skip { get; set; }

        /// <summary>
        /// Result ordering.
        /// </summary>
        public EnumerationOrderEnum? Ordering { get; set; }

        /// <summary>
        /// Whether to include document content in results.
        /// </summary>
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
