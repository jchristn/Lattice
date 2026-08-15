namespace Test.Shared.Suites
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Lattice.Core.Models;
    using Lattice.Core.Search;
    using Touchstone.Core;

    /// <summary>
    /// End-to-end integration tests exercising multiple API groups together: full CRUD pipelines,
    /// cross-collection isolation, and schema sharing.
    /// </summary>
    public static class IntegrationSuite
    {
        /// <summary>
        /// Build the suite.
        /// </summary>
        public static TestSuiteDescriptor Build()
        {
            SuiteBuilder b = new SuiteBuilder("integration", "Integration");

            b.Add("Full CRUD pipeline", async client =>
            {
                Collection c = await client.Collection.Create("Pipeline", "desc");
                Document doc = await client.Document.Ingest(c.Id, @"{""Name"":""Joel"",""Age"":40}", "MyDoc",
                    new List<string> { "vip" }, new Dictionary<string, string> { { "team", "core" } });

                Document read = await client.Document.ReadById(doc.Id, includeContent: true);
                TestAssert.Contains("Joel", read.Content);
                TestAssert.Equal("MyDoc", read.Name);
                TestAssert.Count(1, read.Labels);
                TestAssert.Equal("core", read.Tags["team"]);

                SearchResult r = await client.Search.Search(new SearchQuery
                {
                    CollectionId = c.Id,
                    Filters = new List<SearchFilter> { new SearchFilter("Name", SearchConditionEnum.Equals, "Joel") }
                });
                TestAssert.Count(1, r.Documents);

                await client.Document.Delete(doc.Id);
                TestAssert.False(await client.Document.Exists(doc.Id), "Document should be gone");

                await client.Collection.Delete(c.Id);
                TestAssert.False(await client.Collection.Exists(c.Id), "Collection should be gone");
            });

            b.Add("Multiple collections are isolated", async client =>
            {
                Collection a = await client.Collection.Create("A");
                Collection d = await client.Collection.Create("B");
                await client.Document.Ingest(a.Id, @"{""Name"":""InA""}");
                await client.Document.Ingest(d.Id, @"{""Name"":""InB""}");

                SearchResult searchA = await client.Search.Search(new SearchQuery
                {
                    CollectionId = a.Id,
                    Filters = new List<SearchFilter> { new SearchFilter("Name", SearchConditionEnum.Equals, "InB") }
                });
                TestAssert.Count(0, searchA.Documents);

                List<Document> docsA = await client.Document.ReadAllInCollection(a.Id);
                TestAssert.Count(1, docsA);
            });

            b.Add("Schema shared across identical documents", async client =>
            {
                Collection c = await client.Collection.Create("Shared");
                Document d1 = await client.Document.Ingest(c.Id, @"{""Name"":""A"",""Age"":1}");
                Document d2 = await client.Document.Ingest(c.Id, @"{""Name"":""B"",""Age"":2}");
                Document d3 = await client.Document.Ingest(c.Id, @"{""Name"":""C"",""Age"":3}");
                TestAssert.Equal(d1.SchemaId, d2.SchemaId);
                TestAssert.Equal(d2.SchemaId, d3.SchemaId);
            });

            b.Add("Delete during enumeration reflects updated total", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                List<Document> ingested = new List<Document>();
                for (int i = 0; i < 20; i++) ingested.Add(await client.Document.Ingest(c.Id, $@"{{""Index"":{i}}}"));

                EnumerationResult<Document> before = await client.Search.Enumerate(new EnumerationQuery { CollectionId = c.Id });
                TestAssert.Equal(20L, before.TotalRecords);

                for (int i = 0; i < 5; i++) await client.Document.Delete(ingested[i].Id);

                EnumerationResult<Document> after = await client.Search.Enumerate(new EnumerationQuery { CollectionId = c.Id });
                TestAssert.Equal(15L, after.TotalRecords);
            });

            return b.Build();
        }
    }
}
