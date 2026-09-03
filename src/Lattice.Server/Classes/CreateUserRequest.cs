namespace Lattice.Server.Classes
{
    /// <summary>
    /// Request body for creating a user. The password is provided here and stored only as a hash.
    /// </summary>
    public class CreateUserRequest
    {
        /// <summary>User email (unique within the tenant).</summary>
        public string Email { get; set; } = null;

        /// <summary>User password.</summary>
        public string Password { get; set; } = null;

        /// <summary>First name.</summary>
        public string FirstName { get; set; } = null;

        /// <summary>Last name.</summary>
        public string LastName { get; set; } = null;

        /// <summary>Whether the user is a system administrator. Default false.</summary>
        public bool IsAdmin { get; set; } = false;

        /// <summary>Whether the user is a tenant administrator. Default false.</summary>
        public bool IsTenantAdmin { get; set; } = false;

        /// <summary>Target tenant id; honored only for system administrators acting cross-tenant.</summary>
        public string TenantId { get; set; } = null;
    }
}
