namespace Lattice.Server.Classes
{
    /// <summary>
    /// Error response body returned for non-success (4xx/5xx) HTTP responses. Serialized as
    /// <c>{ "error": "...", "detail": ... }</c>, where <c>detail</c> is omitted when null.
    /// </summary>
    public class ErrorResponse
    {
        /// <summary>
        /// Human-readable error message.
        /// </summary>
        public string Error { get; set; } = null;

        /// <summary>
        /// Optional structured error detail (for example validation errors or document lock
        /// information). Omitted from the serialized body when null.
        /// </summary>
        public object Detail { get; set; } = null;
    }
}
