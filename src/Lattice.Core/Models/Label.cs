namespace Lattice.Core.Models
{
    using System;

    /// <summary>
    /// Represents a label associated with a collection or document.
    /// </summary>
    public class Label
    {
        #region Public-Members

        /// <summary>
        /// Unique identifier for the label (lbl_{prettyid}).
        /// </summary>
        public string Id { get; set; } = null;

        /// <summary>
        /// Identifier of the collection this label belongs to (null for document-only labels).
        /// For document labels, this should be populated with the document's collection ID.
        /// </summary>
        public string CollectionId { get; set; } = null;

        /// <summary>
        /// Identifier of the document this label belongs to (null for collection-only labels).
        /// </summary>
        public string DocumentId { get; set; } = null;

        /// <summary>
        /// The label value.
        /// </summary>
        public string LabelValue { get; set; } = null;

        /// <summary>
        /// Timestamp when the label was created (UTC).
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when the label was last updated (UTC).
        /// </summary>
        public DateTime LastUpdateUtc { get; set; } = DateTime.UtcNow;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public Label()
        {
        }

        #endregion
    }
}
