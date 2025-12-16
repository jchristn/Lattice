using Lattice.Sdk.Models;

namespace Lattice.Sdk.Methods
{
    /// <summary>
    /// Interface for collection management methods.
    /// </summary>
    public interface ICollectionMethods
    {
        /// <summary>
        /// Create a new collection.
        /// </summary>
        Task<Collection?> CreateAsync(
            string name,
            string? description = null,
            string? documentsDirectory = null,
            List<string>? labels = null,
            Dictionary<string, string>? tags = null,
            SchemaEnforcementMode schemaEnforcementMode = SchemaEnforcementMode.None,
            List<FieldConstraint>? fieldConstraints = null,
            IndexingMode indexingMode = IndexingMode.All,
            List<string>? indexedFields = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all collections.
        /// </summary>
        Task<List<Collection>> ReadAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Get a collection by ID.
        /// </summary>
        Task<Collection?> ReadByIdAsync(string collectionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Check if a collection exists.
        /// </summary>
        Task<bool> ExistsAsync(string collectionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete a collection.
        /// </summary>
        Task<bool> DeleteAsync(string collectionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get field constraints for a collection.
        /// </summary>
        Task<ConstraintsResponse?> GetConstraintsAsync(string collectionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update constraints for a collection.
        /// </summary>
        Task<bool> UpdateConstraintsAsync(
            string collectionId,
            SchemaEnforcementMode schemaEnforcementMode,
            List<FieldConstraint>? fieldConstraints = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get indexed fields for a collection.
        /// </summary>
        Task<List<IndexedField>> GetIndexedFieldsAsync(string collectionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update indexing configuration for a collection.
        /// </summary>
        Task<bool> UpdateIndexingAsync(
            string collectionId,
            IndexingMode indexingMode,
            List<string>? indexedFields = null,
            bool rebuildIndexes = false,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Rebuild indexes for a collection.
        /// </summary>
        Task<IndexRebuildResult?> RebuildIndexesAsync(
            string collectionId,
            bool dropUnusedIndexes = true,
            CancellationToken cancellationToken = default);
    }
}
