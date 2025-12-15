namespace Lattice.Core.Models
{
    using System;

    /// <summary>
    /// Represents a field that should be indexed for a collection with selective indexing.
    /// </summary>
    public class IndexedField
    {
        #region Public-Members

        /// <summary>
        /// Unique identifier for the indexed field (ixf_{prettyid}).
        /// </summary>
        public string Id { get; set; } = null;

        /// <summary>
        /// Foreign key to the collection this indexed field belongs to.
        /// </summary>
        public string CollectionId { get; set; } = null;

        /// <summary>
        /// Dot-notation path to the field to be indexed (e.g., "email", "user.name").
        /// </summary>
        public string FieldPath { get; set; } = null;

        /// <summary>
        /// Timestamp when the indexed field was created (UTC).
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when the indexed field was last updated (UTC).
        /// </summary>
        public DateTime LastUpdateUtc { get; set; } = DateTime.UtcNow;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public IndexedField()
        {
        }

        #endregion
    }
}
