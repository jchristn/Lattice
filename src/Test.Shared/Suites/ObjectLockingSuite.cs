namespace Test.Shared.Suites
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Lattice.Core;
    using Lattice.Core.Exceptions;
    using Lattice.Core.Models;
    using Lattice.Core.Repositories;
    using Touchstone.Core;

    /// <summary>
    /// Tests for object locking during ingestion. Locking is opt-in; when enabled a document name is
    /// locked for the duration of an ingest and a contending ingest of the same name fails with
    /// <see cref="DocumentLockedException"/>. A contending lock is seeded directly through the
    /// repository so the collision is deterministic rather than timing-dependent.
    /// </summary>
    public static class ObjectLockingSuite
    {
        /// <summary>
        /// Build the suite.
        /// </summary>
        public static TestSuiteDescriptor Build()
        {
            SuiteBuilder b = new SuiteBuilder("locking", "Object Locking");

            b.AddRaw("Locking enabled: sequential ingests of same name succeed", async ct =>
            {
                string dir = LatticeTestContext.CreateTestDir();
                try
                {
                    using LatticeClient client = LatticeTestContext.CreateClientWith(dir, inMemory: true, enableObjectLocking: true, sqliteFilename: "lock1.db");
                    Collection c = await client.Collection.Create("Test");
                    Document d1 = await client.Document.Ingest(c.Id, @"{""V"":1}", "SameName");
                    Document d2 = await client.Document.Ingest(c.Id, @"{""V"":2}", "SameName");
                    TestAssert.NotEqual(d1.Id, d2.Id, "Both ingests should succeed with distinct ids");
                }
                finally { LatticeTestContext.CleanupTestDir(dir); }
            });

            b.AddRaw("Locking enabled: held lock blocks ingest with DocumentLockedException", async ct =>
            {
                string dir = LatticeTestContext.CreateTestDir();
                try
                {
                    using LatticeClient client = LatticeTestContext.CreateClientWith(dir, inMemory: true, enableObjectLocking: true, sqliteFilename: "lock2.db");
                    Collection c = await client.Collection.Create("Test");

                    // Seed an active lock owned by another host for the target document name.
                    RepositoryBase repo = LatticeTestContext.GetRepository(client);
                    await repo.ObjectLocks.TryAcquireLock(c.Id, "LockedDoc", "another-host");

                    DocumentLockedException ex = await TestAssert.ThrowsAsync<DocumentLockedException>(
                        () => client.Document.Ingest(c.Id, @"{""V"":1}", "LockedDoc"));
                    TestAssert.Equal("LockedDoc", ex.DocumentName);
                    TestAssert.Equal(c.Id, ex.CollectionId);
                }
                finally { LatticeTestContext.CleanupTestDir(dir); }
            });

            b.AddRaw("Locking enabled: held lock blocks batch containing that name", async ct =>
            {
                string dir = LatticeTestContext.CreateTestDir();
                try
                {
                    using LatticeClient client = LatticeTestContext.CreateClientWith(dir, inMemory: true, enableObjectLocking: true, sqliteFilename: "lock3.db");
                    Collection c = await client.Collection.Create("Test");

                    RepositoryBase repo = LatticeTestContext.GetRepository(client);
                    await repo.ObjectLocks.TryAcquireLock(c.Id, "LockedDoc", "another-host");

                    await TestAssert.ThrowsAsync<DocumentLockedException>(() => client.Document.IngestBatch(c.Id, new List<BatchDocument>
                    {
                        new BatchDocument(@"{""V"":1}", "FreeDoc"),
                        new BatchDocument(@"{""V"":2}", "LockedDoc")
                    }));
                }
                finally { LatticeTestContext.CleanupTestDir(dir); }
            });

            b.AddRaw("Locking disabled: held lock does not block ingest", async ct =>
            {
                string dir = LatticeTestContext.CreateTestDir();
                try
                {
                    using LatticeClient client = LatticeTestContext.CreateClientWith(dir, inMemory: true, enableObjectLocking: false, sqliteFilename: "lock4.db");
                    Collection c = await client.Collection.Create("Test");

                    RepositoryBase repo = LatticeTestContext.GetRepository(client);
                    await repo.ObjectLocks.TryAcquireLock(c.Id, "LockedDoc", "another-host");

                    // With locking disabled the seeded lock is ignored.
                    Document doc = await client.Document.Ingest(c.Id, @"{""V"":1}", "LockedDoc");
                    TestAssert.NotNull(doc, "Ingest should succeed when locking is disabled");
                }
                finally { LatticeTestContext.CleanupTestDir(dir); }
            });

            return b.Build();
        }
    }
}
