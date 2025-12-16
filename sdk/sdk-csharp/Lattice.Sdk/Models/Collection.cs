using System.Text.Json.Serialization;

namespace Lattice.Sdk.Models
{
    /// <summary>
    /// Represents a Lattice collection.
    /// </summary>
    public class Collection
    {
        /// <summary>
        /// Gets or sets the unique identifier of the collection.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the collection.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description of the collection.
        /// </summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the directory path where documents are stored.
        /// </summary>
        [JsonPropertyName("documentsDirectory")]
        public string? DocumentsDirectory { get; set; }

        /// <summary>
        /// Gets or sets the labels associated with the collection.
        /// </summary>
        [JsonPropertyName("labels")]
        public List<string> Labels { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the tags (key-value pairs) associated with the collection.
        /// </summary>
        [JsonPropertyName("tags")]
        public Dictionary<string, string> Tags { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Gets or sets the UTC timestamp when the collection was created.
        /// </summary>
        [JsonPropertyName("createdUtc")]
        public DateTime? CreatedUtc { get; set; }

        /// <summary>
        /// Gets or sets the UTC timestamp when the collection was last updated.
        /// </summary>
        [JsonPropertyName("lastUpdateUtc")]
        public DateTime? LastUpdateUtc { get; set; }

        /// <summary>
        /// Gets or sets the schema enforcement mode for the collection.
        /// </summary>
        [JsonPropertyName("schemaEnforcementMode")]
        public SchemaEnforcementMode SchemaEnforcementMode { get; set; } = SchemaEnforcementMode.None;

        /// <summary>
        /// Gets or sets the indexing mode for the collection.
        /// </summary>
        [JsonPropertyName("indexingMode")]
        public IndexingMode IndexingMode { get; set; } = IndexingMode.All;
    }
}
