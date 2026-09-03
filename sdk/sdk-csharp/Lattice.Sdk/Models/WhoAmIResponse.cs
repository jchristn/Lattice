namespace Lattice.Sdk.Models
{
    /// <summary>
    /// Response body describing the resolved principal for the current credentials
    /// (<c>GET /v1.0/whoami</c>).
    /// </summary>
    public class WhoAmIResponse
    {
        /// <summary>Whether the request is authenticated.</summary>
        public bool IsAuthenticated { get; set; }

        /// <summary>The kind of principal (User or Credential).</summary>
        public string? PrincipalType { get; set; }

        /// <summary>Tenant identifier.</summary>
        public string? TenantId { get; set; }

        /// <summary>User identifier.</summary>
        public string? UserId { get; set; }

        /// <summary>Credential identifier, when the principal is a credential.</summary>
        public string? CredentialId { get; set; }

        /// <summary>User email.</summary>
        public string? Email { get; set; }

        /// <summary>Whether the principal is a system administrator.</summary>
        public bool IsAdmin { get; set; }

        /// <summary>Whether the principal is a tenant administrator.</summary>
        public bool IsTenantAdmin { get; set; }
    }
}
