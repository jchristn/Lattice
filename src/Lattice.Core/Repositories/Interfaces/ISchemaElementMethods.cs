namespace Lattice.Core.Repositories.Interfaces
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Lattice.Core.Models;

    /// <summary>
    /// Interface for schema element repository methods.
    /// </summary>
    public interface ISchemaElementMethods
    {
        /// <summary>
        /// Create a schema element.
        /// </summary>
        /// <param name="element">Schema element to create.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Created schema element.</returns>
        Task<SchemaElement> Create(SchemaElement element, CancellationToken token = default);

        /// <summary>
        /// Create multiple schema elements.
        /// </summary>
        /// <param name="elements">Schema elements to create.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Created schema elements.</returns>
        Task<List<SchemaElement>> CreateMany(List<SchemaElement> elements, CancellationToken token = default);

        /// <summary>
        /// Read a schema element by ID.
        /// </summary>
        /// <param name="id">Schema element ID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Schema element or null if not found.</returns>
        Task<SchemaElement> ReadById(string id, CancellationToken token = default);

        /// <summary>
        /// Read all schema elements for a schema.
        /// </summary>
        /// <param name="schemaId">Schema ID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Schema elements.</returns>
        IAsyncEnumerable<SchemaElement> ReadBySchemaId(string schemaId, CancellationToken token = default);

        /// <summary>
        /// Read a schema element by schema ID and key.
        /// </summary>
        /// <param name="schemaId">Schema ID.</param>
        /// <param name="key">Key path.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Schema element or null if not found.</returns>
        Task<SchemaElement> ReadBySchemaIdAndKey(string schemaId, string key, CancellationToken token = default);

        /// <summary>
        /// Delete all schema elements for a schema.
        /// </summary>
        /// <param name="schemaId">Schema ID.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteBySchemaId(string schemaId, CancellationToken token = default);
    }
}
