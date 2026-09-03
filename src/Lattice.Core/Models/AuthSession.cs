namespace Lattice.Core.Models
{
    using System;

    /// <summary>
    /// A revocable, server-side user login session. Session tokens are opaque, AES-256 encrypted, and
    /// validated against this record. Credential (access-key) principals do not create sessions.
    /// </summary>
    public class AuthSession
    {
        #region Public-Members

        /// <summary>
        /// Unique identifier for the session (ses_{prettyid}).
        /// </summary>
        public string Id { get; set; } = null;

        /// <summary>
        /// Identifier of the tenant this session is bound to.
        /// </summary>
        public string TenantId { get; set; } = null;

        /// <summary>
        /// The kind of principal this session resolves to. Sessions are issued for users.
        /// </summary>
        public PrincipalType PrincipalType { get; set; } = PrincipalType.User;

        /// <summary>
        /// Identifier of the user this session belongs to.
        /// </summary>
        public string UserId { get; set; } = null;

        /// <summary>
        /// The random token identifier embedded in the encrypted token payload.
        /// </summary>
        public string TokenId { get; set; } = null;

        /// <summary>
        /// Source IP address of the request that created the session.
        /// </summary>
        public string SourceIp { get; set; } = null;

        /// <summary>
        /// User agent of the request that created the session.
        /// </summary>
        public string UserAgent { get; set; } = null;

        /// <summary>
        /// Expiration time (UTC). After this the token is rejected.
        /// </summary>
        public DateTime ExpiresUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when the session was last used (UTC), or null.
        /// </summary>
        public DateTime? LastUsedUtc { get; set; } = null;

        /// <summary>
        /// Timestamp when the session was revoked (UTC), or null if still valid.
        /// </summary>
        public DateTime? RevokedUtc { get; set; } = null;

        /// <summary>
        /// Reason the session was revoked, or null.
        /// </summary>
        public string RevocationReason { get; set; } = null;

        /// <summary>
        /// Whether the session is active.
        /// </summary>
        public bool Active { get; set; } = true;

        /// <summary>
        /// Timestamp when the session was created (UTC).
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when the session was last updated (UTC).
        /// </summary>
        public DateTime LastUpdateUtc { get; set; } = DateTime.UtcNow;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public AuthSession()
        {
        }

        #endregion
    }
}
