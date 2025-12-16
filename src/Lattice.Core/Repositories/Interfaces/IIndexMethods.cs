namespace Lattice.Core.Repositories.Interfaces
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Lattice.Core.Models;
    using Lattice.Core.Search;

    /// <summary>
    /// Interface for index table repository methods.
    /// </summary>
    public interface IIndexMethods
    {
        /// <summary>
        /// Create an index table mapping.
        /// </summary>
        /// <param name="mapping">Mapping to create.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Created mapping.</returns>
        Task<IndexTableMapping> CreateMapping(IndexTableMapping mapping, CancellationToken token = default);

        /// <summary>
        /// Get mapping by key.
        /// </summary>
        /// <param name="key">The key path.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Mapping or null if not found.</returns>
        Task<IndexTableMapping> GetMappingByKey(string key, CancellationToken token = default);

        /// <summary>
        /// Get all mappings.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>All mappings.</returns>
        IAsyncEnumerable<IndexTableMapping> GetAllMappings(CancellationToken token = default);

        /// <summary>
        /// Create an index table for a key.
        /// </summary>
        /// <param name="tableName">Table name.</param>
        /// <param name="token">Cancellation token.</param>
        Task CreateIndexTable(string tableName, CancellationToken token = default);

        /// <summary>
        /// Check if an index table exists.
        /// </summary>
        /// <param name="tableName">Table name.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>True if exists.</returns>
        Task<bool> IndexTableExists(string tableName, CancellationToken token = default);

        /// <summary>
        /// Insert a value into an index table.
        /// </summary>
        /// <param name="tableName">Index table name.</param>
        /// <param name="value">Value to insert.</param>
        /// <param name="token">Cancellation token.</param>
        Task InsertValue(string tableName, DocumentValue value, CancellationToken token = default);

        /// <summary>
        /// Insert multiple values into an index table.
        /// </summary>
        /// <param name="tableName">Index table name.</param>
        /// <param name="values">Values to insert.</param>
        /// <param name="token">Cancellation token.</param>
        Task InsertValues(string tableName, List<DocumentValue> values, CancellationToken token = default);

        /// <summary>
        /// Insert values into multiple index tables in a single transaction.
        /// </summary>
        /// <param name="valuesByTable">Dictionary of table name to list of values.</param>
        /// <param name="token">Cancellation token.</param>
        Task InsertValuesMultiTable(Dictionary<string, List<DocumentValue>> valuesByTable, CancellationToken token = default);

        /// <summary>
        /// Delete all values for a document from an index table.
        /// </summary>
        /// <param name="tableName">Index table name.</param>
        /// <param name="documentId">Document ID.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteByDocumentId(string tableName, string documentId, CancellationToken token = default);

        /// <summary>
        /// Search for document IDs matching a filter.
        /// </summary>
        /// <param name="tableName">Index table name.</param>
        /// <param name="filter">Search filter.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Matching document IDs.</returns>
        IAsyncEnumerable<string> Search(string tableName, SearchFilter filter, CancellationToken token = default);

        /// <summary>
        /// Drop an index table.
        /// </summary>
        /// <param name="tableName">Table name.</param>
        /// <param name="token">Cancellation token.</param>
        Task DropIndexTable(string tableName, CancellationToken token = default);

        /// <summary>
        /// Get mapping by table name.
        /// </summary>
        /// <param name="tableName">Table name.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Mapping or null if not found.</returns>
        Task<IndexTableMapping> GetMappingByTableName(string tableName, CancellationToken token = default);

        /// <summary>
        /// Get all index table names that contain values for documents in a collection.
        /// </summary>
        /// <param name="collectionId">Collection ID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>List of table names.</returns>
        Task<List<string>> GetIndexTablesForCollection(string collectionId, CancellationToken token = default);

        /// <summary>
        /// Delete all values for documents in a collection from all index tables.
        /// </summary>
        /// <param name="collectionId">Collection ID.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteValuesByCollectionId(string collectionId, CancellationToken token = default);

        /// <summary>
        /// Delete values from a specific index table for a collection.
        /// </summary>
        /// <param name="tableName">Index table name.</param>
        /// <param name="collectionId">Collection ID.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteValuesFromTable(string tableName, string collectionId, CancellationToken token = default);

        /// <summary>
        /// Get entries from an index table with pagination.
        /// </summary>
        /// <param name="tableName">Index table name.</param>
        /// <param name="skip">Number of entries to skip.</param>
        /// <param name="limit">Maximum number of entries to return.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>List of index table entries.</returns>
        Task<List<IndexTableEntry>> GetTableEntries(string tableName, int skip = 0, int limit = 100, CancellationToken token = default);

        /// <summary>
        /// Get the total count of entries in an index table.
        /// </summary>
        /// <param name="tableName">Index table name.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Total count of entries.</returns>
        Task<long> GetTableEntryCount(string tableName, CancellationToken token = default);
    }
}
