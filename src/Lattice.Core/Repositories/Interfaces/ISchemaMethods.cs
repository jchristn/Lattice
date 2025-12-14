namespace Lattice.Core.Repositories.Interfaces
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Lattice.Core.Models;

    /// <summary>
    /// Interface for schema repository methods.
    /// </summary>
    public interface ISchemaMethods
    {
        /// <summary>
        /// Create a schema.
        /// </summary>
        /// <param name="schema">Schema to create.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Created schema.</returns>
        Task<Schema> Create(Schema schema, CancellationToken token = default);

        /// <summary>
        /// Read a schema by ID.
        /// </summary>
        /// <param name="id">Schema ID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Schema or null if not found.</returns>
        Task<Schema> ReadById(string id, CancellationToken token = default);

        /// <summary>
        /// Read a schema by hash.
        /// </summary>
        /// <param name="hash">Schema hash.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Schema or null if not found.</returns>
        Task<Schema> ReadByHash(string hash, CancellationToken token = default);

        /// <summary>
        /// Read all schemas.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Schemas.</returns>
        IAsyncEnumerable<Schema> ReadAll(CancellationToken token = default);

        /// <summary>
        /// Update a schema.
        /// </summary>
        /// <param name="schema">Schema to update.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Updated schema.</returns>
        Task<Schema> Update(Schema schema, CancellationToken token = default);

        /// <summary>
        /// Delete a schema by ID.
        /// </summary>
        /// <param name="id">Schema ID.</param>
        /// <param name="token">Cancellation token.</param>
        Task Delete(string id, CancellationToken token = default);

        /// <summary>
        /// Check if a schema exists by hash.
        /// </summary>
        /// <param name="hash">Schema hash.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>True if exists.</returns>
        Task<bool> ExistsByHash(string hash, CancellationToken token = default);
    }
}
