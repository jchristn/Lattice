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

        /// <summary>
        /// Enable object locking during document ingestion. Default is true.
        /// </summary>
        public bool EnableObjectLocking { get; set; } = true;

        /// <summary>
        /// Object lock expiration time in seconds. Default is 30 seconds.
        /// </summary>
        public int ObjectLockExpirationSeconds
        {
            get => _ObjectLockExpirationSeconds;
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException(nameof(ObjectLockExpirationSeconds), "Must be at least 1 second.");
                _ObjectLockExpirationSeconds = value;
            }
        }

        /// <summary>
        /// Hostname identifier for this node (used for lock ownership). Auto-detected if not set.
        /// </summary>
        public string Hostname
        {
            get => _Hostname ?? Environment.MachineName;
            set => _Hostname = value;
        }

        #endregion

        #region Private-Members

        private DatabaseSettings _Database = new DatabaseSettings();
        private int _ObjectLockExpirationSeconds = 30;
        private string _Hostname = null;

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
