using System.Diagnostics;
using System.Text.Json;
using Lattice.Core;
using Lattice.Core.Models;
using Lattice.Core.Search;

namespace Test.Throughput;

/// <summary>
/// Throughput test suite for Lattice SDK.
/// Tests ingestion of large numbers of objects and performs retrievals, searches, and enumerations
/// under various conditions with response property validation.
///
/// Uses shared test fixtures to minimize redundant data ingestion.
/// </summary>
public class Program
{
    private static readonly List<TestResult> _results = new List<TestResult>();
    private static int _testDirCounter = 0;
    private static readonly object _dirLock = new object();
    private static DatabaseSettings _databaseSettings = null;

    /// <summary>
    /// Represents the outcome of a test method.
    /// </summary>
    private struct TestOutcome
    {
        public bool Success;
        public string Error;

        public static TestOutcome Pass() => new TestOutcome { Success = true, Error = null };
        public static TestOutcome Fail(string error) => new TestOutcome { Success = false, Error = error };
    }

    // Document count tiers to test
    private static readonly int[] _tiers = { 100, 250, 500, 1000, 5000 };

    public static async Task<int> Main(string[] args)
    {
        // Parse command-line arguments
        if (!ParseArguments(args, out string parseError))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: {parseError}");
            Console.ResetColor();
            Console.WriteLine();
            PrintUsage();
            return 1;
        }

        // Validate database connection
        if (!await ValidateDatabaseConnection())
        {
            return 1;
        }

        Console.WriteLine("========================================");
        Console.WriteLine("  LATTICE THROUGHPUT TEST SUITE");
        Console.WriteLine("========================================");
        Console.WriteLine();
        PrintDatabaseInfo();

        Stopwatch overallStopwatch = Stopwatch.StartNew();

        // Run tiered tests with shared fixtures
        foreach (int docCount in _tiers)
        {
            await RunTieredTests(docCount);
        }

        // Run standalone tests that need special setup
        await RunStandaloneTests();

        overallStopwatch.Stop();

        // Print summary
        PrintSummary(overallStopwatch.Elapsed);

