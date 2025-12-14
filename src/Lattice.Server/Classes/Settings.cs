namespace Lattice.Server.Classes
{
    using System;
    using System.IO;
    using System.Text.Json;
    using Lattice.Core;

    /// <summary>
    /// Application settings.
    /// </summary>
    public class Settings
    {
        #region Public-Members

        /// <summary>
        /// Settings creation timestamp.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Logging settings.
        /// </summary>
        public LoggingSettings Logging
        {
            get
            {
                return _Logging;
            }
            set
            {
                if (value == null) _Logging = new LoggingSettings();
                else _Logging = value;
            }
        }

        /// <summary>
        /// REST API settings.
        /// </summary>
        public RestSettings Rest
        {
            get
            {
                return _Rest;
            }
            set
            {
                if (value == null) _Rest = new RestSettings();
                else _Rest = value;
            }
        }

        /// <summary>
        /// Lattice client settings.
        /// </summary>
        public LatticeSettings Lattice
        {
            get
            {
                return _Lattice;
            }
            set
            {
                if (value == null) _Lattice = new LatticeSettings();
                else _Lattice = value;
            }
        }

        #endregion

        #region Private-Members

        private LoggingSettings _Logging = new LoggingSettings();
        private RestSettings _Rest = new RestSettings();
        private LatticeSettings _Lattice = new LatticeSettings();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public Settings()
        {
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Load settings from file.
        /// </summary>
        /// <param name="filename">Filename.</param>
        /// <returns>Settings.</returns>
        /// <exception cref="ArgumentNullException">Thrown when filename is null or empty.</exception>
        public static Settings FromFile(string filename)
        {
            if (String.IsNullOrEmpty(filename)) throw new ArgumentNullException(nameof(filename));

            if (!File.Exists(filename))
            {
                Settings defaultSettings = new Settings();
                defaultSettings.ToFile(filename);
                return defaultSettings;
            }

            string json = File.ReadAllText(filename);

            Settings? settings = JsonSerializer.Deserialize<Settings>(json, GetJsonSerializerOptions());

            return settings ?? new Settings();
        }

        /// <summary>
        /// Save settings to file.
        /// </summary>
        /// <param name="filename">Filename.</param>
        /// <exception cref="ArgumentNullException">Thrown when filename is null or empty.</exception>
        public void ToFile(string filename)
        {
            if (String.IsNullOrEmpty(filename)) throw new ArgumentNullException(nameof(filename));

            string json = JsonSerializer.Serialize(this, GetJsonSerializerOptions());

            File.WriteAllText(filename, json);
        }

        #endregion

        #region Private-Methods

        private static JsonSerializerOptions GetJsonSerializerOptions()
        {
            return new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = null
            };
        }

        #endregion
    }
}
