namespace Lattice.Core.Models
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Assigns a role, or a role-less set of permissions, to a credential at a tenant or resource scope.
    /// Mirrors <see cref="UserRoleAssignment"/> but is keyed on a credential and may carry inline grants.
    /// </summary>
    public class CredentialScopeAssignment
    {
        #region Public-Members

        /// <summary>
        /// Unique identifier for the assignment (csa_{prettyid}).
        /// </summary>
        public string Id { get; set; } = null;

        /// <summary>
        /// Identifier of the owning tenant.
        /// </summary>
        public string TenantId { get; set; } = null;

        /// <summary>
        /// Identifier of the credential this assignment applies to.
        /// </summary>
        public string CredentialId { get; set; } = null;

        /// <summary>
        /// Identifier of the assigned role, or null when referenced by name or when using inline permissions.
        /// </summary>
        public string RoleId { get; set; } = null;

        /// <summary>
        /// Name of the assigned role, used when referenced by name or as a fallback.
        /// </summary>
        public string RoleName { get; set; } = null;

        /// <summary>
        /// Whether the assignment applies tenant-wide or to a specific resource. Default Tenant.
        /// </summary>
        public ResourceScope ResourceScope { get; set; } = ResourceScope.Tenant;

        /// <summary>
        /// Identifier of the specific resource for a Resource-scoped assignment, or null.
        /// </summary>
        public string ResourceId { get; set; } = null;

        /// <summary>
        /// Inline, role-less operation grants applied by this assignment.
        /// </summary>
        public List<OperationType> Permissions
        {
            get => _Permissions;
            set => _Permissions = value ?? new List<OperationType>();
        }

        /// <summary>
        /// Resource types the inline grants apply to.
        /// </summary>
        public List<ResourceType> ResourceTypes
        {
            get => _ResourceTypes;
            set => _ResourceTypes = value ?? new List<ResourceType>();
        }

        /// <summary>
        /// Whether the assignment is active.
        /// </summary>
        public bool Active { get; set; } = true;

        /// <summary>
        /// Timestamp when the assignment was created (UTC).
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when the assignment was last updated (UTC).
        /// </summary>
        public DateTime LastUpdateUtc { get; set; } = DateTime.UtcNow;

        #endregion

        #region Private-Members

        private List<OperationType> _Permissions = new List<OperationType>();
        private List<ResourceType> _ResourceTypes = new List<ResourceType>();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public CredentialScopeAssignment()
        {
        }

        #endregion
    }
}
