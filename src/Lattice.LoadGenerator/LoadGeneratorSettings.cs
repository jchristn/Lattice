namespace Lattice.LoadGenerator
{
    using System;
    using System.Collections.Generic;
    using Lattice.Core;

    /// <summary>
    /// Settings controlling synthetic load generation. Populated by <see cref="ArgumentParser"/>.
    /// Category counts fall back to a preset derived from <see cref="Density"/> when not set explicitly.
    /// </summary>
    public class LoadGeneratorSettings
    {
        #region Public-Members

        /// <summary>Database backend to target.</summary>
        public DatabaseTypeEnum Backend { get; set; } = DatabaseTypeEnum.Sqlite;

        /// <summary>Path to the SQLite database file (used when <see cref="Backend"/> is SQLite).</summary>
        public string SqliteFilename { get; set; } = "lattice.db";

        /// <summary>Database server hostname (used for MySQL, PostgreSQL, and SQL Server).</summary>
        public string Hostname { get; set; } = "localhost";

        /// <summary>Database server port. Zero selects the backend default.</summary>
        public int Port { get; set; } = 0;

        /// <summary>Database name (used for MySQL, PostgreSQL, and SQL Server).</summary>
        public string Database { get; set; } = null;

        /// <summary>Database username (used for MySQL, PostgreSQL, and SQL Server).</summary>
        public string Username { get; set; } = null;

        /// <summary>Database password (used for MySQL, PostgreSQL, and SQL Server).</summary>
        public string Password { get; set; } = null;

        /// <summary>
        /// Name of the tenant to seed data into. When null, the first existing tenant (the seeded default)
        /// is used. When set and no such tenant exists, it is created.
        /// </summary>
        public string TenantName { get; set; } = null;

        /// <summary>Preset information density used for any category count left unset.</summary>
        public DensityLevel Density { get; set; } = DensityLevel.Medium;

        /// <summary>Number of days into the past over which synthetic activity is spread. Minimum 1.</summary>
        public int Days
        {
            get
            {
                return _Days;
            }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException(nameof(Days), "Days must be at least 1.");
                _Days = value;
            }
        }

        /// <summary>Categories of activity to generate. Empty means all.</summary>
        public HashSet<OperationKind> Operations { get; set; } = new HashSet<OperationKind>();

        /// <summary>Explicit collection count override, or null to derive from density.</summary>
        public int? CollectionCount { get; set; } = null;

        /// <summary>Explicit documents-per-collection override, or null to derive from density.</summary>
        public int? DocumentsPerCollection { get; set; } = null;

        /// <summary>Explicit request-history entry count override, or null to derive from density.</summary>
        public int? RequestCount { get; set; } = null;

        /// <summary>Explicit audit entry count override, or null to derive from density.</summary>
        public int? AuditCount { get; set; } = null;

        /// <summary>Explicit synthetic user count override, or null to derive from density.</summary>
        public int? UserCount { get; set; } = null;

        /// <summary>Explicit synthetic credential count override, or null to derive from density.</summary>
        public int? CredentialCount { get; set; } = null;

        /// <summary>Explicit custom-role count override, or null to derive from density.</summary>
        public int? RoleCount { get; set; } = null;

        /// <summary>
        /// Optional base URL of a running Lattice server (e.g. http://localhost:8000). When set together
        /// with an access key, the generator also fires a burst of live requests so telemetry/Grafana light up.
        /// </summary>
        public string ServerUrl { get; set; } = null;

        /// <summary>Access key (bearer token) used for the optional live-traffic burst.</summary>
        public string AccessKey { get; set; } = null;

        /// <summary>Number of live requests to fire against <see cref="ServerUrl"/>. Zero disables the burst.</summary>
        public int LiveRequestCount { get; set; } = 0;

        /// <summary>Random number generator seed. Null selects a time-based seed.</summary>
        public int? Seed { get; set; } = null;

        /// <summary>Delete previously generated synthetic data before seeding.</summary>
        public bool Wipe { get; set; } = false;

        /// <summary>Delete previously generated synthetic data and exit without seeding.</summary>
        public bool WipeOnly { get; set; } = false;

        #endregion

        #region Private-Members

        private int _Days = 7;

        #endregion

        #region Constructors-and-Factories

        /// <summary>Instantiate.</summary>
        public LoadGeneratorSettings()
        {
        }

        #endregion

        #region Public-Methods

        /// <summary>Whether the supplied operation category is enabled.</summary>
        /// <param name="operation">Operation category.</param>
        /// <returns>True when enabled (all categories are enabled when none were specified).</returns>
        public bool IsEnabled(OperationKind operation)
        {
            if (Operations == null || Operations.Count == 0) return true;
            return Operations.Contains(operation);
        }

        /// <summary>Effective number of collections to create.</summary>
        public int EffectiveCollectionCount()
        {
            return CollectionCount ?? DensityPreset(4, 8, 16);
        }

        /// <summary>Effective number of documents per collection.</summary>
        public int EffectiveDocumentsPerCollection()
        {
            return DocumentsPerCollection ?? DensityPreset(25, 120, 500);
        }

        /// <summary>Effective number of request-history entries.</summary>
        public int EffectiveRequestCount()
        {
            return RequestCount ?? DensityPreset(400, 2500, 12000);
        }

        /// <summary>Effective number of audit entries.</summary>
        public int EffectiveAuditCount()
        {
            return AuditCount ?? DensityPreset(80, 400, 1500);
        }

        /// <summary>Effective number of synthetic users.</summary>
        public int EffectiveUserCount()
        {
            return UserCount ?? DensityPreset(5, 15, 40);
        }

        /// <summary>Effective number of synthetic credentials.</summary>
        public int EffectiveCredentialCount()
        {
            return CredentialCount ?? DensityPreset(4, 10, 25);
        }

        /// <summary>Effective number of custom roles.</summary>
        public int EffectiveRoleCount()
        {
            return RoleCount ?? DensityPreset(2, 4, 8);
        }

        #endregion

        #region Private-Methods

        private int DensityPreset(int low, int medium, int high)
        {
            switch (Density)
            {
                case DensityLevel.Low: return low;
                case DensityLevel.High: return high;
                default: return medium;
            }
        }

        #endregion
    }
}
