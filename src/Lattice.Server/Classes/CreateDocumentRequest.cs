namespace Lattice.Server.Classes
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Request model for creating a document.
    /// </summary>
    public class CreateDocumentRequest
    {
        #region Public-Members

        /// <summary>
        /// Document content (JSON object).
        /// </summary>
        public object Content { get; set; } = null!;

        /// <summary>
        /// Document name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Document labels.
        /// </summary>
        public List<string>? Labels { get; set; }

        /// <summary>
        /// Document tags.
        /// </summary>
        public Dictionary<string, string>? Tags { get; set; }

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public CreateDocumentRequest()
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

            if (Content == null)
            {
                errorMessage = "Content is required";
                return false;
            }

            return true;
        }

        #endregion
    }
}
