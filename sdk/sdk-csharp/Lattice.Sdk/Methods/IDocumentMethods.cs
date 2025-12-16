using Lattice.Sdk.Models;

namespace Lattice.Sdk.Methods
{
    /// <summary>
    /// Interface for document management methods.
    /// </summary>
    public interface IDocumentMethods
    {
        /// <summary>
        /// Ingest a new document into a collection.
        /// </summary>
        Task<Document?> IngestAsync(
            string collectionId,
            object content,
            string? name = null,
            List<string>? labels = null,
            Dictionary<string, string>? tags = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all documents in a collection.
        /// </summary>
        Task<List<Document>> ReadAllInCollectionAsync(
            string collectionId,
            bool includeContent = false,
            bool includeLabels = true,
            bool includeTags = true,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get a document by ID.
        /// </summary>
        Task<Document?> ReadByIdAsync(
            string collectionId,
            string documentId,
            bool includeContent = false,
            bool includeLabels = true,
            bool includeTags = true,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Check if a document exists.
        /// </summary>
        Task<bool> ExistsAsync(string collectionId, string documentId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete a document.
        /// </summary>
        Task<bool> DeleteAsync(string collectionId, string documentId, CancellationToken cancellationToken = default);
    }
}
