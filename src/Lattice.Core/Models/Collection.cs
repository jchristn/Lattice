namespace Lattice.Core.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents a collection of documents in the Lattice store.
    /// </summary>
    public class Collection
    {
        #region Public-Members

        /// <summary>
        /// Unique identifier for the collection (col_{prettyid}).
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = null;

        /// <summary>
        /// Name of the collection.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = null;

        /// <summary>
        /// Description of the collection.
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; } = null;

        /// <summary>
        /// Directory path where documents for this collection are stored.
        /// </summary>
        [JsonPropertyName("documentsDirectory")]
        public string DocumentsDirectory { get; set; } = null;

        /// <summary>
        /// Labels associated with the collection.
        /// </summary>
        [JsonPropertyName("labels")]
        public List<string> Labels
        {
            get => _Labels;
            set => _Labels = value ?? new List<string>();
        }

        /// <summary>
        /// Key-value tags associated with the collection.
        /// </summary>
        [JsonPropertyName("tags")]
        public Dictionary<string, string> Tags
        {
            get => _Tags;
            set => _Tags = value ?? new Dictionary<string, string>();
        }

        /// <summary>
        /// Timestamp when the collection was created (UTC).
        /// </summary>
        [JsonPropertyName("createdUtc")]
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when the collection was last updated (UTC).
        /// </summary>
        [JsonPropertyName("lastUpdateUtc")]
        public DateTime LastUpdateUtc { get; set; } = DateTime.UtcNow;

        #endregion

        #region Private-Members

        private List<string> _Labels = new List<string>();
        private Dictionary<string, string> _Tags = new Dictionary<string, string>();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public Collection()
        {
        }

        #endregion
    }
}
