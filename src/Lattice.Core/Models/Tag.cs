namespace Lattice.Core.Models
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents a key-value tag associated with a document.
    /// </summary>
    public class Tag
    {
        #region Public-Members

        /// <summary>
        /// Unique identifier for the tag (tag_{prettyid}).
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = null;

        /// <summary>
        /// Identifier of the collection this tag belongs to (null if document tag).
        /// </summary>
        [JsonPropertyName("collectionId")]
        public string CollectionId { get; set; } = null;

        /// <summary>
        /// Identifier of the document this tag belongs to (null if collection tag).
        /// </summary>
        [JsonPropertyName("documentId")]
        public string DocumentId { get; set; } = null;

        /// <summary>
        /// The tag key.
        /// </summary>
        [JsonPropertyName("key")]
        public string Key { get; set; } = null;

        /// <summary>
        /// The tag value.
        /// </summary>
        [JsonPropertyName("value")]
        public string Value { get; set; } = null;

        /// <summary>
        /// Timestamp when the tag was created (UTC).
        /// </summary>
        [JsonPropertyName("createdUtc")]
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when the tag was last updated (UTC).
        /// </summary>
        [JsonPropertyName("lastUpdateUtc")]
        public DateTime LastUpdateUtc { get; set; } = DateTime.UtcNow;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public Tag()
        {
        }

        #endregion
    }
}
