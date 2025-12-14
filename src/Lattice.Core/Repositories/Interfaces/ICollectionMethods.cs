namespace Lattice.Core.Repositories.Interfaces
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Lattice.Core.Models;

    /// <summary>
    /// Interface for collection repository methods.
    /// </summary>
    public interface ICollectionMethods
    {
        /// <summary>
        /// Create a collection.
        /// </summary>
        /// <param name="collection">Collection to create.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Created collection.</returns>
        Task<Collection> Create(Collection collection, CancellationToken token = default);

        /// <summary>
        /// Read a collection by ID.
        /// </summary>
        /// <param name="id">Collection ID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Collection or null if not found.</returns>
        Task<Collection> ReadById(string id, CancellationToken token = default);

        /// <summary>
        /// Read all collections.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>List of collections.</returns>
        IAsyncEnumerable<Collection> ReadAll(CancellationToken token = default);

        /// <summary>
        /// Update a collection.
        /// </summary>
        /// <param name="collection">Collection to update.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Updated collection.</returns>
        Task<Collection> Update(Collection collection, CancellationToken token = default);

        /// <summary>
        /// Delete a collection by ID.
        /// </summary>
        /// <param name="id">Collection ID.</param>
        /// <param name="token">Cancellation token.</param>
        Task Delete(string id, CancellationToken token = default);

        /// <summary>
        /// Check if a collection exists.
        /// </summary>
        /// <param name="id">Collection ID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>True if exists.</returns>
        Task<bool> Exists(string id, CancellationToken token = default);

        /// <summary>
        /// Get the count of collections.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Count.</returns>
        Task<long> Count(CancellationToken token = default);
    }
}
