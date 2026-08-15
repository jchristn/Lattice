namespace Test.Shared.Suites
{
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using Lattice.Core;
    using Lattice.Core.Models;
    using Touchstone.Core;

    /// <summary>
    /// Tests for the flush API, which persists in-memory state to the backing store. Flush semantics
    /// are exercised against SQLite; the case is skipped for server databases where the in-memory
    /// persistence model does not apply.
    /// </summary>
    public static class FlushSuite
    {
        /// <summary>
        /// Build the suite.
        /// </summary>
        public static TestSuiteDescriptor Build()
        {
            SuiteBuilder b = new SuiteBuilder("flush", "Flush API");

            bool skip = LatticeTestContext.DatabaseType != DatabaseTypeEnum.Sqlite;
            string reason = "Flush persistence is validated against SQLite only.";

            b.AddDirSkip("Flush: persists in-memory data to disk", skip, reason, async (client, dir) =>
            {
                const string filename = "lattice_flush.db";

                using (LatticeClient writer = LatticeTestContext.CreateClientWith(dir, inMemory: true, enableObjectLocking: false, sqliteFilename: filename))
                {
                    Collection c = await writer.Collection.Create("Persisted");
                    await writer.Document.Ingest(c.Id, @"{""Name"":""Joel""}");
                    writer.Flush();

                    string dbPath = Path.Combine(dir, filename);
                    TestAssert.True(File.Exists(dbPath), "Database file was not created by Flush");
                }

                using (LatticeClient reader = LatticeTestContext.CreateClientWith(dir, inMemory: false, enableObjectLocking: false, sqliteFilename: filename))
                {
                    List<Collection> collections = await reader.Collection.ReadAll();
                    TestAssert.True(collections.Count >= 1, "Expected persisted collection after reopening the database");
                }
            });

            b.Add("Flush: no-op is safe to call", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                await client.Document.Ingest(c.Id, @"{""N"":1}");
                client.Flush();
            });

            return b.Build();
        }
    }
}
