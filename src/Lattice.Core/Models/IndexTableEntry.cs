namespace Lattice.Core.Models
{
    using System;

    /// <summary>
    /// Represents an entry in an index table.
    /// </summary>
    public class IndexTableEntry
    {
        #region Public-Members

        /// <summary>
        /// Unique identifier for the entry.
        /// </summary>
        public string Id { get; set; } = null;

        /// <summary>
        /// Identifier of the document this entry belongs to.
        /// </summary>
        public string DocumentId { get; set; } = null;

        /// <summary>
        /// Array position if this value is from an array element, null otherwise.
        /// </summary>
        public int? Position { get; set; } = null;

        /// <summary>
        /// The string representation of the value.
        /// </summary>
        public string Value { get; set; } = null;

        /// <summary>
        /// Timestamp when the entry was created (UTC).
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public IndexTableEntry()
        {
        }

        #endregion
    }
}
