namespace Lattice.Core.Models
{
    using System;

    /// <summary>
    /// A tenant — the top-level isolation boundary. Every tenant-owned record carries a reference to a
    /// tenant, and no data or authorization decision crosses tenants.
    /// </summary>
    public class Tenant
    {
        #region Public-Members

        /// <summary>
        /// Unique identifier for the tenant (ten_{prettyid}).
        /// </summary>
        public string Id { get; set; } = null;

        /// <summary>
        /// Human-readable tenant name.
        /// </summary>
        public string Name { get; set; } = null;

        /// <summary>
        /// Optional region label for the tenant.
        /// </summary>
        public string Region { get; set; } = null;

        /// <summary>
        /// Whether the tenant is active. Inactive tenants cannot authenticate.
        /// </summary>
        public bool Active { get; set; } = true;

        /// <summary>
        /// Whether the tenant is protected from deletion (for example the seeded default tenant).
        /// </summary>
        public bool IsProtected { get; set; } = false;

        /// <summary>
        /// Timestamp when the tenant was created (UTC).
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when the tenant was last updated (UTC).
        /// </summary>
        public DateTime LastUpdateUtc { get; set; } = DateTime.UtcNow;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public Tenant()
        {
        }

        #endregion
    }
}
