namespace Lattice.Server.Classes
{
    /// <summary>
    /// Request type enumeration.
    /// </summary>
    public enum RequestTypeEnum
    {
        /// <summary>
        /// Unknown request type.
        /// </summary>
        Unknown,

        /// <summary>
        /// Health check request.
        /// </summary>
        HealthCheck,

        /// <summary>
        /// Collection management request.
        /// </summary>
        Collection,

        /// <summary>
        /// Document operations request.
        /// </summary>
        Document,

        /// <summary>
        /// Search operation request.
        /// </summary>
        Search
    }
}
