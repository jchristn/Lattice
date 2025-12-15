namespace Lattice.Server.Classes
{
    using System;
    using System.Collections.Generic;
    using Lattice.Core.Models;

    /// <summary>
    /// Request model for creating a collection.
    /// </summary>
    public class CreateCollectionRequest
    {
        #region Public-Members

        /// <summary>
        /// Collection name.
        /// </summary>
        public string Name { get; set; } = null!;

        /// <summary>
        /// Collection description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Directory for storing documents.
        /// </summary>
        public string? DocumentsDirectory { get; set; }

        /// <summary>
        /// Collection labels.
        /// </summary>
        public List<string>? Labels { get; set; }

        /// <summary>
        /// Collection tags.
        /// </summary>
        public Dictionary<string, string>? Tags { get; set; }

        /// <summary>
        /// Schema enforcement mode for documents in this collection.
        /// </summary>
        public SchemaEnforcementMode SchemaEnforcementMode { get; set; } = SchemaEnforcementMode.None;

        /// <summary>
        /// Field constraints for schema validation.
        /// </summary>
        public List<FieldConstraint>? FieldConstraints { get; set; }

        /// <summary>
        /// Indexing mode for documents in this collection.
        /// </summary>
        public IndexingMode IndexingMode { get; set; } = IndexingMode.All;

        /// <summary>
        /// Fields to index (when indexingMode is Selective).
        /// </summary>
        public List<string>? IndexedFields { get; set; }

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public CreateCollectionRequest()
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

            if (String.IsNullOrWhiteSpace(Name))
            {
                errorMessage = "Name is required";
                return false;
            }

            return true;
        }

        #endregion
    }
}
