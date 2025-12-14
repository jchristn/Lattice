namespace Lattice.Server.Classes
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Response context containing the response data and metadata.
    /// </summary>
    public class ResponseContext
    {
        #region Public-Members

        /// <summary>
        /// Unique identifier for this response.
        /// </summary>
        public string Guid { get; set; } = System.Guid.NewGuid().ToString();

        /// <summary>
        /// Whether the operation was successful.
        /// </summary>
        public bool Success { get; set; } = true;

        /// <summary>
        /// Timestamp in UTC.
        /// </summary>
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// HTTP status code.
        /// </summary>
        public int StatusCode { get; set; } = 200;

        /// <summary>
        /// Error message if the operation failed.
        /// </summary>
        public string? ErrorMessage { get; set; } = null;

        /// <summary>
        /// Response data payload.
        /// </summary>
        public object? Data { get; set; } = null;

        /// <summary>
        /// Response headers.
        /// </summary>
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Processing time in milliseconds.
        /// </summary>
        public double ProcessingTimeMs { get; set; } = 0;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public ResponseContext()
        {
        }

        /// <summary>
        /// Instantiate with basic parameters.
        /// </summary>
        /// <param name="success">Whether the operation was successful.</param>
        /// <param name="statusCode">HTTP status code.</param>
        /// <param name="errorMessage">Error message if failed.</param>
        public ResponseContext(bool success, int statusCode, string? errorMessage = null)
        {
            Success = success;
            StatusCode = statusCode;
            ErrorMessage = errorMessage;
        }

        #endregion
    }
}
