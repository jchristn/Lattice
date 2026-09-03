namespace Lattice.Core.Security
{
    using System;

    /// <summary>
    /// The result of an interactive login: the issued session token and its expiry, plus the resolved
    /// principal.
    /// </summary>
    public class LoginResult
    {
        /// <summary>
        /// The opaque session token to present as a bearer on subsequent requests.
        /// </summary>
        public string Token { get; set; } = null;

        /// <summary>
        /// When the token expires (UTC).
        /// </summary>
        public DateTime ExpiresUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// The resolved principal.
        /// </summary>
        public CallerContext Caller { get; set; } = null;

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public LoginResult()
        {
        }
    }
}
