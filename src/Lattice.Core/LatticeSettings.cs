namespace Lattice.Core
{
    using System;

    /// <summary>
    /// Settings for the Lattice client.
    /// </summary>
    public class LatticeSettings
    {
        #region Public-Members

        /// <summary>
        /// Database settings.
        /// </summary>
        public DatabaseSettings Database
        {
            get => _Database;
            set
            {
                if (value == null) _Database = new DatabaseSettings();
                else _Database = value;
            }
        }

        /// <summary>
        /// Whether to use in-memory database.
        /// </summary>
        public bool InMemory { get; set; } = false;

        /// <summary>
        /// Default directory for document storage if not specified per collection.
        /// </summary>
        public string DefaultDocumentsDirectory { get; set; } = "./documents";

        /// <summary>
        /// Enable verbose logging.
        /// </summary>
        public bool EnableLogging { get; set; } = false;

        #endregion

        #region Private-Members

        private DatabaseSettings _Database = new DatabaseSettings();

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
