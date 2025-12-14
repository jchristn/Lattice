namespace Lattice.Core.Models
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents a label associated with a collection.
    /// </summary>
    public class CollectionLabel
    {
        #region Public-Members

        /// <summary>
        /// Unique identifier for the label (clb_{prettyid}).
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = null;

        /// <summary>
        /// Identifier of the collection this label belongs to.
        /// </summary>
        [JsonPropertyName("collectionId")]
        public string CollectionId { get; set; } = null;

        /// <summary>
        /// The label value.
        /// </summary>
        [JsonPropertyName("labelValue")]
        public string LabelValue { get; set; } = null;

        /// <summary>
        /// Timestamp when the label was created (UTC).
        /// </summary>
        [JsonPropertyName("createdUtc")]
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when the label was last updated (UTC).
        /// </summary>
        [JsonPropertyName("lastUpdateUtc")]
        public DateTime LastUpdateUtc { get; set; } = DateTime.UtcNow;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public CollectionLabel()
        {
        }

        #endregion
    }
}
