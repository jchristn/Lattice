namespace Lattice.Core.Models
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents a flattened value from a document stored in an index.
    /// </summary>
    public class DocumentValue
    {
        #region Public-Members

        /// <summary>
        /// Unique identifier for the value (val_{prettyid}).
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = null;

        /// <summary>
        /// Identifier of the document this value belongs to.
        /// </summary>
        [JsonPropertyName("documentId")]
        public string DocumentId { get; set; } = null;

        /// <summary>
        /// Identifier of the schema.
        /// </summary>
        [JsonPropertyName("schemaId")]
        public string SchemaId { get; set; } = null;

        /// <summary>
        /// Identifier of the schema element.
        /// </summary>
        [JsonPropertyName("schemaElementId")]
        public string SchemaElementId { get; set; } = null;

        /// <summary>
        /// Array position if this value is from an array element, null otherwise.
        /// </summary>
        [JsonPropertyName("position")]
        public int? Position { get; set; } = null;

        /// <summary>
        /// The string representation of the value.
        /// </summary>
        [JsonPropertyName("value")]
        public string Value { get; set; } = null;

        /// <summary>
        /// Timestamp when the value was created (UTC).
        /// </summary>
        [JsonPropertyName("createdUtc")]
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when the value was last updated (UTC).
        /// </summary>
        [JsonPropertyName("lastUpdateUtc")]
        public DateTime LastUpdateUtc { get; set; } = DateTime.UtcNow;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public DocumentValue()
        {
        }

        #endregion
    }
}
