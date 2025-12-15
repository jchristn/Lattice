namespace Lattice.Core.Models
{
    using System;

    /// <summary>
    /// Maps a schema element key to its corresponding index table name.
    /// </summary>
    public class IndexTableMapping
    {
        #region Public-Members

        /// <summary>
        /// Unique identifier for the mapping (itm_{prettyid}).
        /// </summary>
        public string Id { get; set; } = null;

        /// <summary>
        /// The dot-notation key path (e.g., "Person.First").
        /// </summary>
        public string Key { get; set; } = null;

        /// <summary>
        /// The index table name (e.g., "index_{hash}").
        /// </summary>
        public string TableName { get; set; } = null;

        /// <summary>
        /// Timestamp when the mapping was created (UTC).
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public IndexTableMapping()
        {
        }

        #endregion
    }
}
