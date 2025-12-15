namespace Lattice.Core.Client.Interfaces
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Lattice.Core.Models;

    /// <summary>
    /// Interface for document methods.
    /// </summary>
    public interface IDocumentMethods
    {
        /// <summary>
        /// Ingest a JSON document into a collection.
        /// </summary>
        /// <param name="collectionId">Collection ID.</param>
        /// <param name="json">JSON document content.</param>
        /// <param name="name">Document name.</param>
        /// <param name="labels">Document labels.</param>
        /// <param name="tags">Document tags.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Ingested document.</returns>
        Task<Document> Ingest(
            string collectionId,
            string json,
            string name = null,
            List<string> labels = null,
            Dictionary<string, string> tags = null,
            CancellationToken token = default);

        /// <summary>
        /// Get a document by ID.
        /// </summary>
        /// <param name="id">Document ID.</param>
        /// <param name="includeContent">Include raw JSON content.</param>
        /// <param name="includeLabels">Include document labels.</param>
        /// <param name="includeTags">Include document tags.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Document or null if not found.</returns>
        Task<Document> ReadById(
            string id,
            bool includeContent = false,
            bool includeLabels = true,
            bool includeTags = true,
            CancellationToken token = default);

        /// <summary>
        /// Get multiple documents by their IDs in a single optimized query.
        /// </summary>
        /// <param name="ids">Document IDs.</param>
        /// <param name="includeContent">Include raw JSON content.</param>
        /// <param name="includeLabels">Include document labels.</param>
        /// <param name="includeTags">Include document tags.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Dictionary of document ID to document (missing IDs are not included).</returns>
        Task<Dictionary<string, Document>> ReadByIds(
            List<string> ids,
            bool includeContent = false,
            bool includeLabels = true,
            bool includeTags = true,
            CancellationToken token = default);

        /// <summary>
        /// Get all documents in a collection.
        /// </summary>
        /// <param name="collectionId">Collection ID.</param>
        /// <param name="includeLabels">Include document labels.</param>
        /// <param name="includeTags">Include document tags.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>List of documents.</returns>
        Task<List<Document>> ReadAllInCollection(
            string collectionId,
            bool includeLabels = true,
            bool includeTags = true,
            CancellationToken token = default);

        /// <summary>
        /// Delete a document.
        /// </summary>
        /// <param name="id">Document ID.</param>
        /// <param name="token">Cancellation token.</param>
        Task Delete(string id, CancellationToken token = default);

        /// <summary>
        /// Check if a document exists.
        /// </summary>
        /// <param name="id">Document ID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>True if exists.</returns>
        Task<bool> Exists(string id, CancellationToken token = default);
    }
}
