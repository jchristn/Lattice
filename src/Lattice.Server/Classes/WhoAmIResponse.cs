namespace Lattice.Server.Classes
{
    /// <summary>
    /// Response body describing the resolved principal for the current request.
    /// </summary>
    public class WhoAmIResponse
    {
        /// <summary>Whether the request is authenticated.</summary>
        public bool IsAuthenticated { get; set; } = false;

        /// <summary>The kind of principal (User or Credential).</summary>
        public string PrincipalType { get; set; } = null;

        /// <summary>Tenant identifier.</summary>
        public string TenantId { get; set; } = null;

        /// <summary>User identifier.</summary>
        public string UserId { get; set; } = null;

        /// <summary>Credential identifier, when the principal is a credential.</summary>
        public string CredentialId { get; set; } = null;

        /// <summary>User email.</summary>
        public string Email { get; set; } = null;

        /// <summary>Whether the principal is a system administrator.</summary>
        public bool IsAdmin { get; set; } = false;

        /// <summary>Whether the principal is a tenant administrator.</summary>
        public bool IsTenantAdmin { get; set; } = false;
    }
}
