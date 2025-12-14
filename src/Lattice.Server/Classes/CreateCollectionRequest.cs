namespace Lattice.Server.Classes
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Request model for creating a collection.
    /// </summary>
    public class CreateCollectionRequest
    {
        #region Public-Members

        /// <summary>
        /// Collection name.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = null!;

        /// <summary>
        /// Collection description.
        /// </summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>
        /// Directory for storing documents.
        /// </summary>
        [JsonPropertyName("documentsDirectory")]
        public string? DocumentsDirectory { get; set; }

        /// <summary>
        /// Collection labels.
        /// </summary>
        [JsonPropertyName("labels")]
        public List<string>? Labels { get; set; }

        /// <summary>
        /// Collection tags.
        /// </summary>
        [JsonPropertyName("tags")]
        public Dictionary<string, string>? Tags { get; set; }

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
