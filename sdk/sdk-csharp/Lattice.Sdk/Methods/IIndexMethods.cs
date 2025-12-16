using Lattice.Sdk.Models;

namespace Lattice.Sdk.Methods
{
    /// <summary>
    /// Interface for index management methods.
    /// </summary>
    public interface IIndexMethods
    {
        /// <summary>
        /// Get all index table mappings.
        /// </summary>
        Task<List<IndexTableMapping>> GetMappingsAsync(CancellationToken cancellationToken = default);
    }
}
