namespace Lattice.Core.Models
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents a collection of documents in the Lattice store.
    /// </summary>
    public class Collection
    {
        #region Public-Members

        /// <summary>
        /// Unique identifier for the collection (col_{prettyid}).
        /// </summary>
        public string Id { get; set; } = null;

        /// <summary>
        /// Name of the collection.
        /// </summary>
        public string Name { get; set; } = null;

        /// <summary>
        /// Description of the collection.
        /// </summary>
        public string Description { get; set; } = null;

        /// <summary>
        /// Directory path where documents for this collection are stored.
        /// </summary>
        public string DocumentsDirectory { get; set; } = null;

        /// <summary>
        /// Labels associated with the collection.
        /// </summary>
        public List<string> Labels
        {
            get => _Labels;
            set => _Labels = value ?? new List<string>();
        }

        /// <summary>
        /// Key-value tags associated with the collection.
        /// </summary>
        public Dictionary<string, string> Tags
        {
            get => _Tags;
            set => _Tags = value ?? new Dictionary<string, string>();
        }

        /// <summary>
        /// Timestamp when the collection was created (UTC).
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when the collection was last updated (UTC).
        /// </summary>
        public DateTime LastUpdateUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Schema enforcement mode for documents in this collection.
        /// </summary>
        public SchemaEnforcementMode SchemaEnforcementMode { get; set; } = SchemaEnforcementMode.None;

        /// <summary>
        /// Indexing mode for documents in this collection.
        /// </summary>
        public IndexingMode IndexingMode { get; set; } = IndexingMode.All;

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
