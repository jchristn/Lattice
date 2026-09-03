namespace Lattice.Core.Models
{
    using System;

    /// <summary>
    /// An interactive user within a tenant. Users authenticate with email and password and receive a
    /// session token. Email is unique within a tenant, not globally.
    /// </summary>
    public class User
    {
        #region Public-Members

        /// <summary>
        /// Unique identifier for the user (usr_{prettyid}).
        /// </summary>
        public string Id { get; set; } = null;

        /// <summary>
        /// Identifier of the tenant this user belongs to.
        /// </summary>
        public string TenantId { get; set; } = null;

        /// <summary>
        /// First name.
        /// </summary>
        public string FirstName { get; set; } = null;

        /// <summary>
        /// Last name.
        /// </summary>
        public string LastName { get; set; } = null;

        /// <summary>
        /// Email address. Unique within the tenant.
        /// </summary>
        public string Email { get; set; } = null;

        /// <summary>
        /// SHA-256 hex hash of the user's password. Never contains a plaintext password.
        /// </summary>
        public string PasswordSha256 { get; set; } = null;

        /// <summary>
        /// Whether the user is a system administrator with full cross-tenant access. Default false.
        /// </summary>
        public bool IsAdmin { get; set; } = false;

        /// <summary>
        /// Whether the user is a tenant administrator, bypassing RBAC within their own tenant. Default false.
        /// </summary>
        public bool IsTenantAdmin { get; set; } = false;

        /// <summary>
        /// Whether the user is active. Inactive users cannot authenticate.
        /// </summary>
        public bool Active { get; set; } = true;

        /// <summary>
        /// Whether the user is protected from deletion (for example the seeded default administrator).
        /// </summary>
        public bool IsProtected { get; set; } = false;

        /// <summary>
        /// Timestamp when the user was created (UTC).
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when the user was last updated (UTC).
        /// </summary>
        public DateTime LastUpdateUtc { get; set; } = DateTime.UtcNow;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public User()
        {
        }

        #endregion
    }
}
