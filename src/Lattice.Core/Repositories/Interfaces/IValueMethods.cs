namespace Lattice.Core.Repositories.Interfaces
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Lattice.Core.Models;

    /// <summary>
    /// Interface for document value repository methods.
    /// </summary>
    public interface IValueMethods
    {
        /// <summary>
        /// Create a document value.
        /// </summary>
        /// <param name="value">Document value to create.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Created document value.</returns>
        Task<DocumentValue> Create(DocumentValue value, CancellationToken token = default);

        /// <summary>
        /// Create multiple document values.
        /// </summary>
        /// <param name="values">Document values to create.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Created document values.</returns>
        Task<List<DocumentValue>> CreateMany(List<DocumentValue> values, CancellationToken token = default);

        /// <summary>
        /// Read all values for a document.
        /// </summary>
        /// <param name="documentId">Document ID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Document values.</returns>
        IAsyncEnumerable<DocumentValue> ReadByDocumentId(string documentId, CancellationToken token = default);

        /// <summary>
        /// Delete all values for a document.
        /// </summary>
        /// <param name="documentId">Document ID.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteByDocumentId(string documentId, CancellationToken token = default);
    }
}
