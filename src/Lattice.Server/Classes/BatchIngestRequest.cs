namespace Lattice.Server.Classes
{
    using System.Collections.Generic;

    /// <summary>
    /// Request model for batch document ingestion.
    /// </summary>
    public class BatchIngestRequest
    {
        #region Public-Members

        /// <summary>
        /// List of documents to ingest.
        /// </summary>
        public List<BatchIngestDocumentEntry> Documents { get; set; } = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public BatchIngestRequest()
        {
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Validate the request.
        /// </summary>
        /// <param name="errorMessage">Error message if validation fails.</param>
        /// <returns>True if valid, false otherwise.</returns>
        public bool Validate(out string errorMessage)
        {
            errorMessage = string.Empty;

            if (Documents == null || Documents.Count == 0)
            {
                errorMessage = "Documents list is required and must contain at least one document";
                return false;
            }

            for (int i = 0; i < Documents.Count; i++)
            {
                if (Documents[i].Content == null)
                {
                    errorMessage = $"Document at index {i} is missing required 'content' field";
                    return false;
                }
            }

            return true;
        }

        #endregion
    }

    /// <summary>
    /// A single document entry within a batch ingestion request.
    /// </summary>
    public class BatchIngestDocumentEntry
    {
        #region Public-Members

        /// <summary>
        /// Document content (JSON object).
        /// </summary>
        public object Content { get; set; } = null!;

        /// <summary>
        /// Optional document name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Optional document labels.
        /// </summary>
        public List<string>? Labels { get; set; }

        /// <summary>
        /// Optional document tags.
        /// </summary>
        public Dictionary<string, string>? Tags { get; set; }

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public BatchIngestDocumentEntry()
        {
        }

        #endregion
    }
}
