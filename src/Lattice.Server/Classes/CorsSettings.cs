namespace Lattice.Server.Classes
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Cross-Origin Resource Sharing (CORS) settings.
    /// Defaults are permissive so that a missing CORS section in configuration
    /// yields a fully-open, browser-friendly instance.
    /// </summary>
    public class CorsSettings
    {
        #region Public-Members

        /// <summary>
        /// Enable or disable CORS support.
        /// Default is true.
        /// </summary>
        public bool Enable { get; set; } = true;

        /// <summary>
        /// List of allowed origins. Use "*" to allow all origins.
        /// When AllowCredentials is true, "*" is not permitted and the matching
        /// request origin is echoed back instead.
        /// Default is "*" (all origins allowed).
        /// </summary>
        public List<string> AllowOrigins
        {
            get => _AllowOrigins;
            set => _AllowOrigins = value ?? new List<string>();
        }

        /// <summary>
        /// List of allowed HTTP methods.
        /// Default: GET, POST, PUT, DELETE, PATCH, HEAD, OPTIONS.
        /// </summary>
        public List<string> AllowMethods
        {
            get => _AllowMethods;
            set => _AllowMethods = value ?? new List<string>();
        }

        /// <summary>
        /// List of allowed request headers.
        /// Default is "*" (all headers allowed).
        /// </summary>
        public List<string> AllowHeaders
        {
            get => _AllowHeaders;
            set => _AllowHeaders = value ?? new List<string>();
        }

        /// <summary>
        /// List of response headers to expose to the browser.
        /// Default exposes the Lattice request-id and content-type headers the dashboard reads.
        /// </summary>
        public List<string> ExposeHeaders
        {
            get => _ExposeHeaders;
            set => _ExposeHeaders = value ?? new List<string>();
        }

        /// <summary>
        /// Whether to allow credentials (cookies, authorization headers).
        /// When true, AllowOrigins cannot contain "*"; the request origin is echoed instead.
        /// Default is false.
        /// </summary>
        public bool AllowCredentials { get; set; } = false;

        /// <summary>
        /// How long (in seconds) browsers should cache preflight results.
        /// Default is 86400 (24 hours). Minimum is 0, maximum is 86400.
        /// </summary>
        public int MaxAgeSeconds
        {
            get => _MaxAgeSeconds;
            set => _MaxAgeSeconds = Math.Clamp(value, 0, 86400);
        }

        #endregion

        #region Private-Members

        private List<string> _AllowOrigins = new List<string> { "*" };
        private List<string> _AllowMethods = new List<string> { "GET", "POST", "PUT", "DELETE", "PATCH", "HEAD", "OPTIONS" };
        private List<string> _AllowHeaders = new List<string> { "*" };
        private List<string> _ExposeHeaders = new List<string> { "Content-Type", "X-Requested-With", "X-Lattice-Request-Id" };
        private int _MaxAgeSeconds = 86400;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public CorsSettings()
        {
        }

        #endregion
    }
}
