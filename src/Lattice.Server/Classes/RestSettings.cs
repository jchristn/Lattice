namespace Lattice.Server.Classes
{
    using System;

    /// <summary>
    /// REST API settings.
    /// </summary>
    public class RestSettings
    {
        #region Public-Members

        /// <summary>
        /// Hostname to bind to.
        /// </summary>
        public string Hostname
        {
            get
            {
                return _Hostname;
            }
            set
            {
                if (String.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(Hostname));
                _Hostname = value;
            }
        }

        /// <summary>
        /// Port number to listen on.
        /// </summary>
        public int Port
        {
            get
            {
                return _Port;
            }
            set
            {
                if (value < 1 || value > 65535) throw new ArgumentOutOfRangeException(nameof(Port));
                _Port = value;
            }
        }

        /// <summary>
        /// Enable SSL/TLS.
        /// </summary>
        public bool Ssl
        {
            get
            {
                return _Ssl;
            }
            set
            {
                _Ssl = value;
            }
        }

        /// <summary>
        /// SSL certificate filename.
        /// </summary>
        public string? SslCertificateFile
        {
            get
            {
                return _SslCertificateFile;
            }
            set
            {
                _SslCertificateFile = value;
            }
        }

        /// <summary>
        /// SSL certificate password.
        /// </summary>
        public string? SslCertificatePassword
        {
            get
            {
                return _SslCertificatePassword;
            }
            set
            {
                _SslCertificatePassword = value;
            }
        }

        /// <summary>
        /// Enable OpenAPI documentation endpoint.
        /// When enabled, the OpenAPI JSON specification is available at /openapi.json.
        /// This provides machine-readable API documentation for client generation and tooling.
        /// </summary>
        public bool EnableOpenApi { get; set; } = true;

        /// <summary>
        /// Enable Swagger UI.
        /// When enabled, an interactive API documentation UI is available at /swagger.
        /// Requires EnableOpenApi to be true for the UI to function.
        /// </summary>
        public bool EnableSwaggerUi { get; set; } = true;

        #endregion

        #region Private-Members

        private string _Hostname = "localhost";
        private int _Port = 8000;
        private bool _Ssl = false;
        private string? _SslCertificateFile = null;
        private string? _SslCertificatePassword = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public RestSettings()
        {
        }

        #endregion
    }
}
