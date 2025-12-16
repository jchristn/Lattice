using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lattice.Sdk.Models
{
    /// <summary>
    /// Represents a Lattice document.
    /// </summary>
    public class Document
    {
        /// <summary>
        /// Gets or sets the unique identifier of the document.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the ID of the collection this document belongs to.
        /// </summary>
        [JsonPropertyName("collectionId")]
        public string CollectionId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the ID of the schema associated with this document.
        /// </summary>
        [JsonPropertyName("schemaId")]
        public string SchemaId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the document.
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the labels associated with the document.
        /// </summary>
        [JsonPropertyName("labels")]
        public List<string> Labels { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the tags (key-value pairs) associated with the document.
        /// </summary>
        [JsonPropertyName("tags")]
        public Dictionary<string, string> Tags { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Gets or sets the UTC timestamp when the document was created.
        /// </summary>
        [JsonPropertyName("createdUtc")]
        public DateTime? CreatedUtc { get; set; }

        /// <summary>
        /// Gets or sets the UTC timestamp when the document was last updated.
        /// </summary>
        [JsonPropertyName("lastUpdateUtc")]
        public DateTime? LastUpdateUtc { get; set; }

        /// <summary>
        /// Gets or sets the JSON content of the document.
        /// </summary>
        [JsonPropertyName("content")]
        public JsonElement? Content { get; set; }

        /// <summary>
        /// Gets or sets the length of the document content in bytes.
        /// </summary>
        [JsonPropertyName("contentLength")]
        public int ContentLength { get; set; }

        /// <summary>
        /// Gets or sets the SHA-256 hash of the document content.
        /// </summary>
        [JsonPropertyName("sha256Hash")]
        public string? Sha256Hash { get; set; }
    }
}
