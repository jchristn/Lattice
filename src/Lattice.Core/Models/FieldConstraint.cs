namespace Lattice.Core.Models
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents a schema constraint for a specific field in a collection.
    /// </summary>
    public class FieldConstraint
    {
        #region Public-Members

        /// <summary>
        /// Unique identifier for the field constraint (fco_{prettyid}).
        /// </summary>
        public string Id { get; set; } = null;

        /// <summary>
        /// Foreign key to the collection this constraint belongs to.
        /// </summary>
        public string CollectionId { get; set; } = null;

        /// <summary>
        /// Dot-notation path to the field (e.g., "Person.Address.City").
        /// </summary>
        public string FieldPath { get; set; } = null;

        /// <summary>
        /// Expected data type: "string", "integer", "number", "boolean", "array", "object".
        /// </summary>
        public string DataType { get; set; } = null;

        /// <summary>
        /// Whether the field must be present in the document.
        /// </summary>
        public bool Required { get; set; } = false;

        /// <summary>
        /// Whether the field value can be null.
        /// </summary>
        public bool Nullable { get; set; } = true;

        /// <summary>
        /// Regular expression pattern for string validation.
        /// </summary>
        public string RegexPattern { get; set; } = null;

        /// <summary>
        /// Minimum value for number fields.
        /// </summary>
        public decimal? MinValue { get; set; } = null;

        /// <summary>
        /// Maximum value for number fields.
        /// </summary>
        public decimal? MaxValue { get; set; } = null;

        /// <summary>
        /// Minimum length for strings or minimum element count for arrays.
        /// </summary>
        public int? MinLength { get; set; } = null;

        /// <summary>
        /// Maximum length for strings or maximum element count for arrays.
        /// </summary>
        public int? MaxLength { get; set; } = null;

        /// <summary>
        /// List of allowed values (enum-like constraint).
        /// </summary>
        public List<string> AllowedValues
        {
            get => _AllowedValues;
            set => _AllowedValues = value ?? new List<string>();
        }

        /// <summary>
        /// Expected element type for array fields.
        /// </summary>
        public string ArrayElementType { get; set; } = null;

        /// <summary>
        /// Timestamp when the constraint was created (UTC).
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when the constraint was last updated (UTC).
        /// </summary>
        public DateTime LastUpdateUtc { get; set; } = DateTime.UtcNow;

        #endregion

        #region Private-Members

        private List<string> _AllowedValues = new List<string>();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public FieldConstraint()
        {
        }

        #endregion
    }
}
