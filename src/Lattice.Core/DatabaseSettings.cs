namespace Lattice.Core
{
    using System;

    /// <summary>
    /// Database connection settings.
    /// </summary>
    public class DatabaseSettings
    {
        #region Public-Members

        /// <summary>
        /// Database type.
        /// </summary>
        public DatabaseTypeEnum Type
        {
            get => _Type;
            set => _Type = value;
        }

        /// <summary>
        /// Database filename (for SQLite).
        /// </summary>
        public string Filename
        {
            get => _Filename;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentNullException(nameof(Filename));
                _Filename = value;
            }
        }

        /// <summary>
        /// Database server hostname.
        /// </summary>
        public string Hostname { get; set; } = "localhost";

        /// <summary>
        /// Database server port.
        /// </summary>
        public int Port { get; set; } = 0;

        /// <summary>
        /// Database name.
        /// </summary>
        public string DatabaseName { get; set; } = null;

        /// <summary>
        /// Database username.
        /// </summary>
        public string Username { get; set; } = null;

        /// <summary>
        /// Database password.
        /// </summary>
        public string Password { get; set; } = null;

        #endregion

        #region Private-Members

        private DatabaseTypeEnum _Type = DatabaseTypeEnum.Sqlite;
        private string _Filename = "lattice.db";

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public DatabaseSettings()
        {
        }

        #endregion
    }
}