        // Return exit code based on overall result
        int failedCount = _results.Count(r => !r.Passed);
        return failedCount > 0 ? 1 : 0;
    }

    #region Tiered Tests with Shared Fixtures

    private static async Task RunTieredTests(int docCount)
    {
        Console.WriteLine();
        Console.WriteLine($"========== TIER: {docCount} DOCUMENTS ==========");

        string testDir = CreateTestDir();
        try
        {
            using LatticeClient client = CreateClient(testDir);
            Collection collection = await client.Collection.Create($"TierTest_{docCount}");
            if (collection == null)
            {
                Console.WriteLine($"  ERROR: Failed to create collection for tier {docCount}");
                return;
            }

            // Shared ingestion for this tier
            Console.WriteLine();
            Console.WriteLine($"--- INGESTION ({docCount} docs) ---");
            Console.WriteLine();

            List<Document> ingestedDocs = new List<Document>();
            await RunTest($"TIER-{docCount}", $"Ingest {docCount} documents", async () =>
            {
                Stopwatch sw = Stopwatch.StartNew();
                for (int i = 0; i < docCount; i++)
                {
                    List<string> labels = new List<string> { $"group_{i % 10}", $"batch_{i / 100}" };
                    if (i % 50 == 0) labels.Add("special");

                    Dictionary<string, string> tags = new Dictionary<string, string>
                    {
                        ["priority"] = (i % 5).ToString(),
                        ["category"] = $"cat_{i % 10}"
                    };

                    Document doc = await client.Document.Ingest(
                        collection.Id,
                        GenerateJsonDocument(i),
                        name: $"Doc_{i}",
                        labels: labels,
                        tags: tags);

                    if (doc == null) return TestOutcome.Fail($"Failed to ingest document {i}");
                    ingestedDocs.Add(doc);
                }
                sw.Stop();

                double docsPerSecond = docCount / sw.Elapsed.TotalSeconds;
                Console.Write($"({docsPerSecond:F1} docs/sec) ");

                return TestOutcome.Pass();
            });

            if (ingestedDocs.Count != docCount)
            {
                Console.WriteLine($"  ERROR: Ingestion incomplete, skipping remaining tests for tier {docCount}");
                return;
            }

            // Validate ingested documents
            await RunTest($"TIER-{docCount}", "Validate ingested properties", async () =>
            {
                // Sample validation - check every 10th document
                for (int i = 0; i < docCount; i += Math.Max(1, docCount / 10))
                {
                    Document doc = ingestedDocs[i];
                    if (string.IsNullOrEmpty(doc.Id) || !doc.Id.StartsWith("doc_"))
                        return TestOutcome.Fail($"Doc {i}: Invalid Id");
                    if (doc.CollectionId != collection.Id)
                        return TestOutcome.Fail($"Doc {i}: CollectionId mismatch");
                    if (doc.Name != $"Doc_{i}")
                        return TestOutcome.Fail($"Doc {i}: Name mismatch");
                    if (string.IsNullOrEmpty(doc.SchemaId))
                        return TestOutcome.Fail($"Doc {i}: SchemaId empty");
                    if (doc.CreatedUtc == default)
                        return TestOutcome.Fail($"Doc {i}: CreatedUtc not set");
                }
                return TestOutcome.Pass();
            });

            // Run retrieval tests against shared data
            Console.WriteLine();
            Console.WriteLine($"--- RETRIEVAL ({docCount} docs) ---");
            Console.WriteLine();

            await RunRetrievalTests(client, collection, ingestedDocs, docCount);

            // Run search tests against shared data
            Console.WriteLine();
            Console.WriteLine($"--- SEARCH ({docCount} docs) ---");
            Console.WriteLine();

            await RunSearchTests(client, collection, docCount);

            // Run enumeration tests against shared data
            Console.WriteLine();
            Console.WriteLine($"--- ENUMERATION ({docCount} docs) ---");
            Console.WriteLine();

            await RunEnumerationTests(client, collection, docCount);
        }
        finally
        {
            CleanupTestDir(testDir);
        }
    }

    private static async Task RunRetrievalTests(LatticeClient client, Collection collection, List<Document> docs, int docCount)
    {
        int sampleSize = Math.Min(100, docCount);

        // GetDocument with content
        await RunTest($"TIER-{docCount}", $"GetDocument {sampleSize}x (with content)", async () =>
        {
            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < sampleSize; i++)
            {
                Document doc = await client.Document.ReadById(docs[i].Id, includeContent: true);
                if (doc == null) return TestOutcome.Fail($"GetDocument returned null for {docs[i].Id}");
                if (string.IsNullOrEmpty(doc.Content)) return TestOutcome.Fail($"Content empty for doc {i}");
            }
            sw.Stop();
            double opsPerSecond = sampleSize / sw.Elapsed.TotalSeconds;
            Console.Write($"({opsPerSecond:F1} ops/sec) ");
            return TestOutcome.Pass();
        });

        // GetDocument without content
        await RunTest($"TIER-{docCount}", $"GetDocument {sampleSize}x (no content)", async () =>
        {
            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < sampleSize; i++)
            {
                Document doc = await client.Document.ReadById(docs[i].Id, includeContent: false);
                if (doc == null) return TestOutcome.Fail($"GetDocument returned null for {docs[i].Id}");
            }
            sw.Stop();
            double opsPerSecond = sampleSize / sw.Elapsed.TotalSeconds;
            Console.Write($"({opsPerSecond:F1} ops/sec) ");
            return TestOutcome.Pass();
        });

        // GetDocuments (bulk)
        await RunTest($"TIER-{docCount}", "GetDocuments (bulk)", async () =>
        {
            Stopwatch sw = Stopwatch.StartNew();
            List<Document> allDocs = await client.Document.ReadAllInCollection(collection.Id);
            sw.Stop();

            if (allDocs == null) return TestOutcome.Fail("GetDocuments returned null");
            if (allDocs.Count != docCount) return TestOutcome.Fail($"Expected {docCount}, got {allDocs.Count}");

            Console.Write($"({sw.Elapsed.TotalMilliseconds:F1}ms) ");
            return TestOutcome.Pass();
        });

        // DocumentExists
        await RunTest($"TIER-{docCount}", $"DocumentExists {sampleSize}x", async () =>
        {
            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < sampleSize; i++)
            {
                bool exists = await client.Document.Exists(docs[i].Id);
                if (!exists) return TestOutcome.Fail($"DocumentExists returned false for {docs[i].Id}");
            }
            sw.Stop();
            double opsPerSecond = sampleSize / sw.Elapsed.TotalSeconds;
            Console.Write($"({opsPerSecond:F1} ops/sec) ");
            return TestOutcome.Pass();
        });

        // Validate retrieved document properties
        await RunTest($"TIER-{docCount}", "Validate retrieved properties", async () =>
        {
            Document doc = await client.Document.ReadById(docs[0].Id, includeContent: true);
            if (doc == null) return TestOutcome.Fail("GetDocument returned null");

            if (doc.Id != docs[0].Id) return TestOutcome.Fail("Id mismatch");
            if (doc.CollectionId != collection.Id) return TestOutcome.Fail("CollectionId mismatch");
            if (doc.Name != "Doc_0") return TestOutcome.Fail($"Name mismatch: {doc.Name}");
            if (string.IsNullOrEmpty(doc.SchemaId)) return TestOutcome.Fail("SchemaId empty");
            if (string.IsNullOrEmpty(doc.Content)) return TestOutcome.Fail("Content empty");
            if (doc.CreatedUtc == default) return TestOutcome.Fail("CreatedUtc not set");
            if (doc.LastUpdateUtc == default) return TestOutcome.Fail("LastUpdateUtc not set");

            return TestOutcome.Pass();
        });

        // GetDocument WITHOUT labels/tags (performance comparison)
        await RunTest($"TIER-{docCount}", $"GetDocument {sampleSize}x (no labels/tags)", async () =>
        {
            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < sampleSize; i++)
            {
                Document doc = await client.Document.ReadById(docs[i].Id, includeContent: false, includeLabels: false, includeTags: false);
                if (doc == null) return TestOutcome.Fail($"GetDocument returned null for {docs[i].Id}");
            }
            sw.Stop();
            double opsPerSecond = sampleSize / sw.Elapsed.TotalSeconds;
            Console.Write($"({opsPerSecond:F1} ops/sec) ");
            return TestOutcome.Pass();
        });

        // Validate labels/tags are excluded when not requested
        await RunTest($"TIER-{docCount}", "Validate labels/tags exclusion", async () =>
        {
            // Get document with labels and tags
            Document docWithAll = await client.Document.ReadById(docs[0].Id, includeContent: false, includeLabels: true, includeTags: true);
            if (docWithAll == null) return TestOutcome.Fail("GetDocument returned null");
            if (docWithAll.Labels.Count == 0) return TestOutcome.Fail("Expected labels when includeLabels=true");
            if (docWithAll.Tags.Count == 0) return TestOutcome.Fail("Expected tags when includeTags=true");

            // Get document without labels and tags
            Document docWithoutLabels = await client.Document.ReadById(docs[0].Id, includeContent: false, includeLabels: false, includeTags: true);
            if (docWithoutLabels == null) return TestOutcome.Fail("GetDocument returned null");
            if (docWithoutLabels.Labels.Count != 0) return TestOutcome.Fail("Expected no labels when includeLabels=false");
            if (docWithoutLabels.Tags.Count == 0) return TestOutcome.Fail("Expected tags when includeTags=true");

            Document docWithoutTags = await client.Document.ReadById(docs[0].Id, includeContent: false, includeLabels: true, includeTags: false);
            if (docWithoutTags == null) return TestOutcome.Fail("GetDocument returned null");
            if (docWithoutTags.Labels.Count == 0) return TestOutcome.Fail("Expected labels when includeLabels=true");
            if (docWithoutTags.Tags.Count != 0) return TestOutcome.Fail("Expected no tags when includeTags=false");

            Document docMinimal = await client.Document.ReadById(docs[0].Id, includeContent: false, includeLabels: false, includeTags: false);
            if (docMinimal == null) return TestOutcome.Fail("GetDocument returned null");
            if (docMinimal.Labels.Count != 0) return TestOutcome.Fail("Expected no labels when includeLabels=false");
            if (docMinimal.Tags.Count != 0) return TestOutcome.Fail("Expected no tags when includeTags=false");

            return TestOutcome.Pass();
        });

        // GetDocuments bulk WITHOUT labels/tags (performance comparison)
        await RunTest($"TIER-{docCount}", "GetDocuments bulk (no labels/tags)", async () =>
        {
            Stopwatch sw = Stopwatch.StartNew();
            List<Document> allDocs = await client.Document.ReadAllInCollection(collection.Id, includeLabels: false, includeTags: false);
            sw.Stop();

            if (allDocs == null) return TestOutcome.Fail("GetDocuments returned null");
            if (allDocs.Count != docCount) return TestOutcome.Fail($"Expected {docCount}, got {allDocs.Count}");

            // Verify labels/tags are empty
            foreach (Document doc in allDocs)
            {
                if (doc.Labels.Count != 0) return TestOutcome.Fail("Expected no labels");
                if (doc.Tags.Count != 0) return TestOutcome.Fail("Expected no tags");
            }

            Console.Write($"({sw.Elapsed.TotalMilliseconds:F1}ms) ");
            return TestOutcome.Pass();
        });
    }

    private static async Task RunSearchTests(LatticeClient client, Collection collection, int docCount)
    {
        int expectedCategory5Count = docCount / 10; // Category_5 matches i%10==5

        // Search with Equals
        await RunTest($"TIER-{docCount}", "Search (Equals filter)", async () =>
        {
            Stopwatch sw = Stopwatch.StartNew();
            SearchResult result = await client.Search.Search(new SearchQuery
            {
                CollectionId = collection.Id,
                Filters = new List<SearchFilter>
                {
                    new SearchFilter("Category", SearchConditionEnum.Equals, "Category_5")
                },
                MaxResults = docCount
            });
            sw.Stop();

            if (result == null) return TestOutcome.Fail("Search returned null");
            if (!result.Success) return TestOutcome.Fail("Search not successful");
            if (result.Documents == null) return TestOutcome.Fail("Documents null");
            if (result.Documents.Count != expectedCategory5Count)
                return TestOutcome.Fail($"Expected {expectedCategory5Count}, got {result.Documents.Count}");

            Console.Write($"({sw.Elapsed.TotalMilliseconds:F1}ms) ");
            return TestOutcome.Pass();
        });

        // Search with Contains
        await RunTest($"TIER-{docCount}", "Search (Contains filter)", async () =>
        {
            Stopwatch sw = Stopwatch.StartNew();
            SearchResult result = await client.Search.Search(new SearchQuery
            {
                CollectionId = collection.Id,
                Filters = new List<SearchFilter>
                {
                    new SearchFilter("Name", SearchConditionEnum.Contains, "Item_1")
                },
                MaxResults = docCount
            });
            sw.Stop();

            if (result == null) return TestOutcome.Fail("Search returned null");
            if (!result.Success) return TestOutcome.Fail("Search not successful");
            // "Item_1" matches 1, 10-19, 100-199, 1000-1999, etc.

            Console.Write($"({sw.Elapsed.TotalMilliseconds:F1}ms, {result.Documents?.Count} results) ");
            return TestOutcome.Pass();
        });

        // Search with GreaterThan
        int thresholdIndex = (int)(docCount * 0.9); // Top 10%
        await RunTest($"TIER-{docCount}", "Search (GreaterThan filter)", async () =>
        {
            Stopwatch sw = Stopwatch.StartNew();
            SearchResult result = await client.Search.Search(new SearchQuery
            {
                CollectionId = collection.Id,
                Filters = new List<SearchFilter>
                {
                    new SearchFilter("Id", SearchConditionEnum.GreaterThan, thresholdIndex.ToString())
                },
                MaxResults = docCount
            });
            sw.Stop();

            if (result == null) return TestOutcome.Fail("Search returned null");
            if (!result.Success) return TestOutcome.Fail("Search not successful");

            int expectedCount = docCount - thresholdIndex - 1;
            if (result.Documents!.Count != expectedCount)
                return TestOutcome.Fail($"Expected {expectedCount}, got {result.Documents.Count}");

            Console.Write($"({sw.Elapsed.TotalMilliseconds:F1}ms) ");
            return TestOutcome.Pass();
        });

        // Search with multiple filters
        await RunTest($"TIER-{docCount}", "Search (multiple filters)", async () =>
        {
            Stopwatch sw = Stopwatch.StartNew();
            SearchResult result = await client.Search.Search(new SearchQuery
            {
                CollectionId = collection.Id,
                Filters = new List<SearchFilter>
                {
                    new SearchFilter("Category", SearchConditionEnum.Equals, "Category_5"),
                    new SearchFilter("IsActive", SearchConditionEnum.Equals, "false")
                },
                MaxResults = docCount
            });
            sw.Stop();

            if (result == null) return TestOutcome.Fail("Search returned null");
            if (!result.Success) return TestOutcome.Fail("Search not successful");
            // Category_5 (i%10==5) AND IsActive=false (i%2==1) - all items ending in 5 are odd
            if (result.Documents!.Count != expectedCategory5Count)
                return TestOutcome.Fail($"Expected {expectedCategory5Count}, got {result.Documents.Count}");

            Console.Write($"({sw.Elapsed.TotalMilliseconds:F1}ms) ");
            return TestOutcome.Pass();
        });

        // Search by label
        int expectedSpecialCount = (docCount + 49) / 50; // Every 50th document has "special" label
        await RunTest($"TIER-{docCount}", "Search (by label)", async () =>
        {
            Stopwatch sw = Stopwatch.StartNew();
            SearchResult result = await client.Search.Search(new SearchQuery
            {
                CollectionId = collection.Id,
                Labels = new List<string> { "special" },
                MaxResults = docCount
            });
            sw.Stop();

            if (result == null) return TestOutcome.Fail("Search returned null");
            if (!result.Success) return TestOutcome.Fail("Search not successful");
            if (result.Documents!.Count != expectedSpecialCount)
                return TestOutcome.Fail($"Expected {expectedSpecialCount}, got {result.Documents.Count}");

            Console.Write($"({sw.Elapsed.TotalMilliseconds:F1}ms) ");
            return TestOutcome.Pass();
        });

        // Search by tag
        int expectedPriority0Count = (docCount + 4) / 5; // Every 5th document has priority=0
        await RunTest($"TIER-{docCount}", "Search (by tag)", async () =>
        {
            Stopwatch sw = Stopwatch.StartNew();
            SearchResult result = await client.Search.Search(new SearchQuery
            {
                CollectionId = collection.Id,
                Tags = new Dictionary<string, string> { ["priority"] = "0" },
                MaxResults = docCount
            });
            sw.Stop();

            if (result == null) return TestOutcome.Fail("Search returned null");
            if (!result.Success) return TestOutcome.Fail("Search not successful");
            if (result.Documents!.Count != expectedPriority0Count)
                return TestOutcome.Fail($"Expected {expectedPriority0Count}, got {result.Documents.Count}");

            Console.Write($"({sw.Elapsed.TotalMilliseconds:F1}ms) ");
            return TestOutcome.Pass();
        });

        // Search with pagination
        await RunTest($"TIER-{docCount}", "Search (pagination)", async () =>
        {
            Stopwatch sw = Stopwatch.StartNew();
            int totalRetrieved = 0;
            int skip = 0;
            int pageSize = Math.Min(100, docCount / 5);
            int iterations = 0;
            int maxIterations = (docCount / pageSize) + 2;

            while (iterations < maxIterations)
            {
                SearchResult result = await client.Search.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    MaxResults = pageSize,
                    Skip = skip
                });

                if (result == null) return TestOutcome.Fail("Search returned null");
                if (!result.Success) return TestOutcome.Fail("Search not successful");

                totalRetrieved += result.Documents!.Count;
                iterations++;

                if (result.EndOfResults || result.Documents.Count == 0)
                    break;

                skip += pageSize;
            }
            sw.Stop();

            if (totalRetrieved != docCount)
                return TestOutcome.Fail($"Expected {docCount}, got {totalRetrieved}");

            Console.Write($"({iterations} pages, {sw.Elapsed.TotalMilliseconds:F1}ms) ");
            return TestOutcome.Pass();
        });

        // Repeated searches (query throughput)
        int queryCount = Math.Min(50, docCount / 2);
        await RunTest($"TIER-{docCount}", $"Search throughput ({queryCount} queries)", async () =>
        {
            Stopwatch sw = Stopwatch.StartNew();
            for (int q = 0; q < queryCount; q++)
            {
                SearchResult result = await client.Search.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Filters = new List<SearchFilter>
                    {
                        new SearchFilter("Category", SearchConditionEnum.Equals, $"Category_{q % 10}")
                    },
                    MaxResults = 100
                });
                if (result == null || !result.Success)
                    return TestOutcome.Fail($"Query {q} failed");
            }
            sw.Stop();

            double queriesPerSecond = queryCount / sw.Elapsed.TotalSeconds;
            Console.Write($"({queriesPerSecond:F1} queries/sec) ");
            return TestOutcome.Pass();
        });

        // SearchBySql
        await RunTest($"TIER-{docCount}", "SearchBySql", async () =>
        {
            int limit = Math.Min(50, docCount);
            Stopwatch sw = Stopwatch.StartNew();
            SearchResult result = await client.Search.SearchBySql(collection.Id, $"SELECT * FROM documents LIMIT {limit}");
            sw.Stop();

            if (result == null) return TestOutcome.Fail("SearchBySql returned null");
            if (!result.Success) return TestOutcome.Fail("Search not successful");
            if (result.Documents!.Count != limit)
                return TestOutcome.Fail($"Expected {limit}, got {result.Documents.Count}");

            Console.Write($"({sw.Elapsed.TotalMilliseconds:F1}ms) ");
            return TestOutcome.Pass();
        });

        // Validate search result properties
        await RunTest($"TIER-{docCount}", "Validate search result properties", async () =>
        {
            SearchResult result = await client.Search.Search(new SearchQuery
            {
                CollectionId = collection.Id,
                MaxResults = 10,
                Skip = 5,
                IncludeContent = true
            });

            if (result == null) return TestOutcome.Fail("Search returned null");
            if (!result.Success) return TestOutcome.Fail("Success should be true");
            if (result.Timestamp == null) return TestOutcome.Fail("Timestamp null");
            if (result.TotalRecords != docCount)
                return TestOutcome.Fail($"TotalRecords should be {docCount}, got {result.TotalRecords}");
            if (result.Documents == null) return TestOutcome.Fail("Documents null");
            if (result.Documents.Count != 10)
                return TestOutcome.Fail($"Expected 10 docs, got {result.Documents.Count}");

            foreach (Document doc in result.Documents)
            {
                if (string.IsNullOrEmpty(doc.Id)) return TestOutcome.Fail("Document Id empty");
                if (doc.CollectionId != collection.Id) return TestOutcome.Fail("CollectionId mismatch");
                if (string.IsNullOrEmpty(doc.Content)) return TestOutcome.Fail("Content should be included");
            }

            return TestOutcome.Pass();
        });

        // Search WITHOUT labels/tags (performance comparison)
        await RunTest($"TIER-{docCount}", "Search (no labels/tags)", async () =>
        {
            Stopwatch sw = Stopwatch.StartNew();
            SearchResult result = await client.Search.Search(new SearchQuery
            {
                CollectionId = collection.Id,
                MaxResults = Math.Min(100, docCount),
                IncludeLabels = false,
                IncludeTags = false
            });
            sw.Stop();

            if (result == null) return TestOutcome.Fail("Search returned null");
            if (!result.Success) return TestOutcome.Fail("Search not successful");

            // Verify labels/tags are empty
            foreach (Document doc in result.Documents!)
            {
                if (doc.Labels.Count != 0) return TestOutcome.Fail("Expected no labels");
                if (doc.Tags.Count != 0) return TestOutcome.Fail("Expected no tags");
            }

            Console.Write($"({sw.Elapsed.TotalMilliseconds:F1}ms) ");
            return TestOutcome.Pass();
        });

        // Search WITH labels/tags (for comparison)
        await RunTest($"TIER-{docCount}", "Search (with labels/tags)", async () =>
        {
            Stopwatch sw = Stopwatch.StartNew();
            SearchResult result = await client.Search.Search(new SearchQuery
            {
                CollectionId = collection.Id,
                MaxResults = Math.Min(100, docCount),
                IncludeLabels = true,
                IncludeTags = true
            });
            sw.Stop();

            if (result == null) return TestOutcome.Fail("Search returned null");
            if (!result.Success) return TestOutcome.Fail("Search not successful");

            // Verify at least some documents have labels/tags
            bool foundLabels = result.Documents!.Any(d => d.Labels.Count > 0);
            bool foundTags = result.Documents!.Any(d => d.Tags.Count > 0);
            if (!foundLabels) return TestOutcome.Fail("Expected some documents to have labels");
            if (!foundTags) return TestOutcome.Fail("Expected some documents to have tags");

            Console.Write($"({sw.Elapsed.TotalMilliseconds:F1}ms) ");
            return TestOutcome.Pass();
        });
    }

    private static async Task RunEnumerationTests(LatticeClient client, Collection collection, int docCount)
    {
        // Basic enumeration (MaxResults capped at 1000 by API)
        int basicMaxResults = Math.Min(1000, docCount);
        await RunTest($"TIER-{docCount}", $"Enumerate (basic, max {basicMaxResults})", async () =>
        {
            Stopwatch sw = Stopwatch.StartNew();
            EnumerationResult<Document> result = await client.Search.Enumerate(new EnumerationQuery
            {
                CollectionId = collection.Id,
                MaxResults = basicMaxResults
            });
            sw.Stop();

            if (result == null) return TestOutcome.Fail("Enumerate returned null");
            if (result.Objects == null) return TestOutcome.Fail("Objects null");
            if (result.Objects.Count != basicMaxResults)
                return TestOutcome.Fail($"Expected {basicMaxResults}, got {result.Objects.Count}");
            if (result.TotalRecords != docCount)
                return TestOutcome.Fail($"TotalRecords should be {docCount}, got {result.TotalRecords}");

            Console.Write($"({sw.Elapsed.TotalMilliseconds:F1}ms) ");
            return TestOutcome.Pass();
        });

        // Enumeration with pagination
        await RunTest($"TIER-{docCount}", "Enumerate (pagination)", async () =>
        {
            Stopwatch sw = Stopwatch.StartNew();
            int totalRetrieved = 0;
            int skip = 0;
            int pageSize = Math.Min(100, docCount / 5);
            int iterations = 0;
            int maxIterations = (docCount / pageSize) + 2;

            while (iterations < maxIterations)
            {
                EnumerationResult<Document> result = await client.Search.Enumerate(new EnumerationQuery
                {
                    CollectionId = collection.Id,
                    MaxResults = pageSize,
                    Skip = skip
                });

                if (result == null) return TestOutcome.Fail("Enumerate returned null");
                if (result.Objects == null) return TestOutcome.Fail("Objects null");

                totalRetrieved += result.Objects.Count;
                iterations++;

                if (result.EndOfResults || result.Objects.Count == 0)
                    break;

                skip += pageSize;
            }
            sw.Stop();

            if (totalRetrieved != docCount)
                return TestOutcome.Fail($"Expected {docCount}, got {totalRetrieved}");

            Console.Write($"({iterations} pages, {sw.Elapsed.TotalMilliseconds:F1}ms) ");
            return TestOutcome.Pass();
        });

        // Enumeration with ascending order
        await RunTest($"TIER-{docCount}", "Enumerate (CreatedAscending)", async () =>
        {
            Stopwatch sw = Stopwatch.StartNew();
            EnumerationResult<Document> result = await client.Search.Enumerate(new EnumerationQuery
            {
                CollectionId = collection.Id,
                MaxResults = 100,
                Ordering = EnumerationOrderEnum.CreatedAscending
            });
            sw.Stop();

            if (result == null) return TestOutcome.Fail("Enumerate returned null");
            if (result.Objects == null) return TestOutcome.Fail("Objects null");

            // Verify ordering
            for (int i = 1; i < result.Objects.Count; i++)
            {
                if (result.Objects[i].CreatedUtc < result.Objects[i - 1].CreatedUtc)
                    return TestOutcome.Fail($"Not in ascending order at index {i}");
            }

            Console.Write($"({sw.Elapsed.TotalMilliseconds:F1}ms) ");
            return TestOutcome.Pass();
        });

        // Enumeration with descending order
        await RunTest($"TIER-{docCount}", "Enumerate (CreatedDescending)", async () =>
        {
            Stopwatch sw = Stopwatch.StartNew();
            EnumerationResult<Document> result = await client.Search.Enumerate(new EnumerationQuery
            {
                CollectionId = collection.Id,
                MaxResults = 100,
                Ordering = EnumerationOrderEnum.CreatedDescending
            });
            sw.Stop();

            if (result == null) return TestOutcome.Fail("Enumerate returned null");
            if (result.Objects == null) return TestOutcome.Fail("Objects null");

            // Verify ordering
            for (int i = 1; i < result.Objects.Count; i++)
            {
                if (result.Objects[i].CreatedUtc > result.Objects[i - 1].CreatedUtc)
                    return TestOutcome.Fail($"Not in descending order at index {i}");
            }

            Console.Write($"({sw.Elapsed.TotalMilliseconds:F1}ms) ");
            return TestOutcome.Pass();
        });

        // Validate enumeration result properties
        await RunTest($"TIER-{docCount}", "Validate enumeration properties", async () =>
        {
            int pageSize = Math.Min(25, docCount / 4);
            int skip = Math.Min(10, docCount / 10);

            EnumerationResult<Document> result = await client.Search.Enumerate(new EnumerationQuery
            {
                CollectionId = collection.Id,
                MaxResults = pageSize,
                Skip = skip
            });

            if (result == null) return TestOutcome.Fail("Enumerate returned null");
            if (result.TotalRecords != docCount)
                return TestOutcome.Fail($"TotalRecords should be {docCount}, got {result.TotalRecords}");
            if (result.Objects == null) return TestOutcome.Fail("Objects null");
            if (result.Objects.Count != pageSize)
                return TestOutcome.Fail($"Expected {pageSize} objects, got {result.Objects.Count}");

            int expectedRemaining = docCount - skip - pageSize;
            if (result.RecordsRemaining != expectedRemaining)
                return TestOutcome.Fail($"RecordsRemaining should be {expectedRemaining}, got {result.RecordsRemaining}");

            foreach (Document doc in result.Objects)
            {
                if (string.IsNullOrEmpty(doc.Id)) return TestOutcome.Fail("Document Id empty");
                if (doc.CollectionId != collection.Id) return TestOutcome.Fail("CollectionId mismatch");
                if (doc.CreatedUtc == default) return TestOutcome.Fail("CreatedUtc not set");
            }

            return TestOutcome.Pass();
        });

        // Enumerate WITHOUT labels/tags (performance comparison)
        await RunTest($"TIER-{docCount}", "Enumerate (no labels/tags)", async () =>
        {
            int maxResults = Math.Min(100, docCount);
            Stopwatch sw = Stopwatch.StartNew();
            EnumerationResult<Document> result = await client.Search.Enumerate(new EnumerationQuery
            {
                CollectionId = collection.Id,
                MaxResults = maxResults,
                IncludeLabels = false,
                IncludeTags = false
            });
            sw.Stop();

            if (result == null) return TestOutcome.Fail("Enumerate returned null");
            if (result.Objects == null) return TestOutcome.Fail("Objects null");

            // Verify labels/tags are empty
            foreach (Document doc in result.Objects)
            {
                if (doc.Labels.Count != 0) return TestOutcome.Fail("Expected no labels");
                if (doc.Tags.Count != 0) return TestOutcome.Fail("Expected no tags");
            }

            Console.Write($"({sw.Elapsed.TotalMilliseconds:F1}ms) ");
            return TestOutcome.Pass();
        });

        // Enumerate WITH labels/tags (for comparison)
        await RunTest($"TIER-{docCount}", "Enumerate (with labels/tags)", async () =>
        {
            int maxResults = Math.Min(100, docCount);
            Stopwatch sw = Stopwatch.StartNew();
            EnumerationResult<Document> result = await client.Search.Enumerate(new EnumerationQuery
            {
                CollectionId = collection.Id,
                MaxResults = maxResults,
                IncludeLabels = true,
                IncludeTags = true
            });
            sw.Stop();

            if (result == null) return TestOutcome.Fail("Enumerate returned null");
            if (result.Objects == null) return TestOutcome.Fail("Objects null");

            // Verify at least some documents have labels/tags
            bool foundLabels = result.Objects.Any(d => d.Labels.Count > 0);
            bool foundTags = result.Objects.Any(d => d.Tags.Count > 0);
            if (!foundLabels) return TestOutcome.Fail("Expected some documents to have labels");
            if (!foundTags) return TestOutcome.Fail("Expected some documents to have tags");

            Console.Write($"({sw.Elapsed.TotalMilliseconds:F1}ms) ");
            return TestOutcome.Pass();
        });
    }

    #endregion

    #region Standalone Tests

    private static async Task RunStandaloneTests()
    {
        Console.WriteLine();
        Console.WriteLine("========== STANDALONE TESTS ==========");
        Console.WriteLine();

        await RunTest("STANDALONE", "Large documents (50 fields)", TestLargeDocuments);
        await RunTest("STANDALONE", "Deeply nested documents", TestNestedDocuments);
        await RunTest("STANDALONE", "Documents with large arrays", TestArrayDocuments);
        await RunTest("STANDALONE", "Schema reuse", TestSchemaReuse);
        await RunTest("STANDALONE", "Delete during enumeration", TestDeleteDuringEnumeration);
        await RunTest("STANDALONE", "Multiple collections isolation", TestMultipleCollections);
    }

    private static async Task<TestOutcome> TestLargeDocuments()
    {
        string testDir = CreateTestDir();
        try
        {
            using LatticeClient client = CreateClient(testDir);
            Collection collection = await client.Collection.Create("LargeDocs");
            if (collection == null) return TestOutcome.Fail("Failed to create collection");

            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < 50; i++)
            {
                Document doc = await client.Document.Ingest(collection.Id, GenerateLargeJsonDocument(i, 50));
                if (doc == null) return TestOutcome.Fail($"Failed to ingest doc {i}");
            }
            sw.Stop();

            List<Document> docs = await client.Document.ReadAllInCollection(collection.Id);
            if (docs?.Count != 50) return TestOutcome.Fail($"Expected 50, got {docs?.Count}");

            double docsPerSecond = 50.0 / sw.Elapsed.TotalSeconds;
            Console.Write($"({docsPerSecond:F1} docs/sec) ");
            return TestOutcome.Pass();
        }
        finally { CleanupTestDir(testDir); }
    }

    private static async Task<TestOutcome> TestNestedDocuments()
    {
        string testDir = CreateTestDir();
        try
        {
            using LatticeClient client = CreateClient(testDir);
            Collection collection = await client.Collection.Create("NestedDocs");
            if (collection == null) return TestOutcome.Fail("Failed to create collection");

            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < 50; i++)
            {
                string json = JsonSerializer.Serialize(new
                {
                    Id = i,
                    Level1 = new
                    {
                        Name = $"L1_{i}",
                        Level2 = new
                        {
                            Name = $"L2_{i}",
                            Level3 = new
                            {
                                Name = $"L3_{i}",
                                Level4 = new
                                {
                                    Name = $"L4_{i}",
                                    Value = i * 10
                                }
                            }
                        }
                    }
                });
                Document doc = await client.Document.Ingest(collection.Id, json);
                if (doc == null) return TestOutcome.Fail($"Failed to ingest doc {i}");
            }
            sw.Stop();

            List<Document> docs = await client.Document.ReadAllInCollection(collection.Id);
            if (docs?.Count != 50) return TestOutcome.Fail($"Expected 50, got {docs?.Count}");

            double docsPerSecond = 50.0 / sw.Elapsed.TotalSeconds;
            Console.Write($"({docsPerSecond:F1} docs/sec) ");
            return TestOutcome.Pass();
        }
        finally { CleanupTestDir(testDir); }
    }

    private static async Task<TestOutcome> TestArrayDocuments()
    {
        string testDir = CreateTestDir();
        try
        {
            using LatticeClient client = CreateClient(testDir);
            Collection collection = await client.Collection.Create("ArrayDocs");
            if (collection == null) return TestOutcome.Fail("Failed to create collection");

            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < 50; i++)
            {
                string json = JsonSerializer.Serialize(new
                {
                    Id = i,
                    Items = Enumerable.Range(0, 20).Select(j => new { Index = j, Value = $"Item_{j}" }).ToArray(),
                    Numbers = Enumerable.Range(0, 50).ToArray(),
                    Tags = Enumerable.Range(0, 10).Select(j => $"tag_{j}").ToArray()
                });
                Document doc = await client.Document.Ingest(collection.Id, json);
                if (doc == null) return TestOutcome.Fail($"Failed to ingest doc {i}");
            }
            sw.Stop();

            List<Document> docs = await client.Document.ReadAllInCollection(collection.Id);
            if (docs?.Count != 50) return TestOutcome.Fail($"Expected 50, got {docs?.Count}");

            double docsPerSecond = 50.0 / sw.Elapsed.TotalSeconds;
            Console.Write($"({docsPerSecond:F1} docs/sec) ");
            return TestOutcome.Pass();
        }
        finally { CleanupTestDir(testDir); }
    }

    private static async Task<TestOutcome> TestSchemaReuse()
    {
        string testDir = CreateTestDir();
        try
        {
            using LatticeClient client = CreateClient(testDir);
            Collection collection = await client.Collection.Create("SchemaReuse");
            if (collection == null) return TestOutcome.Fail("Failed to create collection");

            HashSet<string> schemaIds = new HashSet<string>();
            for (int i = 0; i < 100; i++)
            {
                string json = $@"{{""Name"":""{i}"",""Category"":""Cat"",""Status"":""Active""}}";
                Document doc = await client.Document.Ingest(collection.Id, json);
                if (doc == null) return TestOutcome.Fail($"Failed to ingest doc {i}");
                schemaIds.Add(doc.SchemaId);
            }

            if (schemaIds.Count != 1)
                return TestOutcome.Fail($"Expected 1 schema, got {schemaIds.Count}");

            List<Schema> schemas = await client.Schema.ReadAll();
            if (schemas == null) return TestOutcome.Fail("GetSchemas returned null");

            string schemaId = schemaIds.First();
            Schema schema = await client.Schema.ReadById(schemaId);
            if (schema == null) return TestOutcome.Fail("GetSchema returned null");

            List<SchemaElement> elements = await client.Schema.GetElements(schemaId);
            if (elements == null || elements.Count == 0)
                return TestOutcome.Fail("Schema has no elements");

            return TestOutcome.Pass();
        }
        finally { CleanupTestDir(testDir); }
    }

    private static async Task<TestOutcome> TestDeleteDuringEnumeration()
    {
        string testDir = CreateTestDir();
        try
        {
            using LatticeClient client = CreateClient(testDir);
            Collection collection = await client.Collection.Create("DeleteEnum");
            if (collection == null) return TestOutcome.Fail("Failed to create collection");

            List<string> docIds = new List<string>();
            for (int i = 0; i < 100; i++)
            {
                Document doc = await client.Document.Ingest(collection.Id, GenerateJsonDocument(i));
                if (doc == null) return TestOutcome.Fail($"Failed to ingest doc {i}");
                docIds.Add(doc.Id);
            }

            EnumerationResult<Document> result1 = await client.Search.Enumerate(new EnumerationQuery
            {
                CollectionId = collection.Id,
                MaxResults = 100
            });
            if (result1?.TotalRecords != 100)
                return TestOutcome.Fail($"Expected 100, got {result1?.TotalRecords}");

            // Delete 25 documents
            for (int i = 0; i < 25; i++)
            {
                await client.Document.Delete(docIds[i]);
            }

            EnumerationResult<Document> result2 = await client.Search.Enumerate(new EnumerationQuery
            {
                CollectionId = collection.Id,
                MaxResults = 100
            });
            if (result2?.TotalRecords != 75)
                return TestOutcome.Fail($"Expected 75 after deletion, got {result2?.TotalRecords}");

            return TestOutcome.Pass();
        }
        finally { CleanupTestDir(testDir); }
    }

    private static async Task<TestOutcome> TestMultipleCollections()
    {
        string testDir = CreateTestDir();
        try
        {
            using LatticeClient client = CreateClient(testDir);

            List<Collection> collections = new List<Collection>();
            for (int c = 0; c < 3; c++)
            {
                Collection col = await client.Collection.Create($"Collection_{c}");
                if (col == null) return TestOutcome.Fail($"Failed to create collection {c}");
                collections.Add(col);
            }

            // Ingest 50 docs into each collection
            for (int c = 0; c < 3; c++)
            {
                for (int i = 0; i < 50; i++)
                {
                    Document doc = await client.Document.Ingest(collections[c].Id, GenerateJsonDocument(i, $"Col{c}"));
                    if (doc == null) return TestOutcome.Fail($"Failed to ingest doc {i} into collection {c}");
                }
            }

            // Verify isolation
            for (int c = 0; c < 3; c++)
            {
                SearchResult result = await client.Search.Search(new SearchQuery
                {
                    CollectionId = collections[c].Id,
                    MaxResults = 100
                });
                if (result?.Documents?.Count != 50)
                    return TestOutcome.Fail($"Collection {c} has {result?.Documents?.Count} docs, expected 50");

                foreach (Document doc in result.Documents)
                {
                    if (doc.CollectionId != collections[c].Id)
                        return TestOutcome.Fail($"Document from wrong collection found in {c}");
                }
            }

            return TestOutcome.Pass();
        }
        finally { CleanupTestDir(testDir); }
    }

    #endregion

    #region Test Infrastructure

    private class TestResult
    {
        public string Section { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool Passed { get; set; }
        public string? Error { get; set; }
        public TimeSpan Duration { get; set; }
    }

    private static string CreateTestDir()
    {
        lock (_dirLock)
        {
            _testDirCounter++;
            string path = Path.Combine(Path.GetTempPath(), $"LatticeThruTest_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{_testDirCounter}");
            Directory.CreateDirectory(path);
            return path;
        }
    }

    private static void CleanupTestDir(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }
        catch { /* Ignore cleanup errors */ }
    }

    private static bool ParseArguments(string[] args, out string error)
    {
        error = null;

        if (args.Length == 0)
        {
            error = "No database type specified.";
            return false;
        }

        string dbType = args[0].ToLowerInvariant();

        switch (dbType)
        {
            case "sqlite":
                if (args.Length < 2)
                {
                    error = "SQLite requires a filename argument.";
                    return false;
                }
                _databaseSettings = new DatabaseSettings
                {
                    Type = DatabaseTypeEnum.Sqlite,
                    Filename = args[1]
                };
                return true;

            case "postgresql":
            case "postgres":
                if (args.Length < 6)
                {
                    error = "PostgreSQL requires: hostname port username password database";
                    return false;
                }
                if (!int.TryParse(args[2], out int pgPort))
                {
                    error = "Invalid port number for PostgreSQL.";
                    return false;
                }
                _databaseSettings = new DatabaseSettings
                {
                    Type = DatabaseTypeEnum.Postgres,
                    Hostname = args[1],
                    Port = pgPort,
                    Username = args[3],
                    Password = args[4],
                    DatabaseName = args[5]
                };
                return true;

            case "mysql":
                if (args.Length < 6)
                {
                    error = "MySQL requires: hostname port username password database";
                    return false;
                }
                if (!int.TryParse(args[2], out int mysqlPort))
                {
                    error = "Invalid port number for MySQL.";
                    return false;
                }
                _databaseSettings = new DatabaseSettings
                {
                    Type = DatabaseTypeEnum.Mysql,
                    Hostname = args[1],
                    Port = mysqlPort,
                    Username = args[3],
                    Password = args[4],
                    DatabaseName = args[5]
                };
                return true;

            case "sqlserver":
            case "mssql":
                if (args.Length < 6)
                {
                    error = "SQL Server requires: hostname port username password database";
                    return false;
                }
                if (!int.TryParse(args[2], out int mssqlPort))
                {
                    error = "Invalid port number for SQL Server.";
                    return false;
                }
                _databaseSettings = new DatabaseSettings
                {
                    Type = DatabaseTypeEnum.SqlServer,
                    Hostname = args[1],
                    Port = mssqlPort,
                    Username = args[3],
                    Password = args[4],
                    DatabaseName = args[5]
                };
                return true;

            default:
                error = $"Unknown database type: {dbType}";
                return false;
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  Test.Throughput sqlite <filename>");
        Console.WriteLine("  Test.Throughput postgresql <hostname> <port> <username> <password> <database>");
        Console.WriteLine("  Test.Throughput mysql <hostname> <port> <username> <password> <database>");
        Console.WriteLine("  Test.Throughput sqlserver <hostname> <port> <username> <password> <database>");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  Test.Throughput sqlite ./test.db");
        Console.WriteLine("  Test.Throughput postgresql localhost 5432 postgres password lattice");
        Console.WriteLine("  Test.Throughput mysql localhost 3306 root password lattice");
        Console.WriteLine("  Test.Throughput sqlserver localhost 1433 sa password lattice");
    }

    private static void PrintDatabaseInfo()
    {
        Console.WriteLine($"Database Type: {_databaseSettings.Type}");
        if (_databaseSettings.Type == DatabaseTypeEnum.Sqlite)
        {
            Console.WriteLine($"Database File: {_databaseSettings.Filename}");
        }
        else
        {
            Console.WriteLine($"Host: {_databaseSettings.Hostname}:{_databaseSettings.Port}");
            Console.WriteLine($"Database: {_databaseSettings.DatabaseName}");
            Console.WriteLine($"User: {_databaseSettings.Username}");
        }
        Console.WriteLine();
    }

    private static async Task<bool> ValidateDatabaseConnection()
    {
        Console.WriteLine("Validating database connection...");

        string testDir = CreateTestDir();
        try
        {
            // Use inMemory=false to actually test the real database connection
            using var client = CreateClient(testDir, inMemory: false);

            // Try to create a test collection to verify the connection works
            var testCollection = await client.Collection.Create("__connection_test__");
            if (testCollection == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ERROR: Failed to create test collection. Database may be unavailable.");
                Console.ResetColor();
                return false;
            }

            // Clean up test collection
            await client.Collection.Delete(testCollection.Id);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Database connection validated successfully.");
            Console.ResetColor();
            Console.WriteLine();
            return true;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"ERROR: Database connection failed.");
            Console.WriteLine($"  {ex.GetType().Name}: {ex.Message}");

            // Provide more specific guidance based on exception type
            string msg = ex.Message.ToLowerInvariant();
            if (msg.Contains("password") || msg.Contains("authentication") || msg.Contains("login") || msg.Contains("access denied"))
            {
                Console.WriteLine("  Hint: Check your username and password credentials.");
            }
            else if (msg.Contains("connect") || msg.Contains("network") || msg.Contains("host") || msg.Contains("refused") || msg.Contains("timeout"))
            {
                Console.WriteLine("  Hint: Verify the database server is running and accessible.");
            }
            else if (msg.Contains("database") && (msg.Contains("not exist") || msg.Contains("unknown")))
            {
                Console.WriteLine("  Hint: The specified database may not exist. Create it first.");
            }

            Console.ResetColor();
            return false;
        }
        finally
        {
            CleanupTestDir(testDir);
        }
    }

    private static LatticeClient CreateClient(string testDir, bool inMemory = true)
    {
        DatabaseSettings dbSettings = new DatabaseSettings
        {
            Type = _databaseSettings.Type
        };

        if (_databaseSettings.Type == DatabaseTypeEnum.Sqlite)
        {
            dbSettings.Filename = Path.Combine(testDir, Path.GetFileName(_databaseSettings.Filename));
        }
        else
        {
            dbSettings.Hostname = _databaseSettings.Hostname;
            dbSettings.Port = _databaseSettings.Port;
            dbSettings.Username = _databaseSettings.Username;
            dbSettings.Password = _databaseSettings.Password;
            dbSettings.DatabaseName = _databaseSettings.DatabaseName;
        }

        return new LatticeClient(new LatticeSettings
        {
            Database = dbSettings,
            DefaultDocumentsDirectory = Path.Combine(testDir, "documents"),
            InMemory = inMemory,
            EnableLogging = false
        });
    }

    private static async Task RunTest(string section, string name, Func<Task<TestOutcome>> testFunc)
    {
        Console.Write($"  [{section}] {name}... ");
        Stopwatch sw = Stopwatch.StartNew();
        bool passed;
        string error = null;

        try
        {
            TestOutcome result = await testFunc();
            passed = result.Success;
            error = result.Error;
        }
        catch (Exception ex)
        {
            passed = false;
            error = $"Exception: {ex.Message}";
        }

        sw.Stop();

        TestResult testResult = new TestResult
        {
            Section = section,
            Name = name,
            Passed = passed,
            Error = error,
            Duration = sw.Elapsed
        };
        _results.Add(testResult);

        if (passed)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"PASS ({sw.Elapsed.TotalMilliseconds:F1}ms)");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"FAIL ({sw.Elapsed.TotalMilliseconds:F1}ms)");
            Console.WriteLine($"       Error: {error}");
        }
        Console.ResetColor();
    }

    private static void PrintSummary(TimeSpan totalDuration)
    {
        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine("  TEST SUMMARY");
        Console.WriteLine("========================================");
        Console.WriteLine();

        // Group by section
        IOrderedEnumerable<IGrouping<string, TestResult>> sections = _results.GroupBy(r => r.Section).OrderBy(g => g.Key);
        foreach (IGrouping<string, TestResult> section in sections)
        {
            int sectionPassed = section.Count(r => r.Passed);
            int sectionTotal = section.Count();
            bool sectionSuccess = sectionPassed == sectionTotal;
            TimeSpan sectionTime = TimeSpan.FromMilliseconds(section.Sum(r => r.Duration.TotalMilliseconds));

            Console.ForegroundColor = sectionSuccess ? ConsoleColor.Green : ConsoleColor.Red;
            Console.WriteLine($"  {section.Key}: {sectionPassed}/{sectionTotal} [{(sectionSuccess ? "PASS" : "FAIL")}] ({sectionTime.TotalMilliseconds:F0}ms)");
            Console.ResetColor();

            // Show failed tests in this section
            foreach (TestResult failed in section.Where(r => !r.Passed))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"    - {failed.Name}: {failed.Error}");
                Console.ResetColor();
            }
        }

        Console.WriteLine();

        int totalPassed = _results.Count(r => r.Passed);
        int totalFailed = _results.Count(r => !r.Passed);
        bool overallPass = totalFailed == 0;

        Console.ForegroundColor = overallPass ? ConsoleColor.Green : ConsoleColor.Red;
        Console.WriteLine($"  TOTAL: {totalPassed} passed, {totalFailed} failed [{(overallPass ? "PASS" : "FAIL")}]");
        Console.WriteLine($"  RUNTIME: {totalDuration.TotalMilliseconds:F0}ms ({totalDuration.TotalSeconds:F2}s)");
        Console.ResetColor();
        Console.WriteLine();

        // Print tier comparison tables
        PrintTierComparisonTables();
    }

    private static void PrintTierComparisonTables()
    {
        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine("  TIER COMPARISON TABLES");
        Console.WriteLine("========================================");

        // Get tier results only (exclude STANDALONE)
        List<TestResult> tierResults = _results
            .Where(r => r.Section.StartsWith("TIER-"))
            .ToList();

        if (tierResults.Count == 0)
        {
            Console.WriteLine("  No tier results to display.");
            return;
        }

        // Extract unique tiers in order
        List<int> tiers = tierResults
            .Select(r => int.Parse(r.Section.Replace("TIER-", "")))
            .Distinct()
            .OrderBy(t => t)
            .ToList();

        // Normalize test names by removing tier-specific counts
        // e.g., "Ingest 100 documents" -> "Ingest {n} documents"
        // e.g., "GetDocument 100x (with content)" -> "GetDocument {n}x (with content)"
        string NormalizeTestName(string name, int tier)
        {
            // Replace the tier count in the test name with {n}
            return name
                .Replace($"Ingest {tier} documents", "Ingest {n} documents")
                .Replace($"{Math.Min(100, tier)}x", "{n}x")
                .Replace($"{Math.Min(50, tier / 2)} queries", "{n} queries")
                .Replace($"max {Math.Min(1000, tier)}", "max {n}");
        }

        // Build a lookup: normalizedName -> tier -> TestResult
        Dictionary<string, Dictionary<int, TestResult>> testsByName = new Dictionary<string, Dictionary<int, TestResult>>();

        foreach (TestResult result in tierResults)
        {
            int tier = int.Parse(result.Section.Replace("TIER-", ""));
            string normalizedName = NormalizeTestName(result.Name, tier);

            if (!testsByName.ContainsKey(normalizedName))
                testsByName[normalizedName] = new Dictionary<int, TestResult>();

            testsByName[normalizedName][tier] = result;
        }

        // Group tests by category (Ingestion, Retrieval, Search, Enumeration, Validation)
        Dictionary<string, List<string>> categories = new Dictionary<string, List<string>>
        {
            ["INGESTION"] = new List<string>(),
            ["RETRIEVAL"] = new List<string>(),
            ["SEARCH"] = new List<string>(),
            ["ENUMERATION"] = new List<string>()
        };

        foreach (string testName in testsByName.Keys)
        {
            if (testName.Contains("Ingest") || testName.Contains("Validate ingested"))
                categories["INGESTION"].Add(testName);
            else if (testName.Contains("GetDocument") || testName.Contains("DocumentExists") || testName.Contains("Validate retrieved") || testName.Contains("Validate labels"))
                categories["RETRIEVAL"].Add(testName);
            else if (testName.Contains("Search") || testName.Contains("Validate search"))
                categories["SEARCH"].Add(testName);
            else if (testName.Contains("Enumerate") || testName.Contains("Validate enumeration"))
                categories["ENUMERATION"].Add(testName);
        }

        // Calculate column widths
        int testNameWidth = testsByName.Keys.Max(k => k.Length) + 2;
        int tierColWidth = 12; // "5000 docs" is 9 chars + padding

        // Print each category table
        foreach (KeyValuePair<string, List<string>> category in categories.Where(c => c.Value.Count > 0))
        {
            Console.WriteLine();
            Console.WriteLine($"  --- {category.Key} ---");
            Console.WriteLine();

            // Header row
            Console.Write("  ");
            Console.Write("Test".PadRight(testNameWidth));
            Console.Write(" | ");
            foreach (int tier in tiers)
            {
                Console.Write($"{tier} docs".PadRight(tierColWidth));
            }
            Console.WriteLine();

            // Separator row
            Console.Write("  ");
            Console.Write(new string('-', testNameWidth));
            Console.Write("-+-");
            Console.WriteLine(string.Join("", tiers.Select(_ => new string('-', tierColWidth))));

            // Data rows
            foreach (string testName in category.Value.OrderBy(n => n))
            {
                Dictionary<int, TestResult> tierData = testsByName[testName];

                Console.Write("  ");
                Console.Write(testName.PadRight(testNameWidth));
                Console.Write(" | ");

                foreach (int tier in tiers)
                {
                    if (tierData.TryGetValue(tier, out TestResult result))
                    {
                        string status = result.Passed ? "PASS" : "FAIL";
                        string time = $"{result.Duration.TotalMilliseconds:F0}ms";
                        string cell = $"{status} {time}";

                        Console.ForegroundColor = result.Passed ? ConsoleColor.Green : ConsoleColor.Red;
                        Console.Write(cell.PadRight(tierColWidth));
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.Write("N/A".PadRight(tierColWidth));
                    }
                }
                Console.WriteLine();
            }
        }

        Console.WriteLine();
    }

    private static string GenerateJsonDocument(int index, string namePrefix = null)
    {
        Dictionary<string, object> doc = new Dictionary<string, object>
        {
            ["Id"] = index,
            ["Name"] = $"{namePrefix ?? "Item"}_{index}",
            ["Description"] = $"Test document number {index}",
            ["Value"] = index * 1.5,
            ["IsActive"] = index % 2 == 0,
            ["Category"] = $"Category_{index % 10}",
            ["Tags"] = new[] { "tag_0", "tag_1", "tag_2" }
        };

        return JsonSerializer.Serialize(doc);
    }

    private static string GenerateLargeJsonDocument(int index, int fieldCount)
    {
        Dictionary<string, object> doc = new Dictionary<string, object>
        {
            ["Id"] = index,
            ["Name"] = $"LargeDoc_{index}"
        };

        for (int i = 0; i < fieldCount; i++)
        {
            doc[$"Field_{i}"] = $"Value_{i}_{index}";
        }

        return JsonSerializer.Serialize(doc);
    }

    #endregion
}
