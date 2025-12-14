namespace Lattice.Server.Classes
{
    using System;
    using SyslogLogging;

    /// <summary>
    /// Logging settings.
    /// </summary>
    public class LoggingSettings
    {
        #region Public-Members

        /// <summary>
        /// Enable console logging.
        /// </summary>
        public bool ConsoleLogging
        {
            get
            {
                return _ConsoleLogging;
            }
            set
            {
                _ConsoleLogging = value;
            }
        }

        /// <summary>
        /// Minimum severity level for logging.
        /// </summary>
        public Severity MinimumSeverity
        {
            get
            {
                return _MinimumSeverity;
            }
            set
            {
                _MinimumSeverity = value;
            }
        }

        /// <summary>
        /// Log filename.
        /// </summary>
        public string? LogFilename
        {
            get
            {
                return _LogFilename;
            }
            set
            {
                _LogFilename = value;
            }
        }

        #endregion

        #region Private-Members

        private bool _ConsoleLogging = true;
        private Severity _MinimumSeverity = Severity.Info;
        private string? _LogFilename = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public LoggingSettings()
        {
        }

        #endregion
    }
}
