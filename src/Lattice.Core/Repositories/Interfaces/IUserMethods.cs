namespace Lattice.Core.Repositories.Interfaces
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Lattice.Core.Models;

    /// <summary>
    /// Persistence operations for users. All reads are tenant-scoped except <see cref="ReadById"/>,
    /// whose caller is responsible for verifying the returned user's tenant.
    /// </summary>
    public interface IUserMethods
    {
        /// <summary>Create a user.</summary>
        /// <param name="user">User to create.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The created user.</returns>
        Task<User> Create(User user, CancellationToken token = default);

        /// <summary>Read a user by identifier.</summary>
        /// <param name="id">User identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The user, or null.</returns>
        Task<User> ReadById(string id, CancellationToken token = default);

        /// <summary>Read a user by email within a tenant.</summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="email">Email address.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The user, or null.</returns>
        Task<User> ReadByEmail(string tenantId, string email, CancellationToken token = default);

        /// <summary>Read all users in a tenant.</summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The tenant's users.</returns>
        Task<List<User>> ReadByTenant(string tenantId, CancellationToken token = default);

        /// <summary>Update a user.</summary>
        /// <param name="user">User to update.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The updated user.</returns>
        Task<User> Update(User user, CancellationToken token = default);

        /// <summary>Delete a user and its subordinate credentials, sessions, and assignments.</summary>
        /// <param name="id">User identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>A task.</returns>
        Task Delete(string id, CancellationToken token = default);

        /// <summary>Determine whether a user exists.</summary>
        /// <param name="id">User identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>True if the user exists.</returns>
        Task<bool> Exists(string id, CancellationToken token = default);

        /// <summary>Count users in a tenant.</summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The user count.</returns>
        Task<long> Count(string tenantId, CancellationToken token = default);
    }
}
