namespace Lattice.Core.Models
{
    using System.Collections.Generic;

    /// <summary>
    /// Represents a document to be ingested as part of a batch operation.
    /// </summary>
    public class BatchDocument
    {
        #region Public-Members

        /// <summary>
        /// The raw JSON content of the document.
        /// </summary>
        public string Json { get; set; } = null;

        /// <summary>
        /// Optional name for the document.
        /// </summary>
        public string Name { get; set; } = null;

        /// <summary>
        /// Optional labels for the document.
        /// </summary>
        public List<string> Labels { get; set; } = null;

        /// <summary>
        /// Optional key-value tags for the document.
        /// </summary>
        public Dictionary<string, string> Tags { get; set; } = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public BatchDocument()
        {
        }

        /// <summary>
        /// Instantiate with JSON content.
        /// </summary>
        /// <param name="json">JSON document content.</param>
        /// <param name="name">Optional document name.</param>
        /// <param name="labels">Optional labels.</param>
        /// <param name="tags">Optional tags.</param>
        public BatchDocument(
            string json,
            string name = null,
            List<string> labels = null,
            Dictionary<string, string> tags = null)
        {
            Json = json;
            Name = name;
            Labels = labels;
            Tags = tags;
        }

        #endregion
    }
}
