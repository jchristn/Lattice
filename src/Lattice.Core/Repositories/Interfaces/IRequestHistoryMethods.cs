namespace Lattice.Core.Repositories.Interfaces
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Lattice.Core.Models;

    /// <summary>
    /// Interface for request history repository methods.
    /// </summary>
    public interface IRequestHistoryMethods
    {
        /// <summary>
        /// Create a request history entry.
        /// </summary>
        /// <param name="detail">Request history detail.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Created request history detail.</returns>
        Task<RequestHistoryDetail> Create(RequestHistoryDetail detail, CancellationToken token = default);

        /// <summary>
        /// Read request history entry metadata by identifier.
        /// </summary>
        /// <param name="id">Entry identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Request history entry or null.</returns>
        Task<RequestHistoryEntry> ReadEntryById(string id, CancellationToken token = default);

        /// <summary>
        /// Read full request history detail by identifier.
        /// </summary>
        /// <param name="id">Entry identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Request history detail or null.</returns>
        Task<RequestHistoryDetail> ReadDetailById(string id, CancellationToken token = default);

        /// <summary>
        /// Search request history entries using the supplied filter.
        /// </summary>
        /// <param name="filter">Search filter.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Paged search result.</returns>
        Task<RequestHistorySearchResult> Search(RequestHistorySearchFilter filter, CancellationToken token = default);

        /// <summary>
        /// Retrieve aggregated request history summary buckets.
        /// </summary>
        /// <param name="filter">Search filter.</param>
        /// <param name="interval">Bucket interval.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Summary result.</returns>
        Task<RequestHistorySummaryResult> GetSummary(RequestHistorySearchFilter filter, string interval, CancellationToken token = default);

        /// <summary>
        /// Delete a single request history entry.
        /// </summary>
        /// <param name="id">Entry identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>True if an entry was deleted.</returns>
        Task<bool> Delete(string id, CancellationToken token = default);

        /// <summary>
        /// Delete all request history entries matching the supplied filter.
        /// </summary>
        /// <param name="filter">Delete filter.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Deleted entry count.</returns>
        Task<long> DeleteBulk(RequestHistorySearchFilter filter, CancellationToken token = default);

        /// <summary>
        /// Delete all request history entries older than the supplied cutoff.
        /// </summary>
        /// <param name="cutoffUtc">Exclusive UTC cutoff.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Deleted entry count.</returns>
        Task<long> DeleteOlderThan(DateTime cutoffUtc, CancellationToken token = default);
    }
}
