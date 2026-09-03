namespace Lattice.Sdk.Models
{
    /// <summary>
    /// Error response body returned by the Lattice API for non-success (4xx/5xx) responses,
    /// deserialized from <c>{ "error": "...", "detail": ... }</c>.
    /// </summary>
    public class ApiErrorResponse
    {
        /// <summary>
        /// Human-readable error message.
        /// </summary>
        public string? Error { get; set; }

        /// <summary>
        /// Optional structured error detail, or null.
        /// </summary>
        public object? Detail { get; set; }
    }
}
