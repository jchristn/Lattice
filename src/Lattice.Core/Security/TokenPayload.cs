namespace Lattice.Core.Security
{
    using System;

    /// <summary>
    /// The internal payload carried inside an encrypted session token. Contains only
    /// platform-controlled identifiers — never secrets or client-supplied data.
    /// </summary>
    public class TokenPayload
    {
        /// <summary>
        /// Identifier of the session (matches the authsessions row).
        /// </summary>
        public string SessionId { get; set; } = null;

        /// <summary>
        /// Identifier of the token, also stored on the session row for lookup and revocation.
        /// </summary>
        public string TokenId { get; set; } = null;

        /// <summary>
        /// Identifier of the user the token authenticates.
        /// </summary>
        public string UserId { get; set; } = null;

        /// <summary>
        /// Identifier of the tenant the token is bound to.
        /// </summary>
        public string TenantId { get; set; } = null;

        /// <summary>
        /// Time the token was issued (UTC).
        /// </summary>
        public DateTime IssuedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Time the token expires (UTC).
        /// </summary>
        public DateTime ExpiresUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// A random nonce ensuring token uniqueness.
        /// </summary>
        public string Nonce { get; set; } = null;

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public TokenPayload()
        {
        }
    }
}
