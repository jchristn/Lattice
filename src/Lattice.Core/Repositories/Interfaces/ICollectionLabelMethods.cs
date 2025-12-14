namespace Lattice.Core.Repositories.Interfaces
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Lattice.Core.Models;

    /// <summary>
    /// Interface for collection label repository methods.
    /// </summary>
    public interface ICollectionLabelMethods
    {
        /// <summary>
        /// Create a collection label.
        /// </summary>
        /// <param name="label">Label to create.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Created label.</returns>
        Task<CollectionLabel> Create(CollectionLabel label, CancellationToken token = default);

        /// <summary>
        /// Create multiple labels.
        /// </summary>
        /// <param name="labels">Labels to create.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Created labels.</returns>
        Task<List<CollectionLabel>> CreateMany(List<CollectionLabel> labels, CancellationToken token = default);

        /// <summary>
        /// Read all labels for a collection.
        /// </summary>
        /// <param name="collectionId">Collection ID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Labels.</returns>
        IAsyncEnumerable<CollectionLabel> ReadByCollectionId(string collectionId, CancellationToken token = default);

        /// <summary>
        /// Delete all labels for a collection.
        /// </summary>
        /// <param name="collectionId">Collection ID.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteByCollectionId(string collectionId, CancellationToken token = default);
    }
}
