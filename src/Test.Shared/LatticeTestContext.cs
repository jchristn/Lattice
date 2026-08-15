namespace Test.Shared
{
    using System;
    using System.IO;
    using System.Reflection;
    using Lattice.Core;
    using Lattice.Core.Repositories;

    /// <summary>
    /// Shared configuration and factory helpers used by every Touchstone test case in Test.Shared.
    ///
    /// The context defaults to SQLite so that the xUnit and NUnit adapters (and any developer running
    /// <c>dotnet test</c>) require zero external infrastructure. The Touchstone CLI runner
    /// (Test.Automated) may reconfigure the context via <see cref="Configure(DatabaseSettings, bool)"/>
    /// to exercise the same descriptors against PostgreSQL, MySQL, or SQL Server.
    ///
    /// Every test creates its own isolated temporary directory and a fresh client, so cases are
    /// independent and safe to run in any order.
    /// </summary>
    public static class LatticeTestContext
    {
        #region Public-Members

        /// <summary>
        /// Whether Lattice should use its in-memory database mode. Enabled by default for fast,
        /// fully isolated test runs.
        /// </summary>
        public static bool InMemory { get; set; } = true;

        /// <summary>
        /// Whether object locking is enabled during document ingestion. Disabled by default to match
        /// the historical automated test behavior; the CLI runner can enable it with --enable-locking.
        /// </summary>
        public static bool EnableObjectLocking { get; set; } = false;

        /// <summary>
        /// The database type the suites are currently configured to run against.
        /// </summary>
        public static DatabaseTypeEnum DatabaseType => _Template?.Type ?? DatabaseTypeEnum.Sqlite;

        #endregion

        #region Private-Members

        private static DatabaseSettings _Template = null;

        #endregion

        #region Public-Methods

        /// <summary>
        /// Configure the suites to run against a specific database. Passing null reverts to the
        /// default SQLite behavior. For SQLite the supplied filename is used as a template; each test
        /// still gets its own file inside its own temporary directory.
        /// </summary>
        /// <param name="settings">Database settings template.</param>
        /// <param name="enableObjectLocking">Whether to enable object locking.</param>
        public static void Configure(DatabaseSettings settings, bool enableObjectLocking = false)
        {
            _Template = settings;
            EnableObjectLocking = enableObjectLocking;
        }

        /// <summary>
        /// Create a fresh, isolated temporary directory for a single test.
        /// </summary>
        /// <returns>Absolute path to the created directory.</returns>
        public static string CreateTestDir()
        {
            string dir = Path.Combine(Path.GetTempPath(), "lattice_test_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            return dir;
        }

        /// <summary>
        /// Recursively delete a temporary test directory, ignoring any I/O errors.
        /// </summary>
        /// <param name="dir">Directory to remove.</param>
        public static void CleanupTestDir(string dir)
        {
            try
            {
                if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                    Directory.Delete(dir, true);
            }
            catch
            {
                // Best-effort cleanup; ignore failures (e.g. locked files on Windows).
            }
        }

        /// <summary>
        /// Create a Lattice client scoped to the supplied temporary directory using the currently
        /// configured database. For SQLite the database file is placed inside the directory so the
        /// test is fully self-contained.
        /// </summary>
        /// <param name="testDir">Temporary directory owned by the calling test.</param>
        /// <returns>A new <see cref="LatticeClient"/>.</returns>
        public static LatticeClient CreateClient(string testDir)
        {
            DatabaseSettings db;

            if (_Template == null || _Template.Type == DatabaseTypeEnum.Sqlite)
            {
                string filename = _Template != null ? Path.GetFileName(_Template.Filename) : "lattice_test.db";
                if (string.IsNullOrEmpty(filename)) filename = "lattice_test.db";

                db = new DatabaseSettings
                {
                    Type = DatabaseTypeEnum.Sqlite,
                    Filename = Path.Combine(testDir, filename)
                };
            }
            else
            {
                db = new DatabaseSettings
                {
                    Type = _Template.Type,
                    Hostname = _Template.Hostname,
                    Port = _Template.Port,
                    Username = _Template.Username,
                    Password = _Template.Password,
                    DatabaseName = _Template.DatabaseName
                };
            }

            return new LatticeClient(new LatticeSettings
            {
                InMemory = InMemory,
                DefaultDocumentsDirectory = testDir,
                Database = db,
                EnableObjectLocking = EnableObjectLocking
            });
        }

        /// <summary>
        /// Create a client with explicit overrides for in-memory mode and object locking. Used by
        /// tests that need behavior that differs from the global defaults (e.g. flush persistence and
        /// object-locking scenarios). For SQLite a stable filename inside the directory is used so the
        /// same database can be reopened by a second client.
        /// </summary>
        /// <param name="testDir">Temporary directory owned by the calling test.</param>
        /// <param name="inMemory">Whether to use in-memory mode.</param>
        /// <param name="enableObjectLocking">Whether to enable object locking.</param>
        /// <param name="sqliteFilename">SQLite database filename to use within the directory.</param>
        /// <returns>A new <see cref="LatticeClient"/>.</returns>
        public static LatticeClient CreateClientWith(string testDir, bool inMemory, bool enableObjectLocking, string sqliteFilename = "lattice_custom.db")
        {
            DatabaseSettings db;

            if (_Template == null || _Template.Type == DatabaseTypeEnum.Sqlite)
            {
                db = new DatabaseSettings
                {
                    Type = DatabaseTypeEnum.Sqlite,
                    Filename = Path.Combine(testDir, sqliteFilename)
                };
            }
            else
            {
                db = new DatabaseSettings
                {
                    Type = _Template.Type,
                    Hostname = _Template.Hostname,
                    Port = _Template.Port,
                    Username = _Template.Username,
                    Password = _Template.Password,
                    DatabaseName = _Template.DatabaseName
                };
            }

            return new LatticeClient(new LatticeSettings
            {
                InMemory = inMemory,
                DefaultDocumentsDirectory = testDir,
                Database = db,
                EnableObjectLocking = enableObjectLocking
            });
        }

        /// <summary>
        /// Access the internal repository backing a client via reflection. A handful of tests use this
        /// to drive lower-level repository methods (e.g. seeding an object lock) that are not exposed
        /// on the public client surface.
        /// </summary>
        /// <param name="client">Client to inspect.</param>
        /// <returns>The backing repository.</returns>
        public static RepositoryBase GetRepository(LatticeClient client)
        {
            FieldInfo repoField = typeof(LatticeClient).GetField("_Repo", BindingFlags.Instance | BindingFlags.NonPublic);
            if (repoField == null) throw new InvalidOperationException("Unable to access the internal repository field.");
            RepositoryBase repo = repoField.GetValue(client) as RepositoryBase;
            if (repo == null) throw new InvalidOperationException("The internal repository was not available.");
            return repo;
        }

        #endregion
    }
}
