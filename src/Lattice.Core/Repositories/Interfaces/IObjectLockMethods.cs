namespace Lattice.Core.Repositories.Interfaces
{
    using System.Threading;
    using System.Threading.Tasks;
    using Lattice.Core.Models;

    /// <summary>
    /// Interface for object lock repository methods.
    /// </summary>
    public interface IObjectLockMethods
    {
        /// <summary>
        /// Try to acquire a lock on a document. Returns the existing lock if one exists.
        /// </summary>
        /// <param name="collectionId">Collection ID.</param>
        /// <param name="documentName">Document name.</param>
        /// <param name="hostname">Hostname requesting the lock.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Lock acquisition result containing success status and lock details.</returns>
        Task<LockAcquisitionResult> TryAcquireLock(
            string collectionId,
            string documentName,
            string hostname,
            CancellationToken token = default);

        /// <summary>
        /// Release a lock by its ID.
        /// </summary>
        /// <param name="lockId">Lock ID.</param>
        /// <param name="token">Cancellation token.</param>
        Task ReleaseLock(string lockId, CancellationToken token = default);

        /// <summary>
        /// Release a lock by collection ID and document name.
        /// </summary>
        /// <param name="collectionId">Collection ID.</param>
        /// <param name="documentName">Document name.</param>
        /// <param name="token">Cancellation token.</param>
        Task ReleaseLockByDocument(
            string collectionId,
            string documentName,
            CancellationToken token = default);

        /// <summary>
        /// Read a lock by collection ID and document name.
        /// </summary>
        /// <param name="collectionId">Collection ID.</param>
        /// <param name="documentName">Document name.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Object lock or null if not found.</returns>
        Task<ObjectLock> ReadByCollectionAndDocument(
            string collectionId,
            string documentName,
            CancellationToken token = default);

        /// <summary>
        /// Delete all expired locks.
        /// </summary>
        /// <param name="expirationSeconds">Lock expiration in seconds.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Number of locks deleted.</returns>
        Task<int> DeleteExpiredLocks(int expirationSeconds, CancellationToken token = default);
    }
}
