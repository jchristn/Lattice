namespace Lattice.Core.Exceptions
{
    using System;

    /// <summary>
    /// Exception thrown when a document is locked by another process.
    /// </summary>
    public class DocumentLockedException : Exception
    {
        /// <summary>
        /// The collection ID where the lock exists.
        /// </summary>
        public string CollectionId { get; }

        /// <summary>
        /// The document name that is locked.
        /// </summary>
        public string DocumentName { get; }

        /// <summary>
        /// The hostname holding the lock.
        /// </summary>
        public string LockedByHostname { get; }

        /// <summary>
        /// When the lock was created.
        /// </summary>
        public DateTime LockCreatedUtc { get; }

        /// <summary>
        /// Create a new document locked exception.
        /// </summary>
        /// <param name="collectionId">Collection ID where the lock exists.</param>
        /// <param name="documentName">Document name that is locked.</param>
        /// <param name="lockedByHostname">Hostname holding the lock.</param>
        /// <param name="lockCreatedUtc">When the lock was created.</param>
        public DocumentLockedException(
            string collectionId,
            string documentName,
            string lockedByHostname,
            DateTime lockCreatedUtc)
            : base($"Document '{documentName}' in collection '{collectionId}' is locked by '{lockedByHostname}' since {lockCreatedUtc:O}")
        {
            CollectionId = collectionId;
            DocumentName = documentName;
            LockedByHostname = lockedByHostname;
            LockCreatedUtc = lockCreatedUtc;
        }
    }
}
