namespace Lattice.Server.Classes
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// A role together with the grants it confers. Returned by the role create/read/update endpoints.
    /// </summary>
    public class RoleDetailResponse
    {
        /// <summary>Role identifier.</summary>
        public string Id { get; set; } = null;

        /// <summary>Owning tenant, or null for a global built-in role.</summary>
        public string TenantId { get; set; } = null;

        /// <summary>Role name.</summary>
        public string Name { get; set; } = null;

        /// <summary>Whether this is a global built-in role (not editable).</summary>
        public bool IsBuiltIn { get; set; } = false;

        /// <summary>Whether the role is active.</summary>
        public bool Active { get; set; } = true;

        /// <summary>Whether the role is protected from modification/deletion.</summary>
        public bool IsProtected { get; set; } = false;

        /// <summary>When the role was created (UTC).</summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>When the role was last updated (UTC).</summary>
        public DateTime LastUpdateUtc { get; set; } = DateTime.UtcNow;

        /// <summary>The grants the role confers.</summary>
        public List<RolePermissionSpec> Permissions { get; set; } = new List<RolePermissionSpec>();
    }
}
