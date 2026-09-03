namespace Lattice.Sdk.Models
{
    using System;

    /// <summary>
    /// Response body for a successful login (<c>POST /v1.0/token</c>): the session token and basic
    /// principal information.
    /// </summary>
    public class AuthTokenResponse
    {
        /// <summary>The session token to present as a bearer.</summary>
        public string? Token { get; set; }

        /// <summary>When the token expires (UTC).</summary>
        public DateTime ExpiresUtc { get; set; }

        /// <summary>Tenant identifier.</summary>
        public string? TenantId { get; set; }

        /// <summary>User identifier.</summary>
        public string? UserId { get; set; }

        /// <summary>User email.</summary>
        public string? Email { get; set; }

        /// <summary>Whether the user is a system administrator.</summary>
        public bool IsAdmin { get; set; }

        /// <summary>Whether the user is a tenant administrator.</summary>
        public bool IsTenantAdmin { get; set; }
    }
}
