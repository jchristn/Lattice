namespace Lattice.Core.Repositories.Interfaces
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Lattice.Core.Models;

    /// <summary>
    /// Persistence operations for credentials (access keys used as bearer tokens).
    /// </summary>
    public interface ICredentialMethods
    {
        /// <summary>Create a credential.</summary>
        /// <param name="credential">Credential to create.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The created credential.</returns>
        Task<Credential> Create(Credential credential, CancellationToken token = default);

        /// <summary>Read a credential by identifier.</summary>
        /// <param name="id">Credential identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The credential, or null.</returns>
        Task<Credential> ReadById(string id, CancellationToken token = default);

        /// <summary>Resolve a credential from the SHA-256 hash of a presented access key.</summary>
        /// <param name="accessKeySha256">SHA-256 hex hash of the access key.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The credential, or null.</returns>
        Task<Credential> ReadByAccessKeyHash(string accessKeySha256, CancellationToken token = default);

        /// <summary>Read all credentials in a tenant.</summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The tenant's credentials.</returns>
        Task<List<Credential>> ReadByTenant(string tenantId, CancellationToken token = default);

        /// <summary>Read all credentials owned by a user.</summary>
        /// <param name="userId">User identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The user's credentials.</returns>
        Task<List<Credential>> ReadByUser(string userId, CancellationToken token = default);

        /// <summary>Update a credential.</summary>
        /// <param name="credential">Credential to update.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The updated credential.</returns>
        Task<Credential> Update(Credential credential, CancellationToken token = default);

        /// <summary>Delete a credential.</summary>
        /// <param name="id">Credential identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>A task.</returns>
        Task Delete(string id, CancellationToken token = default);

        /// <summary>Determine whether a credential exists.</summary>
        /// <param name="id">Credential identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>True if the credential exists.</returns>
        Task<bool> Exists(string id, CancellationToken token = default);
    }
}
