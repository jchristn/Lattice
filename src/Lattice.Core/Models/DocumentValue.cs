namespace Lattice.Core.Models
{
    using System;

    /// <summary>
    /// Represents a flattened value from a document stored in an index.
    /// </summary>
    public class DocumentValue
    {
        #region Public-Members

        /// <summary>
        /// Unique identifier for the value (val_{prettyid}).
        /// </summary>
        public string Id { get; set; } = null;

        /// <summary>
        /// Identifier of the document this value belongs to.
        /// </summary>
        public string DocumentId { get; set; } = null;

        /// <summary>
        /// Identifier of the schema.
        /// </summary>
        public string SchemaId { get; set; } = null;

        /// <summary>
        /// Identifier of the schema element.
        /// </summary>
        public string SchemaElementId { get; set; } = null;

        /// <summary>
        /// Array position if this value is from an array element, null otherwise.
        /// </summary>
        public int? Position { get; set; } = null;

        /// <summary>
        /// The string representation of the value.
        /// </summary>
        public string Value { get; set; } = null;

        /// <summary>
        /// Timestamp when the value was created (UTC).
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when the value was last updated (UTC).
        /// </summary>
        public DateTime LastUpdateUtc { get; set; } = DateTime.UtcNow;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public DocumentValue()
        {
        }

        #endregion
    }
}
