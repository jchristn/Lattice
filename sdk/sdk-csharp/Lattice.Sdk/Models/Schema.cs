using System.Text.Json.Serialization;

namespace Lattice.Sdk.Models
{
    /// <summary>
    /// Represents a Lattice schema.
    /// </summary>
    public class Schema
    {
        /// <summary>
        /// Gets or sets the unique identifier of the schema.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the schema.
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the hash of the schema structure.
        /// </summary>
        [JsonPropertyName("hash")]
        public string? Hash { get; set; }

        /// <summary>
        /// Gets or sets the UTC timestamp when the schema was created.
        /// </summary>
        [JsonPropertyName("createdUtc")]
        public DateTime? CreatedUtc { get; set; }

        /// <summary>
        /// Gets or sets the UTC timestamp when the schema was last updated.
        /// </summary>
        [JsonPropertyName("lastUpdateUtc")]
        public DateTime? LastUpdateUtc { get; set; }
    }

    /// <summary>
    /// Represents an element within a schema.
    /// </summary>
    public class SchemaElement
    {
        /// <summary>
        /// Gets or sets the unique identifier of the schema element.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the ID of the schema this element belongs to.
        /// </summary>
        [JsonPropertyName("schemaId")]
        public string SchemaId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the position of this element in the schema.
        /// </summary>
        [JsonPropertyName("position")]
        public int Position { get; set; }

        /// <summary>
        /// Gets or sets the key (field name) of this element.
        /// </summary>
        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the data type of this element.
        /// </summary>
        [JsonPropertyName("dataType")]
        public string DataType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether this element can be null.
        /// </summary>
        [JsonPropertyName("nullable")]
        public bool Nullable { get; set; }

        /// <summary>
        /// Gets or sets the UTC timestamp when this element was created.
        /// </summary>
        [JsonPropertyName("createdUtc")]
        public DateTime? CreatedUtc { get; set; }

        /// <summary>
        /// Gets or sets the UTC timestamp when this element was last updated.
        /// </summary>
        [JsonPropertyName("lastUpdateUtc")]
        public DateTime? LastUpdateUtc { get; set; }
    }
}
