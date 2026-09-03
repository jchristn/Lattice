namespace Lattice.Sdk.Methods
{
    using Lattice.Sdk.Models;

    /// <summary>
    /// Interface for index management methods.
    /// </summary>
    public interface IIndexMethods
    {
        /// <summary>
        /// Get all index table mappings.
        /// </summary>
        /// <param name="maxResults">Optional maximum number of results to return per page.</param>
        /// <param name="skip">Optional number of records to skip.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<EnumerationResult<IndexTableMapping>?> GetMappingsAsync(
            int? maxResults = null,
            int? skip = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get the entries stored in an index table.
        /// </summary>
        /// <param name="tableName">The name of the index table.</param>
        /// <param name="maxResults">Optional maximum number of results to return per page.</param>
        /// <param name="skip">Optional number of records to skip.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<EnumerationResult<IndexTableEntry>?> GetTableEntriesAsync(
            string tableName,
            int? maxResults = null,
            int? skip = null,
            CancellationToken cancellationToken = default);
    }
}
