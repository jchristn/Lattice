namespace Test.Shared.Suites
{
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    using Lattice.Core.Models;
    using Lattice.Core.Search;
    using Touchstone.Core;

    /// <summary>
    /// Edge-case tests exercising unusual but valid JSON content: empty strings, special and unicode
    /// characters, deep nesting, large arrays, and the full range of scalar JSON types.
    /// </summary>
    public static class EdgeCaseSuite
    {
        /// <summary>
        /// Build the suite.
        /// </summary>
        public static TestSuiteDescriptor Build()
        {
            SuiteBuilder b = new SuiteBuilder("edge", "Edge Cases");

            b.Add("Empty string value", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                await client.Document.Ingest(c.Id, @"{""Name"":""""}");
                SearchResult r = await client.Search.Search(new SearchQuery
                {
                    CollectionId = c.Id,
                    Filters = new List<SearchFilter> { new SearchFilter("Name", SearchConditionEnum.Equals, "") }
                });
                TestAssert.Count(1, r.Documents);
            });

            b.Add("Special characters preserved", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                string json = @"{""Text"":""quotes \"" angle <b> & ampersand""}";
                Document doc = await client.Document.Ingest(c.Id, json);
                Document got = await client.Document.ReadById(doc.Id, includeContent: true);
                TestAssert.Contains("ampersand", got.Content);
                TestAssert.Contains("<b>", got.Content);
            });

            b.Add("Deeply nested (5 levels) searchable", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                await client.Document.Ingest(c.Id, @"{""L1"":{""L2"":{""L3"":{""L4"":{""L5"":""DeepValue""}}}}}");
                SearchResult r = await client.Search.Search(new SearchQuery
                {
                    CollectionId = c.Id,
                    Filters = new List<SearchFilter> { new SearchFilter("L1.L2.L3.L4.L5", SearchConditionEnum.Equals, "DeepValue") }
                });
                TestAssert.Count(1, r.Documents);
            });

            b.Add("Large array (100 elements) searchable", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                StringBuilder sb = new StringBuilder(@"{""Items"":[");
                for (int i = 0; i < 100; i++) { if (i > 0) sb.Append(','); sb.Append(i); }
                sb.Append("]}");
                await client.Document.Ingest(c.Id, sb.ToString());
                SearchResult r = await client.Search.Search(new SearchQuery
                {
                    CollectionId = c.Id,
                    Filters = new List<SearchFilter> { new SearchFilter("Items", SearchConditionEnum.Equals, "50") }
                });
                TestAssert.Count(1, r.Documents);
            });

            b.Add("Numeric values", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                await client.Document.Ingest(c.Id, @"{""Int"":42,""Float"":3.14,""Neg"":-7,""Big"":1000000000}");
                SearchResult r = await client.Search.Search(new SearchQuery
                {
                    CollectionId = c.Id,
                    Filters = new List<SearchFilter> { new SearchFilter("Int", SearchConditionEnum.Equals, "42") }
                });
                TestAssert.Count(1, r.Documents);
            });

            b.Add("Boolean values", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                await client.Document.Ingest(c.Id, @"{""Active"":true}");
                await client.Document.Ingest(c.Id, @"{""Active"":false}");
                SearchResult r = await client.Search.Search(new SearchQuery
                {
                    CollectionId = c.Id,
                    Filters = new List<SearchFilter> { new SearchFilter("Active", SearchConditionEnum.Equals, "true") }
                });
                TestAssert.Count(1, r.Documents);
            });

            b.Add("Null values via IsNull", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                await client.Document.Ingest(c.Id, @"{""Name"":null}");
                await client.Document.Ingest(c.Id, @"{""Name"":""set""}");
                SearchResult r = await client.Search.Search(new SearchQuery
                {
                    CollectionId = c.Id,
                    Filters = new List<SearchFilter> { new SearchFilter("Name", SearchConditionEnum.IsNull, null) }
                });
                TestAssert.Count(1, r.Documents);
            });

            b.Add("Unicode characters preserved and searchable", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                string json = @"{""Greeting"":""こんにちは 🌟""}";
                await client.Document.Ingest(c.Id, json);
                SearchResult r = await client.Search.Search(new SearchQuery
                {
                    CollectionId = c.Id,
                    Filters = new List<SearchFilter> { new SearchFilter("Greeting", SearchConditionEnum.Equals, "こんにちは 🌟") }
                });
                TestAssert.Count(1, r.Documents);
            });

            b.Add("Empty JSON object ingests", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                Document doc = await client.Document.Ingest(c.Id, @"{}");
                TestAssert.NotNull(doc, "Empty object document is null");
                TestAssert.True(await client.Document.Exists(doc.Id), "Empty object document should exist");
            });

            b.Add("Very long string value round-trips", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                string longValue = new string('x', 10000);
                Document doc = await client.Document.Ingest(c.Id, $@"{{""Blob"":""{longValue}""}}");
                Document got = await client.Document.ReadById(doc.Id, includeContent: true);
                TestAssert.Contains(longValue, got.Content);
            });

            return b.Build();
        }
    }
}
