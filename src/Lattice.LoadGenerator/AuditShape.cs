namespace Lattice.LoadGenerator
{
    /// <summary>
    /// A synthetic audit template: the event type, the HTTP method and path that produced it, and the
    /// response code. Used to build a realistic audit entry.
    /// </summary>
    public class AuditShape
    {
        #region Public-Members

        /// <summary>Audit event type.</summary>
        public string EventType { get; }

        /// <summary>HTTP method.</summary>
        public string Method { get; }

        /// <summary>Request path.</summary>
        public string Path { get; }

        /// <summary>HTTP response code.</summary>
        public int ResponseCode { get; }

        #endregion

        #region Constructors-and-Factories

        /// <summary>Instantiate.</summary>
        /// <param name="eventType">Audit event type.</param>
        /// <param name="method">HTTP method.</param>
        /// <param name="path">Request path.</param>
        /// <param name="responseCode">HTTP response code.</param>
        public AuditShape(string eventType, string method, string path, int responseCode)
        {
            EventType = eventType;
            Method = method;
            Path = path;
            ResponseCode = responseCode;
        }

        #endregion
    }
}
