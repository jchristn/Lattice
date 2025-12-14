namespace Lattice.Core.Repositories.Interfaces
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Lattice.Core.Models;

    /// <summary>
    /// Interface for label repository methods (unified for collections and documents).
    /// </summary>
    public interface ILabelMethods
    {
        #region Common

        /// <summary>
        /// Create a label.
        /// </summary>
        /// <param name="label">Label to create.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Created label.</returns>
        Task<Label> Create(Label label, CancellationToken token = default);

        /// <summary>
        /// Create multiple labels.
        /// </summary>
        /// <param name="labels">Labels to create.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Created labels.</returns>
        Task<List<Label>> CreateMany(List<Label> labels, CancellationToken token = default);

        #endregion

        #region Document-Labels

        /// <summary>
        /// Read all labels for a document.
        /// </summary>
        /// <param name="documentId">Document ID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Labels.</returns>
        IAsyncEnumerable<Label> ReadByDocumentId(string documentId, CancellationToken token = default);

        /// <summary>
        /// Read all labels for multiple documents in a single query.
        /// </summary>
        /// <param name="documentIds">Document IDs.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Dictionary of document ID to list of label values.</returns>
        Task<Dictionary<string, List<string>>> ReadByDocumentIds(List<string> documentIds, CancellationToken token = default);

        /// <summary>
        /// Delete all labels for a document.
        /// </summary>
        /// <param name="documentId">Document ID.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteByDocumentId(string documentId, CancellationToken token = default);

        /// <summary>
        /// Find document IDs by label.
        /// </summary>
        /// <param name="labelValue">Label value to search for.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Document IDs.</returns>
        IAsyncEnumerable<string> FindDocumentIdsByLabel(string labelValue, CancellationToken token = default);

        /// <summary>
        /// Find document IDs that have ALL specified labels using JOIN queries.
        /// </summary>
        /// <param name="labels">Label values to search for (AND logic - documents must have all labels).</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Document IDs that have all specified labels.</returns>
        Task<HashSet<string>> FindDocumentIdsByLabels(List<string> labels, CancellationToken token = default);

        #endregion

        #region Collection-Labels

        /// <summary>
        /// Read all labels for a collection (collection-level labels only, not document labels).
        /// </summary>
        /// <param name="collectionId">Collection ID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Labels.</returns>
        IAsyncEnumerable<Label> ReadByCollectionId(string collectionId, CancellationToken token = default);

        /// <summary>
        /// Delete all labels for a collection (collection-level labels only, not document labels).
        /// </summary>
        /// <param name="collectionId">Collection ID.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteByCollectionId(string collectionId, CancellationToken token = default);

        #endregion
    }
}
