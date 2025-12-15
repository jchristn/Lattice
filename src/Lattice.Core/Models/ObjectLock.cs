namespace Lattice.Core.Models
{
    using System;

    /// <summary>
    /// Represents a lock on a document for concurrent access control.
    /// </summary>
    public class ObjectLock
    {
        #region Public-Members

        /// <summary>
        /// Unique identifier for the lock (lock_{prettyid}).
        /// </summary>
        public string Id { get; set; } = null;

        /// <summary>
        /// Identifier of the collection containing the document.
        /// </summary>
        public string CollectionId { get; set; } = null;

        /// <summary>
        /// Name of the document being locked.
        /// </summary>
        public string DocumentName { get; set; } = null;

        /// <summary>
        /// Hostname of the node owning the lock.
        /// </summary>
        public string Hostname { get; set; } = null;

        /// <summary>
        /// Timestamp when the lock was created (UTC).
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public ObjectLock()
        {
        }

        #endregion
    }
}
