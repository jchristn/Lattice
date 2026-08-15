namespace Test.Shared.Suites
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Lattice.Core.Models;
    using Lattice.Core.Search;
    using Touchstone.Core;

    /// <summary>
    /// Tests for batch document ingestion via <c>IngestBatch</c>, including metadata handling,
    /// searchability of ingested documents, and argument validation.
    /// </summary>
    public static class BatchSuite
    {
        /// <summary>
        /// Build the suite.
        /// </summary>
        public static TestSuiteDescriptor Build()
        {
            SuiteBuilder b = new SuiteBuilder("batch", "Batch Ingestion API");

            b.Add("IngestBatch: basic batch of 3", async client =>
            {
                Collection c = await client.Collection.Create("TestCollection");
                List<BatchDocument> docs = new List<BatchDocument>
                {
                    new BatchDocument(@"{""N"":1}"),
                    new BatchDocument(@"{""N"":2}"),
                    new BatchDocument(@"{""N"":3}")
                };
                List<Document> result = await client.Document.IngestBatch(c.Id, docs);
                TestAssert.Count(3, result);
            });

            b.Add("IngestBatch: names preserved in order", async client =>
            {
                Collection c = await client.Collection.Create("TestCollection");
                List<BatchDocument> docs = new List<BatchDocument>
                {
                    new BatchDocument(@"{""N"":1}", "First"),
                    new BatchDocument(@"{""N"":2}", "Second"),
                    new BatchDocument(@"{""N"":3}", "Third")
                };
                List<Document> result = await client.Document.IngestBatch(c.Id, docs);
                TestAssert.Equal("First", result[0].Name);
                TestAssert.Equal("Second", result[1].Name);
                TestAssert.Equal("Third", result[2].Name);
            });

            b.Add("IngestBatch: labels and tags preserved", async client =>
            {
                Collection c = await client.Collection.Create("TestCollection");
                List<BatchDocument> docs = new List<BatchDocument>
                {
                    new BatchDocument(@"{""N"":1}", "D1",
                        new List<string> { "x", "y" },
                        new Dictionary<string, string> { { "k", "v" } })
                };
                List<Document> result = await client.Document.IngestBatch(c.Id, docs);
                TestAssert.Count(2, result[0].Labels);
                TestAssert.Equal("v", result[0].Tags["k"]);
            });

            b.Add("IngestBatch: 20 documents, count and persistence", async client =>
            {
                Collection c = await client.Collection.Create("TestCollection");
                List<BatchDocument> docs = new List<BatchDocument>();
                for (int i = 0; i < 20; i++) docs.Add(new BatchDocument($@"{{""Index"":{i}}}", $"Doc_{i}"));
                List<Document> result = await client.Document.IngestBatch(c.Id, docs);
                TestAssert.Count(20, result);
                List<Document> all = await client.Document.ReadAllInCollection(c.Id);
                TestAssert.Count(20, all);
            });

            b.Add("IngestBatch: verify document properties", async client =>
            {
                Collection c = await client.Collection.Create("TestCollection");
                List<Document> result = await client.Document.IngestBatch(c.Id, new List<BatchDocument>
                {
                    new BatchDocument(@"{""Category"":""A""}", "Named")
                });
                Document d = result[0];
                TestAssert.StartsWith("doc_", d.Id, "Id prefix wrong");
                TestAssert.Equal(c.Id, d.CollectionId);
                TestAssert.NotNullOrEmpty(d.SchemaId, "SchemaId empty");
                TestAssert.Equal("Named", d.Name);
                TestAssert.True(d.ContentLength > 0, "ContentLength should be > 0");
                TestAssert.NotNullOrEmpty(d.Sha256Hash, "Sha256Hash empty");
            });

            b.Add("IngestBatch: ingested documents are searchable", async client =>
            {
                Collection c = await client.Collection.Create("TestCollection");
                await client.Document.IngestBatch(c.Id, new List<BatchDocument>
                {
                    new BatchDocument(@"{""Category"":""A""}"),
                    new BatchDocument(@"{""Category"":""A""}"),
                    new BatchDocument(@"{""Category"":""B""}")
                });
                SearchResult r = await client.Search.Search(new SearchQuery
                {
                    CollectionId = c.Id,
                    Filters = new List<SearchFilter> { new SearchFilter("Category", SearchConditionEnum.Equals, "A") }
                });
                TestAssert.Count(2, r.Documents);
            });

            b.Add("IngestBatch: documents with differing structure get distinct schemas", async client =>
            {
                Collection c = await client.Collection.Create("TestCollection");
                await client.Document.IngestBatch(c.Id, new List<BatchDocument>
                {
                    new BatchDocument(@"{""A"":1}"),
                    new BatchDocument(@"{""B"":2,""C"":3}")
                });
                List<Schema> schemas = await client.Schema.ReadAll();
                TestAssert.True(schemas.Count >= 2, $"Expected at least 2 schemas, got {schemas.Count}");
            });

            // ----- Negative -----

            b.Add("IngestBatch: null collectionId throws", async client =>
            {
                await TestAssert.ThrowsAsync<ArgumentNullException>(() =>
                    client.Document.IngestBatch(null, new List<BatchDocument> { new BatchDocument(@"{""a"":1}") }));
            });

            b.Add("IngestBatch: null list throws", async client =>
            {
                Collection c = await client.Collection.Create("TestCollection");
                await TestAssert.ThrowsAsync<ArgumentException>(() => client.Document.IngestBatch(c.Id, null));
            });

            b.Add("IngestBatch: empty list throws", async client =>
            {
                Collection c = await client.Collection.Create("TestCollection");
                await TestAssert.ThrowsAsync<ArgumentException>(() => client.Document.IngestBatch(c.Id, new List<BatchDocument>()));
            });

            b.Add("IngestBatch: entry with empty json throws", async client =>
            {
                Collection c = await client.Collection.Create("TestCollection");
                await TestAssert.ThrowsAsync<ArgumentException>(() => client.Document.IngestBatch(c.Id, new List<BatchDocument>
                {
                    new BatchDocument(@"{""a"":1}"),
                    new BatchDocument("")
                }));
            });

            return b.Build();
        }
    }
}
