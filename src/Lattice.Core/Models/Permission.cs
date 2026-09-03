namespace Lattice.Core.Models
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// A permission — a grant or denial covering a set of resource types and operation types. Deny
    /// permissions win over Permit permissions within a matched tenant and scope.
    /// </summary>
    public class Permission
    {
        #region Public-Members

        /// <summary>
        /// Unique identifier for the permission (perm_{prettyid}).
        /// </summary>
        public string Id { get; set; } = null;

        /// <summary>
        /// Identifier of the owning tenant, or null for a global built-in permission.
        /// </summary>
        public string TenantId { get; set; } = null;

        /// <summary>
        /// Permission name.
        /// </summary>
        public string Name { get; set; } = null;

        /// <summary>
        /// The resource types this permission covers.
        /// </summary>
        public List<ResourceType> ResourceTypes
        {
            get => _ResourceTypes;
            set => _ResourceTypes = value ?? new List<ResourceType>();
        }

        /// <summary>
        /// The operation types this permission covers.
        /// </summary>
        public List<OperationType> OperationTypes
        {
            get => _OperationTypes;
            set => _OperationTypes = value ?? new List<OperationType>();
        }

        /// <summary>
        /// Whether the permission grants (Permit) or denies (Deny). Default Permit.
        /// </summary>
        public PermissionType PermissionType { get; set; } = PermissionType.Permit;

        /// <summary>
        /// Whether the permission is active.
        /// </summary>
        public bool Active { get; set; } = true;

        /// <summary>
        /// Whether the permission is protected from modification and deletion.
        /// </summary>
        public bool IsProtected { get; set; } = false;

        /// <summary>
        /// Timestamp when the permission was created (UTC).
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when the permission was last updated (UTC).
        /// </summary>
        public DateTime LastUpdateUtc { get; set; } = DateTime.UtcNow;

        #endregion

        #region Private-Members

        private List<ResourceType> _ResourceTypes = new List<ResourceType>();
        private List<OperationType> _OperationTypes = new List<OperationType>();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public Permission()
        {
        }

        #endregion
    }
}
