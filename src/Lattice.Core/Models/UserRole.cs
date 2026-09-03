namespace Lattice.Core.Models
{
    using System;

    /// <summary>
    /// A role — a named container that groups permissions. Built-in roles have a null tenant id, are
    /// seeded at first boot, and are protected. Tenant-scoped custom roles carry a tenant id.
    /// </summary>
    public class UserRole
    {
        #region Public-Members

        /// <summary>
        /// Unique identifier for the role (rol_{prettyid}).
        /// </summary>
        public string Id { get; set; } = null;

        /// <summary>
        /// Identifier of the owning tenant, or null for a global built-in role.
        /// </summary>
        public string TenantId { get; set; } = null;

        /// <summary>
        /// Role name (for example TenantAdmin, Editor, Viewer).
        /// </summary>
        public string Name { get; set; } = null;

        /// <summary>
        /// Whether this is a built-in role seeded and refreshed by the platform. Default false.
        /// </summary>
        public bool IsBuiltIn { get; set; } = false;

        /// <summary>
        /// Whether the role is active.
        /// </summary>
        public bool Active { get; set; } = true;

        /// <summary>
        /// Whether the role is protected from modification and deletion.
        /// </summary>
        public bool IsProtected { get; set; } = false;

        /// <summary>
        /// Timestamp when the role was created (UTC).
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when the role was last updated (UTC).
        /// </summary>
        public DateTime LastUpdateUtc { get; set; } = DateTime.UtcNow;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public UserRole()
        {
        }

        #endregion
    }
}
