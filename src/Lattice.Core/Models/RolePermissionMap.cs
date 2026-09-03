namespace Lattice.Core.Models
{
    using System;

    /// <summary>
    /// A many-to-many link between a role and a permission.
    /// </summary>
    public class RolePermissionMap
    {
        #region Public-Members

        /// <summary>
        /// Unique identifier for the mapping (rpm_{prettyid}).
        /// </summary>
        public string Id { get; set; } = null;

        /// <summary>
        /// Identifier of the owning tenant, or null when linking global built-in records.
        /// </summary>
        public string TenantId { get; set; } = null;

        /// <summary>
        /// Identifier of the role.
        /// </summary>
        public string RoleId { get; set; } = null;

        /// <summary>
        /// Identifier of the permission.
        /// </summary>
        public string PermissionId { get; set; } = null;

        /// <summary>
        /// Whether the mapping is active.
        /// </summary>
        public bool Active { get; set; } = true;

        /// <summary>
        /// Timestamp when the mapping was created (UTC).
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when the mapping was last updated (UTC).
        /// </summary>
        public DateTime LastUpdateUtc { get; set; } = DateTime.UtcNow;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public RolePermissionMap()
        {
        }

        #endregion
    }
}
