namespace Lattice.Core.Client.Interfaces
{
    using System.Threading;
    using System.Threading.Tasks;
    using Lattice.Core.Models;
    using Lattice.Core.Search;

    /// <summary>
    /// Interface for search methods.
    /// </summary>
    public interface ISearchMethods
    {
        /// <summary>
        /// Search documents using a SearchQuery.
        /// </summary>
        /// <param name="query">Search query.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Search result.</returns>
        Task<SearchResult> Search(SearchQuery query, CancellationToken token = default);

        /// <summary>
        /// Search documents using a SQL-like expression.
        /// </summary>
        /// <param name="collectionId">Collection ID.</param>
        /// <param name="sql">SQL-like expression.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Search result.</returns>
        Task<SearchResult> SearchBySql(string collectionId, string sql, CancellationToken token = default);

        /// <summary>
        /// Enumerate documents with pagination.
        /// </summary>
        /// <param name="query">Enumeration query.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Enumeration result.</returns>
        Task<EnumerationResult<Document>> Enumerate(EnumerationQuery query, CancellationToken token = default);
    }
}
