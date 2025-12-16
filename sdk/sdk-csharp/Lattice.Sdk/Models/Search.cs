using System.Text.Json.Serialization;

namespace Lattice.Sdk.Models
{
    /// <summary>
    /// Represents a search filter for API requests.
    /// </summary>
    public class SearchFilter
    {
        /// <summary>
        /// Gets or sets the field path to filter on.
        /// </summary>
        public string Field { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the filter condition operator.
        /// </summary>
        public SearchCondition Condition { get; set; } = SearchCondition.Equals;

        /// <summary>
        /// Gets or sets the value to compare against.
        /// </summary>
        public string? Value { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchFilter"/> class.
        /// </summary>
        public SearchFilter()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchFilter"/> class with the specified field, condition, and value.
        /// </summary>
        /// <param name="field">The field path to filter on.</param>
        /// <param name="condition">The filter condition operator.</param>
        /// <param name="value">The value to compare against.</param>
        public SearchFilter(string field, SearchCondition condition, string? value = null)
        {
            Field = field;
            Condition = condition;
            Value = value;
        }
    }

    /// <summary>
    /// Represents a search query.
    /// </summary>
    public class SearchQuery
    {
        /// <summary>
        /// Gets or sets the ID of the collection to search in.
        /// </summary>
        public string CollectionId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the list of filters to apply.
        /// </summary>
        public List<SearchFilter>? Filters { get; set; }

        /// <summary>
        /// Gets or sets the labels to filter by.
        /// </summary>
        public List<string>? Labels { get; set; }

        /// <summary>
        /// Gets or sets the tags to filter by.
        /// </summary>
        public Dictionary<string, string>? Tags { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of results to return.
        /// </summary>
        public int? MaxResults { get; set; }

        /// <summary>
        /// Gets or sets the number of results to skip.
        /// </summary>
        public int? Skip { get; set; }

        /// <summary>
        /// Gets or sets the ordering for results.
        /// </summary>
        public EnumerationOrder? Ordering { get; set; }

        /// <summary>
        /// Gets or sets whether to include document content in results.
        /// </summary>
        public bool IncludeContent { get; set; }
    }

    /// <summary>
    /// Represents search results.
    /// </summary>
    public class SearchResult
    {
        /// <summary>
        /// Gets or sets whether the search was successful.
        /// </summary>
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the timestamp information.
        /// </summary>
        [JsonPropertyName("timestamp")]
        public TimestampInfo? Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of results requested.
        /// </summary>
        [JsonPropertyName("maxResults")]
        public int? MaxResults { get; set; }

        /// <summary>
        /// Gets or sets the continuation token for pagination.
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
        public int TotalRecords { get; set; }

        /// <summary>
        /// Gets or sets the number of records remaining after this page.
        /// </summary>
        [JsonPropertyName("recordsRemaining")]
        public int RecordsRemaining { get; set; }

        /// <summary>
        /// Gets or sets the list of matching documents.
        /// </summary>
        [JsonPropertyName("documents")]
        public List<Document> Documents { get; set; } = new List<Document>();
    }

    /// <summary>
    /// Timestamp information in search results.
    /// </summary>
    public class TimestampInfo
    {
        /// <summary>
        /// Gets or sets the UTC timestamp.
        /// </summary>
        [JsonPropertyName("utc")]
        public DateTime? Utc { get; set; }
    }
}
