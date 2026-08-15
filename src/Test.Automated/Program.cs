namespace Test.Automated
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Lattice.Core;
    using Test.Shared;
    using Touchstone.Cli;

    /// <summary>
    /// Touchstone CLI runner for the Lattice test suites.
    ///
    /// The test descriptors live in Test.Shared (the single source of truth) and are executed here
    /// through the Touchstone console runner. By default the suites run against SQLite with no external
    /// dependencies. A database type and connection details may be supplied on the command line to run
    /// the identical descriptors against PostgreSQL, MySQL, or SQL Server.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// Entry point.
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
        /// <returns>Process exit code (0 = all passed).</returns>
        public static async Task<int> Main(string[] args)
        {
            if (!ParseArguments(args, out DatabaseSettings settings, out bool enableLocking, out string resultsPath, out string error))
            {
                if (!string.IsNullOrEmpty(error))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error: {error}");
                    Console.ResetColor();
                    Console.WriteLine();
                }
                PrintUsage();
                return 1;
            }

            LatticeTestContext.Configure(settings, enableLocking);

            Console.WriteLine("===============================================================================");
            Console.WriteLine("  Lattice Test Suite (Touchstone CLI runner)");
            Console.WriteLine("===============================================================================");
            Console.WriteLine($"  Database: {LatticeTestContext.DatabaseType}");
            if (enableLocking) Console.WriteLine("  Object locking: enabled");
            Console.WriteLine();

            return await ConsoleRunner.RunAsync(LatticeTestSuites.All, resultsPath: resultsPath);
        }

        private static bool ParseArguments(
            string[] args,
            out DatabaseSettings settings,
            out bool enableLocking,
            out string resultsPath,
            out string error)
        {
            settings = null;
            enableLocking = false;
            resultsPath = null;
            error = null;

            List<string> argList = new List<string>(args ?? Array.Empty<string>());

            // Optional flags (parsed and removed before positional parsing).
            if (argList.Remove("--enable-locking")) enableLocking = true;

            int resultsIdx = argList.FindIndex(a => a.Equals("--results", StringComparison.OrdinalIgnoreCase));
            if (resultsIdx >= 0)
            {
                if (resultsIdx + 1 >= argList.Count)
                {
                    error = "--results requires a file path.";
                    return false;
                }
                resultsPath = argList[resultsIdx + 1];
                argList.RemoveAt(resultsIdx + 1);
                argList.RemoveAt(resultsIdx);
            }

            // No positional args -> default to SQLite (settings stay null, context uses SQLite).
            if (argList.Count == 0)
            {
                settings = null;
                return true;
            }

            string dbType = argList[0].ToLowerInvariant();

            switch (dbType)
            {
                case "sqlite":
                    string filename = argList.Count >= 2 ? argList[1] : "lattice_test.db";
                    settings = new DatabaseSettings { Type = DatabaseTypeEnum.Sqlite, Filename = filename };
                    return true;

                case "postgres":
                case "postgresql":
                    return ParseServer(argList, DatabaseTypeEnum.Postgres, "PostgreSQL", out settings, out error);

                case "mysql":
                    return ParseServer(argList, DatabaseTypeEnum.Mysql, "MySQL", out settings, out error);

                case "sqlserver":
                case "mssql":
                    return ParseServer(argList, DatabaseTypeEnum.SqlServer, "SQL Server", out settings, out error);

                default:
                    error = $"Unknown database type: {dbType}";
                    return false;
            }
        }

        private static bool ParseServer(
            List<string> argList,
            DatabaseTypeEnum type,
            string label,
            out DatabaseSettings settings,
            out string error)
        {
            settings = null;
            error = null;

            if (argList.Count < 6)
            {
                error = $"{label} requires: <hostname> <port> <username> <password> <database>";
                return false;
            }

            if (!int.TryParse(argList[2], out int port))
            {
                error = $"Invalid port number for {label}.";
                return false;
            }

            settings = new DatabaseSettings
            {
                Type = type,
                Hostname = argList[1],
                Port = port,
                Username = argList[3],
                Password = argList[4],
                DatabaseName = argList[5]
            };
            return true;
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  Test.Automated [sqlite <filename>]");
            Console.WriteLine("  Test.Automated postgresql <hostname> <port> <username> <password> <database>");
            Console.WriteLine("  Test.Automated mysql <hostname> <port> <username> <password> <database>");
            Console.WriteLine("  Test.Automated sqlserver <hostname> <port> <username> <password> <database>");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --enable-locking      Enable object locking during document ingestion.");
            Console.WriteLine("  --results <path>      Write JSON test results to the given file.");
            Console.WriteLine();
            Console.WriteLine("With no arguments the suite runs against a temporary SQLite database.");
        }
    }
}
