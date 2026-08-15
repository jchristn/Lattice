namespace Test.Shared.Suites
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Lattice.Core.Models;
    using Touchstone.Core;

    /// <summary>
    /// Tests for the collection management API: create, read, list, exists, and delete, plus the
    /// argument-validation behavior of each method.
    /// </summary>
    public static class CollectionSuite
    {
        /// <summary>
        /// Build the suite.
        /// </summary>
        public static TestSuiteDescriptor Build()
        {
            SuiteBuilder b = new SuiteBuilder("collection", "Collection API");

            // ----- Positive: create -----

            b.Add("Create: basic collection", async client =>
            {
                Collection c = await client.Collection.Create("TestCollection");
                TestAssert.NotNull(c, "Collection is null");
                TestAssert.Equal("TestCollection", c.Name);
            });

            b.AddDir("Create: with all parameters", async (client, dir) =>
            {
                Collection c = await client.Collection.Create(
                    "TestCollection",
                    "Test description",
                    dir,
                    new List<string> { "label1", "label2" },
                    new Dictionary<string, string> { { "env", "test" }, { "version", "1.0" } });

                TestAssert.NotNull(c, "Collection is null");
                TestAssert.Equal("TestCollection", c.Name);
                TestAssert.Equal("Test description", c.Description);
                TestAssert.Equal(dir, c.DocumentsDirectory);
                TestAssert.Count(2, c.Labels);
                TestAssert.Count(2, c.Tags);
                TestAssert.Equal("test", c.Tags["env"]);
            });

            b.Add("Create: verify identity and timestamps", async client =>
            {
                DateTime before = DateTime.UtcNow.AddSeconds(-2);
                Collection c = await client.Collection.Create("TestCollection", "Desc");
                DateTime after = DateTime.UtcNow.AddSeconds(2);

                TestAssert.NotNullOrEmpty(c.Id, "Id is empty");
                TestAssert.StartsWith("col_", c.Id, $"Id format wrong: {c.Id}");
                TestAssert.Equal("Desc", c.Description);
                TestAssert.True(c.CreatedUtc >= before && c.CreatedUtc <= after, $"CreatedUtc out of range: {c.CreatedUtc}");
                TestAssert.True(c.LastUpdateUtc >= before && c.LastUpdateUtc <= after, $"LastUpdateUtc out of range: {c.LastUpdateUtc}");
            });

            b.Add("Create: default schema and indexing modes", async client =>
            {
                Collection c = await client.Collection.Create("Defaults");
                TestAssert.Equal(SchemaEnforcementMode.None, c.SchemaEnforcementMode);
                TestAssert.Equal(IndexingMode.All, c.IndexingMode);
            });

            // ----- Positive: read / list / exists -----

            b.Add("ReadById: existing collection", async client =>
            {
                Collection created = await client.Collection.Create("TestCollection", "Desc");
                Collection got = await client.Collection.ReadById(created.Id);
                TestAssert.NotNull(got, "Retrieved collection is null");
                TestAssert.Equal(created.Id, got.Id);
                TestAssert.Equal(created.Name, got.Name);
                TestAssert.Equal(created.Description, got.Description);
            });

            b.Add("ReadById: labels and tags round-trip", async client =>
            {
                Collection created = await client.Collection.Create(
                    "TestCollection", null, null,
                    new List<string> { "a", "b" },
                    new Dictionary<string, string> { { "k", "v" } });
                Collection got = await client.Collection.ReadById(created.Id);
                TestAssert.Count(2, got.Labels);
                TestAssert.True(got.Labels.Contains("a") && got.Labels.Contains("b"), "Labels not round-tripped");
                TestAssert.Equal("v", got.Tags["k"]);
            });

            b.Add("ReadById: non-existent returns null", async client =>
            {
                Collection got = await client.Collection.ReadById("col_nonexistent");
                TestAssert.Null(got, "Expected null for non-existent collection");
            });

            b.Add("ReadAll: empty", async client =>
            {
                List<Collection> all = await client.Collection.ReadAll();
                TestAssert.NotNull(all, "ReadAll returned null");
                TestAssert.Count(0, all);
            });

            b.Add("ReadAll: multiple", async client =>
            {
                await client.Collection.Create("Collection1");
                await client.Collection.Create("Collection2");
                await client.Collection.Create("Collection3");

                List<Collection> all = await client.Collection.ReadAll();
                TestAssert.Count(3, all);
                HashSet<string> names = all.Select(c => c.Name).ToHashSet();
                TestAssert.True(names.Contains("Collection1") && names.Contains("Collection2") && names.Contains("Collection3"), "Missing collection names");
            });

            b.Add("Exists: true when present", async client =>
            {
                Collection c = await client.Collection.Create("TestCollection");
                TestAssert.True(await client.Collection.Exists(c.Id), "Expected true");
            });

            b.Add("Exists: false when absent", async client =>
            {
                TestAssert.False(await client.Collection.Exists("col_nonexistent"), "Expected false");
            });

            // ----- Positive: delete -----

            b.Add("Delete: removes collection", async client =>
            {
                Collection c = await client.Collection.Create("TestCollection");
                await client.Collection.Delete(c.Id);
                TestAssert.False(await client.Collection.Exists(c.Id), "Collection still exists after delete");
            });

            b.Add("Delete: removes associated documents", async client =>
            {
                Collection c = await client.Collection.Create("TestCollection");
                Document doc = await client.Document.Ingest(c.Id, @"{""Name"":""Test""}");
                await client.Collection.Delete(c.Id);
                TestAssert.False(await client.Document.Exists(doc.Id), "Document still exists after collection delete");
            });

            b.Add("Delete: non-existent is a no-op (no throw)", async client =>
            {
                await client.Collection.Delete("col_nonexistent");
            });

            b.Add("Create: two collections have distinct ids", async client =>
            {
                Collection a = await client.Collection.Create("A");
                Collection c = await client.Collection.Create("B");
                TestAssert.NotEqual(a.Id, c.Id, "Collection ids should be unique");
            });

            // ----- Negative: argument validation -----

            b.Add("Create: null name throws", async client =>
            {
                await TestAssert.ThrowsAsync<ArgumentNullException>(() => client.Collection.Create(null));
            });

            b.Add("Create: empty name throws", async client =>
            {
                await TestAssert.ThrowsAsync<ArgumentNullException>(() => client.Collection.Create(""));
            });

            b.Add("Create: whitespace name throws", async client =>
            {
                await TestAssert.ThrowsAsync<ArgumentNullException>(() => client.Collection.Create("   "));
            });

            b.Add("GetConstraints: null collectionId throws", async client =>
            {
                await TestAssert.ThrowsAsync<ArgumentNullException>(() => client.Collection.GetConstraints(null));
            });

            b.Add("GetIndexedFields: null collectionId throws", async client =>
            {
                await TestAssert.ThrowsAsync<ArgumentNullException>(() => client.Collection.GetIndexedFields(null));
            });

            b.Add("UpdateConstraints: non-existent collection throws", async client =>
            {
                await TestAssert.ThrowsAsync<ArgumentException>(() =>
                    client.Collection.UpdateConstraints("col_nonexistent", SchemaEnforcementMode.Flexible));
            });

            b.Add("UpdateIndexing: non-existent collection throws", async client =>
            {
                await TestAssert.ThrowsAsync<ArgumentException>(() =>
                    client.Collection.UpdateIndexing("col_nonexistent", IndexingMode.All));
            });

            b.Add("RebuildIndexes: non-existent collection throws", async client =>
            {
                await TestAssert.ThrowsAsync<ArgumentException>(() =>
                    client.Collection.RebuildIndexes("col_nonexistent"));
            });

            return b.Build();
        }
    }
}
