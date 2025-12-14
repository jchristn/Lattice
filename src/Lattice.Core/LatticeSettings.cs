namespace Lattice.Core
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Settings for the Lattice client.
    /// </summary>
    public class LatticeSettings
    {
        #region Public-Members

        /// <summary>
        /// Database filename for SQLite.
        /// </summary>
        [JsonPropertyName("databaseFilename")]
        public string DatabaseFilename
        {
            get => _DatabaseFilename;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentNullException(nameof(DatabaseFilename));
                _DatabaseFilename = value;
            }
        }

        /// <summary>
        /// Whether to use in-memory database.
        /// </summary>
        [JsonPropertyName("inMemory")]
        public bool InMemory { get; set; } = false;

        /// <summary>
        /// Default directory for document storage if not specified per collection.
        /// </summary>
        [JsonPropertyName("defaultDocumentsDirectory")]
        public string DefaultDocumentsDirectory { get; set; } = "./documents";

        /// <summary>
        /// Enable verbose logging.
        /// </summary>
        [JsonPropertyName("enableLogging")]
        public bool EnableLogging { get; set; } = false;

        #endregion

        #region Private-Members

        private string _DatabaseFilename = "lattice.db";

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public LatticeSettings()
        {
        }

        #endregion
    }
}
