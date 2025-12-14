namespace Lattice.Core.Models
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents an inferred JSON schema in Lattice.
    /// </summary>
    public class Schema
    {
        #region Public-Members

        /// <summary>
        /// Unique identifier for the schema (sch_{prettyid}).
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = null;

        /// <summary>
        /// Optional name for the schema.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = null;

        /// <summary>
        /// Hash of the schema elements for deduplication.
        /// </summary>
        [JsonPropertyName("hash")]
        public string Hash { get; set; } = null;

        /// <summary>
        /// Timestamp when the schema was created (UTC).
        /// </summary>
        [JsonPropertyName("createdUtc")]
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when the schema was last updated (UTC).
        /// </summary>
        [JsonPropertyName("lastUpdateUtc")]
        public DateTime LastUpdateUtc { get; set; } = DateTime.UtcNow;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public Schema()
        {
        }

        #endregion
    }
}
