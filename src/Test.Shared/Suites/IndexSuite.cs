namespace Test.Shared.Suites
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Lattice.Core.Models;
    using Touchstone.Core;

    /// <summary>
    /// Tests for the index inspection API: index-table mappings and index-table entry access.
    /// </summary>
    public static class IndexSuite
    {
        /// <summary>
        /// Build the suite.
        /// </summary>
        public static TestSuiteDescriptor Build()
        {
            SuiteBuilder b = new SuiteBuilder("index", "Index API");

            b.Add("GetMappings: returns mappings after ingest", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                await client.Document.Ingest(c.Id, @"{""Name"":""Joel"",""City"":""Austin""}");
                List<IndexTableMapping> mappings = await client.Index.GetMappings();
                TestAssert.True(mappings.Count >= 2, $"Expected at least 2 mappings, got {mappings.Count}");
            });

            b.Add("GetMappingByKey: existing key", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                await client.Document.Ingest(c.Id, @"{""City"":""Austin""}");
                IndexTableMapping m = await client.Index.GetMappingByKey("City");
                TestAssert.NotNull(m, "Mapping not found for key City");
                TestAssert.Equal("City", m.Key);
                TestAssert.NotNullOrEmpty(m.TableName, "TableName is empty");
            });

            b.Add("GetMappingByKey: non-existent returns null", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                await client.Document.Ingest(c.Id, @"{""City"":""Austin""}");
                IndexTableMapping m = await client.Index.GetMappingByKey("NoSuchKey");
                TestAssert.Null(m, "Expected null for non-existent key");
            });

            b.Add("GetTableEntryCount: reflects ingested values", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                await client.Document.Ingest(c.Id, @"{""City"":""Austin""}");
                await client.Document.Ingest(c.Id, @"{""City"":""Dallas""}");
                IndexTableMapping m = await client.Index.GetMappingByKey("City");
                long count = await client.Index.GetTableEntryCount(m.TableName);
                TestAssert.Equal(2L, count);
            });

            b.Add("GetTableEntries: returns entries", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                await client.Document.Ingest(c.Id, @"{""City"":""Austin""}");
                await client.Document.Ingest(c.Id, @"{""City"":""Dallas""}");
                IndexTableMapping m = await client.Index.GetMappingByKey("City");
                List<IndexTableEntry> entries = await client.Index.GetTableEntries(m.TableName, 0, 100);
                TestAssert.Count(2, entries);
                foreach (IndexTableEntry e in entries) TestAssert.NotNullOrEmpty(e.DocumentId, "Entry missing DocumentId");
            });

            b.Add("GetTableEntries: pagination limit", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                for (int i = 0; i < 5; i++) await client.Document.Ingest(c.Id, $@"{{""City"":""City{i}""}}");
                IndexTableMapping m = await client.Index.GetMappingByKey("City");
                List<IndexTableEntry> entries = await client.Index.GetTableEntries(m.TableName, 0, 3);
                TestAssert.Count(3, entries);
            });

            return b.Build();
        }
    }
}
