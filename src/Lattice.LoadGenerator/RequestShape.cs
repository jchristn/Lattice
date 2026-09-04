namespace Lattice.LoadGenerator
{
    /// <summary>
    /// A synthetic request template: the request type, HTTP method, path, and the collection it targets
    /// (if any). Used to build a realistic request-history entry.
    /// </summary>
    public class RequestShape
    {
        #region Public-Members

        /// <summary>Request type bucket (healthCheck, collection, document, search).</summary>
        public string RequestType { get; }

        /// <summary>HTTP method.</summary>
        public string Method { get; }

        /// <summary>Request path.</summary>
        public string Path { get; }

        /// <summary>Targeted collection identifier, or null.</summary>
        public string CollectionId { get; }

        #endregion

        #region Constructors-and-Factories

        /// <summary>Instantiate.</summary>
        /// <param name="requestType">Request type bucket.</param>
        /// <param name="method">HTTP method.</param>
        /// <param name="path">Request path.</param>
        /// <param name="collectionId">Targeted collection identifier, or null.</param>
        public RequestShape(string requestType, string method, string path, string collectionId)
        {
            RequestType = requestType;
            Method = method;
            Path = path;
            CollectionId = collectionId;
        }

        #endregion
    }
}
