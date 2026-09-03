namespace Lattice.Core.Security
{
    using Lattice.Core.Models;

    /// <summary>
    /// The resolved principal for a request: who they are, which tenant they belong to, and their
    /// administrative status. Produced by the authentication service and consumed by authorization.
    /// </summary>
    public class CallerContext
    {
        #region Public-Members

        /// <summary>
        /// Whether the request was successfully authenticated.
        /// </summary>
        public bool IsAuthenticated { get; set; } = false;

        /// <summary>
        /// The kind of principal.
        /// </summary>
        public PrincipalType PrincipalType { get; set; } = PrincipalType.User;

        /// <summary>
        /// Identifier of the principal (user id or credential id).
        /// </summary>
        public string PrincipalId { get; set; } = null;

        /// <summary>
        /// Identifier of the resolved tenant.
        /// </summary>
        public string TenantId { get; set; } = null;

        /// <summary>
        /// Identifier of the user (the credential owner when the principal is a credential).
        /// </summary>
        public string UserId { get; set; } = null;

        /// <summary>
        /// Identifier of the credential, when the principal is a credential.
        /// </summary>
        public string CredentialId { get; set; } = null;

        /// <summary>
        /// Identifier of the session, when authenticated by a session token.
        /// </summary>
        public string SessionId { get; set; } = null;

        /// <summary>
        /// Email of the resolved user, when available.
        /// </summary>
        public string Email { get; set; } = null;

        /// <summary>
        /// Whether the principal is a system administrator with full cross-tenant access.
        /// </summary>
        public bool IsAdmin { get; set; } = false;

        /// <summary>
        /// Whether the principal is a tenant administrator, bypassing RBAC within its tenant.
        /// </summary>
        public bool IsTenantAdmin { get; set; } = false;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public CallerContext()
        {
        }

        #endregion
    }
}
