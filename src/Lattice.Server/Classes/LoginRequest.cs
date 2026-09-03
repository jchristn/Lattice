namespace Lattice.Server.Classes
{
    /// <summary>
    /// Request body for <c>POST /v1.0/token</c>: interactive login by email, password, and tenant.
    /// </summary>
    public class LoginRequest
    {
        /// <summary>User email.</summary>
        public string Email { get; set; } = null;

        /// <summary>User password.</summary>
        public string Password { get; set; } = null;

        /// <summary>Tenant identifier.</summary>
        public string TenantId { get; set; } = null;
    }
}
