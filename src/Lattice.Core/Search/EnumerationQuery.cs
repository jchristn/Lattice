namespace Lattice.Core.Search
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents an enumeration query for paginated results.
    /// </summary>
    public class EnumerationQuery
    {
        #region Public-Members

        /// <summary>
        /// Collection ID to enumerate within.
        /// </summary>
        public string CollectionId { get; set; } = null;

        /// <summary>
        /// Maximum number of results to return.
        /// </summary>
        public int MaxResults
        {
            get => _MaxResults;
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException(nameof(MaxResults));
                if (value > 1000) value = 1000;
                _MaxResults = value;
            }
        }

        /// <summary>
        /// Number of records to skip.
        /// </summary>
        public int Skip
        {
            get => _Skip;
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(Skip));
                _Skip = value;
            }
        }

        /// <summary>
        /// Continuation token for pagination.
        /// </summary>
        public string ContinuationToken { get; set; } = null;

        /// <summary>
        /// Ordering for results.
        /// </summary>
        public EnumerationOrderEnum Ordering { get; set; } = EnumerationOrderEnum.CreatedDescending;

        /// <summary>
        /// Search filters to apply.
        /// </summary>
        public List<SearchFilter> Filters
        {
            get => _Filters;
            set => _Filters = value ?? new List<SearchFilter>();
        }

        /// <summary>
        /// Include related data (e.g., document content).
        /// </summary>
        public bool IncludeData { get; set; } = false;

        /// <summary>
        /// Include document labels in results.
        /// </summary>
        public bool IncludeLabels { get; set; } = true;

        /// <summary>
        /// Include document tags in results.
        /// </summary>
        public bool IncludeTags { get; set; } = true;

        /// <summary>
        /// Prefix filter for name-based enumeration.
        /// </summary>
        public string Prefix { get; set; } = null;

        /// <summary>
        /// Suffix filter for name-based enumeration.
        /// </summary>
        public string Suffix { get; set; } = null;

        #endregion

        #region Private-Members

        private int _MaxResults = 100;
        private int _Skip = 0;
        private List<SearchFilter> _Filters = new List<SearchFilter>();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public EnumerationQuery()
        {
        }

        #endregion
    }
}
