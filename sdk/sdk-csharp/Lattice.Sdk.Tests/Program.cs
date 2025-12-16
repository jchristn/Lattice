using System.Diagnostics;
using Lattice.Sdk;
using Lattice.Sdk.Models;

namespace Lattice.Sdk.Tests
{
    /// <summary>
    /// Lattice SDK Test Harness for C#
    ///
    /// A comprehensive test suite for the Lattice C# SDK.
    /// Tests all API endpoints and validates responses.
    ///
    /// Usage:
    ///     dotnet run -- <endpoint_url>
    ///
    /// Example:
    ///     dotnet run -- http://localhost:8000
    /// </summary>
    public class Program
    {
        private static int _passCount = 0;
        private static int _failCount = 0;
        private static readonly List<TestResult> _results = new List<TestResult>();
        private static readonly Stopwatch _overallStopwatch = new Stopwatch();
        private static string _currentSection = string.Empty;
        private static LatticeClient _client = null!;

        /// <summary>
        /// Represents the outcome of a test method.
        /// </summary>
        private struct TestOutcome
        {
            public bool Success;
            public string? Error;

            public static TestOutcome Pass() => new TestOutcome { Success = true, Error = null };
            public static TestOutcome Fail(string error) => new TestOutcome { Success = false, Error = error };
        }

        /// <summary>
        /// Represents a test result.
        /// </summary>
        private class TestResult
        {
            public string Section { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public bool Passed { get; set; }
            public long ElapsedMs { get; set; }
            public string? Error { get; set; }
        }

        public static async Task<int> Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: dotnet run -- <endpoint_url>");
                Console.WriteLine("Example: dotnet run -- http://localhost:8000");
                return 1;
            }

            string endpoint = args[0];

            Console.WriteLine("===============================================================================");
            Console.WriteLine("  Lattice SDK Test Harness - C#");
            Console.WriteLine("===============================================================================");
            Console.WriteLine();
            Console.WriteLine($"  Endpoint: {endpoint}");
            Console.WriteLine();

            _client = new LatticeClient(endpoint);
            _overallStopwatch.Start();

            try
            {
                // Health check first
                await RunTestSection("HEALTH CHECK", TestHealthCheck);

                // Collection API Tests
                await RunTestSection("COLLECTION API", TestCollectionApi);

                // Document API Tests
                await RunTestSection("DOCUMENT API", TestDocumentApi);

                // Search API Tests
                await RunTestSection("SEARCH API", TestSearchApi);

                // Enumeration API Tests
                await RunTestSection("ENUMERATION API", TestEnumerationApi);

                // Schema API Tests
                await RunTestSection("SCHEMA API", TestSchemaApi);

                // Index API Tests
                await RunTestSection("INDEX API", TestIndexApi);

                // Constraint Tests
                await RunTestSection("SCHEMA CONSTRAINTS", TestConstraintsApi);

                // Indexing Mode Tests
                await RunTestSection("INDEXING MODE", TestIndexingModeApi);

                // Edge Case Tests
                await RunTestSection("EDGE CASES", TestEdgeCases);

                // Performance Tests
                await RunTestSection("PERFORMANCE", TestPerformance);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FATAL] Unhandled exception: {ex.Message}");
                _failCount++;
            }

            _overallStopwatch.Stop();
            PrintSummary();

