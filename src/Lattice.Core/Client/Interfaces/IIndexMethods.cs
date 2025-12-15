namespace Lattice.Core.Client.Interfaces
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Lattice.Core.Models;

    /// <summary>
    /// Interface for index methods.
    /// </summary>
    public interface IIndexMethods
    {
        /// <summary>
        /// Get all index table mappings.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>List of index table mappings.</returns>
        Task<List<IndexTableMapping>> GetMappings(CancellationToken token = default);

        /// <summary>
        /// Get an index table mapping by key.
        /// </summary>
        /// <param name="key">Field key.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Index table mapping or null if not found.</returns>
        Task<IndexTableMapping> GetMappingByKey(string key, CancellationToken token = default);
    }
}
