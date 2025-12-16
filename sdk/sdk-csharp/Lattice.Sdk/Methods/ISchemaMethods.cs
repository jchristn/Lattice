using Lattice.Sdk.Models;

namespace Lattice.Sdk.Methods
{
    /// <summary>
    /// Interface for schema management methods.
    /// </summary>
    public interface ISchemaMethods
    {
        /// <summary>
        /// Get all schemas.
        /// </summary>
        Task<List<Schema>> ReadAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Get a schema by ID.
        /// </summary>
        Task<Schema?> ReadByIdAsync(string schemaId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get elements for a schema.
        /// </summary>
        Task<List<SchemaElement>> GetElementsAsync(string schemaId, CancellationToken cancellationToken = default);
    }
}
