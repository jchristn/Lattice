namespace Lattice.Core.Repositories.Interfaces
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Lattice.Core.Models;

    /// <summary>
    /// Persistence operations for tenants.
    /// </summary>
    public interface ITenantMethods
    {
        /// <summary>Create a tenant.</summary>
        /// <param name="tenant">Tenant to create.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The created tenant.</returns>
        Task<Tenant> Create(Tenant tenant, CancellationToken token = default);

        /// <summary>Read a tenant by identifier.</summary>
        /// <param name="id">Tenant identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The tenant, or null.</returns>
        Task<Tenant> ReadById(string id, CancellationToken token = default);

        /// <summary>Read a tenant by name.</summary>
        /// <param name="name">Tenant name.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The tenant, or null.</returns>
        Task<Tenant> ReadByName(string name, CancellationToken token = default);

        /// <summary>Read all tenants.</summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>All tenants.</returns>
        Task<List<Tenant>> ReadAll(CancellationToken token = default);

        /// <summary>Update a tenant.</summary>
        /// <param name="tenant">Tenant to update.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The updated tenant.</returns>
        Task<Tenant> Update(Tenant tenant, CancellationToken token = default);

        /// <summary>Delete a tenant and its subordinate records.</summary>
        /// <param name="id">Tenant identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>A task.</returns>
        Task Delete(string id, CancellationToken token = default);

        /// <summary>Determine whether a tenant exists.</summary>
        /// <param name="id">Tenant identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>True if the tenant exists.</returns>
        Task<bool> Exists(string id, CancellationToken token = default);

        /// <summary>Count all tenants.</summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The tenant count.</returns>
        Task<long> Count(CancellationToken token = default);
    }
}
