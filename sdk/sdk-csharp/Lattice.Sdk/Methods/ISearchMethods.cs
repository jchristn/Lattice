using Lattice.Sdk.Models;

namespace Lattice.Sdk.Methods
{
    /// <summary>
    /// Interface for search methods.
    /// </summary>
    public interface ISearchMethods
    {
        /// <summary>
        /// Search for documents.
        /// </summary>
        Task<SearchResult?> SearchAsync(SearchQuery query, CancellationToken cancellationToken = default);

        /// <summary>
        /// Search documents using a SQL-like expression.
        /// </summary>
        Task<SearchResult?> SearchBySqlAsync(string collectionId, string sqlExpression, CancellationToken cancellationToken = default);

        /// <summary>
        /// Enumerate documents in a collection.
        /// </summary>
        Task<SearchResult?> EnumerateAsync(SearchQuery query, CancellationToken cancellationToken = default);
    }
}
