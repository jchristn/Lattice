namespace Lattice.Core.Repositories.Interfaces
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Lattice.Core.Models;

    /// <summary>
    /// Interface for tag repository methods (unified for collections and documents).
    /// </summary>
    public interface ITagMethods
    {
        #region Document-Tags

        /// <summary>
        /// Create a tag.
        /// </summary>
        /// <param name="tag">Tag to create.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Created tag.</returns>
        Task<Tag> Create(Tag tag, CancellationToken token = default);

        /// <summary>
        /// Create multiple tags.
        /// </summary>
        /// <param name="tags">Tags to create.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Created tags.</returns>
        Task<List<Tag>> CreateMany(List<Tag> tags, CancellationToken token = default);

        /// <summary>
        /// Read all tags for a document.
        /// </summary>
        /// <param name="documentId">Document ID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Tags.</returns>
        IAsyncEnumerable<Tag> ReadByDocumentId(string documentId, CancellationToken token = default);

        /// <summary>
        /// Read all tags for multiple documents in a single query.
        /// </summary>
        /// <param name="documentIds">Document IDs.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Dictionary of document ID to dictionary of tag key-value pairs.</returns>
        Task<Dictionary<string, Dictionary<string, string>>> ReadByDocumentIds(List<string> documentIds, CancellationToken token = default);

        /// <summary>
        /// Delete all tags for a document.
        /// </summary>
        /// <param name="documentId">Document ID.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteByDocumentId(string documentId, CancellationToken token = default);

        /// <summary>
        /// Find document IDs by tag key and value.
        /// </summary>
        /// <param name="key">Tag key.</param>
        /// <param name="value">Tag value.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Document IDs.</returns>
        IAsyncEnumerable<string> FindDocumentIdsByTag(string key, string value, CancellationToken token = default);

        /// <summary>
        /// Find document IDs that have ALL specified tags using JOIN queries.
        /// </summary>
        /// <param name="tags">Tag key-value pairs to search for (AND logic - documents must have all tags).</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Document IDs that have all specified tags.</returns>
        Task<HashSet<string>> FindDocumentIdsByTags(Dictionary<string, string> tags, CancellationToken token = default);

        #endregion

        #region Collection-Tags

        /// <summary>
        /// Read all tags for a collection.
        /// </summary>
        /// <param name="collectionId">Collection ID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Tags.</returns>
        IAsyncEnumerable<Tag> ReadByCollectionId(string collectionId, CancellationToken token = default);

        /// <summary>
        /// Delete all tags for a collection.
        /// </summary>
        /// <param name="collectionId">Collection ID.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteByCollectionId(string collectionId, CancellationToken token = default);

        #endregion
    }
}
