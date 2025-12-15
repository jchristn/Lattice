namespace Lattice.Core.Search
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;
    using Lattice.Core.Models;

    /// <summary>
    /// Represents the result of a search operation.
    /// </summary>
    public class SearchResult
    {
        #region Public-Members

        /// <summary>
        /// Whether the search was successful.
        /// </summary>
        public bool Success { get; set; } = true;

        /// <summary>
        /// Timestamp information for the operation.
        /// </summary>
        public Timestamp Timestamp { get; set; } = new Timestamp();

        /// <summary>
        /// Maximum results requested.
        /// </summary>
        public int MaxResults
        {
            get => _MaxResults;
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(MaxResults));
                _MaxResults = value;
            }
        }

        /// <summary>
        /// Continuation token for retrieving more results.
        /// </summary>
        public string ContinuationToken { get; set; } = null;

        /// <summary>
        /// Whether this is the end of results.
        /// </summary>
        public bool EndOfResults { get; set; } = true;

        /// <summary>
        /// Total number of matching records.
        /// </summary>
        public long TotalRecords
        {
            get => _TotalRecords;
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(TotalRecords));
                _TotalRecords = value;
            }
        }

        /// <summary>
        /// Number of records remaining.
        /// </summary>
        public long RecordsRemaining
        {
            get => _RecordsRemaining;
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(RecordsRemaining));
                _RecordsRemaining = value;
            }
        }

        /// <summary>
        /// Documents matching the search.
        /// </summary>
        [JsonPropertyOrder(999)]
        public List<Document> Documents
        {
            get => _Documents;
            set => _Documents = value ?? new List<Document>();
        }

        #endregion

        #region Private-Members

        private int _MaxResults = 0;
        private long _TotalRecords = 0;
        private long _RecordsRemaining = 0;
        private List<Document> _Documents = new List<Document>();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public SearchResult()
        {
        }

        #endregion
    }
}
