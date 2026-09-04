namespace Lattice.Sdk.Models
{
    /// <summary>
    /// A tenant offered during login when a set of credentials matches users in more than one tenant.
    /// </summary>
    public class TenantOption
    {
        /// <summary>Tenant identifier.</summary>
        public string? TenantId { get; set; }

        /// <summary>Tenant name.</summary>
        public string? TenantName { get; set; }
    }
}
