namespace Lattice.Core.Models
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents a single element (field) within a schema.
    /// </summary>
    public class SchemaElement
    {
        #region Public-Members

        /// <summary>
        /// Unique identifier for the schema element (sel_{prettyid}).
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = null;

        /// <summary>
        /// Identifier of the parent schema.
        /// </summary>
        [JsonPropertyName("schemaId")]
        public string SchemaId { get; set; } = null;

        /// <summary>
        /// Position/order of this element within the schema.
        /// </summary>
        [JsonPropertyName("position")]
        public int Position { get; set; } = 0;

        /// <summary>
        /// Dot-notation key path (e.g., "Person.First", "Person.Addresses").
        /// </summary>
        [JsonPropertyName("key")]
        public string Key { get; set; } = null;

        /// <summary>
        /// Inferred data type (string, number, boolean, null, array, object).
        /// </summary>
        [JsonPropertyName("dataType")]
        public string DataType { get; set; } = null;

        /// <summary>
        /// Whether this element can be null.
        /// </summary>
        [JsonPropertyName("nullable")]
        public bool Nullable { get; set; } = true;

        /// <summary>
        /// Timestamp when the element was created (UTC).
        /// </summary>
        [JsonPropertyName("createdUtc")]
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when the element was last updated (UTC).
        /// </summary>
        [JsonPropertyName("lastUpdateUtc")]
        public DateTime LastUpdateUtc { get; set; } = DateTime.UtcNow;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public SchemaElement()
        {
        }

        #endregion
    }
}
