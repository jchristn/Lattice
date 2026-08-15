namespace Test.Shared.Suites
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Lattice.Core.Models;
    using Lattice.Core.Search;
    using Touchstone.Core;

    /// <summary>
    /// Tests for indexing modes (All / Selective / None), updating the indexing configuration, and
    /// rebuilding indexes (including progress reporting and dropping unused index tables).
    /// </summary>
    public static class IndexingModeSuite
    {
        private sealed class ListProgress : IProgress<IndexRebuildProgress>
        {
            public readonly List<IndexRebuildProgress> Reports = new List<IndexRebuildProgress>();
            public void Report(IndexRebuildProgress value) { Reports.Add(value); }
        }

        private static SearchQuery Q(string collectionId, string field, string value)
        {
            return new SearchQuery
            {
                CollectionId = collectionId,
                Filters = new List<SearchFilter> { new SearchFilter(field, SearchConditionEnum.Equals, value) }
            };
        }

        /// <summary>
        /// Build the suite.
        /// </summary>
        public static TestSuiteDescriptor Build()
        {
            SuiteBuilder b = new SuiteBuilder("indexing", "Indexing Modes");

            b.Add("Selective mode indexes only specified fields", async client =>
            {
                Collection c = await client.Collection.Create("Test", indexingMode: IndexingMode.Selective, indexedFields: new List<string> { "Name" });
                TestAssert.Equal(IndexingMode.Selective, c.IndexingMode);
                await client.Document.Ingest(c.Id, @"{""Name"":""Joel"",""Age"":40}");
                List<IndexedField> indexed = await client.Collection.GetIndexedFields(c.Id);
                TestAssert.Count(1, indexed);
                SearchResult byName = await client.Search.Search(Q(c.Id, "Name", "Joel"));
                TestAssert.Count(1, byName.Documents);
            });

            b.Add("Selective mode: non-indexed field search returns empty", async client =>
            {
                Collection c = await client.Collection.Create("Test", indexingMode: IndexingMode.Selective, indexedFields: new List<string> { "Name" });
                await client.Document.Ingest(c.Id, @"{""Name"":""Joel"",""Age"":40}");
                SearchResult byAge = await client.Search.Search(Q(c.Id, "Age", "40"));
                TestAssert.Count(0, byAge.Documents);
            });

            b.Add("All mode indexes every field", async client =>
            {
                Collection c = await client.Collection.Create("Test", indexingMode: IndexingMode.All);
                await client.Document.Ingest(c.Id, @"{""Name"":""Joel"",""Age"":40,""City"":""Austin""}");
                TestAssert.Count(1, (await client.Search.Search(Q(c.Id, "Name", "Joel"))).Documents);
                TestAssert.Count(1, (await client.Search.Search(Q(c.Id, "Age", "40"))).Documents);
                TestAssert.Count(1, (await client.Search.Search(Q(c.Id, "City", "Austin"))).Documents);
            });

            b.Add("None mode: document retrievable by id", async client =>
            {
                Collection c = await client.Collection.Create("Test", indexingMode: IndexingMode.None);
                Document doc = await client.Document.Ingest(c.Id, @"{""Name"":""Joel""}");
                Document got = await client.Document.ReadById(doc.Id);
                TestAssert.NotNull(got, "Document should be retrievable even with indexing disabled");
            });

            b.Add("Selective mode: nested field indexing", async client =>
            {
                Collection c = await client.Collection.Create("Test", indexingMode: IndexingMode.Selective, indexedFields: new List<string> { "Address.City" });
                await client.Document.Ingest(c.Id, @"{""Address"":{""City"":""Austin"",""Zip"":""78701""}}");
                SearchResult r = await client.Search.Search(Q(c.Id, "Address.City", "Austin"));
                TestAssert.Count(1, r.Documents);
            });

            b.Add("UpdateIndexing switches mode and fields", async client =>
            {
                Collection c = await client.Collection.Create("Test", indexingMode: IndexingMode.All);
                await client.Collection.UpdateIndexing(c.Id, IndexingMode.Selective, new List<string> { "Email" });
                Collection updated = await client.Collection.ReadById(c.Id);
                TestAssert.Equal(IndexingMode.Selective, updated.IndexingMode);
                TestAssert.Count(1, await client.Collection.GetIndexedFields(c.Id));
            });

            b.Add("RebuildIndexes processes all documents", async client =>
            {
                Collection c = await client.Collection.Create("Test", indexingMode: IndexingMode.All);
                await client.Document.Ingest(c.Id, @"{""Name"":""A""}");
                await client.Document.Ingest(c.Id, @"{""Name"":""B""}");
                await client.Collection.UpdateIndexing(c.Id, IndexingMode.Selective, new List<string> { "Name" });
                IndexRebuildResult result = await client.Collection.RebuildIndexes(c.Id, dropUnusedIndexes: true);
                TestAssert.Equal(2, result.DocumentsProcessed);
                TestAssert.True(result.Success, $"Rebuild reported errors: {string.Join("; ", result.Errors)}");
            });

            b.Add("RebuildIndexes reports progress", async client =>
            {
                Collection c = await client.Collection.Create("Test", indexingMode: IndexingMode.All);
                for (int i = 0; i < 10; i++) await client.Document.Ingest(c.Id, $@"{{""Index"":{i}}}");
                ListProgress progress = new ListProgress();
                IndexRebuildResult result = await client.Collection.RebuildIndexes(c.Id, dropUnusedIndexes: false, progress: progress);
                TestAssert.Equal(10, result.DocumentsProcessed);
                TestAssert.True(progress.Reports.Count > 0, "Expected progress reports");
            });

            b.Add("RebuildIndexes drops unused index tables", async client =>
            {
                Collection c = await client.Collection.Create("Test", indexingMode: IndexingMode.All);
                await client.Document.Ingest(c.Id, @"{""Name"":""Joel"",""Age"":40}");
                await client.Collection.UpdateIndexing(c.Id, IndexingMode.Selective, new List<string> { "Name" });
                IndexRebuildResult result = await client.Collection.RebuildIndexes(c.Id, dropUnusedIndexes: true);
                TestAssert.True(result.Success, $"Rebuild reported errors: {string.Join("; ", result.Errors)}");
                SearchResult byName = await client.Search.Search(Q(c.Id, "Name", "Joel"));
                TestAssert.Count(1, byName.Documents);
            });

            return b.Build();
        }
    }
}
