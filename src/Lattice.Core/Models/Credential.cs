namespace Lattice.Core.Models
{
    using System;

    /// <summary>
    /// A machine credential owned by a user within a tenant. The credential's access key is used directly
    /// as a bearer token; only its SHA-256 hash and last four characters are stored. The raw access key
    /// is returned once, at creation, via <see cref="AccessKey"/> and never persisted.
    /// </summary>
    public class Credential
    {
        #region Public-Members

        /// <summary>
        /// Unique identifier for the credential (crd_{prettyid}).
        /// </summary>
        public string Id { get; set; } = null;

        /// <summary>
        /// Identifier of the tenant this credential belongs to.
        /// </summary>
        public string TenantId { get; set; } = null;

        /// <summary>
        /// Identifier of the owning user.
        /// </summary>
        public string UserId { get; set; } = null;

        /// <summary>
        /// Human-readable credential name.
        /// </summary>
        public string Name { get; set; } = null;

        /// <summary>
        /// The raw access key. Populated only on creation so it can be returned once; null when read back.
        /// </summary>
        public string AccessKey { get; set; } = null;

        /// <summary>
        /// SHA-256 hex hash of the access key, used to resolve a bearer token to this credential.
        /// </summary>
        public string AccessKeySha256 { get; set; } = null;

        /// <summary>
        /// The last four characters of the access key, retained for display.
        /// </summary>
        public string AccessKeyLast4 { get; set; } = null;

        /// <summary>
        /// Optional expiration time (UTC). Null means the credential does not expire.
        /// </summary>
        public DateTime? ExpiresUtc { get; set; } = null;

        /// <summary>
        /// Timestamp when the credential was last used to authenticate (UTC), or null.
        /// </summary>
        public DateTime? LastUsedUtc { get; set; } = null;

        /// <summary>
        /// Whether the credential is active. Inactive credentials cannot authenticate.
        /// </summary>
        public bool Active { get; set; } = true;

        /// <summary>
        /// Whether the credential is protected from deletion (for example the seeded default credential).
        /// </summary>
        public bool IsProtected { get; set; } = false;

        /// <summary>
        /// Timestamp when the credential was created (UTC).
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when the credential was last updated (UTC).
        /// </summary>
        public DateTime LastUpdateUtc { get; set; } = DateTime.UtcNow;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public Credential()
        {
        }

        #endregion
    }
}
