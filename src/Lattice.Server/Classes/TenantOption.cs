namespace Lattice.Server.Classes
{
    /// <summary>
    /// A tenant offered to the caller to choose from during login, when a set of credentials matches users
    /// in more than one tenant.
    /// </summary>
    public class TenantOption
    {
        /// <summary>Tenant identifier.</summary>
        public string TenantId { get; set; } = null;

        /// <summary>Tenant name.</summary>
        public string TenantName { get; set; } = null;
    }
}
