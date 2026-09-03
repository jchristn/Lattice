namespace Lattice.Core.Security
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Lattice.Core.Models;
    using Lattice.Core.Repositories.Interfaces;

    /// <summary>
    /// Authorizes a request for a principal. System admins and tenant admins bypass evaluation; everyone
    /// else is evaluated against their effective grants with deny-over-permit ordering.
    /// </summary>
    public class AuthorizationService
    {
        #region Private-Members

        private readonly IRoleMethods _Roles;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the authorization service.
        /// </summary>
        /// <param name="roles">Role, permission, and assignment repository.</param>
        public AuthorizationService(IRoleMethods roles)
        {
            _Roles = roles ?? throw new ArgumentNullException(nameof(roles));
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Determine whether a caller may perform an operation on a resource type and optional resource id.
        /// </summary>
        /// <param name="caller">The resolved principal.</param>
        /// <param name="resourceType">The resource type.</param>
        /// <param name="operation">The operation.</param>
        /// <param name="resourceId">The specific resource id, or null.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The authorization verdict.</returns>
        public async Task<AuthorizationVerdict> AuthorizeAsync(
            CallerContext caller,
            ResourceType resourceType,
            OperationType operation,
            string resourceId = null,
            CancellationToken token = default)
        {
            if (caller == null || !caller.IsAuthenticated) return AuthorizationVerdict.DeniedImplicit;
            if (caller.IsAdmin) return AuthorizationVerdict.Permitted;
            if (caller.IsTenantAdmin) return AuthorizationVerdict.Permitted;

            List<EffectiveGrant> grants = await ResolveGrantsAsync(caller, token).ConfigureAwait(false);
            return PermissionEvaluator.Evaluate(grants, resourceType, operation, resourceId);
        }

        /// <summary>
        /// Resolve the full set of effective grants for a caller, used both by authorization and by the
        /// effective-permissions inspection endpoints.
        /// </summary>
        /// <param name="caller">The resolved principal.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The caller's effective grants.</returns>
        public async Task<List<EffectiveGrant>> ResolveGrantsAsync(CallerContext caller, CancellationToken token = default)
        {
            List<EffectiveGrant> grants = new List<EffectiveGrant>();
            if (caller == null || !caller.IsAuthenticated) return grants;

            if (caller.PrincipalType == PrincipalType.Credential && !String.IsNullOrEmpty(caller.CredentialId))
            {
                List<CredentialScopeAssignment> scopes =
                    await _Roles.ReadCredentialScopeAssignments(caller.TenantId, caller.CredentialId, token).ConfigureAwait(false);

                if (scopes != null && scopes.Count > 0)
                {
                    foreach (CredentialScopeAssignment scope in scopes)
                    {
                        await AddRoleGrantsAsync(grants, caller.TenantId, scope.RoleId, scope.RoleName, scope.ResourceScope, scope.ResourceId, true, token).ConfigureAwait(false);

                        // Inline, role-less grants carried directly on the assignment.
                        if (scope.Permissions != null && scope.Permissions.Count > 0 && scope.ResourceTypes != null && scope.ResourceTypes.Count > 0)
                        {
                            grants.Add(new EffectiveGrant
                            {
                                PermissionType = PermissionType.Permit,
                                ResourceTypes = new List<ResourceType>(scope.ResourceTypes),
                                OperationTypes = new List<OperationType>(scope.Permissions),
                                ResourceScope = scope.ResourceScope,
                                ResourceId = scope.ResourceId,
                                InheritsToChildren = true
                            });
                        }
                    }

                    return grants;
                }

                // Owner ceiling (v1 simplification): a credential with no scope assignments inherits the
                // grants of its owning user.
            }

            string userId = caller.UserId;
            if (String.IsNullOrEmpty(userId)) return grants;

            List<UserRoleAssignment> assignments =
                await _Roles.ReadUserRoleAssignments(caller.TenantId, userId, token).ConfigureAwait(false);
            if (assignments == null) return grants;

            foreach (UserRoleAssignment assignment in assignments)
            {
                await AddRoleGrantsAsync(grants, caller.TenantId, assignment.RoleId, assignment.RoleName, assignment.ResourceScope, assignment.ResourceId, assignment.InheritsToChildren, token).ConfigureAwait(false);
            }

            return grants;
        }

        #endregion

        #region Private-Methods

        private async Task AddRoleGrantsAsync(
            List<EffectiveGrant> grants,
            string tenantId,
            string roleId,
            string roleName,
            ResourceScope scope,
            string resourceId,
            bool inheritsToChildren,
            CancellationToken token)
        {
            string resolvedRoleId = roleId;

            if (String.IsNullOrEmpty(resolvedRoleId) && !String.IsNullOrEmpty(roleName))
            {
                UserRole role = await _Roles.ReadRoleByName(tenantId, roleName, token).ConfigureAwait(false);
                if (role != null) resolvedRoleId = role.Id;
            }

            if (String.IsNullOrEmpty(resolvedRoleId)) return;

            List<Permission> permissions = await _Roles.ReadPermissionsForRole(resolvedRoleId, token).ConfigureAwait(false);
            if (permissions == null) return;

            foreach (Permission permission in permissions)
            {
                if (!permission.Active) continue;
                grants.Add(new EffectiveGrant
                {
                    PermissionType = permission.PermissionType,
                    ResourceTypes = new List<ResourceType>(permission.ResourceTypes),
                    OperationTypes = new List<OperationType>(permission.OperationTypes),
                    ResourceScope = scope,
                    ResourceId = resourceId,
                    InheritsToChildren = inheritsToChildren
                });
            }
        }

        #endregion
    }
}