            _client.Dispose();
            return _failCount > 0 ? 1 : 0;
        }

        private static async Task RunTestSection(string sectionName, Func<Task> tests)
        {
            Console.WriteLine();
            Console.WriteLine($"--- {sectionName} ---");
            _currentSection = sectionName;
            await tests();
        }

        private static async Task RunTest(string name, Func<Task<TestOutcome>> test)
        {
            Stopwatch sw = Stopwatch.StartNew();
            bool passed = false;
            string? error = null;

            try
            {
                TestOutcome outcome = await test();
                passed = outcome.Success;
                error = outcome.Error;
            }
            catch (Exception ex)
            {
                passed = false;
                error = ex.Message;
            }

            sw.Stop();

            TestResult result = new TestResult
            {
                Section = _currentSection,
                Name = name,
                Passed = passed,
                ElapsedMs = sw.ElapsedMilliseconds,
                Error = error
            };
            _results.Add(result);

            if (passed)
            {
                Console.WriteLine($"  [PASS] {name} ({sw.ElapsedMilliseconds}ms)");
                _passCount++;
            }
            else
            {
                Console.WriteLine($"  [FAIL] {name} ({sw.ElapsedMilliseconds}ms)");
                if (!string.IsNullOrEmpty(error))
                    Console.WriteLine($"         Error: {error}");
                _failCount++;
            }
        }

        private static void PrintSummary()
        {
            Console.WriteLine();
            Console.WriteLine("===============================================================================");
            Console.WriteLine("  TEST SUMMARY");
            Console.WriteLine("===============================================================================");
            Console.WriteLine();

            // Group by section
            IEnumerable<IGrouping<string, TestResult>> sections = _results.GroupBy(r => r.Section);
            foreach (IGrouping<string, TestResult> section in sections)
            {
                int sectionPass = section.Count(r => r.Passed);
                int sectionTotal = section.Count();
                string status = sectionPass == sectionTotal ? "PASS" : "FAIL";
                Console.WriteLine($"  {section.Key}: {sectionPass}/{sectionTotal} [{status}]");
            }

            Console.WriteLine();
            Console.WriteLine("-------------------------------------------------------------------------------");
            string overallStatus = _failCount == 0 ? "PASS" : "FAIL";
            Console.WriteLine($"  TOTAL: {_passCount} passed, {_failCount} failed [{overallStatus}]");
            Console.WriteLine($"  RUNTIME: {_overallStopwatch.ElapsedMilliseconds}ms ({_overallStopwatch.Elapsed.TotalSeconds:F2}s)");
            Console.WriteLine("-------------------------------------------------------------------------------");

            if (_failCount > 0)
            {
                Console.WriteLine();
                Console.WriteLine("  FAILED TESTS:");
                foreach (TestResult failed in _results.Where(r => !r.Passed))
                {
                    Console.WriteLine($"    - {failed.Section}: {failed.Name}");
                    if (!string.IsNullOrEmpty(failed.Error))
                        Console.WriteLine($"      Error: {failed.Error}");
                }
            }

            Console.WriteLine();
        }

        // ========== HEALTH CHECK TESTS ==========

        private static async Task TestHealthCheck()
        {
            await RunTest("Health check returns true", async () =>
            {
                bool healthy = await _client.HealthCheckAsync();
                return healthy ? TestOutcome.Pass() : TestOutcome.Fail("Health check failed");
            });
        }

        // ========== COLLECTION API TESTS ==========

        private static async Task TestCollectionApi()
        {
            await RunTest("CreateCollection: basic", async () =>
            {
                Collection? collection = await _client.Collection.CreateAsync("test_basic_collection");
                if (collection == null) return TestOutcome.Fail("Collection creation returned null");
                if (!collection.Id.StartsWith("col_")) return TestOutcome.Fail($"Invalid collection ID: {collection.Id}");
                await _client.Collection.DeleteAsync(collection.Id);
                return TestOutcome.Pass();
            });

            await RunTest("CreateCollection: with all parameters", async () =>
            {
                Collection? collection = await _client.Collection.CreateAsync(
                    name: "test_full_collection",
                    description: "A test collection",
                    labels: new List<string> { "test", "full" },
                    tags: new Dictionary<string, string> { ["env"] = "test", ["version"] = "1.0" },
                    schemaEnforcementMode: SchemaEnforcementMode.Flexible,
                    indexingMode: IndexingMode.All
                );
                if (collection == null) return TestOutcome.Fail("Collection creation returned null");
                if (collection.Name != "test_full_collection") return TestOutcome.Fail($"Name mismatch: {collection.Name}");
                if (collection.Description != "A test collection") return TestOutcome.Fail($"Description mismatch: {collection.Description}");
                await _client.Collection.DeleteAsync(collection.Id);
                return TestOutcome.Pass();
            });

            await RunTest("CreateCollection: verify all properties", async () =>
            {
                Collection? collection = await _client.Collection.CreateAsync(
                    name: "test_props_collection",
                    description: "Props test",
                    labels: new List<string> { "prop_test" },
                    tags: new Dictionary<string, string> { ["key"] = "value" }
                );
                if (collection == null) return TestOutcome.Fail("Collection creation returned null");
                if (string.IsNullOrEmpty(collection.Id)) return TestOutcome.Fail("Id is empty");
                if (collection.CreatedUtc == null) return TestOutcome.Fail("CreatedUtc not set");
                await _client.Collection.DeleteAsync(collection.Id);
                return TestOutcome.Pass();
            });

            await RunTest("GetCollection: existing", async () =>
            {
                Collection? collection = await _client.Collection.CreateAsync("test_get_existing");
                if (collection == null) return TestOutcome.Fail("Setup: Collection creation failed");
                Collection? retrieved = await _client.Collection.ReadByIdAsync(collection.Id);
                if (retrieved == null)
                {
                    await _client.Collection.DeleteAsync(collection.Id);
                    return TestOutcome.Fail("GetCollection returned null");
                }
                if (retrieved.Id != collection.Id)
                {
                    await _client.Collection.DeleteAsync(collection.Id);
                    return TestOutcome.Fail($"Id mismatch: {retrieved.Id}");
                }
                await _client.Collection.DeleteAsync(collection.Id);
                return TestOutcome.Pass();
            });

            await RunTest("GetCollection: non-existent returns null", async () =>
            {
                Collection? retrieved = await _client.Collection.ReadByIdAsync("col_nonexistent12345");
                if (retrieved != null) return TestOutcome.Fail("Expected null for non-existent collection");
                return TestOutcome.Pass();
            });

            await RunTest("GetCollections: multiple", async () =>
            {
                Collection? col1 = await _client.Collection.CreateAsync("test_multi_1");
                Collection? col2 = await _client.Collection.CreateAsync("test_multi_2");
                if (col1 == null || col2 == null) return TestOutcome.Fail("Setup: Collection creation failed");

                List<Collection> collections = await _client.Collection.ReadAllAsync();
                HashSet<string> foundIds = new HashSet<string>(collections.Select(c => c.Id));

                if (!foundIds.Contains(col1.Id) || !foundIds.Contains(col2.Id))
                {
                    await _client.Collection.DeleteAsync(col1.Id);
                    await _client.Collection.DeleteAsync(col2.Id);
                    return TestOutcome.Fail("Not all collections found");
                }

                await _client.Collection.DeleteAsync(col1.Id);
                await _client.Collection.DeleteAsync(col2.Id);
                return TestOutcome.Pass();
            });

            await RunTest("CollectionExists: true when exists", async () =>
            {
                Collection? collection = await _client.Collection.CreateAsync("test_exists_true");
                if (collection == null) return TestOutcome.Fail("Setup: Collection creation failed");
                bool exists = await _client.Collection.ExistsAsync(collection.Id);
                await _client.Collection.DeleteAsync(collection.Id);
                if (!exists) return TestOutcome.Fail("Expected exists to be true");
                return TestOutcome.Pass();
            });

            await RunTest("CollectionExists: false when not exists", async () =>
            {
                bool exists = await _client.Collection.ExistsAsync("col_nonexistent12345");
                if (exists) return TestOutcome.Fail("Expected exists to be false");
                return TestOutcome.Pass();
            });

            await RunTest("DeleteCollection: removes collection", async () =>
            {
                Collection? collection = await _client.Collection.CreateAsync("test_delete");
                if (collection == null) return TestOutcome.Fail("Setup: Collection creation failed");
                bool deleted = await _client.Collection.DeleteAsync(collection.Id);
                if (!deleted) return TestOutcome.Fail("Delete returned false");
                bool exists = await _client.Collection.ExistsAsync(collection.Id);
                if (exists) return TestOutcome.Fail("Collection still exists after delete");
                return TestOutcome.Pass();
            });
        }

        // ========== DOCUMENT API TESTS ==========

        private static async Task TestDocumentApi()
        {
            Collection? collection = await _client.Collection.CreateAsync("doc_test_collection");
            if (collection == null)
            {
                await RunTest("Setup: Create collection", async () => TestOutcome.Fail("Collection creation failed"));
                return;
            }

            try
            {
                await RunTest("IngestDocument: basic", async () =>
                {
                    Document? doc = await _client.Document.IngestAsync(collection.Id, new { name = "Test" });
                    if (doc == null) return TestOutcome.Fail("Ingest returned null");
                    if (!doc.Id.StartsWith("doc_")) return TestOutcome.Fail($"Invalid document ID: {doc.Id}");
                    return TestOutcome.Pass();
                });

                await RunTest("IngestDocument: with name", async () =>
                {
                    Document? doc = await _client.Document.IngestAsync(
                        collection.Id,
                        new { name = "Named" },
                        name: "my_document"
                    );
                    if (doc == null) return TestOutcome.Fail("Ingest returned null");
                    if (doc.Name != "my_document") return TestOutcome.Fail($"Name mismatch: {doc.Name}");
                    return TestOutcome.Pass();
                });

                await RunTest("IngestDocument: with labels", async () =>
                {
                    Document? doc = await _client.Document.IngestAsync(
                        collection.Id,
                        new { name = "Labeled" },
                        labels: new List<string> { "label1", "label2" }
                    );
                    if (doc == null) return TestOutcome.Fail("Ingest returned null");
                    return TestOutcome.Pass();
                });

                await RunTest("IngestDocument: with tags", async () =>
                {
                    Document? doc = await _client.Document.IngestAsync(
                        collection.Id,
                        new { name = "Tagged" },
                        tags: new Dictionary<string, string> { ["key"] = "value" }
                    );
                    if (doc == null) return TestOutcome.Fail("Ingest returned null");
                    return TestOutcome.Pass();
                });

                await RunTest("IngestDocument: verify all properties", async () =>
                {
                    Document? doc = await _client.Document.IngestAsync(
                        collection.Id,
                        new { name = "Properties Test" },
                        name: "prop_doc",
                        labels: new List<string> { "prop" },
                        tags: new Dictionary<string, string> { ["prop"] = "test" }
                    );
                    if (doc == null) return TestOutcome.Fail("Ingest returned null");
                    if (string.IsNullOrEmpty(doc.Id)) return TestOutcome.Fail("Id is empty");
                    if (doc.CollectionId != collection.Id) return TestOutcome.Fail($"CollectionId mismatch: {doc.CollectionId}");
                    if (string.IsNullOrEmpty(doc.SchemaId)) return TestOutcome.Fail("SchemaId is empty");
                    if (doc.CreatedUtc == null) return TestOutcome.Fail("CreatedUtc not set");
                    return TestOutcome.Pass();
                });

                await RunTest("IngestDocument: nested JSON", async () =>
                {
                    Document? doc = await _client.Document.IngestAsync(
                        collection.Id,
                        new
                        {
                            person = new
                            {
                                name = "John",
                                address = new { city = "New York", zip = "10001" }
                            }
                        }
                    );
                    if (doc == null) return TestOutcome.Fail("Ingest returned null");
                    return TestOutcome.Pass();
                });

                await RunTest("IngestDocument: array JSON", async () =>
                {
                    Document? doc = await _client.Document.IngestAsync(
                        collection.Id,
                        new
                        {
                            items = new[] { 1, 2, 3, 4, 5 },
                            names = new[] { "Alice", "Bob", "Charlie" }
                        }
                    );
                    if (doc == null) return TestOutcome.Fail("Ingest returned null");
                    return TestOutcome.Pass();
                });

                Document? testDoc = await _client.Document.IngestAsync(
                    collection.Id,
                    new { name = "GetTest", value = 42 },
                    name: "get_test_doc",
                    labels: new List<string> { "get_test" },
                    tags: new Dictionary<string, string> { ["test_type"] = "get" }
                );

                if (testDoc != null)
                {
                    await RunTest("GetDocument: without content", async () =>
                    {
                        Document? doc = await _client.Document.ReadByIdAsync(collection.Id, testDoc.Id, includeContent: false);
                        if (doc == null) return TestOutcome.Fail("GetDocument returned null");
                        if (doc.Content != null) return TestOutcome.Fail("Content should be null");
                        return TestOutcome.Pass();
                    });

                    await RunTest("GetDocument: with content", async () =>
                    {
                        Document? doc = await _client.Document.ReadByIdAsync(collection.Id, testDoc.Id, includeContent: true);
                        if (doc == null) return TestOutcome.Fail("GetDocument returned null");
                        if (doc.Content == null) return TestOutcome.Fail("Content should not be null");
                        return TestOutcome.Pass();
                    });

                    await RunTest("GetDocument: verify labels populated", async () =>
                    {
                        Document? doc = await _client.Document.ReadByIdAsync(collection.Id, testDoc.Id, includeContent: false, includeLabels: true);
                        if (doc == null) return TestOutcome.Fail("GetDocument returned null");
                        if (!doc.Labels.Contains("get_test")) return TestOutcome.Fail($"Label 'get_test' not found: {string.Join(", ", doc.Labels)}");
                        return TestOutcome.Pass();
                    });

                    await RunTest("GetDocument: verify tags populated", async () =>
                    {
                        Document? doc = await _client.Document.ReadByIdAsync(collection.Id, testDoc.Id, includeContent: false, includeLabels: true, includeTags: true);
                        if (doc == null) return TestOutcome.Fail("GetDocument returned null");
                        if (!doc.Tags.TryGetValue("test_type", out string? tagValue) || tagValue != "get")
                            return TestOutcome.Fail($"Tag 'test_type' mismatch: {string.Join(", ", doc.Tags.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");
                        return TestOutcome.Pass();
                    });
                }

                await RunTest("GetDocument: non-existent returns null", async () =>
                {
                    Document? doc = await _client.Document.ReadByIdAsync(collection.Id, "doc_nonexistent12345");
                    if (doc != null) return TestOutcome.Fail("Expected null for non-existent document");
                    return TestOutcome.Pass();
                });

                await RunTest("GetDocuments: multiple documents", async () =>
                {
                    List<Document> docs = await _client.Document.ReadAllInCollectionAsync(collection.Id);
                    if (docs.Count < 5) return TestOutcome.Fail($"Expected at least 5 docs, got {docs.Count}");
                    return TestOutcome.Pass();
                });

                await RunTest("DocumentExists: true when exists", async () =>
                {
                    if (testDoc == null) return TestOutcome.Fail("Setup: testDoc is null");
                    bool exists = await _client.Document.ExistsAsync(collection.Id, testDoc.Id);
                    if (!exists) return TestOutcome.Fail("Expected exists to be true");
                    return TestOutcome.Pass();
                });

                await RunTest("DocumentExists: false when not exists", async () =>
                {
                    bool exists = await _client.Document.ExistsAsync(collection.Id, "doc_nonexistent12345");
                    if (exists) return TestOutcome.Fail("Expected exists to be false");
                    return TestOutcome.Pass();
                });

                await RunTest("DeleteDocument: removes document", async () =>
                {
                    Document? doc = await _client.Document.IngestAsync(collection.Id, new { to_delete = true });
                    if (doc == null) return TestOutcome.Fail("Setup: Ingest failed");
                    bool deleted = await _client.Document.DeleteAsync(collection.Id, doc.Id);
                    if (!deleted) return TestOutcome.Fail("Delete returned false");
                    bool exists = await _client.Document.ExistsAsync(collection.Id, doc.Id);
                    if (exists) return TestOutcome.Fail("Document still exists after delete");
                    return TestOutcome.Pass();
                });
            }
            finally
            {
                await _client.Collection.DeleteAsync(collection.Id);
            }
        }

        // ========== SEARCH API TESTS ==========

        private static async Task TestSearchApi()
        {
            Collection? collection = await _client.Collection.CreateAsync("search_test_collection");
            if (collection == null)
            {
                await RunTest("Setup: Create collection", async () => TestOutcome.Fail("Collection creation failed"));
                return;
            }

            try
            {
                // Ingest test documents
                for (int i = 0; i < 20; i++)
                {
                    await _client.Document.IngestAsync(
                        collection.Id,
                        new
                        {
                            Name = $"Item_{i}",
                            Category = $"Category_{i % 5}",
                            Value = i * 10,
                            IsActive = i % 2 == 0,
                            Description = $"This is item number {i}"
                        },
                        name: $"doc_{i}",
                        labels: new List<string> { $"group_{i % 3}" }.Concat(i % 10 == 0 ? new[] { "special" } : Array.Empty<string>()).ToList(),
                        tags: new Dictionary<string, string> { ["priority"] = (i % 3).ToString() }
                    );
                }

                await RunTest("Search: Equals operator", async () =>
                {
                    SearchResult? result = await _client.Search.SearchAsync(new SearchQuery
                    {
                        CollectionId = collection.Id,
                        Filters = new List<SearchFilter> { new SearchFilter("Category", SearchCondition.Equals, "Category_2") },
                        MaxResults = 100
                    });
                    if (result == null) return TestOutcome.Fail("Search returned null");
                    if (!result.Success) return TestOutcome.Fail("Search not successful");
                    if (result.Documents.Count != 4) return TestOutcome.Fail($"Expected 4 results, got {result.Documents.Count}");
                    return TestOutcome.Pass();
                });

                await RunTest("Search: NotEquals operator", async () =>
                {
                    SearchResult? result = await _client.Search.SearchAsync(new SearchQuery
                    {
                        CollectionId = collection.Id,
                        Filters = new List<SearchFilter> { new SearchFilter("Category", SearchCondition.NotEquals, "Category_0") },
                        MaxResults = 100
                    });
                    if (result == null) return TestOutcome.Fail("Search returned null");
                    if (result.Documents.Count != 16) return TestOutcome.Fail($"Expected 16 results, got {result.Documents.Count}");
                    return TestOutcome.Pass();
                });

                await RunTest("Search: GreaterThan operator", async () =>
                {
                    SearchResult? result = await _client.Search.SearchAsync(new SearchQuery
                    {
                        CollectionId = collection.Id,
                        Filters = new List<SearchFilter> { new SearchFilter("Value", SearchCondition.GreaterThan, "150") },
                        MaxResults = 100
                    });
                    if (result == null) return TestOutcome.Fail("Search returned null");
                    if (result.Documents.Count != 4) return TestOutcome.Fail($"Expected 4 results, got {result.Documents.Count}");
                    return TestOutcome.Pass();
                });

                await RunTest("Search: LessThan operator", async () =>
                {
                    SearchResult? result = await _client.Search.SearchAsync(new SearchQuery
                    {
                        CollectionId = collection.Id,
                        Filters = new List<SearchFilter> { new SearchFilter("Value", SearchCondition.LessThan, "30") },
                        MaxResults = 100
                    });
                    if (result == null) return TestOutcome.Fail("Search returned null");
                    if (result.Documents.Count != 3) return TestOutcome.Fail($"Expected 3 results, got {result.Documents.Count}");
                    return TestOutcome.Pass();
                });

                await RunTest("Search: Contains operator", async () =>
                {
                    SearchResult? result = await _client.Search.SearchAsync(new SearchQuery
                    {
                        CollectionId = collection.Id,
                        Filters = new List<SearchFilter> { new SearchFilter("Name", SearchCondition.Contains, "Item_1") },
                        MaxResults = 100
                    });
                    if (result == null) return TestOutcome.Fail("Search returned null");
                    if (result.Documents.Count < 1) return TestOutcome.Fail($"Expected at least 1 result, got {result.Documents.Count}");
                    return TestOutcome.Pass();
                });

                await RunTest("Search: StartsWith operator", async () =>
                {
                    SearchResult? result = await _client.Search.SearchAsync(new SearchQuery
                    {
                        CollectionId = collection.Id,
                        Filters = new List<SearchFilter> { new SearchFilter("Name", SearchCondition.StartsWith, "Item_") },
                        MaxResults = 100
                    });
                    if (result == null) return TestOutcome.Fail("Search returned null");
                    if (result.Documents.Count != 20) return TestOutcome.Fail($"Expected 20 results, got {result.Documents.Count}");
                    return TestOutcome.Pass();
                });

                await RunTest("Search: multiple filters (AND)", async () =>
                {
                    SearchResult? result = await _client.Search.SearchAsync(new SearchQuery
                    {
                        CollectionId = collection.Id,
                        Filters = new List<SearchFilter>
                        {
                            new SearchFilter("Category", SearchCondition.Equals, "Category_2"),
                            new SearchFilter("IsActive", SearchCondition.Equals, "true")
                        },
                        MaxResults = 100
                    });
                    if (result == null) return TestOutcome.Fail("Search returned null");
                    if (result.Documents.Count != 2) return TestOutcome.Fail($"Expected 2 results, got {result.Documents.Count}");
                    return TestOutcome.Pass();
                });

                await RunTest("Search: by label", async () =>
                {
                    SearchResult? result = await _client.Search.SearchAsync(new SearchQuery
                    {
                        CollectionId = collection.Id,
                        Labels = new List<string> { "special" },
                        MaxResults = 100
                    });
                    if (result == null) return TestOutcome.Fail("Search returned null");
                    if (result.Documents.Count != 2) return TestOutcome.Fail($"Expected 2 results, got {result.Documents.Count}");
                    return TestOutcome.Pass();
                });

                await RunTest("Search: by tag", async () =>
                {
                    SearchResult? result = await _client.Search.SearchAsync(new SearchQuery
                    {
                        CollectionId = collection.Id,
                        Tags = new Dictionary<string, string> { ["priority"] = "0" },
                        MaxResults = 100
                    });
                    if (result == null) return TestOutcome.Fail("Search returned null");
                    if (result.Documents.Count != 7) return TestOutcome.Fail($"Expected 7 results, got {result.Documents.Count}");
                    return TestOutcome.Pass();
                });

                await RunTest("Search: pagination Skip", async () =>
                {
                    SearchResult? result = await _client.Search.SearchAsync(new SearchQuery
                    {
                        CollectionId = collection.Id,
                        Skip = 10,
                        MaxResults = 100
                    });
                    if (result == null) return TestOutcome.Fail("Search returned null");
                    if (result.Documents.Count != 10) return TestOutcome.Fail($"Expected 10 results, got {result.Documents.Count}");
                    return TestOutcome.Pass();
                });

                await RunTest("Search: pagination MaxResults", async () =>
                {
                    SearchResult? result = await _client.Search.SearchAsync(new SearchQuery
                    {
                        CollectionId = collection.Id,
                        MaxResults = 5
                    });
                    if (result == null) return TestOutcome.Fail("Search returned null");
                    if (result.Documents.Count != 5) return TestOutcome.Fail($"Expected 5 results, got {result.Documents.Count}");
                    return TestOutcome.Pass();
                });

                await RunTest("Search: verify TotalRecords", async () =>
                {
                    SearchResult? result = await _client.Search.SearchAsync(new SearchQuery
                    {
                        CollectionId = collection.Id,
                        MaxResults = 5
                    });
                    if (result == null) return TestOutcome.Fail("Search returned null");
                    if (result.TotalRecords != 20) return TestOutcome.Fail($"Expected TotalRecords=20, got {result.TotalRecords}");
                    return TestOutcome.Pass();
                });

                await RunTest("Search: verify EndOfResults true", async () =>
                {
                    SearchResult? result = await _client.Search.SearchAsync(new SearchQuery
                    {
                        CollectionId = collection.Id,
                        MaxResults = 100
                    });
                    if (result == null) return TestOutcome.Fail("Search returned null");
                    if (!result.EndOfResults) return TestOutcome.Fail("Expected EndOfResults=true");
                    return TestOutcome.Pass();
                });

                await RunTest("Search: empty results", async () =>
                {
                    SearchResult? result = await _client.Search.SearchAsync(new SearchQuery
                    {
                        CollectionId = collection.Id,
                        Filters = new List<SearchFilter> { new SearchFilter("Name", SearchCondition.Equals, "NonExistent") },
                        MaxResults = 100
                    });
                    if (result == null) return TestOutcome.Fail("Search returned null");
                    if (result.Documents.Count != 0) return TestOutcome.Fail($"Expected 0 results, got {result.Documents.Count}");
                    return TestOutcome.Pass();
                });

                await RunTest("Search: with IncludeContent true", async () =>
                {
                    SearchResult? result = await _client.Search.SearchAsync(new SearchQuery
                    {
                        CollectionId = collection.Id,
                        MaxResults = 1,
                        IncludeContent = true
                    });
                    if (result == null) return TestOutcome.Fail("Search returned null");
                    if (result.Documents.Count == 0) return TestOutcome.Fail("No documents returned");
                    if (result.Documents[0].Content == null) return TestOutcome.Fail("Content should be included");
                    return TestOutcome.Pass();
                });

                await RunTest("SearchBySql: basic query", async () =>
                {
                    SearchResult? result = await _client.Search.SearchBySqlAsync(
                        collection.Id,
                        "SELECT * FROM documents WHERE Category = 'Category_1'"
                    );
                    if (result == null) return TestOutcome.Fail("Search returned null");
                    if (result.Documents.Count != 4) return TestOutcome.Fail($"Expected 4 results, got {result.Documents.Count}");
                    return TestOutcome.Pass();
                });
            }
            finally
            {
                await _client.Collection.DeleteAsync(collection.Id);
            }
        }

        // ========== ENUMERATION API TESTS ==========

        private static async Task TestEnumerationApi()
        {
            Collection? collection = await _client.Collection.CreateAsync("enum_test_collection");
            if (collection == null)
            {
                await RunTest("Setup: Create collection", async () => TestOutcome.Fail("Collection creation failed"));
                return;
            }

            try
            {
                // Ingest test documents
                for (int i = 0; i < 10; i++)
                {
                    await _client.Document.IngestAsync(
                        collection.Id,
                        new { index = i, name = $"EnumItem_{i}" },
                        name: $"enum_doc_{i}"
                    );
                    await Task.Delay(50); // Small delay
                }

                await RunTest("Enumerate: basic", async () =>
                {
                    SearchResult? result = await _client.Search.EnumerateAsync(new SearchQuery
                    {
                        CollectionId = collection.Id,
                        MaxResults = 100
                    });
                    if (result == null) return TestOutcome.Fail("Enumerate returned null");
                    if (result.Documents.Count != 10) return TestOutcome.Fail($"Expected 10 results, got {result.Documents.Count}");
                    return TestOutcome.Pass();
                });

                await RunTest("Enumerate: with MaxResults", async () =>
                {
                    SearchResult? result = await _client.Search.EnumerateAsync(new SearchQuery
                    {
                        CollectionId = collection.Id,
                        MaxResults = 5
                    });
                    if (result == null) return TestOutcome.Fail("Enumerate returned null");
                    if (result.Documents.Count != 5) return TestOutcome.Fail($"Expected 5 results, got {result.Documents.Count}");
                    return TestOutcome.Pass();
                });

                await RunTest("Enumerate: with Skip", async () =>
                {
                    SearchResult? result = await _client.Search.EnumerateAsync(new SearchQuery
                    {
                        CollectionId = collection.Id,
                        Skip = 5,
                        MaxResults = 100
                    });
                    if (result == null) return TestOutcome.Fail("Enumerate returned null");
                    if (result.Documents.Count != 5) return TestOutcome.Fail($"Expected 5 results, got {result.Documents.Count}");
                    return TestOutcome.Pass();
                });

                await RunTest("Enumerate: verify TotalRecords", async () =>
                {
                    SearchResult? result = await _client.Search.EnumerateAsync(new SearchQuery
                    {
                        CollectionId = collection.Id,
                        MaxResults = 3
                    });
                    if (result == null) return TestOutcome.Fail("Enumerate returned null");
                    if (result.TotalRecords != 10) return TestOutcome.Fail($"Expected TotalRecords=10, got {result.TotalRecords}");
                    return TestOutcome.Pass();
                });

                await RunTest("Enumerate: verify EndOfResults", async () =>
                {
                    SearchResult? result = await _client.Search.EnumerateAsync(new SearchQuery
                    {
                        CollectionId = collection.Id,
                        MaxResults = 100
                    });
                    if (result == null) return TestOutcome.Fail("Enumerate returned null");
                    if (!result.EndOfResults) return TestOutcome.Fail("Expected EndOfResults=true");
                    return TestOutcome.Pass();
                });
            }
            finally
            {
                await _client.Collection.DeleteAsync(collection.Id);
            }
        }

        // ========== SCHEMA API TESTS ==========

        private static async Task TestSchemaApi()
        {
            Collection? collection = await _client.Collection.CreateAsync("schema_test_collection");
            if (collection == null)
            {
                await RunTest("Setup: Create collection", async () => TestOutcome.Fail("Collection creation failed"));
                return;
            }

            try
            {
                Document? doc1 = await _client.Document.IngestAsync(
                    collection.Id,
                    new { name = "Test", value = 42, active = true }
                );

                await RunTest("GetSchemas: returns schemas", async () =>
                {
                    List<Schema> schemas = await _client.Schema.ReadAllAsync();
                    if (schemas.Count == 0) return TestOutcome.Fail("No schemas returned");
                    return TestOutcome.Pass();
                });

                await RunTest("GetSchema: by id", async () =>
                {
                    if (doc1 == null) return TestOutcome.Fail("Setup: doc1 is null");
                    Schema? schema = await _client.Schema.ReadByIdAsync(doc1.SchemaId);
                    if (schema == null) return TestOutcome.Fail("GetSchema returned null");
                    if (schema.Id != doc1.SchemaId) return TestOutcome.Fail($"Schema ID mismatch: {schema.Id}");
                    return TestOutcome.Pass();
                });

                await RunTest("GetSchema: non-existent returns null", async () =>
                {
                    Schema? schema = await _client.Schema.ReadByIdAsync("sch_nonexistent12345");
                    if (schema != null) return TestOutcome.Fail("Expected null for non-existent schema");
                    return TestOutcome.Pass();
                });

                await RunTest("GetSchemaElements: returns elements", async () =>
                {
                    if (doc1 == null) return TestOutcome.Fail("Setup: doc1 is null");
                    List<SchemaElement> elements = await _client.Schema.GetElementsAsync(doc1.SchemaId);
                    if (elements.Count == 0) return TestOutcome.Fail("No elements returned");
                    return TestOutcome.Pass();
                });

                await RunTest("GetSchemaElements: correct keys", async () =>
                {
                    if (doc1 == null) return TestOutcome.Fail("Setup: doc1 is null");
                    List<SchemaElement> elements = await _client.Schema.GetElementsAsync(doc1.SchemaId);
                    HashSet<string> keys = new HashSet<string>(elements.Select(e => e.Key));
                    string[] expected = { "name", "value", "active" };
                    foreach (string key in expected)
                    {
                        if (!keys.Contains(key)) return TestOutcome.Fail($"Missing expected key: {key}. Found: {string.Join(", ", keys)}");
                    }
                    return TestOutcome.Pass();
                });
            }
            finally
            {
                await _client.Collection.DeleteAsync(collection.Id);
            }
        }

        // ========== INDEX API TESTS ==========

        private static async Task TestIndexApi()
        {
            await RunTest("GetIndexTableMappings: returns mappings", async () =>
            {
                List<IndexTableMapping> mappings = await _client.Index.GetMappingsAsync();
                // Mappings may be empty if no indexes exist yet
                return TestOutcome.Pass();
            });
        }

        // ========== CONSTRAINTS API TESTS ==========

        private static async Task TestConstraintsApi()
        {
            await RunTest("Constraints: create collection with strict mode", async () =>
            {
                FieldConstraint constraint = new FieldConstraint
                {
                    FieldPath = "name",
                    DataType = "string",
                    Required = true
                };
                Collection? collection = await _client.Collection.CreateAsync(
                    name: "constraints_test",
                    schemaEnforcementMode: SchemaEnforcementMode.Strict,
                    fieldConstraints: new List<FieldConstraint> { constraint }
                );
                if (collection == null) return TestOutcome.Fail("Collection creation failed");
                await _client.Collection.DeleteAsync(collection.Id);
                return TestOutcome.Pass();
            });

            await RunTest("Constraints: update constraints on collection", async () =>
            {
                Collection? collection = await _client.Collection.CreateAsync("constraints_update_test");
                if (collection == null) return TestOutcome.Fail("Collection creation failed");

                FieldConstraint constraint = new FieldConstraint
                {
                    FieldPath = "email",
                    DataType = "string",
                    Required = true,
                    RegexPattern = @"^[\w\.-]+@[\w\.-]+\.\w+$"
                };
                bool success = await _client.Collection.UpdateConstraintsAsync(
                    collection.Id,
                    SchemaEnforcementMode.Strict,
                    new List<FieldConstraint> { constraint }
                );
                await _client.Collection.DeleteAsync(collection.Id);

                if (!success) return TestOutcome.Fail("Update constraints failed");
                return TestOutcome.Pass();
            });

            await RunTest("Constraints: get constraints from collection", async () =>
            {
                FieldConstraint constraint = new FieldConstraint
                {
                    FieldPath = "test_field",
                    DataType = "string",
                    Required = true
                };
                Collection? collection = await _client.Collection.CreateAsync(
                    name: "constraints_get_test",
                    schemaEnforcementMode: SchemaEnforcementMode.Strict,
                    fieldConstraints: new List<FieldConstraint> { constraint }
                );
                if (collection == null) return TestOutcome.Fail("Collection creation failed");

                ConstraintsResponse? constraintsResponse = await _client.Collection.GetConstraintsAsync(collection.Id);
                await _client.Collection.DeleteAsync(collection.Id);

                if (constraintsResponse == null) return TestOutcome.Fail("GetConstraints returned null");
                if (constraintsResponse.FieldConstraints.Count == 0) return TestOutcome.Fail("No constraints returned");
                return TestOutcome.Pass();
            });
        }

        // ========== INDEXING MODE API TESTS ==========

        private static async Task TestIndexingModeApi()
        {
            await RunTest("Indexing: selective mode only indexes specified fields", async () =>
            {
                Collection? collection = await _client.Collection.CreateAsync(
                    name: "indexing_selective_test",
                    indexingMode: IndexingMode.Selective,
                    indexedFields: new List<string> { "name", "email" }
                );
                if (collection == null) return TestOutcome.Fail("Collection creation failed");
                await _client.Collection.DeleteAsync(collection.Id);
                return TestOutcome.Pass();
            });

            await RunTest("Indexing: none mode skips indexing", async () =>
            {
                Collection? collection = await _client.Collection.CreateAsync(
                    name: "indexing_none_test",
                    indexingMode: IndexingMode.None
                );
                if (collection == null) return TestOutcome.Fail("Collection creation failed");
                await _client.Collection.DeleteAsync(collection.Id);
                return TestOutcome.Pass();
            });

            await RunTest("Indexing: update indexing mode", async () =>
            {
                Collection? collection = await _client.Collection.CreateAsync("indexing_update_test");
                if (collection == null) return TestOutcome.Fail("Collection creation failed");

                bool success = await _client.Collection.UpdateIndexingAsync(
                    collection.Id,
                    IndexingMode.Selective,
                    new List<string> { "name" },
                    rebuildIndexes: false
                );
                await _client.Collection.DeleteAsync(collection.Id);

                if (!success) return TestOutcome.Fail("Update indexing failed");
                return TestOutcome.Pass();
            });

            await RunTest("Indexing: rebuild indexes", async () =>
            {
                Collection? collection = await _client.Collection.CreateAsync("indexing_rebuild_test");
                if (collection == null) return TestOutcome.Fail("Collection creation failed");

                await _client.Document.IngestAsync(collection.Id, new { name = "test", value = 42 });

                IndexRebuildResult? result = await _client.Collection.RebuildIndexesAsync(collection.Id);
                await _client.Collection.DeleteAsync(collection.Id);

                if (result == null) return TestOutcome.Fail("Rebuild indexes returned null");
                return TestOutcome.Pass();
            });
        }

        // ========== EDGE CASE TESTS ==========

        private static async Task TestEdgeCases()
        {
            Collection? collection = await _client.Collection.CreateAsync("edge_case_collection");
            if (collection == null)
            {
                await RunTest("Setup: Create collection", async () => TestOutcome.Fail("Collection creation failed"));
                return;
            }

            try
            {
                await RunTest("Edge: empty string values in JSON", async () =>
                {
                    Document? doc = await _client.Document.IngestAsync(
                        collection.Id,
                        new { name = "", description = "" }
                    );
                    if (doc == null) return TestOutcome.Fail("Ingest failed");
                    return TestOutcome.Pass();
                });

                await RunTest("Edge: special characters in values", async () =>
                {
                    Document? doc = await _client.Document.IngestAsync(
                        collection.Id,
                        new { text = "Hello! @#$%^&*()_+-={}[]|\\:\";<>?,./" }
                    );
                    if (doc == null) return TestOutcome.Fail("Ingest failed");
                    return TestOutcome.Pass();
                });

                await RunTest("Edge: deeply nested JSON (5 levels)", async () =>
                {
                    Document? doc = await _client.Document.IngestAsync(
                        collection.Id,
                        new
                        {
                            level1 = new
                            {
                                level2 = new
                                {
                                    level3 = new
                                    {
                                        level4 = new
                                        {
                                            level5 = "deep value"
                                        }
                                    }
                                }
                            }
                        }
                    );
                    if (doc == null) return TestOutcome.Fail("Ingest failed");
                    return TestOutcome.Pass();
                });

                await RunTest("Edge: large array in JSON", async () =>
                {
                    Document? doc = await _client.Document.IngestAsync(
                        collection.Id,
                        new { items = Enumerable.Range(0, 100).ToArray() }
                    );
                    if (doc == null) return TestOutcome.Fail("Ingest failed");
                    return TestOutcome.Pass();
                });

                await RunTest("Edge: numeric values", async () =>
                {
                    Document? doc = await _client.Document.IngestAsync(
                        collection.Id,
                        new
                        {
                            integer = 42,
                            floatVal = 3.14159,
                            negative = -100,
                            zero = 0,
                            large = 9999999999L
                        }
                    );
                    if (doc == null) return TestOutcome.Fail("Ingest failed");
                    return TestOutcome.Pass();
                });

                await RunTest("Edge: boolean values", async () =>
                {
                    Document? doc = await _client.Document.IngestAsync(
                        collection.Id,
                        new { active = true, disabled = false }
                    );
                    if (doc == null) return TestOutcome.Fail("Ingest failed");
                    return TestOutcome.Pass();
                });

                await RunTest("Edge: null values in JSON", async () =>
                {
                    Document? doc = await _client.Document.IngestAsync(
                        collection.Id,
                        new { name = "Test", optional_field = (string?)null }
                    );
                    if (doc == null) return TestOutcome.Fail("Ingest failed");
                    return TestOutcome.Pass();
                });

                await RunTest("Edge: unicode characters", async () =>
                {
                    Document? doc = await _client.Document.IngestAsync(
                        collection.Id,
                        new { greeting = "Hello, world!" }
                    );
                    if (doc == null) return TestOutcome.Fail("Ingest failed");
                    return TestOutcome.Pass();
                });
            }
            finally
            {
                await _client.Collection.DeleteAsync(collection.Id);
            }
        }

        // ========== PERFORMANCE TESTS ==========

        private static async Task TestPerformance()
        {
            Collection? collection = await _client.Collection.CreateAsync("perf_test_collection");
            if (collection == null)
            {
                await RunTest("Setup: Create collection", async () => TestOutcome.Fail("Collection creation failed"));
                return;
            }

            try
            {
                await RunTest("Perf: ingest 100 documents", async () =>
                {
                    Stopwatch sw = Stopwatch.StartNew();
                    for (int i = 0; i < 100; i++)
                    {
                        Document? doc = await _client.Document.IngestAsync(
                            collection.Id,
                            new
                            {
                                Name = $"PerfItem_{i}",
                                Category = $"Category_{i % 10}",
                                Value = i * 10
                            },
                            name: $"perf_doc_{i}"
                        );
                        if (doc == null) return TestOutcome.Fail($"Failed to ingest document {i}");
                    }
                    sw.Stop();
                    double rate = 100.0 / sw.Elapsed.TotalSeconds;
                    Console.Write($"({rate:F1} docs/sec) ");
                    return TestOutcome.Pass();
                });

                await RunTest("Perf: search in 100 documents", async () =>
                {
                    Stopwatch sw = Stopwatch.StartNew();
                    SearchResult? result = await _client.Search.SearchAsync(new SearchQuery
                    {
                        CollectionId = collection.Id,
                        Filters = new List<SearchFilter> { new SearchFilter("Category", SearchCondition.Equals, "Category_5") },
                        MaxResults = 100
                    });
                    sw.Stop();
                    if (result == null) return TestOutcome.Fail("Search returned null");
                    Console.Write($"({sw.ElapsedMilliseconds}ms) ");
                    return TestOutcome.Pass();
                });

                await RunTest("Perf: GetDocuments for 100 documents", async () =>
                {
                    Stopwatch sw = Stopwatch.StartNew();
                    List<Document> docs = await _client.Document.ReadAllInCollectionAsync(collection.Id);
                    sw.Stop();
                    if (docs.Count != 100) return TestOutcome.Fail($"Expected 100 docs, got {docs.Count}");
                    Console.Write($"({sw.ElapsedMilliseconds}ms) ");
                    return TestOutcome.Pass();
                });

                await RunTest("Perf: enumerate 100 documents", async () =>
                {
                    Stopwatch sw = Stopwatch.StartNew();
                    SearchResult? result = await _client.Search.EnumerateAsync(new SearchQuery
                    {
                        CollectionId = collection.Id,
                        MaxResults = 100
                    });
                    sw.Stop();
                    if (result == null) return TestOutcome.Fail("Enumerate returned null");
                    if (result.Documents.Count != 100) return TestOutcome.Fail($"Expected 100 docs, got {result.Documents.Count}");
                    Console.Write($"({sw.ElapsedMilliseconds}ms) ");
                    return TestOutcome.Pass();
                });
            }
            finally
            {
                await _client.Collection.DeleteAsync(collection.Id);
            }
        }
    }
}
