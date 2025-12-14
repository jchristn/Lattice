namespace Lattice.Core.Search
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;
    using Lattice.Core.Models;

    /// <summary>
    /// Represents the result of an enumeration operation.
    /// </summary>
    /// <typeparam name="T">Type of objects being enumerated.</typeparam>
    public class EnumerationResult<T>
    {
        #region Public-Members

        /// <summary>
        /// Whether the operation was successful.
        /// </summary>
        [JsonPropertyName("success")]
        public bool Success { get; set; } = true;

        /// <summary>
        /// Timestamp information for the operation.
        /// </summary>
        [JsonPropertyName("timestamp")]
        public Timestamp Timestamp { get; set; } = new Timestamp();

        /// <summary>
        /// Maximum results requested.
        /// </summary>
        [JsonPropertyName("maxResults")]
        public int MaxResults
        {
            get => _MaxResults;
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException(nameof(MaxResults));
                _MaxResults = value;
            }
        }

        /// <summary>
        /// Number of records skipped.
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
        /// Number of iterations required.
        /// </summary>
        [JsonPropertyName("iterationsRequired")]
        public int IterationsRequired
        {
            get => _IterationsRequired;
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException(nameof(IterationsRequired));
                _IterationsRequired = value;
            }
        }

        /// <summary>
        /// Continuation token for retrieving more results.
        /// </summary>
        [JsonPropertyName("continuationToken")]
        public string ContinuationToken { get; set; } = null;

        /// <summary>
        /// Whether this is the end of results.
        /// </summary>
        [JsonPropertyName("endOfResults")]
        public bool EndOfResults { get; set; } = true;

        /// <summary>
        /// Total number of matching records.
        /// </summary>
        [JsonPropertyName("totalRecords")]
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
        [JsonPropertyName("recordsRemaining")]
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
        /// The enumerated objects.
        /// </summary>
        [JsonPropertyName("objects")]
        [JsonPropertyOrder(999)]
        public List<T> Objects
        {
            get => _Objects;
            set => _Objects = value ?? new List<T>();
        }

        #endregion

        #region Private-Members

        private int _MaxResults = 100;
        private int _Skip = 0;
        private int _IterationsRequired = 1;
        private long _TotalRecords = 0;
        private long _RecordsRemaining = 0;
        private List<T> _Objects = new List<T>();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public EnumerationResult()
        {
        }

        #endregion
    }
}
