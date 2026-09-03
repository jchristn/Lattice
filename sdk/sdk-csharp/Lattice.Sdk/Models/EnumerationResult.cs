namespace Lattice.Sdk.Models
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents a paginated enumeration result returned by Lattice list endpoints.
    /// </summary>
    /// <typeparam name="T">The type of the enumerated objects.</typeparam>
    public class EnumerationResult<T>
    {
        /// <summary>
        /// Gets or sets whether the enumeration was successful.
        /// </summary>
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the timing information for the enumeration operation.
        /// </summary>
        [JsonPropertyName("timestamp")]
        public Timestamp? Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of results requested for this page.
        /// </summary>
        [JsonPropertyName("maxResults")]
        public int MaxResults { get; set; }

        /// <summary>
        /// Gets or sets the number of records skipped before this page.
        /// </summary>
        [JsonPropertyName("skip")]
        public int Skip { get; set; }

        /// <summary>
        /// Gets or sets the number of iterations required to produce this page.
        /// </summary>
        [JsonPropertyName("iterationsRequired")]
        public int IterationsRequired { get; set; }

        /// <summary>
        /// Gets or sets the continuation token for retrieving the next page, if any.
        /// </summary>
        [JsonPropertyName("continuationToken")]
        public string? ContinuationToken { get; set; }

        /// <summary>
        /// Gets or sets whether all results have been returned.
        /// </summary>
        [JsonPropertyName("endOfResults")]
        public bool EndOfResults { get; set; }

        /// <summary>
        /// Gets or sets the total number of matching records.
        /// </summary>
        [JsonPropertyName("totalRecords")]
        public long TotalRecords { get; set; }

        /// <summary>
        /// Gets or sets the number of records remaining after this page.
        /// </summary>
        [JsonPropertyName("recordsRemaining")]
        public long RecordsRemaining { get; set; }

        /// <summary>
        /// Gets or sets the list of enumerated objects for this page.
        /// </summary>
        [JsonPropertyName("objects")]
        public List<T> Objects { get; set; } = new List<T>();
    }
}
