namespace Lattice.Server.Classes
{
    using System;

    /// <summary>
    /// Response body for a successful login: the session token and basic principal information.
    /// </summary>
    public class AuthTokenResponse
    {
        /// <summary>The session token to present as a bearer.</summary>
        public string Token { get; set; } = null;

        /// <summary>When the token expires (UTC).</summary>
        public DateTime ExpiresUtc { get; set; } = DateTime.UtcNow;

        /// <summary>Tenant identifier.</summary>
        public string TenantId { get; set; } = null;

        /// <summary>User identifier.</summary>
        public string UserId { get; set; } = null;

        /// <summary>User email.</summary>
        public string Email { get; set; } = null;

        /// <summary>Whether the user is a system administrator.</summary>
        public bool IsAdmin { get; set; } = false;

        /// <summary>Whether the user is a tenant administrator.</summary>
        public bool IsTenantAdmin { get; set; } = false;
    }
}
