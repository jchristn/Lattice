namespace Test.Shared.Suites
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Lattice.Core.Models;
    using Lattice.Core.Search;
    using Touchstone.Core;

    /// <summary>
    /// Tests for the enumeration API: paging through a collection, ordering, and the result envelope.
    /// </summary>
    public static class EnumerationSuite
    {
        /// <summary>
        /// Build the suite.
        /// </summary>
        public static TestSuiteDescriptor Build()
        {
            SuiteBuilder b = new SuiteBuilder("enumeration", "Enumeration API");

            b.Add("Basic enumeration", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                await client.Document.Ingest(c.Id, @"{""N"":1}");
                await client.Document.Ingest(c.Id, @"{""N"":2}");
                EnumerationResult<Document> r = await client.Search.Enumerate(new EnumerationQuery { CollectionId = c.Id });
                TestAssert.Count(2, r.Objects);
            });

            b.Add("MaxResults", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                for (int i = 0; i < 10; i++) await client.Document.Ingest(c.Id, $@"{{""Index"":{i}}}");
                EnumerationResult<Document> r = await client.Search.Enumerate(new EnumerationQuery { CollectionId = c.Id, MaxResults = 5 });
                TestAssert.Count(5, r.Objects);
            });

            b.Add("Skip", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                for (int i = 0; i < 10; i++) await client.Document.Ingest(c.Id, $@"{{""Index"":{i}}}");
                EnumerationResult<Document> r = await client.Search.Enumerate(new EnumerationQuery { CollectionId = c.Id, Skip = 7, MaxResults = 100 });
                TestAssert.Count(3, r.Objects);
            });

            b.Add("Pagination pages do not overlap", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                for (int i = 0; i < 6; i++) await client.Document.Ingest(c.Id, $@"{{""Index"":{i}}}");
                EnumerationResult<Document> page1 = await client.Search.Enumerate(new EnumerationQuery { CollectionId = c.Id, Skip = 0, MaxResults = 3 });
                EnumerationResult<Document> page2 = await client.Search.Enumerate(new EnumerationQuery { CollectionId = c.Id, Skip = 3, MaxResults = 3 });
                HashSet<string> ids1 = new HashSet<string>();
                foreach (Document d in page1.Objects) ids1.Add(d.Id);
                foreach (Document d in page2.Objects) TestAssert.False(ids1.Contains(d.Id), "Pages overlap");
            });

            b.Add("Ordering CreatedAscending", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                for (int i = 0; i < 5; i++) { await client.Document.Ingest(c.Id, $@"{{""Index"":{i}}}"); await Task.Delay(5); }
                EnumerationResult<Document> r = await client.Search.Enumerate(new EnumerationQuery { CollectionId = c.Id, Ordering = EnumerationOrderEnum.CreatedAscending });
                for (int i = 1; i < r.Objects.Count; i++)
                    TestAssert.True(r.Objects[i].CreatedUtc >= r.Objects[i - 1].CreatedUtc, "Not ascending by CreatedUtc");
            });

            b.Add("Ordering CreatedDescending", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                for (int i = 0; i < 5; i++) { await client.Document.Ingest(c.Id, $@"{{""Index"":{i}}}"); await Task.Delay(5); }
                EnumerationResult<Document> r = await client.Search.Enumerate(new EnumerationQuery { CollectionId = c.Id, Ordering = EnumerationOrderEnum.CreatedDescending });
                for (int i = 1; i < r.Objects.Count; i++)
                    TestAssert.True(r.Objects[i].CreatedUtc <= r.Objects[i - 1].CreatedUtc, "Not descending by CreatedUtc");
            });

            b.Add("Envelope: TotalRecords", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                for (int i = 0; i < 10; i++) await client.Document.Ingest(c.Id, $@"{{""Index"":{i}}}");
                EnumerationResult<Document> r = await client.Search.Enumerate(new EnumerationQuery { CollectionId = c.Id, MaxResults = 3 });
                TestAssert.Equal(10L, r.TotalRecords);
            });

            b.Add("Envelope: RecordsRemaining", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                for (int i = 0; i < 10; i++) await client.Document.Ingest(c.Id, $@"{{""Index"":{i}}}");
                EnumerationResult<Document> r = await client.Search.Enumerate(new EnumerationQuery { CollectionId = c.Id, Skip = 2, MaxResults = 3 });
                TestAssert.Equal(5L, r.RecordsRemaining);
            });

            b.Add("Envelope: EndOfResults", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                for (int i = 0; i < 5; i++) await client.Document.Ingest(c.Id, $@"{{""Index"":{i}}}");
                EnumerationResult<Document> r = await client.Search.Enumerate(new EnumerationQuery { CollectionId = c.Id, MaxResults = 10 });
                TestAssert.True(r.EndOfResults, "Expected EndOfResults true");
            });

            b.Add("Empty collection", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                EnumerationResult<Document> r = await client.Search.Enumerate(new EnumerationQuery { CollectionId = c.Id });
                TestAssert.Count(0, r.Objects);
                TestAssert.Equal(0L, r.TotalRecords);
                TestAssert.True(r.EndOfResults, "Expected EndOfResults true for empty collection");
            });

            b.Add("IncludeData true returns objects without error", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                await client.Document.Ingest(c.Id, @"{""Name"":""Joel""}");
                EnumerationResult<Document> r = await client.Search.Enumerate(new EnumerationQuery { CollectionId = c.Id, IncludeData = true });
                TestAssert.Count(1, r.Objects);
                TestAssert.NotNullOrEmpty(r.Objects[0].Id, "Enumerated document should have an id");
            });

            b.Add("Labels and tags included by default", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                await client.Document.Ingest(c.Id, @"{""N"":1}",
                    labels: new List<string> { "flag" }, tags: new Dictionary<string, string> { { "k", "v" } });
                EnumerationResult<Document> r = await client.Search.Enumerate(new EnumerationQuery { CollectionId = c.Id });
                TestAssert.Count(1, r.Objects);
                TestAssert.Count(1, r.Objects[0].Labels);
                TestAssert.Equal("v", r.Objects[0].Tags["k"]);
            });

            b.Add("Labels and tags excluded when not requested", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                await client.Document.Ingest(c.Id, @"{""N"":1}",
                    labels: new List<string> { "flag" }, tags: new Dictionary<string, string> { { "k", "v" } });
                EnumerationResult<Document> r = await client.Search.Enumerate(new EnumerationQuery { CollectionId = c.Id, IncludeLabels = false, IncludeTags = false });
                TestAssert.Count(1, r.Objects);
                TestAssert.Count(0, r.Objects[0].Labels);
                TestAssert.Count(0, r.Objects[0].Tags);
            });

            return b.Build();
        }
    }
}
