namespace Lattice.Sdk.Models
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents an entry in an index table.
    /// </summary>
    public class IndexTableEntry
    {
        /// <summary>
        /// Gets or sets the unique identifier for the entry.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the identifier of the document this entry belongs to.
        /// </summary>
        [JsonPropertyName("documentId")]
        public string DocumentId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the array position if this value is from an array element, null otherwise.
        /// </summary>
        [JsonPropertyName("position")]
        public int? Position { get; set; }

        /// <summary>
        /// Gets or sets the string representation of the value.
        /// </summary>
        [JsonPropertyName("value")]
        public string? Value { get; set; }

        /// <summary>
        /// Gets or sets the UTC timestamp when the entry was created.
        /// </summary>
        [JsonPropertyName("createdUtc")]
        public DateTime? CreatedUtc { get; set; }
    }
}
