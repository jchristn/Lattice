namespace Lattice.Server.Classes
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;

    /// <summary>
    /// Request context containing metadata about the incoming request.
    /// </summary>
    public class RequestContext
    {
        #region Public-Members

        /// <summary>
        /// Unique identifier for this request.
        /// </summary>
        public string Guid { get; set; } = System.Guid.NewGuid().ToString();

        /// <summary>
        /// API version.
        /// </summary>
        public string ApiVersion { get; set; } = "1.0";

        /// <summary>
        /// Request type.
        /// </summary>
        public RequestTypeEnum RequestType { get; set; } = RequestTypeEnum.Unknown;

        /// <summary>
        /// Client IP address.
        /// </summary>
        public string? IpAddress { get; set; } = null;

        /// <summary>
        /// HTTP method.
        /// </summary>
        public string? Method { get; set; } = null;

        /// <summary>
        /// Full URL of the request.
        /// </summary>
        public string? Url { get; set; } = null;

        /// <summary>
        /// Query parameters.
        /// </summary>
        public NameValueCollection QueryParams { get; set; } = new NameValueCollection();

        /// <summary>
        /// Request headers.
        /// </summary>
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public RequestContext()
        {
        }

        #endregion
    }
}
