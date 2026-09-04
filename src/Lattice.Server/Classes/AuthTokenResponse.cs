namespace Lattice.Server.Classes
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Response body for <c>POST /v1.0/token</c>. On a successful login it carries the session token and
    /// basic principal information. When the supplied credentials match users in more than one tenant and
    /// no tenant was specified, <see cref="TenantSelectionRequired"/> is true and <see cref="Tenants"/>
    /// lists the tenants to choose from (no token is issued); the caller re-submits with a chosen tenant id.
    /// </summary>
    public class AuthTokenResponse
    {
        /// <summary>The session token to present as a bearer. Null when tenant selection is required.</summary>
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

        /// <summary>
        /// True when the credentials match users in multiple tenants and the caller must choose one.
        /// When true, no token is issued and <see cref="Tenants"/> lists the options.
        /// </summary>
        public bool TenantSelectionRequired { get; set; } = false;

        /// <summary>The tenants to choose from, when <see cref="TenantSelectionRequired"/> is true.</summary>
        public List<TenantOption> Tenants { get; set; } = null;
    }
}
