namespace Lattice.Server.Classes
{
    /// <summary>
    /// Authentication and authorization settings. When enabled, every route except health, the OpenAPI
    /// spec, and the login endpoint requires a bearer token (a session token or a credential access key),
    /// and requests are authorized against the RBAC model.
    /// </summary>
    public class AuthSettings
    {
        /// <summary>
        /// Whether authentication and authorization are enforced. Default true.
        /// </summary>
        public bool Enable { get; set; } = true;

        /// <summary>
        /// The server-side secret used to encrypt session tokens. Change this per deployment; the default
        /// is suitable only for local development.
        /// </summary>
        public string TokenSecret { get; set; } = "lattice-token-secret-change-me";

        /// <summary>
        /// Session lifetime in minutes. Default 60, clamped to 5..1440 when applied.
        /// </summary>
        public int SessionTtlMinutes { get; set; } = 60;

        /// <summary>
        /// Name of the default tenant created on first run.
        /// </summary>
        public string DefaultTenantName { get; set; } = "Default Tenant";

        /// <summary>
        /// Email of the default administrator created on first run.
        /// </summary>
        public string DefaultAdminEmail { get; set; } = "admin@lattice";

        /// <summary>
        /// Password of the default administrator created on first run. Change this for any shared deployment.
        /// </summary>
        public string DefaultAdminPassword { get; set; } = "password";
    }
}
