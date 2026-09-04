namespace Lattice.LoadGenerator
{
    using System;
    using System.Threading.Tasks;
    using Lattice.Core;

    /// <summary>
    /// LoadGenerator entry point. Seeds a Lattice database with realistic synthetic activity.
    /// </summary>
    public static class Program
    {
        #region Public-Methods

        /// <summary>Entry point.</summary>
        /// <param name="args">Command-line arguments.</param>
        /// <returns>Zero on success, non-zero on failure.</returns>
        public static async Task<int> Main(string[] args)
        {
            if (ArgumentParser.IsHelpRequested(args))
            {
                ArgumentParser.PrintUsage();
                return 0;
            }

            LoadGeneratorSettings settings;

            try
            {
                settings = ArgumentParser.Parse(args);
            }
            catch (ArgumentException e)
            {
                Console.Error.WriteLine("Error: " + e.Message);
                return 1;
            }

            int seed = settings.Seed.HasValue ? settings.Seed.Value : Environment.TickCount;
            Random random = new Random(seed);

            Console.WriteLine("Lattice LoadGenerator");
            Console.WriteLine("Backend  : " + settings.Backend.ToString().ToLowerInvariant() + (settings.Backend == DatabaseTypeEnum.Sqlite ? (" " + settings.SqliteFilename) : (" " + settings.Hostname + "/" + settings.Database)));
            Console.WriteLine("Tenant   : " + (String.IsNullOrEmpty(settings.TenantName) ? "(first existing)" : settings.TenantName));
            Console.WriteLine("Density  : " + settings.Density.ToString().ToLowerInvariant() + ", window " + settings.Days + " day(s), seed " + seed);
            Console.WriteLine();

            try
            {
                LatticeSettings latticeSettings = BuildLatticeSettings(settings);

                using (LatticeClient client = new LatticeClient(latticeSettings))
                {
                    Seeder seeder = new Seeder(client, settings, random);

                    if (settings.Wipe || settings.WipeOnly)
                    {
                        await seeder.WipeAsync().ConfigureAwait(false);
                        if (settings.WipeOnly) return 0;
                        Console.WriteLine();
                    }

                    SeedSummary summary = await seeder.SeedAsync().ConfigureAwait(false);
                    client.Flush();

                    Console.WriteLine();
                    Console.WriteLine(summary.Render());
                }

                return 0;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Failed: " + e.Message);
                Console.Error.WriteLine(e.ToString());
                return 1;
            }
        }

        #endregion

        #region Private-Methods

        private static LatticeSettings BuildLatticeSettings(LoadGeneratorSettings settings)
        {
            LatticeSettings latticeSettings = new LatticeSettings();
            latticeSettings.InMemory = false;
            latticeSettings.Database.Type = settings.Backend;

            if (settings.Backend == DatabaseTypeEnum.Sqlite)
            {
                latticeSettings.Database.Filename = settings.SqliteFilename;
            }
            else
            {
                latticeSettings.Database.Hostname = settings.Hostname;
                latticeSettings.Database.Port = settings.Port;
                latticeSettings.Database.DatabaseName = settings.Database;
                latticeSettings.Database.Username = settings.Username;
                latticeSettings.Database.Password = settings.Password;
            }

            return latticeSettings;
        }

        #endregion
    }
}
