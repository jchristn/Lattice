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
    private static readonly List<TestResult> _results = new();
    private static int _testDirCounter = 0;
    private static readonly object _dirLock = new();

    // Document count tiers to test
    private static readonly int[] _tiers = { 100, 250, 500, 1000, 5000 };

    public static async Task<int> Main(string[] args)
    {
        Console.WriteLine("========================================");
        Console.WriteLine("  LATTICE THROUGHPUT TEST SUITE");
        Console.WriteLine("========================================");
        Console.WriteLine();

        var overallStopwatch = Stopwatch.StartNew();

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
            using var client = CreateClient(testDir);
            var collection = await client.CreateCollection($"TierTest_{docCount}");
            if (collection == null)
            {
                Console.WriteLine($"  ERROR: Failed to create collection for tier {docCount}");
                return;
            }

            // Shared ingestion for this tier
            Console.WriteLine();
            Console.WriteLine($"--- INGESTION ({docCount} docs) ---");
            Console.WriteLine();

            var ingestedDocs = new List<Document>();
            await RunTest($"TIER-{docCount}", $"Ingest {docCount} documents", async () =>
            {
                var sw = Stopwatch.StartNew();
                for (int i = 0; i < docCount; i++)
                {
                    var labels = new List<string> { $"group_{i % 10}", $"batch_{i / 100}" };
                    if (i % 50 == 0) labels.Add("special");

                    var tags = new Dictionary<string, string>
                    {
                        ["priority"] = (i % 5).ToString(),
                        ["category"] = $"cat_{i % 10}"
                    };

                    var doc = await client.IngestDocument(
                        collection.Id,
                        GenerateJsonDocument(i),
                        name: $"Doc_{i}",
                        labels: labels,
                        tags: tags);

                    if (doc == null) return (false, $"Failed to ingest document {i}");
                    ingestedDocs.Add(doc);
                }
                sw.Stop();

                double docsPerSecond = docCount / sw.Elapsed.TotalSeconds;
                Console.Write($"({docsPerSecond:F1} docs/sec) ");

                return (true, null);
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
                    var doc = ingestedDocs[i];
                    if (string.IsNullOrEmpty(doc.Id) || !doc.Id.StartsWith("doc_"))
                        return (false, $"Doc {i}: Invalid Id");
                    if (doc.CollectionId != collection.Id)
                        return (false, $"Doc {i}: CollectionId mismatch");
                    if (doc.Name != $"Doc_{i}")
                        return (false, $"Doc {i}: Name mismatch");
                    if (string.IsNullOrEmpty(doc.SchemaId))
                        return (false, $"Doc {i}: SchemaId empty");
                    if (doc.CreatedUtc == default)
                        return (false, $"Doc {i}: CreatedUtc not set");
                }
                return (true, null);
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
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < sampleSize; i++)
            {
                var doc = await client.GetDocument(docs[i].Id, includeContent: true);
                if (doc == null) return (false, $"GetDocument returned null for {docs[i].Id}");
                if (string.IsNullOrEmpty(doc.Content)) return (false, $"Content empty for doc {i}");
            }
            sw.Stop();
            double opsPerSecond = sampleSize / sw.Elapsed.TotalSeconds;
            Console.Write($"({opsPerSecond:F1} ops/sec) ");
            return (true, null);
        });

        // GetDocument without content
        await RunTest($"TIER-{docCount}", $"GetDocument {sampleSize}x (no content)", async () =>
        {
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < sampleSize; i++)
            {
                var doc = await client.GetDocument(docs[i].Id, includeContent: false);
                if (doc == null) return (false, $"GetDocument returned null for {docs[i].Id}");
            }
            sw.Stop();
            double opsPerSecond = sampleSize / sw.Elapsed.TotalSeconds;
            Console.Write($"({opsPerSecond:F1} ops/sec) ");
            return (true, null);
        });

        // GetDocuments (bulk)
        await RunTest($"TIER-{docCount}", "GetDocuments (bulk)", async () =>
        {
            var sw = Stopwatch.StartNew();
            var allDocs = await client.GetDocuments(collection.Id);
            sw.Stop();

            if (allDocs == null) return (false, "GetDocuments returned null");
            if (allDocs.Count != docCount) return (false, $"Expected {docCount}, got {allDocs.Count}");

            Console.Write($"({sw.Elapsed.TotalMilliseconds:F1}ms) ");
            return (true, null);
        });

        // DocumentExists
        await RunTest($"TIER-{docCount}", $"DocumentExists {sampleSize}x", async () =>
        {
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < sampleSize; i++)
            {
                bool exists = await client.DocumentExists(docs[i].Id);
                if (!exists) return (false, $"DocumentExists returned false for {docs[i].Id}");
            }
            sw.Stop();
            double opsPerSecond = sampleSize / sw.Elapsed.TotalSeconds;
            Console.Write($"({opsPerSecond:F1} ops/sec) ");
            return (true, null);
        });

        // Validate retrieved document properties
        await RunTest($"TIER-{docCount}", "Validate retrieved properties", async () =>
        {
            var doc = await client.GetDocument(docs[0].Id, includeContent: true);
            if (doc == null) return (false, "GetDocument returned null");

            if (doc.Id != docs[0].Id) return (false, "Id mismatch");
            if (doc.CollectionId != collection.Id) return (false, "CollectionId mismatch");
            if (doc.Name != "Doc_0") return (false, $"Name mismatch: {doc.Name}");
            if (string.IsNullOrEmpty(doc.SchemaId)) return (false, "SchemaId empty");
            if (string.IsNullOrEmpty(doc.Content)) return (false, "Content empty");
            if (doc.CreatedUtc == default) return (false, "CreatedUtc not set");
            if (doc.LastUpdateUtc == default) return (false, "LastUpdateUtc not set");

            return (true, null);
        });

        // GetDocument WITHOUT labels/tags (performance comparison)
        await RunTest($"TIER-{docCount}", $"GetDocument {sampleSize}x (no labels/tags)", async () =>
        {
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < sampleSize; i++)
            {
                var doc = await client.GetDocument(docs[i].Id, includeContent: false, includeLabels: false, includeTags: false);
                if (doc == null) return (false, $"GetDocument returned null for {docs[i].Id}");
            }
            sw.Stop();
            double opsPerSecond = sampleSize / sw.Elapsed.TotalSeconds;
            Console.Write($"({opsPerSecond:F1} ops/sec) ");
            return (true, null);
        });

        // Validate labels/tags are excluded when not requested
        await RunTest($"TIER-{docCount}", "Validate labels/tags exclusion", async () =>
        {
            // Get document with labels and tags
            var docWithAll = await client.GetDocument(docs[0].Id, includeContent: false, includeLabels: true, includeTags: true);
            if (docWithAll == null) return (false, "GetDocument returned null");
            if (docWithAll.Labels.Count == 0) return (false, "Expected labels when includeLabels=true");
            if (docWithAll.Tags.Count == 0) return (false, "Expected tags when includeTags=true");

            // Get document without labels and tags
            var docWithoutLabels = await client.GetDocument(docs[0].Id, includeContent: false, includeLabels: false, includeTags: true);
            if (docWithoutLabels == null) return (false, "GetDocument returned null");
            if (docWithoutLabels.Labels.Count != 0) return (false, "Expected no labels when includeLabels=false");
            if (docWithoutLabels.Tags.Count == 0) return (false, "Expected tags when includeTags=true");

            var docWithoutTags = await client.GetDocument(docs[0].Id, includeContent: false, includeLabels: true, includeTags: false);
            if (docWithoutTags == null) return (false, "GetDocument returned null");
            if (docWithoutTags.Labels.Count == 0) return (false, "Expected labels when includeLabels=true");
            if (docWithoutTags.Tags.Count != 0) return (false, "Expected no tags when includeTags=false");

            var docMinimal = await client.GetDocument(docs[0].Id, includeContent: false, includeLabels: false, includeTags: false);
            if (docMinimal == null) return (false, "GetDocument returned null");
            if (docMinimal.Labels.Count != 0) return (false, "Expected no labels when includeLabels=false");
            if (docMinimal.Tags.Count != 0) return (false, "Expected no tags when includeTags=false");

            return (true, null);
        });

        // GetDocuments bulk WITHOUT labels/tags (performance comparison)
        await RunTest($"TIER-{docCount}", "GetDocuments bulk (no labels/tags)", async () =>
        {
            var sw = Stopwatch.StartNew();
            var allDocs = await client.GetDocuments(collection.Id, includeLabels: false, includeTags: false);
            sw.Stop();

            if (allDocs == null) return (false, "GetDocuments returned null");
            if (allDocs.Count != docCount) return (false, $"Expected {docCount}, got {allDocs.Count}");

            // Verify labels/tags are empty
            foreach (var doc in allDocs)
            {
                if (doc.Labels.Count != 0) return (false, "Expected no labels");
                if (doc.Tags.Count != 0) return (false, "Expected no tags");
            }

            Console.Write($"({sw.Elapsed.TotalMilliseconds:F1}ms) ");
            return (true, null);
        });
    }

    private static async Task RunSearchTests(LatticeClient client, Collection collection, int docCount)
    {
        int expectedCategory5Count = docCount / 10; // Category_5 matches i%10==5

        // Search with Equals
        await RunTest($"TIER-{docCount}", "Search (Equals filter)", async () =>
        {
            var sw = Stopwatch.StartNew();
            var result = await client.Search(new SearchQuery
            {
                CollectionId = collection.Id,
                Filters = new List<SearchFilter>
                {
                    new SearchFilter("Category", SearchConditionEnum.Equals, "Category_5")
                },
                MaxResults = docCount
            });
            sw.Stop();

            if (result == null) return (false, "Search returned null");
            if (!result.Success) return (false, "Search not successful");
            if (result.Documents == null) return (false, "Documents null");
            if (result.Documents.Count != expectedCategory5Count)
                return (false, $"Expected {expectedCategory5Count}, got {result.Documents.Count}");

            Console.Write($"({sw.Elapsed.TotalMilliseconds:F1}ms) ");
            return (true, null);
        });

        // Search with Contains
        await RunTest($"TIER-{docCount}", "Search (Contains filter)", async () =>
        {
            var sw = Stopwatch.StartNew();
            var result = await client.Search(new SearchQuery
            {
                CollectionId = collection.Id,
                Filters = new List<SearchFilter>
                {
                    new SearchFilter("Name", SearchConditionEnum.Contains, "Item_1")
                },
                MaxResults = docCount
            });
            sw.Stop();

            if (result == null) return (false, "Search returned null");
            if (!result.Success) return (false, "Search not successful");
            // "Item_1" matches 1, 10-19, 100-199, 1000-1999, etc.

            Console.Write($"({sw.Elapsed.TotalMilliseconds:F1}ms, {result.Documents?.Count} results) ");
            return (true, null);
        });

        // Search with GreaterThan
        int thresholdIndex = (int)(docCount * 0.9); // Top 10%
        await RunTest($"TIER-{docCount}", "Search (GreaterThan filter)", async () =>
        {
            var sw = Stopwatch.StartNew();
            var result = await client.Search(new SearchQuery
            {
                CollectionId = collection.Id,
                Filters = new List<SearchFilter>
                {
                    new SearchFilter("Id", SearchConditionEnum.GreaterThan, thresholdIndex.ToString())
                },
                MaxResults = docCount
            });
            sw.Stop();

            if (result == null) return (false, "Search returned null");
            if (!result.Success) return (false, "Search not successful");

            int expectedCount = docCount - thresholdIndex - 1;
            if (result.Documents!.Count != expectedCount)
                return (false, $"Expected {expectedCount}, got {result.Documents.Count}");

            Console.Write($"({sw.Elapsed.TotalMilliseconds:F1}ms) ");
            return (true, null);
        });

        // Search with multiple filters
        await RunTest($"TIER-{docCount}", "Search (multiple filters)", async () =>
        {
            var sw = Stopwatch.StartNew();
            var result = await client.Search(new SearchQuery
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

            if (result == null) return (false, "Search returned null");
            if (!result.Success) return (false, "Search not successful");
            // Category_5 (i%10==5) AND IsActive=false (i%2==1) - all items ending in 5 are odd
            if (result.Documents!.Count != expectedCategory5Count)
                return (false, $"Expected {expectedCategory5Count}, got {result.Documents.Count}");

            Console.Write($"({sw.Elapsed.TotalMilliseconds:F1}ms) ");
            return (true, null);
        });

        // Search by label
        int expectedSpecialCount = (docCount + 49) / 50; // Every 50th document has "special" label
        await RunTest($"TIER-{docCount}", "Search (by label)", async () =>
        {
            var sw = Stopwatch.StartNew();
            var result = await client.Search(new SearchQuery
            {
                CollectionId = collection.Id,
                Labels = new List<string> { "special" },
                MaxResults = docCount
            });
            sw.Stop();

            if (result == null) return (false, "Search returned null");
            if (!result.Success) return (false, "Search not successful");
            if (result.Documents!.Count != expectedSpecialCount)
                return (false, $"Expected {expectedSpecialCount}, got {result.Documents.Count}");

            Console.Write($"({sw.Elapsed.TotalMilliseconds:F1}ms) ");
            return (true, null);
        });

        // Search by tag
        int expectedPriority0Count = (docCount + 4) / 5; // Every 5th document has priority=0
        await RunTest($"TIER-{docCount}", "Search (by tag)", async () =>
        {
            var sw = Stopwatch.StartNew();
            var result = await client.Search(new SearchQuery
            {
                CollectionId = collection.Id,
                Tags = new Dictionary<string, string> { ["priority"] = "0" },
                MaxResults = docCount
            });
            sw.Stop();

            if (result == null) return (false, "Search returned null");
            if (!result.Success) return (false, "Search not successful");
            if (result.Documents!.Count != expectedPriority0Count)
                return (false, $"Expected {expectedPriority0Count}, got {result.Documents.Count}");

            Console.Write($"({sw.Elapsed.TotalMilliseconds:F1}ms) ");
            return (true, null);
        });

        // Search with pagination
        await RunTest($"TIER-{docCount}", "Search (pagination)", async () =>
        {
            var sw = Stopwatch.StartNew();
            int totalRetrieved = 0;
            int skip = 0;
            int pageSize = Math.Min(100, docCount / 5);
            int iterations = 0;
            int maxIterations = (docCount / pageSize) + 2;

            while (iterations < maxIterations)
            {
                var result = await client.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    MaxResults = pageSize,
                    Skip = skip
                });

                if (result == null) return (false, "Search returned null");
                if (!result.Success) return (false, "Search not successful");

                totalRetrieved += result.Documents!.Count;
                iterations++;

                if (result.EndOfResults || result.Documents.Count == 0)
                    break;

                skip += pageSize;
            }
            sw.Stop();

            if (totalRetrieved != docCount)
                return (false, $"Expected {docCount}, got {totalRetrieved}");

            Console.Write($"({iterations} pages, {sw.Elapsed.TotalMilliseconds:F1}ms) ");
            return (true, null);
        });

        // Repeated searches (query throughput)
        int queryCount = Math.Min(50, docCount / 2);
        await RunTest($"TIER-{docCount}", $"Search throughput ({queryCount} queries)", async () =>
        {
            var sw = Stopwatch.StartNew();
            for (int q = 0; q < queryCount; q++)
            {
                var result = await client.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Filters = new List<SearchFilter>
                    {
                        new SearchFilter("Category", SearchConditionEnum.Equals, $"Category_{q % 10}")
                    },
                    MaxResults = 100
                });
                if (result == null || !result.Success)
                    return (false, $"Query {q} failed");
            }
            sw.Stop();

            double queriesPerSecond = queryCount / sw.Elapsed.TotalSeconds;
            Console.Write($"({queriesPerSecond:F1} queries/sec) ");
            return (true, null);
        });

        // SearchBySql
        await RunTest($"TIER-{docCount}", "SearchBySql", async () =>
        {
            int limit = Math.Min(50, docCount);
            var sw = Stopwatch.StartNew();
            var result = await client.SearchBySql(collection.Id, $"SELECT * FROM documents LIMIT {limit}");
            sw.Stop();

            if (result == null) return (false, "SearchBySql returned null");
            if (!result.Success) return (false, "Search not successful");
            if (result.Documents!.Count != limit)
                return (false, $"Expected {limit}, got {result.Documents.Count}");

            Console.Write($"({sw.Elapsed.TotalMilliseconds:F1}ms) ");
            return (true, null);
        });

        // Validate search result properties
        await RunTest($"TIER-{docCount}", "Validate search result properties", async () =>
        {
            var result = await client.Search(new SearchQuery
            {
                CollectionId = collection.Id,
                MaxResults = 10,
                Skip = 5,
                IncludeContent = true
            });

            if (result == null) return (false, "Search returned null");
            if (!result.Success) return (false, "Success should be true");
            if (result.Timestamp == null) return (false, "Timestamp null");
            if (result.TotalRecords != docCount)
                return (false, $"TotalRecords should be {docCount}, got {result.TotalRecords}");
            if (result.Documents == null) return (false, "Documents null");
            if (result.Documents.Count != 10)
                return (false, $"Expected 10 docs, got {result.Documents.Count}");

            foreach (var doc in result.Documents)
            {
                if (string.IsNullOrEmpty(doc.Id)) return (false, "Document Id empty");
                if (doc.CollectionId != collection.Id) return (false, "CollectionId mismatch");
                if (string.IsNullOrEmpty(doc.Content)) return (false, "Content should be included");
            }

            return (true, null);
        });

        // Search WITHOUT labels/tags (performance comparison)
        await RunTest($"TIER-{docCount}", "Search (no labels/tags)", async () =>
        {
            var sw = Stopwatch.StartNew();
            var result = await client.Search(new SearchQuery
            {
                CollectionId = collection.Id,
                MaxResults = Math.Min(100, docCount),
                IncludeLabels = false,
                IncludeTags = false
            });
            sw.Stop();

            if (result == null) return (false, "Search returned null");
            if (!result.Success) return (false, "Search not successful");

            // Verify labels/tags are empty
            foreach (var doc in result.Documents!)
            {
                if (doc.Labels.Count != 0) return (false, "Expected no labels");
                if (doc.Tags.Count != 0) return (false, "Expected no tags");
            }

            Console.Write($"({sw.Elapsed.TotalMilliseconds:F1}ms) ");
            return (true, null);
        });

        // Search WITH labels/tags (for comparison)
        await RunTest($"TIER-{docCount}", "Search (with labels/tags)", async () =>
        {
            var sw = Stopwatch.StartNew();
            var result = await client.Search(new SearchQuery
            {
                CollectionId = collection.Id,
                MaxResults = Math.Min(100, docCount),
                IncludeLabels = true,
                IncludeTags = true
            });
            sw.Stop();

            if (result == null) return (false, "Search returned null");
            if (!result.Success) return (false, "Search not successful");

            // Verify at least some documents have labels/tags
            bool foundLabels = result.Documents!.Any(d => d.Labels.Count > 0);
            bool foundTags = result.Documents!.Any(d => d.Tags.Count > 0);
            if (!foundLabels) return (false, "Expected some documents to have labels");
            if (!foundTags) return (false, "Expected some documents to have tags");

            Console.Write($"({sw.Elapsed.TotalMilliseconds:F1}ms) ");
            return (true, null);
        });
    }

    private static async Task RunEnumerationTests(LatticeClient client, Collection collection, int docCount)
    {
        // Basic enumeration (MaxResults capped at 1000 by API)
        int basicMaxResults = Math.Min(1000, docCount);
        await RunTest($"TIER-{docCount}", $"Enumerate (basic, max {basicMaxResults})", async () =>
        {
            var sw = Stopwatch.StartNew();
            var result = await client.Enumerate(new EnumerationQuery
            {
                CollectionId = collection.Id,
                MaxResults = basicMaxResults
            });
            sw.Stop();

            if (result == null) return (false, "Enumerate returned null");
            if (result.Objects == null) return (false, "Objects null");
            if (result.Objects.Count != basicMaxResults)
                return (false, $"Expected {basicMaxResults}, got {result.Objects.Count}");
            if (result.TotalRecords != docCount)
                return (false, $"TotalRecords should be {docCount}, got {result.TotalRecords}");

            Console.Write($"({sw.Elapsed.TotalMilliseconds:F1}ms) ");
            return (true, null);
        });

        // Enumeration with pagination
        await RunTest($"TIER-{docCount}", "Enumerate (pagination)", async () =>
        {
            var sw = Stopwatch.StartNew();
            int totalRetrieved = 0;
            int skip = 0;
            int pageSize = Math.Min(100, docCount / 5);
            int iterations = 0;
            int maxIterations = (docCount / pageSize) + 2;

            while (iterations < maxIterations)
            {
                var result = await client.Enumerate(new EnumerationQuery
                {
                    CollectionId = collection.Id,
                    MaxResults = pageSize,
                    Skip = skip
                });

                if (result == null) return (false, "Enumerate returned null");
                if (result.Objects == null) return (false, "Objects null");

                totalRetrieved += result.Objects.Count;
                iterations++;

                if (result.EndOfResults || result.Objects.Count == 0)
                    break;

                skip += pageSize;
            }
            sw.Stop();

            if (totalRetrieved != docCount)
                return (false, $"Expected {docCount}, got {totalRetrieved}");

            Console.Write($"({iterations} pages, {sw.Elapsed.TotalMilliseconds:F1}ms) ");
            return (true, null);
        });

        // Enumeration with ascending order
        await RunTest($"TIER-{docCount}", "Enumerate (CreatedAscending)", async () =>
        {
            var sw = Stopwatch.StartNew();
            var result = await client.Enumerate(new EnumerationQuery
            {
                CollectionId = collection.Id,
                MaxResults = 100,
                Ordering = EnumerationOrderEnum.CreatedAscending
            });
            sw.Stop();

            if (result == null) return (false, "Enumerate returned null");
            if (result.Objects == null) return (false, "Objects null");

            // Verify ordering
            for (int i = 1; i < result.Objects.Count; i++)
            {
                if (result.Objects[i].CreatedUtc < result.Objects[i - 1].CreatedUtc)
                    return (false, $"Not in ascending order at index {i}");
            }

            Console.Write($"({sw.Elapsed.TotalMilliseconds:F1}ms) ");
            return (true, null);
        });

        // Enumeration with descending order
        await RunTest($"TIER-{docCount}", "Enumerate (CreatedDescending)", async () =>
        {
            var sw = Stopwatch.StartNew();
            var result = await client.Enumerate(new EnumerationQuery
            {
                CollectionId = collection.Id,
                MaxResults = 100,
                Ordering = EnumerationOrderEnum.CreatedDescending
            });
            sw.Stop();

            if (result == null) return (false, "Enumerate returned null");
            if (result.Objects == null) return (false, "Objects null");

            // Verify ordering
            for (int i = 1; i < result.Objects.Count; i++)
            {
                if (result.Objects[i].CreatedUtc > result.Objects[i - 1].CreatedUtc)
                    return (false, $"Not in descending order at index {i}");
            }

            Console.Write($"({sw.Elapsed.TotalMilliseconds:F1}ms) ");
            return (true, null);
        });

        // Validate enumeration result properties
        await RunTest($"TIER-{docCount}", "Validate enumeration properties", async () =>
        {
            int pageSize = Math.Min(25, docCount / 4);
            int skip = Math.Min(10, docCount / 10);

            var result = await client.Enumerate(new EnumerationQuery
            {
                CollectionId = collection.Id,
                MaxResults = pageSize,
                Skip = skip
            });

            if (result == null) return (false, "Enumerate returned null");
            if (result.TotalRecords != docCount)
                return (false, $"TotalRecords should be {docCount}, got {result.TotalRecords}");
            if (result.Objects == null) return (false, "Objects null");
            if (result.Objects.Count != pageSize)
                return (false, $"Expected {pageSize} objects, got {result.Objects.Count}");

            int expectedRemaining = docCount - skip - pageSize;
            if (result.RecordsRemaining != expectedRemaining)
                return (false, $"RecordsRemaining should be {expectedRemaining}, got {result.RecordsRemaining}");

            foreach (var doc in result.Objects)
            {
                if (string.IsNullOrEmpty(doc.Id)) return (false, "Document Id empty");
                if (doc.CollectionId != collection.Id) return (false, "CollectionId mismatch");
                if (doc.CreatedUtc == default) return (false, "CreatedUtc not set");
            }

            return (true, null);
        });

        // Enumerate WITHOUT labels/tags (performance comparison)
        await RunTest($"TIER-{docCount}", "Enumerate (no labels/tags)", async () =>
        {
            int maxResults = Math.Min(100, docCount);
            var sw = Stopwatch.StartNew();
            var result = await client.Enumerate(new EnumerationQuery
            {
                CollectionId = collection.Id,
                MaxResults = maxResults,
                IncludeLabels = false,
                IncludeTags = false
            });
            sw.Stop();

            if (result == null) return (false, "Enumerate returned null");
            if (result.Objects == null) return (false, "Objects null");

            // Verify labels/tags are empty
            foreach (var doc in result.Objects)
            {
                if (doc.Labels.Count != 0) return (false, "Expected no labels");
                if (doc.Tags.Count != 0) return (false, "Expected no tags");
            }

            Console.Write($"({sw.Elapsed.TotalMilliseconds:F1}ms) ");
            return (true, null);
        });

        // Enumerate WITH labels/tags (for comparison)
        await RunTest($"TIER-{docCount}", "Enumerate (with labels/tags)", async () =>
        {
            int maxResults = Math.Min(100, docCount);
            var sw = Stopwatch.StartNew();
            var result = await client.Enumerate(new EnumerationQuery
            {
                CollectionId = collection.Id,
                MaxResults = maxResults,
                IncludeLabels = true,
                IncludeTags = true
            });
            sw.Stop();

            if (result == null) return (false, "Enumerate returned null");
            if (result.Objects == null) return (false, "Objects null");

            // Verify at least some documents have labels/tags
            bool foundLabels = result.Objects.Any(d => d.Labels.Count > 0);
            bool foundTags = result.Objects.Any(d => d.Tags.Count > 0);
            if (!foundLabels) return (false, "Expected some documents to have labels");
            if (!foundTags) return (false, "Expected some documents to have tags");

            Console.Write($"({sw.Elapsed.TotalMilliseconds:F1}ms) ");
            return (true, null);
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

    private static async Task<(bool, string?)> TestLargeDocuments()
    {
        string testDir = CreateTestDir();
        try
        {
            using var client = CreateClient(testDir);
            var collection = await client.CreateCollection("LargeDocs");
            if (collection == null) return (false, "Failed to create collection");

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 50; i++)
            {
                var doc = await client.IngestDocument(collection.Id, GenerateLargeJsonDocument(i, 50));
                if (doc == null) return (false, $"Failed to ingest doc {i}");
            }
            sw.Stop();

            var docs = await client.GetDocuments(collection.Id);
            if (docs?.Count != 50) return (false, $"Expected 50, got {docs?.Count}");

            double docsPerSecond = 50.0 / sw.Elapsed.TotalSeconds;
            Console.Write($"({docsPerSecond:F1} docs/sec) ");
            return (true, null);
        }
        finally { CleanupTestDir(testDir); }
    }

    private static async Task<(bool, string?)> TestNestedDocuments()
    {
        string testDir = CreateTestDir();
        try
        {
            using var client = CreateClient(testDir);
            var collection = await client.CreateCollection("NestedDocs");
            if (collection == null) return (false, "Failed to create collection");

            var sw = Stopwatch.StartNew();
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
                var doc = await client.IngestDocument(collection.Id, json);
                if (doc == null) return (false, $"Failed to ingest doc {i}");
            }
            sw.Stop();

            var docs = await client.GetDocuments(collection.Id);
            if (docs?.Count != 50) return (false, $"Expected 50, got {docs?.Count}");

            double docsPerSecond = 50.0 / sw.Elapsed.TotalSeconds;
            Console.Write($"({docsPerSecond:F1} docs/sec) ");
            return (true, null);
        }
        finally { CleanupTestDir(testDir); }
    }

    private static async Task<(bool, string?)> TestArrayDocuments()
    {
        string testDir = CreateTestDir();
        try
        {
            using var client = CreateClient(testDir);
            var collection = await client.CreateCollection("ArrayDocs");
            if (collection == null) return (false, "Failed to create collection");

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 50; i++)
            {
                string json = JsonSerializer.Serialize(new
                {
                    Id = i,
                    Items = Enumerable.Range(0, 20).Select(j => new { Index = j, Value = $"Item_{j}" }).ToArray(),
                    Numbers = Enumerable.Range(0, 50).ToArray(),
                    Tags = Enumerable.Range(0, 10).Select(j => $"tag_{j}").ToArray()
                });
                var doc = await client.IngestDocument(collection.Id, json);
                if (doc == null) return (false, $"Failed to ingest doc {i}");
            }
            sw.Stop();

            var docs = await client.GetDocuments(collection.Id);
            if (docs?.Count != 50) return (false, $"Expected 50, got {docs?.Count}");

            double docsPerSecond = 50.0 / sw.Elapsed.TotalSeconds;
            Console.Write($"({docsPerSecond:F1} docs/sec) ");
            return (true, null);
        }
        finally { CleanupTestDir(testDir); }
    }

    private static async Task<(bool, string?)> TestSchemaReuse()
    {
        string testDir = CreateTestDir();
        try
        {
            using var client = CreateClient(testDir);
            var collection = await client.CreateCollection("SchemaReuse");
            if (collection == null) return (false, "Failed to create collection");

            var schemaIds = new HashSet<string>();
            for (int i = 0; i < 100; i++)
            {
                string json = $@"{{""Name"":""{i}"",""Category"":""Cat"",""Status"":""Active""}}";
                var doc = await client.IngestDocument(collection.Id, json);
                if (doc == null) return (false, $"Failed to ingest doc {i}");
                schemaIds.Add(doc.SchemaId);
            }

            if (schemaIds.Count != 1)
                return (false, $"Expected 1 schema, got {schemaIds.Count}");

            var schemas = await client.GetSchemas();
            if (schemas == null) return (false, "GetSchemas returned null");

            var schemaId = schemaIds.First();
            var schema = await client.GetSchema(schemaId);
            if (schema == null) return (false, "GetSchema returned null");

            var elements = await client.GetSchemaElements(schemaId);
            if (elements == null || elements.Count == 0)
                return (false, "Schema has no elements");

            return (true, null);
        }
        finally { CleanupTestDir(testDir); }
    }

    private static async Task<(bool, string?)> TestDeleteDuringEnumeration()
    {
        string testDir = CreateTestDir();
        try
        {
            using var client = CreateClient(testDir);
            var collection = await client.CreateCollection("DeleteEnum");
            if (collection == null) return (false, "Failed to create collection");

            var docIds = new List<string>();
            for (int i = 0; i < 100; i++)
            {
                var doc = await client.IngestDocument(collection.Id, GenerateJsonDocument(i));
                if (doc == null) return (false, $"Failed to ingest doc {i}");
                docIds.Add(doc.Id);
            }

            var result1 = await client.Enumerate(new EnumerationQuery
            {
                CollectionId = collection.Id,
                MaxResults = 100
            });
            if (result1?.TotalRecords != 100)
                return (false, $"Expected 100, got {result1?.TotalRecords}");

            // Delete 25 documents
            for (int i = 0; i < 25; i++)
            {
                await client.DeleteDocument(docIds[i]);
            }

            var result2 = await client.Enumerate(new EnumerationQuery
            {
                CollectionId = collection.Id,
                MaxResults = 100
            });
            if (result2?.TotalRecords != 75)
                return (false, $"Expected 75 after deletion, got {result2?.TotalRecords}");

            return (true, null);
        }
        finally { CleanupTestDir(testDir); }
    }

    private static async Task<(bool, string?)> TestMultipleCollections()
    {
        string testDir = CreateTestDir();
        try
        {
            using var client = CreateClient(testDir);

            var collections = new List<Collection>();
            for (int c = 0; c < 3; c++)
            {
                var col = await client.CreateCollection($"Collection_{c}");
                if (col == null) return (false, $"Failed to create collection {c}");
                collections.Add(col);
            }

            // Ingest 50 docs into each collection
            for (int c = 0; c < 3; c++)
            {
                for (int i = 0; i < 50; i++)
                {
                    var doc = await client.IngestDocument(collections[c].Id, GenerateJsonDocument(i, $"Col{c}"));
                    if (doc == null) return (false, $"Failed to ingest doc {i} into collection {c}");
                }
            }

            // Verify isolation
            for (int c = 0; c < 3; c++)
            {
                var result = await client.Search(new SearchQuery
                {
                    CollectionId = collections[c].Id,
                    MaxResults = 100
                });
                if (result?.Documents?.Count != 50)
                    return (false, $"Collection {c} has {result?.Documents?.Count} docs, expected 50");

                foreach (var doc in result.Documents)
                {
                    if (doc.CollectionId != collections[c].Id)
                        return (false, $"Document from wrong collection found in {c}");
                }
            }

            return (true, null);
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

    private static LatticeClient CreateClient(string testDir, bool inMemory = true)
    {
        return new LatticeClient(new LatticeSettings
        {
            Database = new Lattice.Core.DatabaseSettings { Filename = Path.Combine(testDir, "lattice.db") },
            DefaultDocumentsDirectory = Path.Combine(testDir, "documents"),
            InMemory = inMemory,
            EnableLogging = false
        });
    }

    private static async Task RunTest(string section, string name, Func<Task<(bool success, string? error)>> testFunc)
    {
        Console.Write($"  [{section}] {name}... ");
        var sw = Stopwatch.StartNew();
        bool passed;
        string? error = null;

        try
        {
            var result = await testFunc();
            passed = result.success;
            error = result.error;
        }
        catch (Exception ex)
        {
            passed = false;
            error = $"Exception: {ex.Message}";
        }

        sw.Stop();

        var testResult = new TestResult
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
        var sections = _results.GroupBy(r => r.Section).OrderBy(g => g.Key);
        foreach (var section in sections)
        {
            int sectionPassed = section.Count(r => r.Passed);
            int sectionTotal = section.Count();
            bool sectionSuccess = sectionPassed == sectionTotal;
            var sectionTime = TimeSpan.FromMilliseconds(section.Sum(r => r.Duration.TotalMilliseconds));

            Console.ForegroundColor = sectionSuccess ? ConsoleColor.Green : ConsoleColor.Red;
            Console.WriteLine($"  {section.Key}: {sectionPassed}/{sectionTotal} [{(sectionSuccess ? "PASS" : "FAIL")}] ({sectionTime.TotalMilliseconds:F0}ms)");
            Console.ResetColor();

            // Show failed tests in this section
            foreach (var failed in section.Where(r => !r.Passed))
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
        var tierResults = _results
            .Where(r => r.Section.StartsWith("TIER-"))
            .ToList();

        if (tierResults.Count == 0)
        {
            Console.WriteLine("  No tier results to display.");
            return;
        }

        // Extract unique tiers in order
        var tiers = tierResults
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
        var testsByName = new Dictionary<string, Dictionary<int, TestResult>>();

        foreach (var result in tierResults)
        {
            int tier = int.Parse(result.Section.Replace("TIER-", ""));
            string normalizedName = NormalizeTestName(result.Name, tier);

            if (!testsByName.ContainsKey(normalizedName))
                testsByName[normalizedName] = new Dictionary<int, TestResult>();

            testsByName[normalizedName][tier] = result;
        }

        // Group tests by category (Ingestion, Retrieval, Search, Enumeration, Validation)
        var categories = new Dictionary<string, List<string>>
        {
            ["INGESTION"] = new List<string>(),
            ["RETRIEVAL"] = new List<string>(),
            ["SEARCH"] = new List<string>(),
            ["ENUMERATION"] = new List<string>()
        };

        foreach (var testName in testsByName.Keys)
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
        foreach (var category in categories.Where(c => c.Value.Count > 0))
        {
            Console.WriteLine();
            Console.WriteLine($"  --- {category.Key} ---");
            Console.WriteLine();

            // Header row
            Console.Write("  ");
            Console.Write("Test".PadRight(testNameWidth));
            Console.Write(" | ");
            foreach (var tier in tiers)
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
            foreach (var testName in category.Value.OrderBy(n => n))
            {
                var tierData = testsByName[testName];

                Console.Write("  ");
                Console.Write(testName.PadRight(testNameWidth));
                Console.Write(" | ");

                foreach (var tier in tiers)
                {
                    if (tierData.TryGetValue(tier, out var result))
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

    private static string GenerateJsonDocument(int index, string? namePrefix = null)
    {
        var doc = new Dictionary<string, object>
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
        var doc = new Dictionary<string, object>
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
