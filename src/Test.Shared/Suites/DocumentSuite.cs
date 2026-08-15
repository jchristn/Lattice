namespace Test.Shared.Suites
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Lattice.Core.Models;
    using Touchstone.Core;

    /// <summary>
    /// Tests for single-document ingestion and retrieval: ingest with metadata, read with and without
    /// content/labels/tags, existence checks, deletion (including on-disk file removal), and the
    /// argument-validation behavior of each method.
    /// </summary>
    public static class DocumentSuite
    {
        /// <summary>
        /// Build the suite.
        /// </summary>
        public static TestSuiteDescriptor Build()
        {
            SuiteBuilder b = new SuiteBuilder("document", "Document API");

            // ----- Positive: ingest -----

            b.Add("Ingest: basic", async client =>
            {
                Collection c = await client.Collection.Create("TestCollection");
                Document doc = await client.Document.Ingest(c.Id, @"{""Name"":""Joel""}");
                TestAssert.NotNull(doc, "Document is null");
                TestAssert.NotNullOrEmpty(doc.Id, "Document Id is empty");
            });

            b.Add("Ingest: with name", async client =>
            {
                Collection c = await client.Collection.Create("TestCollection");
                Document doc = await client.Document.Ingest(c.Id, @"{""Data"":""test""}", "MyDocument");
                TestAssert.Equal("MyDocument", doc.Name);
            });

            b.Add("Ingest: with labels", async client =>
            {
                Collection c = await client.Collection.Create("TestCollection");
                Document doc = await client.Document.Ingest(c.Id, @"{""Data"":""test""}", labels: new List<string> { "important", "reviewed" });
                TestAssert.Count(2, doc.Labels);
                TestAssert.True(doc.Labels.Contains("important") && doc.Labels.Contains("reviewed"), "Missing labels");
            });

            b.Add("Ingest: with tags", async client =>
            {
                Collection c = await client.Collection.Create("TestCollection");
                Document doc = await client.Document.Ingest(c.Id, @"{""Data"":""test""}", tags: new Dictionary<string, string> { { "author", "Joel" }, { "status", "draft" } });
                TestAssert.Count(2, doc.Tags);
                TestAssert.Equal("Joel", doc.Tags["author"]);
                TestAssert.Equal("draft", doc.Tags["status"]);
            });

            b.Add("Ingest: verify identity and metadata", async client =>
            {
                Collection c = await client.Collection.Create("TestCollection");
                DateTime before = DateTime.UtcNow.AddSeconds(-2);
                Document doc = await client.Document.Ingest(c.Id, @"{""Name"":""Joel""}");
                DateTime after = DateTime.UtcNow.AddSeconds(2);

                TestAssert.StartsWith("doc_", doc.Id, $"Id format wrong: {doc.Id}");
                TestAssert.Equal(c.Id, doc.CollectionId);
                TestAssert.NotNullOrEmpty(doc.SchemaId, "SchemaId is empty");
                TestAssert.True(doc.ContentLength > 0, "ContentLength should be > 0");
                TestAssert.NotNullOrEmpty(doc.Sha256Hash, "Sha256Hash is empty");
                TestAssert.True(doc.CreatedUtc >= before && doc.CreatedUtc <= after, "CreatedUtc out of range");
                TestAssert.True(doc.LastUpdateUtc >= before && doc.LastUpdateUtc <= after, "LastUpdateUtc out of range");
            });

            b.Add("Ingest: identical content yields distinct documents", async client =>
            {
                Collection c = await client.Collection.Create("TestCollection");
                Document a = await client.Document.Ingest(c.Id, @"{""Name"":""Joel""}");
                Document d = await client.Document.Ingest(c.Id, @"{""Name"":""Joel""}");
                TestAssert.NotEqual(a.Id, d.Id, "Document ids should be unique");
                TestAssert.Equal(a.Sha256Hash, d.Sha256Hash, "Identical content should hash identically");
            });

            // ----- Positive: read -----

            b.Add("ReadById: without content", async client =>
            {
                Collection c = await client.Collection.Create("TestCollection");
                Document ingested = await client.Document.Ingest(c.Id, @"{""Name"":""Joel""}");
                Document got = await client.Document.ReadById(ingested.Id, includeContent: false);
                TestAssert.NotNull(got, "Retrieved document is null");
                TestAssert.Null(got.Content, "Content should be null");
            });

            b.Add("ReadById: with content", async client =>
            {
                Collection c = await client.Collection.Create("TestCollection");
                Document ingested = await client.Document.Ingest(c.Id, @"{""Name"":""Joel""}");
                Document got = await client.Document.ReadById(ingested.Id, includeContent: true);
                TestAssert.NotNull(got.Content, "Content is null");
                TestAssert.Contains("Joel", got.Content);
            });

            b.Add("ReadById: labels populated", async client =>
            {
                Collection c = await client.Collection.Create("TestCollection");
                Document ingested = await client.Document.Ingest(c.Id, @"{""Data"":""test""}", labels: new List<string> { "label1", "label2" });
                Document got = await client.Document.ReadById(ingested.Id);
                TestAssert.Count(2, got.Labels);
            });

            b.Add("ReadById: tags populated", async client =>
            {
                Collection c = await client.Collection.Create("TestCollection");
                Document ingested = await client.Document.Ingest(c.Id, @"{""Data"":""test""}", tags: new Dictionary<string, string> { { "key1", "val1" }, { "key2", "val2" } });
                Document got = await client.Document.ReadById(ingested.Id);
                TestAssert.Equal("val1", got.Tags["key1"]);
                TestAssert.Equal("val2", got.Tags["key2"]);
            });

            b.Add("ReadById: labels/tags excluded when not requested", async client =>
            {
                Collection c = await client.Collection.Create("TestCollection");
                Document ingested = await client.Document.Ingest(c.Id, @"{""Data"":""test""}",
                    labels: new List<string> { "l" }, tags: new Dictionary<string, string> { { "k", "v" } });
                Document got = await client.Document.ReadById(ingested.Id, includeContent: false, includeLabels: false, includeTags: false);
                TestAssert.Count(0, got.Labels);
                TestAssert.Count(0, got.Tags);
            });

            b.Add("ReadById: non-existent returns null", async client =>
            {
                Document got = await client.Document.ReadById("doc_nonexistent");
                TestAssert.Null(got, "Expected null");
            });

            b.Add("ReadByIds: returns present, omits missing", async client =>
            {
                Collection c = await client.Collection.Create("TestCollection");
                Document d1 = await client.Document.Ingest(c.Id, @"{""N"":1}");
                Document d2 = await client.Document.Ingest(c.Id, @"{""N"":2}");
                Dictionary<string, Document> map = await client.Document.ReadByIds(new List<string> { d1.Id, d2.Id, "doc_missing" });
                TestAssert.Count(2, map);
                TestAssert.True(map.ContainsKey(d1.Id) && map.ContainsKey(d2.Id), "Missing expected ids");
                TestAssert.False(map.ContainsKey("doc_missing"), "Missing id should not be present");
            });

            b.Add("ReadByIds: empty input returns empty map", async client =>
            {
                Dictionary<string, Document> map = await client.Document.ReadByIds(new List<string>());
                TestAssert.Count(0, map);
            });

            b.Add("ReadAllInCollection: empty", async client =>
            {
                Collection c = await client.Collection.Create("TestCollection");
                List<Document> docs = await client.Document.ReadAllInCollection(c.Id);
                TestAssert.Count(0, docs);
            });

            b.Add("ReadAllInCollection: multiple", async client =>
            {
                Collection c = await client.Collection.Create("TestCollection");
                await client.Document.Ingest(c.Id, @"{""Name"":""Doc1""}");
                await client.Document.Ingest(c.Id, @"{""Name"":""Doc2""}");
                await client.Document.Ingest(c.Id, @"{""Name"":""Doc3""}");
                List<Document> docs = await client.Document.ReadAllInCollection(c.Id);
                TestAssert.Count(3, docs);
            });

            b.Add("ReadAllInCollection: labels and tags populated", async client =>
            {
                Collection c = await client.Collection.Create("TestCollection");
                await client.Document.Ingest(c.Id, @"{""N"":1}",
                    labels: new List<string> { "a" }, tags: new Dictionary<string, string> { { "k", "v" } });
                List<Document> docs = await client.Document.ReadAllInCollection(c.Id);
                TestAssert.Count(1, docs);
                TestAssert.Count(1, docs[0].Labels);
                TestAssert.Equal("v", docs[0].Tags["k"]);
            });

            // ----- Positive: exists / delete -----

            b.Add("Exists: true when present", async client =>
            {
                Collection c = await client.Collection.Create("TestCollection");
                Document doc = await client.Document.Ingest(c.Id, @"{""Data"":1}");
                TestAssert.True(await client.Document.Exists(doc.Id), "Expected true");
            });

            b.Add("Exists: false when absent", async client =>
            {
                TestAssert.False(await client.Document.Exists("doc_nonexistent"), "Expected false");
            });

            b.Add("Delete: removes document", async client =>
            {
                Collection c = await client.Collection.Create("TestCollection");
                Document doc = await client.Document.Ingest(c.Id, @"{""Data"":1}");
                await client.Document.Delete(doc.Id);
                TestAssert.False(await client.Document.Exists(doc.Id), "Document still exists");
            });

            b.Add("Delete: on-disk JSON file removed", async client =>
            {
                Collection c = await client.Collection.Create("TestCollection");
                Document doc = await client.Document.Ingest(c.Id, @"{""Data"":1}");
                string filePath = Path.Combine(c.DocumentsDirectory, $"{doc.Id}.json");
                TestAssert.True(File.Exists(filePath), "File wasn't created");
                await client.Document.Delete(doc.Id);
                TestAssert.False(File.Exists(filePath), "File still exists after delete");
            });

            b.Add("Delete: non-existent is a no-op (no throw)", async client =>
            {
                await client.Document.Delete("doc_nonexistent");
            });

            // ----- Negative: argument validation -----

            b.Add("Ingest: null collectionId throws", async client =>
            {
                await TestAssert.ThrowsAsync<ArgumentNullException>(() => client.Document.Ingest(null, @"{""a"":1}"));
            });

            b.Add("Ingest: null json throws", async client =>
            {
                Collection c = await client.Collection.Create("TestCollection");
                await TestAssert.ThrowsAsync<ArgumentNullException>(() => client.Document.Ingest(c.Id, null));
            });

            b.Add("Ingest: empty json throws", async client =>
            {
                Collection c = await client.Collection.Create("TestCollection");
                await TestAssert.ThrowsAsync<ArgumentNullException>(() => client.Document.Ingest(c.Id, "   "));
            });

            b.Add("Ingest: non-existent collection throws", async client =>
            {
                await TestAssert.ThrowsAsync<ArgumentException>(() => client.Document.Ingest("col_nonexistent", @"{""a"":1}"));
            });

            b.Add("Ingest: malformed json throws", async client =>
            {
                Collection c = await client.Collection.Create("TestCollection");
                await TestAssert.ThrowsAsync<JsonException>(() => client.Document.Ingest(c.Id, @"{ this is not valid json "));
            });

            return b.Build();
        }
    }
}
