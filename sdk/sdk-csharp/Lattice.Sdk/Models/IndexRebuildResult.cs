using System.Text.Json.Serialization;

namespace Lattice.Sdk.Models
{
    /// <summary>
    /// Represents the result of an index rebuild operation.
    /// </summary>
    public class IndexRebuildResult
    {
        /// <summary>
        /// Gets or sets the ID of the collection that was rebuilt.
        /// </summary>
        [JsonPropertyName("collectionId")]
        public string CollectionId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the number of documents processed during the rebuild.
        /// </summary>
        [JsonPropertyName("documentsProcessed")]
        public int DocumentsProcessed { get; set; }

        /// <summary>
        /// Gets or sets the number of indexes created during the rebuild.
        /// </summary>
        [JsonPropertyName("indexesCreated")]
        public int IndexesCreated { get; set; }

        /// <summary>
        /// Gets or sets the number of indexes dropped during the rebuild.
        /// </summary>
        [JsonPropertyName("indexesDropped")]
        public int IndexesDropped { get; set; }

        /// <summary>
        /// Gets or sets the number of index values inserted during the rebuild.
        /// </summary>
        [JsonPropertyName("valuesInserted")]
        public int ValuesInserted { get; set; }

        /// <summary>
        /// Gets or sets the formatted duration string of the rebuild operation.
        /// </summary>
        [JsonPropertyName("duration")]
        public string? Duration { get; set; }

        /// <summary>
        /// Gets or sets the duration of the rebuild operation in milliseconds.
        /// </summary>
        [JsonPropertyName("durationMs")]
        public double DurationMs { get; set; }

        /// <summary>
        /// Gets or sets any errors that occurred during the rebuild.
        /// </summary>
        [JsonPropertyName("errors")]
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets whether the rebuild operation was successful.
        /// </summary>
        [JsonPropertyName("success")]
        public bool Success { get; set; }
    }
}
