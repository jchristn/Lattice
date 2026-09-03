namespace Lattice.Core.Models
{
    using System;

    /// <summary>
    /// Assigns a role to a user at a tenant or resource scope. A role may be referenced by id or, as a
    /// fallback, by name (which resolves to a built-in definition when no record matches).
    /// </summary>
    public class UserRoleAssignment
    {
        #region Public-Members

        /// <summary>
        /// Unique identifier for the assignment (ura_{prettyid}).
        /// </summary>
        public string Id { get; set; } = null;

        /// <summary>
        /// Identifier of the owning tenant.
        /// </summary>
        public string TenantId { get; set; } = null;

        /// <summary>
        /// Identifier of the user this assignment applies to.
        /// </summary>
        public string UserId { get; set; } = null;

        /// <summary>
        /// Identifier of the assigned role, or null when referenced by name.
        /// </summary>
        public string RoleId { get; set; } = null;

        /// <summary>
        /// Name of the assigned role, used when the role is referenced by name or as a fallback.
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
        /// For a Tenant-scoped assignment, whether it also satisfies checks on child resources. Default true.
        /// </summary>
        public bool InheritsToChildren { get; set; } = true;

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

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public UserRoleAssignment()
        {
        }

        #endregion
    }
}
