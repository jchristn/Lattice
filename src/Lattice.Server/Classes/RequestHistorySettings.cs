namespace Lattice.Server.Classes
{
    using System;

    /// <summary>
    /// Request history settings.
    /// </summary>
    public class RequestHistorySettings
    {
        #region Public-Members

        /// <summary>
        /// Enable request history capture.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Maximum request body bytes to capture.
        /// </summary>
        public int MaxRequestBodyBytes
        {
            get => _MaxRequestBodyBytes;
            set => _MaxRequestBodyBytes = value < 1024 ? 1024 : (value > 1048576 ? 1048576 : value);
        }

        /// <summary>
        /// Maximum response body bytes to capture.
        /// </summary>
        public int MaxResponseBodyBytes
        {
            get => _MaxResponseBodyBytes;
            set => _MaxResponseBodyBytes = value < 1024 ? 1024 : (value > 1048576 ? 1048576 : value);
        }

        /// <summary>
        /// Number of days to retain request history records.
        /// </summary>
        public int RetentionDays
        {
            get => _RetentionDays;
            set => _RetentionDays = value < 1 ? 1 : (value > 3650 ? 3650 : value);
        }

        /// <summary>
        /// Background prune interval in minutes.
        /// </summary>
        public int PruneIntervalMinutes
        {
            get => _PruneIntervalMinutes;
            set => _PruneIntervalMinutes = value < 1 ? 1 : (value > 1440 ? 1440 : value);
        }

        #endregion

        #region Private-Members

        private int _MaxRequestBodyBytes = 65536;
        private int _MaxResponseBodyBytes = 65536;
        private int _RetentionDays = 30;
        private int _PruneIntervalMinutes = 60;

        #endregion
    }
}
