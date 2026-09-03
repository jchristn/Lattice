namespace Lattice.Core.Repositories.Interfaces
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Lattice.Core.Models;

    /// <summary>
    /// Persistence operations for the append-only security audit store.
    /// </summary>
    public interface IAuditMethods
    {
        /// <summary>Append an audit entry.</summary>
        /// <param name="entry">Entry to append.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The appended entry.</returns>
        Task<AuditEntry> Create(AuditEntry entry, CancellationToken token = default);

        /// <summary>Read an audit entry by identifier.</summary>
        /// <param name="id">Entry identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The entry, or null.</returns>
        Task<AuditEntry> ReadById(string id, CancellationToken token = default);

        /// <summary>
        /// Search audit entries within a tenant with optional filters and paging. A null
        /// <paramref name="tenantId"/> searches across all tenants (system-admin use).
        /// </summary>
        /// <param name="tenantId">Tenant identifier, or null for all tenants.</param>
        /// <param name="eventType">Optional event type filter.</param>
        /// <param name="fromUtc">Optional inclusive start time (UTC).</param>
        /// <param name="toUtc">Optional inclusive end time (UTC).</param>
        /// <param name="skip">Records to skip.</param>
        /// <param name="maxResults">Maximum records to return.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The matching entries.</returns>
        Task<List<AuditEntry>> Search(string tenantId, string eventType, DateTime? fromUtc, DateTime? toUtc, int skip, int maxResults, CancellationToken token = default);

        /// <summary>Count audit entries matching the same filters as <see cref="Search"/>.</summary>
        /// <param name="tenantId">Tenant identifier, or null for all tenants.</param>
        /// <param name="eventType">Optional event type filter.</param>
        /// <param name="fromUtc">Optional inclusive start time (UTC).</param>
        /// <param name="toUtc">Optional inclusive end time (UTC).</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The matching count.</returns>
        Task<long> Count(string tenantId, string eventType, DateTime? fromUtc, DateTime? toUtc, CancellationToken token = default);

        /// <summary>Delete an audit entry by identifier.</summary>
        /// <param name="id">Entry identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>A task.</returns>
        Task Delete(string id, CancellationToken token = default);

        /// <summary>Delete audit entries older than a cutoff.</summary>
        /// <param name="cutoffUtc">Cutoff time (UTC).</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The number of entries deleted.</returns>
        Task<long> DeleteOlderThan(DateTime cutoffUtc, CancellationToken token = default);
    }
}
