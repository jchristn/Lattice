namespace Lattice.Core.Repositories.Interfaces
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Lattice.Core.Models;

    /// <summary>
    /// Persistence operations for the RBAC model: roles, permissions, role/permission maps, and the
    /// user and credential assignments that bind principals to roles.
    /// </summary>
    public interface IRoleMethods
    {
        #region Roles

        /// <summary>Create a role.</summary>
        /// <param name="role">Role to create.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The created role.</returns>
        Task<UserRole> CreateRole(UserRole role, CancellationToken token = default);

        /// <summary>Read a role by identifier.</summary>
        /// <param name="id">Role identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The role, or null.</returns>
        Task<UserRole> ReadRoleById(string id, CancellationToken token = default);

        /// <summary>Read a role by name, matching a tenant role or a global built-in role.</summary>
        /// <param name="tenantId">Tenant identifier, or null to match a built-in.</param>
        /// <param name="name">Role name.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The role, or null.</returns>
        Task<UserRole> ReadRoleByName(string tenantId, string name, CancellationToken token = default);

        /// <summary>Read the roles visible to a tenant (its custom roles plus global built-ins).</summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The roles.</returns>
        Task<List<UserRole>> ReadRoles(string tenantId, CancellationToken token = default);

        /// <summary>Update a role.</summary>
        /// <param name="role">Role to update.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The updated role.</returns>
        Task<UserRole> UpdateRole(UserRole role, CancellationToken token = default);

        /// <summary>Delete a role.</summary>
        /// <param name="id">Role identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>A task.</returns>
        Task DeleteRole(string id, CancellationToken token = default);

        #endregion

        #region Permissions

        /// <summary>Create a permission.</summary>
        /// <param name="permission">Permission to create.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The created permission.</returns>
        Task<Permission> CreatePermission(Permission permission, CancellationToken token = default);

        /// <summary>Read a permission by identifier.</summary>
        /// <param name="id">Permission identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The permission, or null.</returns>
        Task<Permission> ReadPermissionById(string id, CancellationToken token = default);

        /// <summary>Read the permissions visible to a tenant (its own plus global built-ins).</summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The permissions.</returns>
        Task<List<Permission>> ReadPermissions(string tenantId, CancellationToken token = default);

        /// <summary>Update a permission.</summary>
        /// <param name="permission">Permission to update.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The updated permission.</returns>
        Task<Permission> UpdatePermission(Permission permission, CancellationToken token = default);

        /// <summary>Delete a permission.</summary>
        /// <param name="id">Permission identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>A task.</returns>
        Task DeletePermission(string id, CancellationToken token = default);

        #endregion

        #region Role-Permission-Maps

        /// <summary>Link a permission to a role.</summary>
        /// <param name="map">Mapping to create.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The created mapping.</returns>
        Task<RolePermissionMap> CreateRolePermissionMap(RolePermissionMap map, CancellationToken token = default);

        /// <summary>Read the permissions granted by a role.</summary>
        /// <param name="roleId">Role identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The role's permissions.</returns>
        Task<List<Permission>> ReadPermissionsForRole(string roleId, CancellationToken token = default);

        /// <summary>Delete a role/permission mapping.</summary>
        /// <param name="id">Mapping identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>A task.</returns>
        Task DeleteRolePermissionMap(string id, CancellationToken token = default);

        /// <summary>Delete every permission mapping for a role (used when editing or deleting a role).</summary>
        /// <param name="roleId">Role identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>A task.</returns>
        Task DeleteRolePermissionMapsByRole(string roleId, CancellationToken token = default);

        #endregion

        #region User-Role-Assignments

        /// <summary>Assign a role to a user.</summary>
        /// <param name="assignment">Assignment to create.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The created assignment.</returns>
        Task<UserRoleAssignment> CreateUserRoleAssignment(UserRoleAssignment assignment, CancellationToken token = default);

        /// <summary>Read a user role assignment by identifier.</summary>
        /// <param name="id">Assignment identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The assignment, or null.</returns>
        Task<UserRoleAssignment> ReadUserRoleAssignmentById(string id, CancellationToken token = default);

        /// <summary>Read the role assignments for a user.</summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="userId">User identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The user's assignments.</returns>
        Task<List<UserRoleAssignment>> ReadUserRoleAssignments(string tenantId, string userId, CancellationToken token = default);

        /// <summary>Read all user role assignments in a tenant.</summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The tenant's user assignments.</returns>
        Task<List<UserRoleAssignment>> ReadAllUserRoleAssignments(string tenantId, CancellationToken token = default);

        /// <summary>Delete a user role assignment.</summary>
        /// <param name="id">Assignment identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>A task.</returns>
        Task DeleteUserRoleAssignment(string id, CancellationToken token = default);

        #endregion

        #region Credential-Scope-Assignments

        /// <summary>Assign a role or inline grants to a credential.</summary>
        /// <param name="assignment">Assignment to create.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The created assignment.</returns>
        Task<CredentialScopeAssignment> CreateCredentialScopeAssignment(CredentialScopeAssignment assignment, CancellationToken token = default);

        /// <summary>Read a credential scope assignment by identifier.</summary>
        /// <param name="id">Assignment identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The assignment, or null.</returns>
        Task<CredentialScopeAssignment> ReadCredentialScopeAssignmentById(string id, CancellationToken token = default);

        /// <summary>Read the scope assignments for a credential.</summary>
        /// <param name="tenantId">Tenant identifier.</param>
        /// <param name="credentialId">Credential identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The credential's assignments.</returns>
        Task<List<CredentialScopeAssignment>> ReadCredentialScopeAssignments(string tenantId, string credentialId, CancellationToken token = default);

        /// <summary>Delete a credential scope assignment.</summary>
        /// <param name="id">Assignment identifier.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>A task.</returns>
        Task DeleteCredentialScopeAssignment(string id, CancellationToken token = default);

        #endregion
    }
}
