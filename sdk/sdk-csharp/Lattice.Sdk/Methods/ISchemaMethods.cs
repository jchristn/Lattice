namespace Lattice.Sdk.Methods
{
    using Lattice.Sdk.Models;

    /// <summary>
    /// Interface for schema management methods.
    /// </summary>
    public interface ISchemaMethods
    {
        /// <summary>
        /// Get all schemas.
        /// </summary>
        /// <param name="maxResults">Optional maximum number of results to return per page.</param>
        /// <param name="skip">Optional number of records to skip.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<EnumerationResult<Schema>?> ReadAllAsync(
            int? maxResults = null,
            int? skip = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get a schema by ID.
        /// </summary>
        Task<Schema?> ReadByIdAsync(string schemaId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get elements for a schema.
        /// </summary>
        /// <param name="schemaId">The schema identifier.</param>
        /// <param name="maxResults">Optional maximum number of results to return per page.</param>
        /// <param name="skip">Optional number of records to skip.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<EnumerationResult<SchemaElement>?> GetElementsAsync(
            string schemaId,
            int? maxResults = null,
            int? skip = null,
            CancellationToken cancellationToken = default);
    }
}
