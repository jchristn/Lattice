namespace Lattice.Core.Repositories.Interfaces
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Lattice.Core.Models;

    /// <summary>
    /// Interface for indexed field repository methods.
    /// </summary>
    public interface IIndexedFieldMethods
    {
        /// <summary>
        /// Create multiple indexed fields.
        /// </summary>
        /// <param name="fields">Indexed fields to create.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Created indexed fields.</returns>
        Task<List<IndexedField>> CreateMany(List<IndexedField> fields, CancellationToken token = default);

        /// <summary>
        /// Read all indexed fields for a collection.
        /// </summary>
        /// <param name="collectionId">Collection ID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>List of indexed fields.</returns>
        Task<List<IndexedField>> ReadByCollectionId(string collectionId, CancellationToken token = default);

        /// <summary>
        /// Delete all indexed fields for a collection.
        /// </summary>
        /// <param name="collectionId">Collection ID.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteByCollectionId(string collectionId, CancellationToken token = default);
    }
}
