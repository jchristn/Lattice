namespace Lattice.Core.Repositories.Interfaces
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Lattice.Core.Models;

    /// <summary>
    /// Persistence operations for user login sessions.
    /// </summary>
    public interface IAuthSessionMethods
    {
        /// <summary>Create a session.</summary>
        /// <param name="session">Session to create.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The created session.</returns>
        Task<AuthSession> Create(AuthSession session, CancellationToken token = default);

        /// <summary>Read a session by identifier.</summary>
        /// <param name="id">Session identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The session, or null.</returns>
        Task<AuthSession> ReadById(string id, CancellationToken token = default);

        /// <summary>Read a session by its embedded token identifier.</summary>
        /// <param name="tokenId">Token identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The session, or null.</returns>
        Task<AuthSession> ReadByTokenId(string tokenId, CancellationToken token = default);

        /// <summary>Update a session (for example to record last-used or revocation).</summary>
        /// <param name="session">Session to update.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The updated session.</returns>
        Task<AuthSession> Update(AuthSession session, CancellationToken token = default);

        /// <summary>Delete a session.</summary>
        /// <param name="id">Session identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>A task.</returns>
        Task Delete(string id, CancellationToken token = default);

        /// <summary>Delete all sessions that expired before the given cutoff.</summary>
        /// <param name="cutoffUtc">Cutoff time (UTC).</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The number of sessions deleted.</returns>
        Task<long> DeleteExpired(DateTime cutoffUtc, CancellationToken token = default);
    }
}
