namespace Lattice.Sdk.Methods
{
    using Lattice.Sdk.Models;

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
        /// Ingest multiple documents into a collection in a single batch operation.
        /// </summary>
        Task<List<Document>?> IngestBatchAsync(
            string collectionId,
            List<BatchIngestDocument> documents,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all documents in a collection.
        /// </summary>
        /// <param name="collectionId">The collection identifier.</param>
        /// <param name="includeContent">Whether to include document content.</param>
        /// <param name="includeLabels">Whether to include document labels.</param>
        /// <param name="includeTags">Whether to include document tags.</param>
        /// <param name="maxResults">Optional maximum number of results to return per page.</param>
        /// <param name="skip">Optional number of records to skip.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<EnumerationResult<Document>?> ReadAllInCollectionAsync(
            string collectionId,
            bool includeContent = false,
            bool includeLabels = true,
            bool includeTags = true,
            int? maxResults = null,
            int? skip = null,
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
