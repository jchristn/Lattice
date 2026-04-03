namespace Lattice.Core.Models
{
    using System.Collections.Generic;

    /// <summary>
    /// Paged request history search result.
    /// </summary>
    public class RequestHistorySearchResult
    {
        /// <summary>
        /// Search result data.
        /// </summary>
        public List<RequestHistoryEntry> Data
        {
            get => _Data;
            set => _Data = value ?? new List<RequestHistoryEntry>();
        }

        /// <summary>
        /// Current page number.
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// Page size.
        /// </summary>
        public int PageSize { get; set; } = 25;

        /// <summary>
        /// Total matching record count.
        /// </summary>
        public long TotalCount { get; set; } = 0;

        /// <summary>
        /// Total page count.
        /// </summary>
        public int TotalPages => PageSize < 1 ? 0 : (int)((TotalCount + PageSize - 1) / PageSize);

        private List<RequestHistoryEntry> _Data = new List<RequestHistoryEntry>();
    }
}
