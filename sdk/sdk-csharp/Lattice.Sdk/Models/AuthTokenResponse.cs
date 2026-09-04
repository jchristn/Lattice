namespace Lattice.Sdk.Models
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Response body for <c>POST /v1.0/token</c>: on success the session token and basic principal
    /// information; when the credentials match multiple tenants and none was supplied,
    /// <see cref="TenantSelectionRequired"/> is set with the candidate <see cref="Tenants"/> and no token.
    /// </summary>
    public class AuthTokenResponse
    {
        /// <summary>The session token to present as a bearer. Null when tenant selection is required.</summary>
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

        /// <summary>True when the credentials match multiple tenants and the caller must choose one.</summary>
        public bool TenantSelectionRequired { get; set; }

        /// <summary>The tenants to choose from, when <see cref="TenantSelectionRequired"/> is true.</summary>
        public List<TenantOption>? Tenants { get; set; }
    }
}
