namespace Lattice.Core.Validation
{
    using System.Collections.Generic;

    /// <summary>
    /// Represents the result of a schema validation operation.
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// Whether the validation passed.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// List of validation errors (empty if valid).
        /// </summary>
        public List<ValidationError> Errors { get; set; } = new List<ValidationError>();

        /// <summary>
        /// Create a successful validation result.
        /// </summary>
        public static ValidationResult Success() => new ValidationResult { IsValid = true };

        /// <summary>
        /// Create a failed validation result with a single error.
        /// </summary>
        public static ValidationResult Failure(ValidationError error)
        {
            return new ValidationResult
            {
                IsValid = false,
                Errors = new List<ValidationError> { error }
            };
        }

        /// <summary>
        /// Create a failed validation result with multiple errors.
        /// </summary>
        public static ValidationResult Failure(List<ValidationError> errors)
        {
            return new ValidationResult
            {
                IsValid = false,
                Errors = errors ?? new List<ValidationError>()
            };
        }
    }

    /// <summary>
    /// Represents a single validation error.
    /// </summary>
    public class ValidationError
    {
        /// <summary>
        /// The field path that failed validation.
        /// </summary>
        public string FieldPath { get; set; }

        /// <summary>
        /// Error code identifying the type of validation failure.
        /// </summary>
        public string ErrorCode { get; set; }

        /// <summary>
        /// Human-readable error message.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// The actual value that failed validation.
        /// </summary>
        public object ActualValue { get; set; }

        /// <summary>
        /// The expected value or constraint that was violated.
        /// </summary>
        public object ExpectedValue { get; set; }
    }

    /// <summary>
    /// Error codes for schema validation failures.
    /// </summary>
    public static class ValidationErrorCodes
    {
        /// <summary>Required field not present.</summary>
        public const string MissingRequiredField = "MISSING_REQUIRED_FIELD";

        /// <summary>Field not in schema (Strict mode).</summary>
        public const string UnexpectedField = "UNEXPECTED_FIELD";

        /// <summary>Value type doesn't match expected type.</summary>
        public const string TypeMismatch = "TYPE_MISMATCH";

        /// <summary>Null value in non-nullable field.</summary>
        public const string NullNotAllowed = "NULL_NOT_ALLOWED";

        /// <summary>String doesn't match regex pattern.</summary>
        public const string PatternMismatch = "PATTERN_MISMATCH";

        /// <summary>Number below MinValue.</summary>
        public const string ValueTooSmall = "VALUE_TOO_SMALL";

        /// <summary>Number above MaxValue.</summary>
        public const string ValueTooLarge = "VALUE_TOO_LARGE";

        /// <summary>String below MinLength.</summary>
        public const string StringTooShort = "STRING_TOO_SHORT";

        /// <summary>String above MaxLength.</summary>
        public const string StringTooLong = "STRING_TOO_LONG";

        /// <summary>Array below MinLength.</summary>
        public const string ArrayTooShort = "ARRAY_TOO_SHORT";

        /// <summary>Array above MaxLength.</summary>
        public const string ArrayTooLong = "ARRAY_TOO_LONG";

        /// <summary>Value not in AllowedValues list.</summary>
        public const string ValueNotAllowed = "VALUE_NOT_ALLOWED";

        /// <summary>Array element doesn't match expected type.</summary>
        public const string InvalidArrayElement = "INVALID_ARRAY_ELEMENT";
    }
}
