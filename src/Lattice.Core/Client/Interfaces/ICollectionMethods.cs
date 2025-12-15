namespace Lattice.Core.Client.Interfaces
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Lattice.Core.Models;

    /// <summary>
    /// Interface for collection methods.
    /// </summary>
    public interface ICollectionMethods
    {
        /// <summary>
        /// Create a new collection.
        /// </summary>
        /// <param name="name">Collection name.</param>
        /// <param name="description">Collection description.</param>
        /// <param name="documentsDirectory">Directory for storing documents.</param>
        /// <param name="labels">Collection labels.</param>
        /// <param name="tags">Collection tags.</param>
        /// <param name="schemaEnforcementMode">Schema enforcement mode for documents.</param>
        /// <param name="fieldConstraints">Field constraints for schema validation.</param>
        /// <param name="indexingMode">Indexing mode for documents.</param>
        /// <param name="indexedFields">Fields to index (when indexingMode is Selective).</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Created collection.</returns>
        Task<Collection> Create(
            string name,
            string description = null,
            string documentsDirectory = null,
            List<string> labels = null,
            Dictionary<string, string> tags = null,
            SchemaEnforcementMode schemaEnforcementMode = SchemaEnforcementMode.None,
            List<FieldConstraint> fieldConstraints = null,
            IndexingMode indexingMode = IndexingMode.All,
            List<string> indexedFields = null,
            CancellationToken token = default);

        /// <summary>
        /// Get a collection by ID.
        /// </summary>
        /// <param name="id">Collection ID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Collection or null if not found.</returns>
        Task<Collection> ReadById(string id, CancellationToken token = default);

        /// <summary>
        /// Get all collections.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>List of collections.</returns>
        Task<List<Collection>> ReadAll(CancellationToken token = default);

        /// <summary>
        /// Delete a collection and all its documents.
        /// </summary>
        /// <param name="id">Collection ID.</param>
        /// <param name="token">Cancellation token.</param>
        Task Delete(string id, CancellationToken token = default);

        /// <summary>
        /// Check if a collection exists.
        /// </summary>
        /// <param name="id">Collection ID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>True if exists.</returns>
        Task<bool> Exists(string id, CancellationToken token = default);

        /// <summary>
        /// Get the field constraints for a collection.
        /// </summary>
        /// <param name="collectionId">Collection ID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>List of field constraints.</returns>
        Task<List<FieldConstraint>> GetConstraints(string collectionId, CancellationToken token = default);

        /// <summary>
        /// Get the indexed fields for a collection.
        /// </summary>
        /// <param name="collectionId">Collection ID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>List of indexed fields.</returns>
        Task<List<IndexedField>> GetIndexedFields(string collectionId, CancellationToken token = default);

        /// <summary>
        /// Update the schema constraints for a collection.
        /// Existing documents are NOT re-validated. New documents will be validated against the new schema.
        /// </summary>
        /// <param name="collectionId">Collection ID.</param>
        /// <param name="schemaEnforcementMode">New schema enforcement mode.</param>
        /// <param name="fieldConstraints">New field constraints (replaces existing).</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Updated collection.</returns>
        Task<Collection> UpdateConstraints(
            string collectionId,
            SchemaEnforcementMode schemaEnforcementMode,
            List<FieldConstraint> fieldConstraints = null,
            CancellationToken token = default);

        /// <summary>
        /// Update the indexing configuration for a collection.
        /// NOTE: Does not automatically rebuild indexes. Call RebuildIndexes() after this.
        /// </summary>
        /// <param name="collectionId">Collection ID.</param>
        /// <param name="indexingMode">New indexing mode.</param>
        /// <param name="indexedFields">Fields to index (when mode is Selective).</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Updated collection.</returns>
        Task<Collection> UpdateIndexing(
            string collectionId,
            IndexingMode indexingMode,
            List<string> indexedFields = null,
            CancellationToken token = default);

        /// <summary>
        /// Rebuild all indexes for a collection based on current IndexingMode.
        /// This is a potentially long-running operation for large collections.
        /// </summary>
        /// <param name="collectionId">Collection ID.</param>
        /// <param name="dropUnusedIndexes">Whether to drop index tables not in the indexed fields list.</param>
        /// <param name="progress">Progress reporter.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Rebuild result.</returns>
        Task<IndexRebuildResult> RebuildIndexes(
            string collectionId,
            bool dropUnusedIndexes = true,
            IProgress<IndexRebuildProgress> progress = null,
            CancellationToken token = default);
    }
}
