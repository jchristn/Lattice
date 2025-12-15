namespace Lattice.Core.Client.Interfaces
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Lattice.Core.Models;

    /// <summary>
    /// Interface for schema methods.
    /// </summary>
    public interface ISchemaMethods
    {
        /// <summary>
        /// Get all schemas.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>List of schemas.</returns>
        Task<List<Schema>> ReadAll(CancellationToken token = default);

        /// <summary>
        /// Get a schema by ID.
        /// </summary>
        /// <param name="id">Schema ID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Schema or null if not found.</returns>
        Task<Schema> ReadById(string id, CancellationToken token = default);

        /// <summary>
        /// Get schema elements for a schema.
        /// </summary>
        /// <param name="schemaId">Schema ID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>List of schema elements.</returns>
        Task<List<SchemaElement>> GetElements(string schemaId, CancellationToken token = default);
    }
}
