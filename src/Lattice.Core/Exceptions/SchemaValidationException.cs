namespace Lattice.Core.Exceptions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Lattice.Core.Validation;

    /// <summary>
    /// Exception thrown when a document fails schema validation.
    /// </summary>
    public class SchemaValidationException : Exception
    {
        /// <summary>
        /// The collection ID where validation failed.
        /// </summary>
        public string CollectionId { get; }

        /// <summary>
        /// The list of validation errors.
        /// </summary>
        public List<ValidationError> Errors { get; }

        /// <summary>
        /// Create a new schema validation exception.
        /// </summary>
        /// <param name="collectionId">Collection ID where validation failed.</param>
        /// <param name="errors">List of validation errors.</param>
        public SchemaValidationException(string collectionId, List<ValidationError> errors)
            : base($"Document failed schema validation for collection {collectionId}: {FormatErrors(errors)}")
        {
            CollectionId = collectionId;
            Errors = errors ?? new List<ValidationError>();
        }

        /// <summary>
        /// Create a new schema validation exception with a single error.
        /// </summary>
        /// <param name="collectionId">Collection ID where validation failed.</param>
        /// <param name="error">Validation error.</param>
        public SchemaValidationException(string collectionId, ValidationError error)
            : this(collectionId, new List<ValidationError> { error })
        {
        }

        private static string FormatErrors(List<ValidationError> errors)
        {
            if (errors == null || errors.Count == 0)
                return "No specific errors";

            return string.Join("; ", errors.Select(e => $"{e.FieldPath}: {e.Message}"));
        }
    }
}
