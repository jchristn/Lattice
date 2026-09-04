namespace Lattice.LoadGenerator
{
    using System;
    using System.Globalization;
    using Lattice.Core;

    /// <summary>
    /// Command-line argument parser for the load generator.
    /// </summary>
    public static class ArgumentParser
    {
        #region Public-Methods

        /// <summary>Determine whether the supplied arguments contain a help flag.</summary>
        /// <param name="args">Command-line arguments.</param>
        /// <returns>True when help was requested.</returns>
        public static bool IsHelpRequested(string[] args)
        {
            if (args == null) return false;

            foreach (string arg in args)
            {
                if (arg == null) continue;
                if (arg.Equals("/?") || arg.Equals("-?") || arg.Equals("--help") || arg.Equals("-h")) return true;
            }

            return false;
        }

        /// <summary>Parse command-line arguments into settings.</summary>
        /// <param name="args">Command-line arguments.</param>
        /// <returns>Parsed settings.</returns>
        /// <exception cref="ArgumentException">Thrown when an argument is unknown, malformed, or missing a value.</exception>
        public static LoadGeneratorSettings Parse(string[] args)
        {
            if (args == null) throw new ArgumentNullException(nameof(args));

            LoadGeneratorSettings settings = new LoadGeneratorSettings();

            int i = 0;
            while (i < args.Length)
            {
                string arg = args[i];

                switch (arg)
                {
                    case "--backend":
                        settings.Backend = ParseBackend(NextValue(args, ref i, arg));
                        break;
                    case "--sqlite-file":
                        settings.SqliteFilename = NextValue(args, ref i, arg);
                        break;
                    case "--host":
                        settings.Hostname = NextValue(args, ref i, arg);
                        break;
                    case "--port":
                        settings.Port = ParseInt(NextValue(args, ref i, arg), arg);
                        break;
                    case "--database":
                        settings.Database = NextValue(args, ref i, arg);
                        break;
                    case "--username":
                        settings.Username = NextValue(args, ref i, arg);
                        break;
                    case "--password":
                        settings.Password = NextValue(args, ref i, arg);
                        break;
                    case "--tenant":
                        settings.TenantName = NextValue(args, ref i, arg);
                        break;
                    case "--density":
                        settings.Density = ParseDensity(NextValue(args, ref i, arg));
                        break;
                    case "--days":
                        settings.Days = ParseInt(NextValue(args, ref i, arg), arg);
                        break;
                    case "--operations":
                        ParseOperations(NextValue(args, ref i, arg), settings);
                        break;
                    case "--collections":
                        settings.CollectionCount = ParseInt(NextValue(args, ref i, arg), arg);
                        break;
                    case "--documents":
                        settings.DocumentsPerCollection = ParseInt(NextValue(args, ref i, arg), arg);
                        break;
                    case "--requests":
                        settings.RequestCount = ParseInt(NextValue(args, ref i, arg), arg);
                        break;
                    case "--audit":
                        settings.AuditCount = ParseInt(NextValue(args, ref i, arg), arg);
                        break;
                    case "--users":
                        settings.UserCount = ParseInt(NextValue(args, ref i, arg), arg);
                        break;
                    case "--credentials":
                        settings.CredentialCount = ParseInt(NextValue(args, ref i, arg), arg);
                        break;
                    case "--roles":
                        settings.RoleCount = ParseInt(NextValue(args, ref i, arg), arg);
                        break;
                    case "--server-url":
                        settings.ServerUrl = NextValue(args, ref i, arg);
                        break;
                    case "--access-key":
                        settings.AccessKey = NextValue(args, ref i, arg);
                        break;
                    case "--live-requests":
                        settings.LiveRequestCount = ParseInt(NextValue(args, ref i, arg), arg);
                        break;
                    case "--seed":
                        settings.Seed = ParseInt(NextValue(args, ref i, arg), arg);
                        break;
                    case "--wipe":
                        settings.Wipe = true;
                        break;
                    case "--wipe-only":
                        settings.WipeOnly = true;
                        break;
                    default:
                        throw new ArgumentException("Unknown argument '" + arg + "'. Use --help for usage.");
                }

                i++;
            }

            if (settings.Backend == DatabaseTypeEnum.Sqlite && String.IsNullOrEmpty(settings.SqliteFilename))
                throw new ArgumentException("--sqlite-file is required when --backend is sqlite.");

            if (settings.Backend != DatabaseTypeEnum.Sqlite && String.IsNullOrEmpty(settings.Database))
                throw new ArgumentException("--database is required when --backend is " + settings.Backend.ToString().ToLowerInvariant() + ".");

            return settings;
        }

        /// <summary>Print the usage menu to the console.</summary>
        public static void PrintUsage()
        {
            Console.WriteLine();
            Console.WriteLine("Lattice LoadGenerator");
            Console.WriteLine("Seeds a Lattice database with realistic synthetic collections, documents, request history,");
            Console.WriteLine("audit, and identity/RBAC activity so the dashboard renders a fully hydrated system for demos");
            Console.WriteLine("and screenshots. Writes directly to the database; activity is backdated across the time window.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  latticeload --backend sqlite --sqlite-file <file> [options]");
            Console.WriteLine("  latticeload --backend postgresql --host <h> --port <n> --database <db> --username <u> --password <p> [options]");
            Console.WriteLine();
            Console.WriteLine("Database target:");
            Console.WriteLine("  --backend <name>         sqlite | postgresql | mysql | sqlserver (sqlite)");
            Console.WriteLine("  --sqlite-file <file>     SQLite database file (lattice.db); point at the server's live DB to");
            Console.WriteLine("                           populate a running dashboard, e.g. docker/server/data/lattice.db");
            Console.WriteLine("  --host <host>            Database host for non-SQLite backends (localhost)");
            Console.WriteLine("  --port <n>               Database port (backend default)");
            Console.WriteLine("  --database <name>        Database name for non-SQLite backends");
            Console.WriteLine("  --username <user>        Database username for non-SQLite backends");
            Console.WriteLine("  --password <pass>        Database password for non-SQLite backends");
            Console.WriteLine();
            Console.WriteLine("What to generate (defaults in parentheses):");
            Console.WriteLine("  --tenant <name>          Tenant to seed into; created if named and absent (first existing tenant)");
            Console.WriteLine("  --density <level>        low | medium | high preset for all counts (medium)");
            Console.WriteLine("  --days <n>               Days into the past to spread activity across (7)");
            Console.WriteLine("  --operations <csv>       Categories to generate: all, or a comma-separated subset of");
            Console.WriteLine("                           collections,documents,requests,audit,users,credentials,roles (all)");
            Console.WriteLine("  --collections <n>        Override collection count");
            Console.WriteLine("  --documents <n>          Override documents per collection");
            Console.WriteLine("  --requests <n>           Override request-history entry count");
            Console.WriteLine("  --audit <n>              Override audit entry count");
            Console.WriteLine("  --users <n>              Override synthetic user count");
            Console.WriteLine("  --credentials <n>        Override synthetic credential count");
            Console.WriteLine("  --roles <n>              Override custom role count");
            Console.WriteLine();
            Console.WriteLine("Optional live traffic (lights up telemetry / Grafana on a running server):");
            Console.WriteLine("  --server-url <url>       Base URL of a running server, e.g. http://localhost:8000");
            Console.WriteLine("  --access-key <key>       Bearer access key (key_...) for the live-traffic burst");
            Console.WriteLine("  --live-requests <n>      Number of live requests to fire (0)");
            Console.WriteLine();
            Console.WriteLine("General:");
            Console.WriteLine("  --seed <n>               Random seed for reproducible output (random)");
            Console.WriteLine("  --wipe                   Remove previously generated synthetic entities first, then seed");
            Console.WriteLine("  --wipe-only              Remove previously generated synthetic entities and exit");
            Console.WriteLine("  /? -? -h --help          Show this help");
            Console.WriteLine();
            Console.WriteLine("Synthetic entities are marked (collections labelled 'synthetic' + tag generator=loadgen, users");
            Console.WriteLine("under @loadgen.synthetic, roles prefixed 'LG-') so --wipe can find them.");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  latticeload --backend sqlite --sqlite-file docker/server/data/lattice.db --wipe");
            Console.WriteLine("  latticeload --backend sqlite --sqlite-file lattice.db --density high --days 30");
            Console.WriteLine("  latticeload --backend sqlite --sqlite-file lattice.db --operations collections,documents,requests");
            Console.WriteLine("  latticeload --backend postgresql --host localhost --port 5432 --database lattice --username lattice --password lattice");
            Console.WriteLine();
        }

        #endregion

        #region Private-Methods

        private static void ParseOperations(string value, LoadGeneratorSettings settings)
        {
            settings.Operations.Clear();
            if (String.IsNullOrWhiteSpace(value)) return;
            if (value.Trim().Equals("all", StringComparison.OrdinalIgnoreCase)) return;

            string[] parts = value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string part in parts)
            {
                string trimmed = part.Trim().ToLowerInvariant();
                switch (trimmed)
                {
                    case "collections": settings.Operations.Add(OperationKind.Collections); break;
                    case "documents": settings.Operations.Add(OperationKind.Documents); break;
                    case "requests": settings.Operations.Add(OperationKind.Requests); break;
                    case "audit": settings.Operations.Add(OperationKind.Audit); break;
                    case "users": settings.Operations.Add(OperationKind.Users); break;
                    case "credentials": settings.Operations.Add(OperationKind.Credentials); break;
                    case "roles": settings.Operations.Add(OperationKind.Roles); break;
                    default: throw new ArgumentException("Unknown operation '" + part.Trim() + "'. Valid: collections, documents, requests, audit, users, credentials, roles, all.");
                }
            }
        }

        private static DatabaseTypeEnum ParseBackend(string value)
        {
            switch (value.Trim().ToLowerInvariant())
            {
                case "sqlite": return DatabaseTypeEnum.Sqlite;
                case "mysql": return DatabaseTypeEnum.Mysql;
                case "postgres":
                case "postgresql": return DatabaseTypeEnum.Postgres;
                case "sqlserver":
                case "mssql": return DatabaseTypeEnum.SqlServer;
                default: throw new ArgumentException("Unknown backend '" + value + "'. Valid: sqlite, postgresql, mysql, sqlserver.");
            }
        }

        private static DensityLevel ParseDensity(string value)
        {
            switch (value.Trim().ToLowerInvariant())
            {
                case "low": return DensityLevel.Low;
                case "medium": return DensityLevel.Medium;
                case "high": return DensityLevel.High;
                default: throw new ArgumentException("Unknown density '" + value + "'. Valid: low, medium, high.");
            }
        }

        private static string NextValue(string[] args, ref int i, string arg)
        {
            if (i + 1 >= args.Length) throw new ArgumentException("Argument '" + arg + "' requires a value.");
            i++;
            return args[i];
        }

        private static int ParseInt(string value, string arg)
        {
            if (!Int32.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
                throw new ArgumentException("Argument '" + arg + "' requires an integer value, received '" + value + "'.");
            return result;
        }

        #endregion
    }
}
