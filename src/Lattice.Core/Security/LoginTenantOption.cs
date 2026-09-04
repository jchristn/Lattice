namespace Lattice.Core.Security
{
    /// <summary>
    /// A tenant a set of login credentials resolves to, offered to the client to choose from when an email
    /// and password match a user in more than one tenant.
    /// </summary>
    public class LoginTenantOption
    {
        /// <summary>Tenant identifier.</summary>
        public string TenantId { get; set; } = null;

        /// <summary>Tenant name.</summary>
        public string TenantName { get; set; } = null;

        /// <summary>Instantiate the object.</summary>
        public LoginTenantOption()
        {
        }
    }
}
