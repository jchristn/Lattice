namespace Lattice.Core.Repositories.Interfaces
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Lattice.Core.Models;

    /// <summary>
    /// Interface for field constraint repository methods.
    /// </summary>
    public interface IFieldConstraintMethods
    {
        /// <summary>
        /// Create multiple field constraints.
        /// </summary>
        /// <param name="constraints">Constraints to create.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Created constraints.</returns>
        Task<List<FieldConstraint>> CreateMany(List<FieldConstraint> constraints, CancellationToken token = default);

        /// <summary>
        /// Read all field constraints for a collection.
        /// </summary>
        /// <param name="collectionId">Collection ID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>List of field constraints.</returns>
        Task<List<FieldConstraint>> ReadByCollectionId(string collectionId, CancellationToken token = default);

        /// <summary>
        /// Delete all field constraints for a collection.
        /// </summary>
        /// <param name="collectionId">Collection ID.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteByCollectionId(string collectionId, CancellationToken token = default);
    }
}
