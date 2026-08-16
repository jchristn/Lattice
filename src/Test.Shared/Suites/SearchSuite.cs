namespace Test.Shared.Suites
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Lattice.Core.Models;
    using Lattice.Core.Search;
    using Touchstone.Core;

    /// <summary>
    /// Tests for the search API: every comparison/condition operator, label and tag filters,
    /// pagination, the result envelope (TotalRecords/RecordsRemaining/EndOfResults/Timestamp),
    /// content inclusion, and SQL-expression search.
    /// </summary>
    public static class SearchSuite
    {
        private static SearchQuery Query(string collectionId, params SearchFilter[] filters)
        {
            return new SearchQuery
            {
                CollectionId = collectionId,
                Filters = new List<SearchFilter>(filters)
            };
        }

        /// <summary>
        /// Build the suite.
        /// </summary>
        public static TestSuiteDescriptor Build()
        {
            SuiteBuilder b = new SuiteBuilder("search", "Search API");

            // ----- Comparison operators -----

            b.Add("Equals", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                await client.Document.Ingest(c.Id, @"{""Name"":""Joel""}");
                await client.Document.Ingest(c.Id, @"{""Name"":""John""}");
                await client.Document.Ingest(c.Id, @"{""Name"":""Joel""}");
                SearchResult r = await client.Search.Search(Query(c.Id, new SearchFilter("Name", SearchConditionEnum.Equals, "Joel")));
                TestAssert.Count(2, r.Documents);
            });

            b.Add("NotEquals", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                await client.Document.Ingest(c.Id, @"{""Name"":""Joel""}");
                await client.Document.Ingest(c.Id, @"{""Name"":""John""}");
                await client.Document.Ingest(c.Id, @"{""Name"":""Jane""}");
                SearchResult r = await client.Search.Search(Query(c.Id, new SearchFilter("Name", SearchConditionEnum.NotEquals, "Joel")));
                TestAssert.Count(2, r.Documents);
            });

            b.Add("GreaterThan", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                await client.Document.Ingest(c.Id, @"{""Age"":25}");
                await client.Document.Ingest(c.Id, @"{""Age"":30}");
                await client.Document.Ingest(c.Id, @"{""Age"":35}");
                SearchResult r = await client.Search.Search(Query(c.Id, new SearchFilter("Age", SearchConditionEnum.GreaterThan, "30")));
                TestAssert.Count(1, r.Documents);
            });

            b.Add("GreaterThanOrEqualTo", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                await client.Document.Ingest(c.Id, @"{""Age"":25}");
                await client.Document.Ingest(c.Id, @"{""Age"":30}");
                await client.Document.Ingest(c.Id, @"{""Age"":35}");
                SearchResult r = await client.Search.Search(Query(c.Id, new SearchFilter("Age", SearchConditionEnum.GreaterThanOrEqualTo, "30")));
                TestAssert.Count(2, r.Documents);
            });

            b.Add("LessThan", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                await client.Document.Ingest(c.Id, @"{""Age"":25}");
                await client.Document.Ingest(c.Id, @"{""Age"":30}");
                await client.Document.Ingest(c.Id, @"{""Age"":35}");
                SearchResult r = await client.Search.Search(Query(c.Id, new SearchFilter("Age", SearchConditionEnum.LessThan, "30")));
                TestAssert.Count(1, r.Documents);
            });

            b.Add("LessThanOrEqualTo", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                await client.Document.Ingest(c.Id, @"{""Age"":25}");
                await client.Document.Ingest(c.Id, @"{""Age"":30}");
                await client.Document.Ingest(c.Id, @"{""Age"":35}");
                SearchResult r = await client.Search.Search(Query(c.Id, new SearchFilter("Age", SearchConditionEnum.LessThanOrEqualTo, "30")));
                TestAssert.Count(2, r.Documents);
            });

            // ----- String operators -----

            b.Add("Contains", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                await client.Document.Ingest(c.Id, @"{""City"":""San Francisco""}");
                await client.Document.Ingest(c.Id, @"{""City"":""San Diego""}");
                await client.Document.Ingest(c.Id, @"{""City"":""Austin""}");
                SearchResult r = await client.Search.Search(Query(c.Id, new SearchFilter("City", SearchConditionEnum.Contains, "San")));
                TestAssert.Count(2, r.Documents);
            });

            b.Add("StartsWith", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                await client.Document.Ingest(c.Id, @"{""Name"":""Johnson""}");
                await client.Document.Ingest(c.Id, @"{""Name"":""John""}");
                await client.Document.Ingest(c.Id, @"{""Name"":""Jane""}");
                SearchResult r = await client.Search.Search(Query(c.Id, new SearchFilter("Name", SearchConditionEnum.StartsWith, "John")));
                TestAssert.Count(2, r.Documents);
            });

            b.Add("EndsWith", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                await client.Document.Ingest(c.Id, @"{""Email"":""a@example.com""}");
                await client.Document.Ingest(c.Id, @"{""Email"":""b@example.com""}");
                await client.Document.Ingest(c.Id, @"{""Email"":""c@example.org""}");
                SearchResult r = await client.Search.Search(Query(c.Id, new SearchFilter("Email", SearchConditionEnum.EndsWith, ".com")));
                TestAssert.Count(2, r.Documents);
            });

            // ----- Null operators -----

            b.Add("IsNull", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                await client.Document.Ingest(c.Id, @"{""Email"":null}");
                await client.Document.Ingest(c.Id, @"{""Email"":""x@y.com""}");
                SearchResult r = await client.Search.Search(Query(c.Id, new SearchFilter("Email", SearchConditionEnum.IsNull, null)));
                TestAssert.Count(1, r.Documents);
            });

            b.Add("IsNotNull", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                await client.Document.Ingest(c.Id, @"{""Email"":null}");
                await client.Document.Ingest(c.Id, @"{""Email"":""x@y.com""}");
                SearchResult r = await client.Search.Search(Query(c.Id, new SearchFilter("Email", SearchConditionEnum.IsNotNull, null)));
                TestAssert.Count(1, r.Documents);
            });

            // ----- Combined / metadata filters -----

            b.Add("Multiple filters (AND)", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                await client.Document.Ingest(c.Id, @"{""City"":""Austin"",""Age"":40}");
                await client.Document.Ingest(c.Id, @"{""City"":""Austin"",""Age"":30}");
                await client.Document.Ingest(c.Id, @"{""City"":""Dallas"",""Age"":40}");
                SearchResult r = await client.Search.Search(Query(c.Id,
                    new SearchFilter("City", SearchConditionEnum.Equals, "Austin"),
                    new SearchFilter("Age", SearchConditionEnum.GreaterThan, "35")));
                TestAssert.Count(1, r.Documents);
            });

            b.Add("Filter by label", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                await client.Document.Ingest(c.Id, @"{""N"":1}", labels: new List<string> { "important" });
                await client.Document.Ingest(c.Id, @"{""N"":2}", labels: new List<string> { "important" });
                await client.Document.Ingest(c.Id, @"{""N"":3}", labels: new List<string> { "other" });
                SearchResult r = await client.Search.Search(new SearchQuery { CollectionId = c.Id, Labels = new List<string> { "important" } });
                TestAssert.Count(2, r.Documents);
            });

            b.Add("Filter by tag", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                await client.Document.Ingest(c.Id, @"{""N"":1}", tags: new Dictionary<string, string> { { "status", "active" } });
                await client.Document.Ingest(c.Id, @"{""N"":2}", tags: new Dictionary<string, string> { { "status", "active" } });
                await client.Document.Ingest(c.Id, @"{""N"":3}", tags: new Dictionary<string, string> { { "status", "inactive" } });
                SearchResult r = await client.Search.Search(new SearchQuery { CollectionId = c.Id, Tags = new Dictionary<string, string> { { "status", "active" } } });
                TestAssert.Count(2, r.Documents);
            });

            b.Add("Filter by label and tag combined", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                await client.Document.Ingest(c.Id, @"{""N"":1}", labels: new List<string> { "important" }, tags: new Dictionary<string, string> { { "status", "active" } });
                await client.Document.Ingest(c.Id, @"{""N"":2}", labels: new List<string> { "important" }, tags: new Dictionary<string, string> { { "status", "inactive" } });
                SearchResult r = await client.Search.Search(new SearchQuery
                {
                    CollectionId = c.Id,
                    Labels = new List<string> { "important" },
                    Tags = new Dictionary<string, string> { { "status", "active" } }
                });
                TestAssert.Count(1, r.Documents);
            });

            // ----- Pagination -----

            b.Add("Pagination: Skip", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                for (int i = 0; i < 10; i++) await client.Document.Ingest(c.Id, $@"{{""Index"":{i}}}");
                SearchResult r = await client.Search.Search(new SearchQuery { CollectionId = c.Id, Skip = 5, MaxResults = 100 });
                TestAssert.Count(5, r.Documents);
            });

            b.Add("Pagination: MaxResults", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                for (int i = 0; i < 10; i++) await client.Document.Ingest(c.Id, $@"{{""Index"":{i}}}");
                SearchResult r = await client.Search.Search(new SearchQuery { CollectionId = c.Id, MaxResults = 3 });
                TestAssert.Count(3, r.Documents);
            });

            b.Add("Pagination: Skip and MaxResults", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                for (int i = 0; i < 10; i++) await client.Document.Ingest(c.Id, $@"{{""Index"":{i}}}");
                SearchResult r = await client.Search.Search(new SearchQuery { CollectionId = c.Id, Skip = 3, MaxResults = 4 });
                TestAssert.Count(4, r.Documents);
            });

            // ----- Result envelope -----

            b.Add("Envelope: TotalRecords", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                for (int i = 0; i < 10; i++) await client.Document.Ingest(c.Id, $@"{{""Index"":{i}}}");
                SearchResult r = await client.Search.Search(new SearchQuery { CollectionId = c.Id, MaxResults = 3 });
                TestAssert.Equal(10L, r.TotalRecords);
            });

            b.Add("Envelope: RecordsRemaining", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                for (int i = 0; i < 10; i++) await client.Document.Ingest(c.Id, $@"{{""Index"":{i}}}");
                SearchResult r = await client.Search.Search(new SearchQuery { CollectionId = c.Id, Skip = 2, MaxResults = 3 });
                TestAssert.Equal(5L, r.RecordsRemaining);
            });

            b.Add("Envelope: EndOfResults true", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                for (int i = 0; i < 5; i++) await client.Document.Ingest(c.Id, $@"{{""Index"":{i}}}");
                SearchResult r = await client.Search.Search(new SearchQuery { CollectionId = c.Id, MaxResults = 10 });
                TestAssert.True(r.EndOfResults, "Expected EndOfResults true");
            });

            b.Add("Envelope: EndOfResults false", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                for (int i = 0; i < 10; i++) await client.Document.Ingest(c.Id, $@"{{""Index"":{i}}}");
                SearchResult r = await client.Search.Search(new SearchQuery { CollectionId = c.Id, MaxResults = 5 });
                TestAssert.False(r.EndOfResults, "Expected EndOfResults false");
            });

            b.Add("Envelope: Timestamp populated", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                await client.Document.Ingest(c.Id, @"{""N"":1}");
                SearchResult r = await client.Search.Search(new SearchQuery { CollectionId = c.Id });
                TestAssert.NotNull(r.Timestamp, "Timestamp is null");
                TestAssert.True(r.Timestamp.End >= r.Timestamp.Start, "Timestamp End should be >= Start");
            });

            b.Add("Empty results", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                await client.Document.Ingest(c.Id, @"{""Name"":""Joel""}");
                SearchResult r = await client.Search.Search(Query(c.Id, new SearchFilter("Name", SearchConditionEnum.Equals, "Nobody")));
                TestAssert.Count(0, r.Documents);
                TestAssert.Equal(0L, r.TotalRecords);
                TestAssert.True(r.EndOfResults, "Expected EndOfResults true for empty result");
            });

            b.Add("IncludeContent true populates content", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                await client.Document.Ingest(c.Id, @"{""Name"":""Joel""}");
                SearchResult r = await client.Search.Search(new SearchQuery { CollectionId = c.Id, IncludeContent = true });
                TestAssert.Count(1, r.Documents);
                TestAssert.NotNull(r.Documents[0].Content, "Content should be populated");
                TestAssert.Contains("Joel", r.Documents[0].Content);
            });

            b.Add("IncludeContent false leaves content null", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                await client.Document.Ingest(c.Id, @"{""Name"":""Joel""}");
                SearchResult r = await client.Search.Search(new SearchQuery { CollectionId = c.Id, IncludeContent = false });
                TestAssert.Count(1, r.Documents);
                TestAssert.Null(r.Documents[0].Content, "Content should be null");
            });

            b.Add("Nested field search", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                await client.Document.Ingest(c.Id, @"{""Person"":{""Name"":{""First"":""Joel"",""Last"":""Christner""}}}");
                SearchResult r = await client.Search.Search(Query(c.Id, new SearchFilter("Person.Name.First", SearchConditionEnum.Equals, "Joel")));
                TestAssert.Count(1, r.Documents);
            });

            b.Add("Array element search", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                await client.Document.Ingest(c.Id, @"{""Tags"":[""red"",""green"",""blue""]}");
                SearchResult r = await client.Search.Search(Query(c.Id, new SearchFilter("Tags", SearchConditionEnum.Equals, "green")));
                TestAssert.Count(1, r.Documents);
            });

            // ----- SQL expression search -----

            b.Add("SearchBySql: basic WHERE", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                await client.Document.Ingest(c.Id, @"{""Name"":""Joel""}");
                await client.Document.Ingest(c.Id, @"{""Name"":""John""}");
                SearchResult r = await client.Search.SearchBySql(c.Id, "SELECT * FROM documents WHERE Name = 'Joel'");
                TestAssert.Count(1, r.Documents);
            });

            b.Add("SearchBySql: LIMIT", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                for (int i = 0; i < 10; i++) await client.Document.Ingest(c.Id, $@"{{""Index"":{i}}}");
                SearchResult r = await client.Search.SearchBySql(c.Id, "SELECT * FROM documents LIMIT 3");
                TestAssert.Count(3, r.Documents);
            });

            b.Add("SearchBySql: LIMIT with OFFSET", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                for (int i = 0; i < 10; i++) await client.Document.Ingest(c.Id, $@"{{""Index"":{i}}}");
                SearchResult r = await client.Search.SearchBySql(c.Id, "SELECT * FROM documents LIMIT 3 OFFSET 2");
                TestAssert.Count(3, r.Documents);
            });

            b.Add("SearchBySql: with label and tag filters", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                await client.Document.Ingest(c.Id, @"{""Name"":""Joel""}",
                    labels: new List<string> { "important" }, tags: new Dictionary<string, string> { { "env", "prod" } });
                await client.Document.Ingest(c.Id, @"{""Name"":""Joel""}",
                    labels: new List<string> { "other" }, tags: new Dictionary<string, string> { { "env", "prod" } });
                await client.Document.Ingest(c.Id, @"{""Name"":""John""}",
                    labels: new List<string> { "important" }, tags: new Dictionary<string, string> { { "env", "prod" } });

                // WHERE Name = 'Joel' matches docs 1 and 2; the "important" label narrows to doc 1;
                // the env=prod tag is satisfied by all three, so the result is exactly doc 1.
                SearchResult r = await client.Search.SearchBySql(
                    c.Id,
                    "SELECT * FROM documents WHERE Name = 'Joel'",
                    new List<string> { "important" },
                    new Dictionary<string, string> { { "env", "prod" } });
                TestAssert.Count(1, r.Documents);
            });

            // ----- Like operator (SQL LIKE wildcards) -----

            b.Add("Like: percent wildcard matches pattern", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                await client.Document.Ingest(c.Id, @"{""Word"":""apple""}");
                await client.Document.Ingest(c.Id, @"{""Word"":""apricot""}");
                await client.Document.Ingest(c.Id, @"{""Word"":""grape""}");
                // "a%t" => starts with 'a', ends with 't' => only "apricot".
                SearchResult r = await client.Search.Search(Query(c.Id, new SearchFilter("Word", SearchConditionEnum.Like, "a%t")));
                TestAssert.Count(1, r.Documents);
            });

            b.Add("Like: underscore wildcard matches single character", async client =>
            {
                Collection c = await client.Collection.Create("Test");
                await client.Document.Ingest(c.Id, @"{""Word"":""grape""}");
                await client.Document.Ingest(c.Id, @"{""Word"":""gripe""}");
                await client.Document.Ingest(c.Id, @"{""Word"":""grumble""}");
                // "gr_pe" => 'gr' + exactly one char + 'pe' => "grape" and "gripe", but not "grumble".
                SearchResult r = await client.Search.Search(Query(c.Id, new SearchFilter("Word", SearchConditionEnum.Like, "gr_pe")));
                TestAssert.Count(2, r.Documents);
            });

            // ----- Negative: argument validation -----

            b.Add("Search: null query throws", async client =>
            {
                await TestAssert.ThrowsAsync<System.ArgumentNullException>(() => client.Search.Search(null));
            });

            b.Add("SearchFilter: null field throws", async client =>
            {
                await TestAssert.ThrowsAsync<System.ArgumentNullException>(() =>
                {
                    _ = new SearchFilter(null, SearchConditionEnum.Equals, "x");
                    return Task.CompletedTask;
                });
            });

            return b.Build();
        }
    }
}
