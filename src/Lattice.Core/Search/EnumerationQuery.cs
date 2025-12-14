namespace Lattice.Core.Search
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents an enumeration query for paginated results.
    /// </summary>
    public class EnumerationQuery
    {
        #region Public-Members

        /// <summary>
        /// Collection ID to enumerate within.
        /// </summary>
        [JsonPropertyName("collectionId")]
        public string CollectionId { get; set; } = null;

        /// <summary>
        /// Maximum number of results to return.
        /// </summary>
        [JsonPropertyName("maxResults")]
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
        [JsonPropertyName("skip")]
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
        [JsonPropertyName("continuationToken")]
        public string ContinuationToken { get; set; } = null;

        /// <summary>
        /// Ordering for results.
        /// </summary>
        [JsonPropertyName("ordering")]
        public EnumerationOrderEnum Ordering { get; set; } = EnumerationOrderEnum.CreatedDescending;

        /// <summary>
        /// Search filters to apply.
        /// </summary>
        [JsonPropertyName("filters")]
        public List<SearchFilter> Filters
        {
            get => _Filters;
            set => _Filters = value ?? new List<SearchFilter>();
        }

        /// <summary>
        /// Include related data (e.g., document content).
        /// </summary>
        [JsonPropertyName("includeData")]
        public bool IncludeData { get; set; } = false;

        /// <summary>
        /// Prefix filter for name-based enumeration.
        /// </summary>
        [JsonPropertyName("prefix")]
        public string Prefix { get; set; } = null;

        /// <summary>
        /// Suffix filter for name-based enumeration.
        /// </summary>
        [JsonPropertyName("suffix")]
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
