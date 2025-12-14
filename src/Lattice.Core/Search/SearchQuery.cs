namespace Lattice.Core.Search
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents a search query for documents.
    /// </summary>
    public class SearchQuery
    {
        #region Public-Members

        /// <summary>
        /// Collection ID to search within.
        /// </summary>
        [JsonPropertyName("collectionId")]
        public string CollectionId { get; set; } = null;

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
        /// Labels to filter by (documents must have all specified labels).
        /// </summary>
        [JsonPropertyName("labels")]
        public List<string> Labels
        {
            get => _Labels;
            set => _Labels = value ?? new List<string>();
        }

        /// <summary>
        /// Tags to filter by (documents must have all specified tags).
        /// </summary>
        [JsonPropertyName("tags")]
        public Dictionary<string, string> Tags
        {
            get => _Tags;
            set => _Tags = value ?? new Dictionary<string, string>();
        }

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
        /// SQL-like expression for querying (e.g., "Person.First = 'Joel' AND Person.Last = 'Christner'").
        /// </summary>
        [JsonPropertyName("sqlExpression")]
        public string SqlExpression { get; set; } = null;

        /// <summary>
        /// Include document content in results.
        /// </summary>
        [JsonPropertyName("includeContent")]
        public bool IncludeContent { get; set; } = false;

        /// <summary>
        /// Include document labels in results.
        /// </summary>
        [JsonPropertyName("includeLabels")]
        public bool IncludeLabels { get; set; } = true;

        /// <summary>
        /// Include document tags in results.
        /// </summary>
        [JsonPropertyName("includeTags")]
        public bool IncludeTags { get; set; } = true;

        #endregion

        #region Private-Members

        private List<SearchFilter> _Filters = new List<SearchFilter>();
        private List<string> _Labels = new List<string>();
        private Dictionary<string, string> _Tags = new Dictionary<string, string>();
        private int _MaxResults = 100;
        private int _Skip = 0;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public SearchQuery()
        {
        }

        #endregion
    }
}
