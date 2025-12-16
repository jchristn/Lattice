namespace Lattice.Sdk.Exceptions
{
    /// <summary>
    /// Base exception for all Lattice SDK errors.
    /// </summary>
    public class LatticeException : Exception
    {
        /// <summary>
        /// Gets the HTTP status code associated with the error, if any.
        /// </summary>
        public int? StatusCode { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LatticeException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public LatticeException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LatticeException"/> class with a specified error message and status code.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="statusCode">The HTTP status code associated with the error.</param>
        public LatticeException(string message, int statusCode) : base(message)
        {
            StatusCode = statusCode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LatticeException"/> class with a specified error message and inner exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public LatticeException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Raised when unable to connect to the Lattice server.
    /// </summary>
    public class LatticeConnectionException : LatticeException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LatticeConnectionException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public LatticeConnectionException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LatticeConnectionException"/> class with a specified error message and inner exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public LatticeConnectionException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Raised when the API returns an error response.
    /// </summary>
    public class LatticeApiException : LatticeException
    {
        /// <summary>
        /// Gets the error message returned by the API.
        /// </summary>
        public string? ApiErrorMessage { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LatticeApiException"/> class with a specified error message, status code, and optional API error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="statusCode">The HTTP status code returned by the API.</param>
        /// <param name="apiErrorMessage">The error message returned by the API.</param>
        public LatticeApiException(string message, int statusCode, string? apiErrorMessage = null)
            : base(message, statusCode)
        {
            ApiErrorMessage = apiErrorMessage ?? message;
        }
    }

    /// <summary>
    /// Raised when request validation fails.
    /// </summary>
    public class LatticeValidationException : LatticeException
    {
        /// <summary>
        /// Gets the name of the field that failed validation, if any.
        /// </summary>
        public string? Field { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LatticeValidationException"/> class with a specified error message and optional field name.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="field">The name of the field that failed validation.</param>
        public LatticeValidationException(string message, string? field = null) : base(message)
        {
            Field = field;
        }
    }
}
