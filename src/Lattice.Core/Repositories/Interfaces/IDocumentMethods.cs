namespace Lattice.Core.Repositories.Interfaces
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Lattice.Core.Models;
    using Lattice.Core.Search;

    /// <summary>
    /// Interface for document repository methods.
    /// </summary>
    public interface IDocumentMethods
    {
        /// <summary>
        /// Create a document.
        /// </summary>
        /// <param name="document">Document to create.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Created document.</returns>
        Task<Document> Create(Document document, CancellationToken token = default);

        /// <summary>
        /// Read a document by ID.
        /// </summary>
        /// <param name="id">Document ID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Document or null if not found.</returns>
        Task<Document> ReadById(string id, CancellationToken token = default);

        /// <summary>
        /// Read a document by ID with labels and tags in a single JOIN query.
        /// </summary>
        /// <param name="id">Document ID.</param>
        /// <param name="includeLabels">Include labels.</param>
        /// <param name="includeTags">Include tags.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Document with labels and tags populated, or null if not found.</returns>
        Task<Document> ReadByIdWithLabelsAndTags(string id, bool includeLabels = true, bool includeTags = true, CancellationToken token = default);

        /// <summary>
        /// Read multiple documents by their IDs in a single query.
        /// </summary>
        /// <param name="ids">Document IDs.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Dictionary of document ID to document (missing IDs are not included).</returns>
        Task<Dictionary<string, Document>> ReadByIds(List<string> ids, CancellationToken token = default);

        /// <summary>
        /// Read multiple documents by their IDs with labels and tags in a single JOIN query.
        /// </summary>
        /// <param name="ids">Document IDs.</param>
        /// <param name="includeLabels">Include labels.</param>
        /// <param name="includeTags">Include tags.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Dictionary of document ID to document with labels and tags populated.</returns>
        Task<Dictionary<string, Document>> ReadByIdsWithLabelsAndTags(List<string> ids, bool includeLabels = true, bool includeTags = true, CancellationToken token = default);

        /// <summary>
        /// Read all documents in a collection.
        /// </summary>
        /// <param name="collectionId">Collection ID.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">Number of records to skip.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Documents.</returns>
        IAsyncEnumerable<Document> ReadAllInCollection(
            string collectionId,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            CancellationToken token = default);

        /// <summary>
        /// Update a document.
        /// </summary>
        /// <param name="document">Document to update.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Updated document.</returns>
        Task<Document> Update(Document document, CancellationToken token = default);

        /// <summary>
        /// Delete a document by ID.
        /// </summary>
        /// <param name="id">Document ID.</param>
        /// <param name="token">Cancellation token.</param>
        Task Delete(string id, CancellationToken token = default);

        /// <summary>
        /// Delete all documents in a collection.
        /// </summary>
        /// <param name="collectionId">Collection ID.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteAllInCollection(string collectionId, CancellationToken token = default);

        /// <summary>
        /// Check if a document exists.
        /// </summary>
        /// <param name="id">Document ID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>True if exists.</returns>
        Task<bool> Exists(string id, CancellationToken token = default);

        /// <summary>
        /// Get the count of documents in a collection.
        /// </summary>
        /// <param name="collectionId">Collection ID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Count.</returns>
        Task<long> CountInCollection(string collectionId, CancellationToken token = default);

        /// <summary>
        /// Get the count of documents using a specific schema.
        /// </summary>
        /// <param name="schemaId">Schema ID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Count.</returns>
        Task<long> CountBySchemaId(string schemaId, CancellationToken token = default);

        /// <summary>
        /// Enumerate documents with pagination.
        /// </summary>
        /// <param name="query">Enumeration query.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Enumeration result.</returns>
        Task<EnumerationResult<Document>> Enumerate(EnumerationQuery query, CancellationToken token = default);
    }
}
