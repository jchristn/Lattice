namespace Test.Automated
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Lattice.Core;
    using Lattice.Core.Models;
    using Lattice.Core.Search;

    class Program
    {
        private static int _PassCount = 0;
        private static int _FailCount = 0;
        private static readonly List<TestResult> _Results = new List<TestResult>();
        private static readonly Stopwatch _OverallStopwatch = new Stopwatch();

        static async Task Main(string[] args)
        {
            Console.WriteLine("===============================================================================");
            Console.WriteLine("  Lattice Automated Test Suite - Comprehensive");
            Console.WriteLine("===============================================================================");
            Console.WriteLine();

            _OverallStopwatch.Start();

            try
            {
                // ===== COLLECTION API TESTS =====
                await RunTestSection("COLLECTION API", async () =>
                {
                    await RunTest("CreateCollection: basic", TestCreateCollectionBasic);
                    await RunTest("CreateCollection: with all parameters", TestCreateCollectionFull);
                    await RunTest("CreateCollection: verify all properties", TestCreateCollectionProperties);
                    await RunTest("GetCollection: existing", TestGetCollectionExisting);
                    await RunTest("GetCollection: non-existent returns null", TestGetCollectionNonExistent);
                    await RunTest("GetCollections: empty", TestGetCollectionsEmpty);
                    await RunTest("GetCollections: multiple", TestGetCollectionsMultiple);
                    await RunTest("CollectionExists: true when exists", TestCollectionExistsTrue);
                    await RunTest("CollectionExists: false when not exists", TestCollectionExistsFalse);
                    await RunTest("DeleteCollection: removes collection", TestDeleteCollection);
                    await RunTest("DeleteCollection: removes associated documents", TestDeleteCollectionRemovesDocs);
                });

                // ===== DOCUMENT API TESTS =====
                await RunTestSection("DOCUMENT API", async () =>
                {
                    await RunTest("IngestDocument: basic", TestIngestDocumentBasic);
                    await RunTest("IngestDocument: with name", TestIngestDocumentWithName);
                    await RunTest("IngestDocument: with labels", TestIngestDocumentWithLabels);
                    await RunTest("IngestDocument: with tags", TestIngestDocumentWithTags);
                    await RunTest("IngestDocument: verify all properties", TestIngestDocumentProperties);
                    await RunTest("IngestDocument: nested JSON", TestIngestDocumentNested);
                    await RunTest("IngestDocument: array JSON", TestIngestDocumentArray);
                    await RunTest("GetDocument: without content", TestGetDocumentWithoutContent);
                    await RunTest("GetDocument: with content", TestGetDocumentWithContent);
                    await RunTest("GetDocument: verify labels populated", TestGetDocumentLabels);
                    await RunTest("GetDocument: verify tags populated", TestGetDocumentTags);
                    await RunTest("GetDocument: non-existent returns null", TestGetDocumentNonExistent);
                    await RunTest("GetDocuments: empty collection", TestGetDocumentsEmpty);
                    await RunTest("GetDocuments: multiple documents", TestGetDocumentsMultiple);
                    await RunTest("GetDocuments: verify labels and tags", TestGetDocumentsWithLabelsAndTags);
                    await RunTest("DocumentExists: true when exists", TestDocumentExistsTrue);
                    await RunTest("DocumentExists: false when not exists", TestDocumentExistsFalse);
                    await RunTest("DeleteDocument: removes document", TestDeleteDocument);
                    await RunTest("DeleteDocument: file removed from disk", TestDeleteDocumentFileRemoved);
                });

                // ===== SEARCH API TESTS =====
                await RunTestSection("SEARCH API", async () =>
                {
                    await RunTest("Search: Equals operator", TestSearchEquals);
                    await RunTest("Search: NotEquals operator", TestSearchNotEquals);
                    await RunTest("Search: GreaterThan operator", TestSearchGreaterThan);
                    await RunTest("Search: GreaterThanOrEqualTo operator", TestSearchGreaterThanOrEqualTo);
                    await RunTest("Search: LessThan operator", TestSearchLessThan);
                    await RunTest("Search: LessThanOrEqualTo operator", TestSearchLessThanOrEqualTo);
                    await RunTest("Search: Contains operator", TestSearchContains);
                    await RunTest("Search: StartsWith operator", TestSearchStartsWith);
                    await RunTest("Search: EndsWith operator", TestSearchEndsWith);
                    await RunTest("Search: IsNull operator", TestSearchIsNull);
                    await RunTest("Search: IsNotNull operator", TestSearchIsNotNull);
                    await RunTest("Search: multiple filters (AND)", TestSearchMultipleFilters);
                    await RunTest("Search: by label", TestSearchByLabel);
                    await RunTest("Search: by tag", TestSearchByTag);
                    await RunTest("Search: by label and tag combined", TestSearchByLabelAndTag);
                    await RunTest("Search: pagination Skip", TestSearchPaginationSkip);
                    await RunTest("Search: pagination MaxResults", TestSearchPaginationMaxResults);
                    await RunTest("Search: pagination Skip and MaxResults", TestSearchPaginationBoth);
                    await RunTest("Search: verify TotalRecords", TestSearchTotalRecords);
                    await RunTest("Search: verify RecordsRemaining", TestSearchRecordsRemaining);
                    await RunTest("Search: verify EndOfResults true", TestSearchEndOfResultsTrue);
                    await RunTest("Search: verify EndOfResults false", TestSearchEndOfResultsFalse);
                    await RunTest("Search: verify Timestamp", TestSearchTimestamp);
                    await RunTest("Search: empty results", TestSearchEmptyResults);
                    await RunTest("Search: with IncludeContent true", TestSearchWithContent);
                    await RunTest("Search: with IncludeContent false", TestSearchWithoutContent);
                    await RunTest("SearchBySql: basic query", TestSearchBySqlBasic);
                    await RunTest("SearchBySql: with ORDER BY", TestSearchBySqlOrderBy);
                    await RunTest("SearchBySql: with LIMIT OFFSET", TestSearchBySqlLimitOffset);
                });

                // ===== ENUMERATION API TESTS =====
                await RunTestSection("ENUMERATION API", async () =>
                {
                    await RunTest("Enumerate: basic", TestEnumerateBasic);
                    await RunTest("Enumerate: with MaxResults", TestEnumerateMaxResults);
                    await RunTest("Enumerate: with Skip", TestEnumerateSkip);
                    await RunTest("Enumerate: pagination", TestEnumeratePagination);
                    await RunTest("Enumerate: CreatedAscending order", TestEnumerateCreatedAsc);
                    await RunTest("Enumerate: CreatedDescending order", TestEnumerateCreatedDesc);
                    await RunTest("Enumerate: verify TotalRecords", TestEnumerateTotalRecords);
                    await RunTest("Enumerate: verify RecordsRemaining", TestEnumerateRecordsRemaining);
                    await RunTest("Enumerate: verify EndOfResults", TestEnumerateEndOfResults);
                    await RunTest("Enumerate: empty collection", TestEnumerateEmpty);
                });

                // ===== SCHEMA API TESTS =====
                await RunTestSection("SCHEMA API", async () =>
                {
                    await RunTest("GetSchemas: returns schemas", TestGetSchemas);
                    await RunTest("GetSchemas: schema reuse for same structure", TestGetSchemasReuse);
                    await RunTest("GetSchema: by id", TestGetSchemaById);
                    await RunTest("GetSchema: non-existent returns null", TestGetSchemaNonExistent);
                    await RunTest("GetSchemaElements: returns elements", TestGetSchemaElements);
                    await RunTest("GetSchemaElements: correct keys", TestGetSchemaElementsKeys);
                    await RunTest("GetSchemaElements: correct types", TestGetSchemaElementsTypes);
                });

                // ===== INDEX API TESTS =====
                await RunTestSection("INDEX API", async () =>
                {
                    await RunTest("GetIndexTableMappings: returns mappings", TestGetIndexTableMappings);
                    await RunTest("GetIndexTableMapping: by key", TestGetIndexTableMappingByKey);
                    await RunTest("GetIndexTableMapping: non-existent returns null", TestGetIndexTableMappingNonExistent);
                });

                // ===== FLUSH API TESTS =====
                await RunTestSection("FLUSH API", async () =>
                {
                    await RunTest("Flush: persists in-memory to disk", TestFlushPersistence);
                });

                // ===== EDGE CASE TESTS =====
                await RunTestSection("EDGE CASES", async () =>
                {
                    await RunTest("Edge: empty string values in JSON", TestEdgeEmptyStrings);
                    await RunTest("Edge: special characters in values", TestEdgeSpecialCharacters);
                    await RunTest("Edge: deeply nested JSON (5 levels)", TestEdgeDeeplyNested);
                    await RunTest("Edge: large array in JSON", TestEdgeLargeArray);
                    await RunTest("Edge: numeric values", TestEdgeNumericValues);
                    await RunTest("Edge: boolean values", TestEdgeBooleanValues);
                    await RunTest("Edge: null values in JSON", TestEdgeNullValues);
                    await RunTest("Edge: unicode characters", TestEdgeUnicodeCharacters);
                });

                // ===== PERFORMANCE TESTS =====
                await RunTestSection("PERFORMANCE", async () =>
                {
                    await RunTest("Perf: ingest 100 documents", TestPerfIngest100);
                    await RunTest("Perf: search in 100 documents", TestPerfSearch100);
                    await RunTest("Perf: GetDocuments for 100 documents", TestPerfGetDocuments100);
                    await RunTest("Perf: enumerate 100 documents", TestPerfEnumerate100);
                });

                // ===== INTEGRATION TESTS =====
                await RunTestSection("INTEGRATION", async () =>
                {
                    await RunTest("Integration: full CRUD pipeline", TestIntegrationFullCrud);
                    await RunTest("Integration: multiple collections", TestIntegrationMultipleCollections);
                    await RunTest("Integration: schema sharing across documents", TestIntegrationSchemaSharing);
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FATAL] Unhandled exception: {ex}");
                _FailCount++;
            }

            _OverallStopwatch.Stop();

            // Print summary
            PrintSummary();

            Environment.ExitCode = _FailCount > 0 ? 1 : 0;
        }

        #region Test Framework

        private class TestResult
        {
            public string Section { get; set; }
            public string Name { get; set; }
            public bool Passed { get; set; }
            public long ElapsedMs { get; set; }
            public string Error { get; set; }
        }

        private static string _CurrentSection = "";

        private static async Task RunTestSection(string sectionName, Func<Task> tests)
        {
            Console.WriteLine();
            Console.WriteLine($"--- {sectionName} ---");
            _CurrentSection = sectionName;
            await tests();
        }

        private static async Task RunTest(string name, Func<Task<(bool success, string error)>> test)
        {
            var sw = Stopwatch.StartNew();
            bool passed = false;
            string error = null;

            try
            {
                var result = await test();
                passed = result.success;
                error = result.error;
            }
            catch (Exception ex)
            {
                passed = false;
                error = ex.Message;
            }

            sw.Stop();

            var testResult = new TestResult
            {
                Section = _CurrentSection,
                Name = name,
                Passed = passed,
                ElapsedMs = sw.ElapsedMilliseconds,
                Error = error
            };
            _Results.Add(testResult);

            if (passed)
            {
                Console.WriteLine($"  [PASS] {name} ({sw.ElapsedMilliseconds}ms)");
                _PassCount++;
            }
            else
            {
                Console.WriteLine($"  [FAIL] {name} ({sw.ElapsedMilliseconds}ms)");
                if (!string.IsNullOrEmpty(error))
                    Console.WriteLine($"         Error: {error}");
                _FailCount++;
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
            var sections = _Results.GroupBy(r => r.Section);
            foreach (var section in sections)
            {
                int sectionPass = section.Count(r => r.Passed);
                int sectionTotal = section.Count();
                string status = sectionPass == sectionTotal ? "PASS" : "FAIL";
                Console.WriteLine($"  {section.Key}: {sectionPass}/{sectionTotal} [{status}]");
            }

            Console.WriteLine();
            Console.WriteLine("-------------------------------------------------------------------------------");
            string overallStatus = _FailCount == 0 ? "PASS" : "FAIL";
            Console.WriteLine($"  TOTAL: {_PassCount} passed, {_FailCount} failed [{overallStatus}]");
            Console.WriteLine($"  RUNTIME: {_OverallStopwatch.ElapsedMilliseconds}ms ({_OverallStopwatch.Elapsed.TotalSeconds:F2}s)");
            Console.WriteLine("-------------------------------------------------------------------------------");

            if (_FailCount > 0)
            {
                Console.WriteLine();
                Console.WriteLine("  FAILED TESTS:");
                foreach (var failed in _Results.Where(r => !r.Passed))
                {
                    Console.WriteLine($"    - {failed.Section}: {failed.Name}");
                    if (!string.IsNullOrEmpty(failed.Error))
                        Console.WriteLine($"      Error: {failed.Error}");
                }
            }

            Console.WriteLine();
        }

        private static string CreateTestDir()
        {
            string dir = Path.Combine(Path.GetTempPath(), "lattice_test_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            return dir;
        }

        private static void CleanupTestDir(string dir)
        {
            try
            {
                if (Directory.Exists(dir))
                    Directory.Delete(dir, true);
            }
            catch { }
        }

        private static LatticeClient CreateClient(string testDir, bool inMemory = true)
        {
            return new LatticeClient(new LatticeSettings
            {
                InMemory = inMemory,
                DefaultDocumentsDirectory = testDir,
                DatabaseFilename = Path.Combine(testDir, "lattice.db")
            });
        }

        #endregion

        #region Collection API Tests

        private static async Task<(bool, string)> TestCreateCollectionBasic()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("TestCollection");

                if (collection == null) return (false, "Collection is null");
                if (collection.Name != "TestCollection") return (false, $"Name mismatch: {collection.Name}");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestCreateCollectionFull()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection(
                    "TestCollection",
                    "Test description",
                    testDir,
                    new List<string> { "label1", "label2" },
                    new Dictionary<string, string> { { "env", "test" }, { "version", "1.0" } }
                );

                if (collection == null) return (false, "Collection is null");
                if (collection.Name != "TestCollection") return (false, "Name mismatch");
                if (collection.Description != "Test description") return (false, "Description mismatch");
                if (collection.DocumentsDirectory != testDir) return (false, "DocumentsDirectory mismatch");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestCreateCollectionProperties()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var before = DateTime.UtcNow.AddSeconds(-1);
                var collection = await client.CreateCollection("TestCollection", "Desc");
                var after = DateTime.UtcNow.AddSeconds(1);

                if (string.IsNullOrEmpty(collection.Id)) return (false, "Id is empty");
                if (!collection.Id.StartsWith("col_")) return (false, $"Id format wrong: {collection.Id}");
                if (collection.Name != "TestCollection") return (false, "Name mismatch");
                if (collection.Description != "Desc") return (false, "Description mismatch");
                if (collection.CreatedUtc < before || collection.CreatedUtc > after)
                    return (false, $"CreatedUtc out of range: {collection.CreatedUtc}");
                if (collection.LastUpdateUtc < before || collection.LastUpdateUtc > after)
                    return (false, $"LastUpdateUtc out of range: {collection.LastUpdateUtc}");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestGetCollectionExisting()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var created = await client.CreateCollection("TestCollection", "Desc");
                var retrieved = await client.GetCollection(created.Id);

                if (retrieved == null) return (false, "Retrieved collection is null");
                if (retrieved.Id != created.Id) return (false, "Id mismatch");
                if (retrieved.Name != created.Name) return (false, "Name mismatch");
                if (retrieved.Description != created.Description) return (false, "Description mismatch");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestGetCollectionNonExistent()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var retrieved = await client.GetCollection("col_nonexistent");

                if (retrieved != null) return (false, "Expected null for non-existent collection");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestGetCollectionsEmpty()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collections = await client.GetCollections();

                if (collections == null) return (false, "Collections is null");
                if (collections.Count != 0) return (false, $"Expected 0 collections, got {collections.Count}");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestGetCollectionsMultiple()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                await client.CreateCollection("Collection1");
                await client.CreateCollection("Collection2");
                await client.CreateCollection("Collection3");

                var collections = await client.GetCollections();

                if (collections.Count != 3) return (false, $"Expected 3 collections, got {collections.Count}");

                var names = collections.Select(c => c.Name).ToHashSet();
                if (!names.Contains("Collection1")) return (false, "Missing Collection1");
                if (!names.Contains("Collection2")) return (false, "Missing Collection2");
                if (!names.Contains("Collection3")) return (false, "Missing Collection3");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestCollectionExistsTrue()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("TestCollection");
                bool exists = await client.CollectionExists(collection.Id);

                if (!exists) return (false, "Expected true");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestCollectionExistsFalse()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                bool exists = await client.CollectionExists("col_nonexistent");

                if (exists) return (false, "Expected false");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestDeleteCollection()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("TestCollection");
                await client.DeleteCollection(collection.Id);

                bool exists = await client.CollectionExists(collection.Id);
                if (exists) return (false, "Collection still exists after delete");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestDeleteCollectionRemovesDocs()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("TestCollection");
                var doc = await client.IngestDocument(collection.Id, @"{""Name"":""Test""}");

                await client.DeleteCollection(collection.Id);

                bool docExists = await client.DocumentExists(doc.Id);
                if (docExists) return (false, "Document still exists after collection delete");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        #endregion

        #region Document API Tests

        private static async Task<(bool, string)> TestIngestDocumentBasic()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("TestCollection");
                var doc = await client.IngestDocument(collection.Id, @"{""Name"":""Joel""}");

                if (doc == null) return (false, "Document is null");
                if (string.IsNullOrEmpty(doc.Id)) return (false, "Document Id is empty");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestIngestDocumentWithName()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("TestCollection");
                var doc = await client.IngestDocument(collection.Id, @"{""Data"":""test""}", "MyDocument");

                if (doc.Name != "MyDocument") return (false, $"Name mismatch: {doc.Name}");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestIngestDocumentWithLabels()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("TestCollection");
                var labels = new List<string> { "important", "reviewed" };
                var doc = await client.IngestDocument(collection.Id, @"{""Data"":""test""}", labels: labels);

                if (doc.Labels.Count != 2) return (false, $"Expected 2 labels, got {doc.Labels.Count}");
                if (!doc.Labels.Contains("important")) return (false, "Missing 'important' label");
                if (!doc.Labels.Contains("reviewed")) return (false, "Missing 'reviewed' label");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestIngestDocumentWithTags()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("TestCollection");
                var tags = new Dictionary<string, string> { { "author", "Joel" }, { "status", "draft" } };
                var doc = await client.IngestDocument(collection.Id, @"{""Data"":""test""}", tags: tags);

                if (doc.Tags.Count != 2) return (false, $"Expected 2 tags, got {doc.Tags.Count}");
                if (!doc.Tags.ContainsKey("author") || doc.Tags["author"] != "Joel")
                    return (false, "Missing or wrong 'author' tag");
                if (!doc.Tags.ContainsKey("status") || doc.Tags["status"] != "draft")
                    return (false, "Missing or wrong 'status' tag");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestIngestDocumentProperties()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("TestCollection");
                var before = DateTime.UtcNow.AddSeconds(-1);
                var doc = await client.IngestDocument(collection.Id, @"{""Name"":""Joel""}");
                var after = DateTime.UtcNow.AddSeconds(1);

                if (!doc.Id.StartsWith("doc_")) return (false, $"Id format wrong: {doc.Id}");
                if (doc.CollectionId != collection.Id) return (false, "CollectionId mismatch");
                if (string.IsNullOrEmpty(doc.SchemaId)) return (false, "SchemaId is empty");
                if (doc.CreatedUtc < before || doc.CreatedUtc > after)
                    return (false, $"CreatedUtc out of range");
                if (doc.LastUpdateUtc < before || doc.LastUpdateUtc > after)
                    return (false, $"LastUpdateUtc out of range");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestIngestDocumentNested()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("TestCollection");
                string json = @"{""Person"":{""Name"":{""First"":""Joel"",""Last"":""Christner""},""Age"":40}}";
                var doc = await client.IngestDocument(collection.Id, json);

                // Verify we can search on nested fields
                var result = await client.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Filters = new List<SearchFilter>
                    {
                        new SearchFilter("Person.Name.First", SearchConditionEnum.Equals, "Joel")
                    }
                });

                if (result.Documents.Count != 1) return (false, "Could not search nested field");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestIngestDocumentArray()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("TestCollection");
                string json = @"{""Tags"":[""red"",""green"",""blue""]}";
                var doc = await client.IngestDocument(collection.Id, json);

                // Verify we can search array elements
                var result = await client.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Filters = new List<SearchFilter>
                    {
                        new SearchFilter("Tags", SearchConditionEnum.Equals, "green")
                    }
                });

                if (result.Documents.Count != 1) return (false, "Could not search array element");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestGetDocumentWithoutContent()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("TestCollection");
                var ingested = await client.IngestDocument(collection.Id, @"{""Name"":""Joel""}");
                var retrieved = await client.GetDocument(ingested.Id, includeContent: false);

                if (retrieved == null) return (false, "Retrieved document is null");
                if (retrieved.Content != null) return (false, "Content should be null");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestGetDocumentWithContent()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("TestCollection");
                var ingested = await client.IngestDocument(collection.Id, @"{""Name"":""Joel""}");
                var retrieved = await client.GetDocument(ingested.Id, includeContent: true);

                if (retrieved.Content == null) return (false, "Content is null");
                if (!retrieved.Content.Contains("Joel")) return (false, "Content doesn't contain expected value");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestGetDocumentLabels()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("TestCollection");
                var labels = new List<string> { "label1", "label2" };
                var ingested = await client.IngestDocument(collection.Id, @"{""Data"":""test""}", labels: labels);
                var retrieved = await client.GetDocument(ingested.Id);

                if (retrieved.Labels.Count != 2) return (false, $"Expected 2 labels, got {retrieved.Labels.Count}");
                if (!retrieved.Labels.Contains("label1")) return (false, "Missing label1");
                if (!retrieved.Labels.Contains("label2")) return (false, "Missing label2");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestGetDocumentTags()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("TestCollection");
                var tags = new Dictionary<string, string> { { "key1", "val1" }, { "key2", "val2" } };
                var ingested = await client.IngestDocument(collection.Id, @"{""Data"":""test""}", tags: tags);
                var retrieved = await client.GetDocument(ingested.Id);

                if (retrieved.Tags.Count != 2) return (false, $"Expected 2 tags, got {retrieved.Tags.Count}");
                if (retrieved.Tags["key1"] != "val1") return (false, "Wrong value for key1");
                if (retrieved.Tags["key2"] != "val2") return (false, "Wrong value for key2");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestGetDocumentNonExistent()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var retrieved = await client.GetDocument("doc_nonexistent");

                if (retrieved != null) return (false, "Expected null");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestGetDocumentsEmpty()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("TestCollection");
                var docs = await client.GetDocuments(collection.Id);

                if (docs.Count != 0) return (false, $"Expected 0, got {docs.Count}");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestGetDocumentsMultiple()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("TestCollection");
                await client.IngestDocument(collection.Id, @"{""Name"":""Doc1""}");
                await client.IngestDocument(collection.Id, @"{""Name"":""Doc2""}");
                await client.IngestDocument(collection.Id, @"{""Name"":""Doc3""}");

                var docs = await client.GetDocuments(collection.Id);

                if (docs.Count != 3) return (false, $"Expected 3, got {docs.Count}");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestGetDocumentsWithLabelsAndTags()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("TestCollection");
                await client.IngestDocument(collection.Id, @"{""N"":1}",
                    labels: new List<string> { "a" },
                    tags: new Dictionary<string, string> { { "k", "v" } });

                var docs = await client.GetDocuments(collection.Id);

                if (docs[0].Labels.Count != 1) return (false, "Labels not populated");
                if (docs[0].Tags.Count != 1) return (false, "Tags not populated");
                if (!docs[0].Labels.Contains("a")) return (false, "Wrong label");
                if (docs[0].Tags["k"] != "v") return (false, "Wrong tag");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestDocumentExistsTrue()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("TestCollection");
                var doc = await client.IngestDocument(collection.Id, @"{""Data"":1}");

                bool exists = await client.DocumentExists(doc.Id);
                if (!exists) return (false, "Expected true");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestDocumentExistsFalse()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                bool exists = await client.DocumentExists("doc_nonexistent");

                if (exists) return (false, "Expected false");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestDeleteDocument()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("TestCollection");
                var doc = await client.IngestDocument(collection.Id, @"{""Data"":1}");

                await client.DeleteDocument(doc.Id);

                bool exists = await client.DocumentExists(doc.Id);
                if (exists) return (false, "Document still exists");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestDeleteDocumentFileRemoved()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("TestCollection");
                var doc = await client.IngestDocument(collection.Id, @"{""Data"":1}");

                // File is stored in collection's DocumentsDirectory, not testDir directly
                string filePath = Path.Combine(collection.DocumentsDirectory, $"{doc.Id}.json");
                if (!File.Exists(filePath)) return (false, "File wasn't created");

                await client.DeleteDocument(doc.Id);

                if (File.Exists(filePath)) return (false, "File still exists after delete");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        #endregion

        #region Search API Tests

        private static async Task<(bool, string)> TestSearchEquals()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("Test");
                await client.IngestDocument(collection.Id, @"{""Name"":""Joel""}");
                await client.IngestDocument(collection.Id, @"{""Name"":""John""}");
                await client.IngestDocument(collection.Id, @"{""Name"":""Joel""}");

                var result = await client.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Filters = new List<SearchFilter>
                    {
                        new SearchFilter("Name", SearchConditionEnum.Equals, "Joel")
                    }
                });

                if (result.Documents.Count != 2) return (false, $"Expected 2, got {result.Documents.Count}");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestSearchNotEquals()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("Test");
                await client.IngestDocument(collection.Id, @"{""Name"":""Joel""}");
                await client.IngestDocument(collection.Id, @"{""Name"":""John""}");
                await client.IngestDocument(collection.Id, @"{""Name"":""Jane""}");

                var result = await client.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Filters = new List<SearchFilter>
                    {
                        new SearchFilter("Name", SearchConditionEnum.NotEquals, "Joel")
                    }
                });

                if (result.Documents.Count != 2) return (false, $"Expected 2, got {result.Documents.Count}");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestSearchGreaterThan()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("Test");
                await client.IngestDocument(collection.Id, @"{""Age"":25}");
                await client.IngestDocument(collection.Id, @"{""Age"":30}");
                await client.IngestDocument(collection.Id, @"{""Age"":35}");

                var result = await client.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Filters = new List<SearchFilter>
                    {
                        new SearchFilter("Age", SearchConditionEnum.GreaterThan, "30")
                    }
                });

                if (result.Documents.Count != 1) return (false, $"Expected 1, got {result.Documents.Count}");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestSearchGreaterThanOrEqualTo()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("Test");
                await client.IngestDocument(collection.Id, @"{""Age"":25}");
                await client.IngestDocument(collection.Id, @"{""Age"":30}");
                await client.IngestDocument(collection.Id, @"{""Age"":35}");

                var result = await client.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Filters = new List<SearchFilter>
                    {
                        new SearchFilter("Age", SearchConditionEnum.GreaterThanOrEqualTo, "30")
                    }
                });

                if (result.Documents.Count != 2) return (false, $"Expected 2, got {result.Documents.Count}");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestSearchLessThan()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("Test");
                await client.IngestDocument(collection.Id, @"{""Age"":25}");
                await client.IngestDocument(collection.Id, @"{""Age"":30}");
                await client.IngestDocument(collection.Id, @"{""Age"":35}");

                var result = await client.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Filters = new List<SearchFilter>
                    {
                        new SearchFilter("Age", SearchConditionEnum.LessThan, "30")
                    }
                });

                if (result.Documents.Count != 1) return (false, $"Expected 1, got {result.Documents.Count}");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestSearchLessThanOrEqualTo()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("Test");
                await client.IngestDocument(collection.Id, @"{""Age"":25}");
                await client.IngestDocument(collection.Id, @"{""Age"":30}");
                await client.IngestDocument(collection.Id, @"{""Age"":35}");

                var result = await client.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Filters = new List<SearchFilter>
                    {
                        new SearchFilter("Age", SearchConditionEnum.LessThanOrEqualTo, "30")
                    }
                });

                if (result.Documents.Count != 2) return (false, $"Expected 2, got {result.Documents.Count}");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestSearchContains()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("Test");
                await client.IngestDocument(collection.Id, @"{""City"":""San Francisco""}");
                await client.IngestDocument(collection.Id, @"{""City"":""San Diego""}");
                await client.IngestDocument(collection.Id, @"{""City"":""New York""}");

                var result = await client.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Filters = new List<SearchFilter>
                    {
                        new SearchFilter("City", SearchConditionEnum.Contains, "San")
                    }
                });

                if (result.Documents.Count != 2) return (false, $"Expected 2, got {result.Documents.Count}");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestSearchStartsWith()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("Test");
                await client.IngestDocument(collection.Id, @"{""Name"":""Johnson""}");
                await client.IngestDocument(collection.Id, @"{""Name"":""John""}");
                await client.IngestDocument(collection.Id, @"{""Name"":""James""}");

                var result = await client.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Filters = new List<SearchFilter>
                    {
                        new SearchFilter("Name", SearchConditionEnum.StartsWith, "John")
                    }
                });

                if (result.Documents.Count != 2) return (false, $"Expected 2, got {result.Documents.Count}");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestSearchEndsWith()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("Test");
                await client.IngestDocument(collection.Id, @"{""Email"":""joel@test.com""}");
                await client.IngestDocument(collection.Id, @"{""Email"":""john@test.org""}");
                await client.IngestDocument(collection.Id, @"{""Email"":""jane@test.com""}");

                var result = await client.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Filters = new List<SearchFilter>
                    {
                        new SearchFilter("Email", SearchConditionEnum.EndsWith, ".com")
                    }
                });

                if (result.Documents.Count != 2) return (false, $"Expected 2, got {result.Documents.Count}");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestSearchIsNull()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("Test");
                await client.IngestDocument(collection.Id, @"{""Name"":""Joel"",""Email"":null}");
                await client.IngestDocument(collection.Id, @"{""Name"":""John"",""Email"":""john@test.com""}");

                var result = await client.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Filters = new List<SearchFilter>
                    {
                        new SearchFilter("Email", SearchConditionEnum.IsNull, null)
                    }
                });

                if (result.Documents.Count != 1) return (false, $"Expected 1, got {result.Documents.Count}");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestSearchIsNotNull()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("Test");
                await client.IngestDocument(collection.Id, @"{""Name"":""Joel"",""Email"":null}");
                await client.IngestDocument(collection.Id, @"{""Name"":""John"",""Email"":""john@test.com""}");

                var result = await client.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Filters = new List<SearchFilter>
                    {
                        new SearchFilter("Email", SearchConditionEnum.IsNotNull, null)
                    }
                });

                if (result.Documents.Count != 1) return (false, $"Expected 1, got {result.Documents.Count}");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestSearchMultipleFilters()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("Test");
                await client.IngestDocument(collection.Id, @"{""City"":""Austin"",""Age"":30}");
                await client.IngestDocument(collection.Id, @"{""City"":""Austin"",""Age"":40}");
                await client.IngestDocument(collection.Id, @"{""City"":""Denver"",""Age"":30}");

                var result = await client.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Filters = new List<SearchFilter>
                    {
                        new SearchFilter("City", SearchConditionEnum.Equals, "Austin"),
                        new SearchFilter("Age", SearchConditionEnum.GreaterThan, "35")
                    }
                });

                if (result.Documents.Count != 1) return (false, $"Expected 1, got {result.Documents.Count}");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestSearchByLabel()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("Test");
                await client.IngestDocument(collection.Id, @"{""N"":1}", labels: new List<string> { "important" });
                await client.IngestDocument(collection.Id, @"{""N"":2}", labels: new List<string> { "normal" });
                await client.IngestDocument(collection.Id, @"{""N"":3}", labels: new List<string> { "important" });

                var result = await client.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Labels = new List<string> { "important" }
                });

                if (result.Documents.Count != 2) return (false, $"Expected 2, got {result.Documents.Count}");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestSearchByTag()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("Test");
                await client.IngestDocument(collection.Id, @"{""N"":1}",
                    tags: new Dictionary<string, string> { { "status", "active" } });
                await client.IngestDocument(collection.Id, @"{""N"":2}",
                    tags: new Dictionary<string, string> { { "status", "inactive" } });
                await client.IngestDocument(collection.Id, @"{""N"":3}",
                    tags: new Dictionary<string, string> { { "status", "active" } });

                var result = await client.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Tags = new Dictionary<string, string> { { "status", "active" } }
                });

                if (result.Documents.Count != 2) return (false, $"Expected 2, got {result.Documents.Count}");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestSearchByLabelAndTag()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("Test");
                await client.IngestDocument(collection.Id, @"{""N"":1}",
                    labels: new List<string> { "important" },
                    tags: new Dictionary<string, string> { { "status", "active" } });
                await client.IngestDocument(collection.Id, @"{""N"":2}",
                    labels: new List<string> { "important" },
                    tags: new Dictionary<string, string> { { "status", "inactive" } });
                await client.IngestDocument(collection.Id, @"{""N"":3}",
                    labels: new List<string> { "normal" },
                    tags: new Dictionary<string, string> { { "status", "active" } });

                var result = await client.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Labels = new List<string> { "important" },
                    Tags = new Dictionary<string, string> { { "status", "active" } }
                });

                if (result.Documents.Count != 1) return (false, $"Expected 1, got {result.Documents.Count}");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestSearchPaginationSkip()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("Test");
                for (int i = 0; i < 10; i++)
                    await client.IngestDocument(collection.Id, $@"{{""N"":{i}}}");

                var result = await client.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Skip = 5
                });

                if (result.Documents.Count != 5) return (false, $"Expected 5, got {result.Documents.Count}");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestSearchPaginationMaxResults()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("Test");
                for (int i = 0; i < 10; i++)
                    await client.IngestDocument(collection.Id, $@"{{""N"":{i}}}");

                var result = await client.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    MaxResults = 3
                });

                if (result.Documents.Count != 3) return (false, $"Expected 3, got {result.Documents.Count}");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestSearchPaginationBoth()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("Test");
                for (int i = 0; i < 10; i++)
                    await client.IngestDocument(collection.Id, $@"{{""N"":{i}}}");

                var result = await client.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Skip = 3,
                    MaxResults = 4
                });

                if (result.Documents.Count != 4) return (false, $"Expected 4, got {result.Documents.Count}");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestSearchTotalRecords()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("Test");
                for (int i = 0; i < 10; i++)
                    await client.IngestDocument(collection.Id, $@"{{""N"":{i}}}");

                var result = await client.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    MaxResults = 3
                });

                if (result.TotalRecords != 10) return (false, $"Expected TotalRecords=10, got {result.TotalRecords}");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestSearchRecordsRemaining()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("Test");
                for (int i = 0; i < 10; i++)
                    await client.IngestDocument(collection.Id, $@"{{""N"":{i}}}");

                var result = await client.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Skip = 2,
                    MaxResults = 3
                });

                // Total=10, Skip=2, Returned=3, Remaining=10-2-3=5
                if (result.RecordsRemaining != 5) return (false, $"Expected RecordsRemaining=5, got {result.RecordsRemaining}");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestSearchEndOfResultsTrue()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("Test");
                for (int i = 0; i < 5; i++)
                    await client.IngestDocument(collection.Id, $@"{{""N"":{i}}}");

                var result = await client.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    MaxResults = 10
                });

                if (!result.EndOfResults) return (false, "Expected EndOfResults=true");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestSearchEndOfResultsFalse()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("Test");
                for (int i = 0; i < 10; i++)
                    await client.IngestDocument(collection.Id, $@"{{""N"":{i}}}");

                var result = await client.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    MaxResults = 5
                });

                if (result.EndOfResults) return (false, "Expected EndOfResults=false");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestSearchTimestamp()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("Test");
                await client.IngestDocument(collection.Id, @"{""N"":1}");

                var before = DateTime.UtcNow.AddSeconds(-1);
                var result = await client.Search(new SearchQuery { CollectionId = collection.Id });
                var after = DateTime.UtcNow.AddSeconds(1);

                if (result.Timestamp == null) return (false, "Timestamp is null");
                if (result.Timestamp.Start < before || result.Timestamp.Start > after)
                    return (false, "Timestamp.Start out of range");
                if (result.Timestamp.End < result.Timestamp.Start)
                    return (false, "Timestamp.End < Timestamp.Start");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestSearchEmptyResults()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("Test");
                await client.IngestDocument(collection.Id, @"{""Name"":""Joel""}");

                var result = await client.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Filters = new List<SearchFilter>
                    {
                        new SearchFilter("Name", SearchConditionEnum.Equals, "NonExistent")
                    }
                });

                if (result.Documents.Count != 0) return (false, $"Expected 0, got {result.Documents.Count}");
                if (result.TotalRecords != 0) return (false, $"Expected TotalRecords=0");
                if (!result.EndOfResults) return (false, "Expected EndOfResults=true");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestSearchWithContent()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("Test");
                await client.IngestDocument(collection.Id, @"{""Name"":""Joel""}");

                var result = await client.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    IncludeContent = true
                });

                if (result.Documents[0].Content == null) return (false, "Content is null");
                if (!result.Documents[0].Content.Contains("Joel")) return (false, "Content missing expected value");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestSearchWithoutContent()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("Test");
                await client.IngestDocument(collection.Id, @"{""Name"":""Joel""}");

                var result = await client.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    IncludeContent = false
                });

                if (result.Documents[0].Content != null) return (false, "Content should be null");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestSearchBySqlBasic()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("Test");
                await client.IngestDocument(collection.Id, @"{""Name"":""Joel""}");
                await client.IngestDocument(collection.Id, @"{""Name"":""John""}");

                var result = await client.SearchBySql(collection.Id, "SELECT * FROM documents WHERE Name = 'Joel'");

                if (result.Documents.Count != 1) return (false, $"Expected 1, got {result.Documents.Count}");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestSearchBySqlOrderBy()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("Test");
                await client.IngestDocument(collection.Id, @"{""N"":1}");
                await Task.Delay(10);
                await client.IngestDocument(collection.Id, @"{""N"":2}");

                var result = await client.SearchBySql(collection.Id, "SELECT * FROM documents ORDER BY createdutc ASC");

                if (result.Documents.Count != 2) return (false, $"Expected 2, got {result.Documents.Count}");
                // First doc should be older
                if (result.Documents[0].CreatedUtc > result.Documents[1].CreatedUtc)
                    return (false, "Order incorrect");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestSearchBySqlLimitOffset()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("Test");
                for (int i = 0; i < 10; i++)
                    await client.IngestDocument(collection.Id, $@"{{""N"":{i}}}");

                var result = await client.SearchBySql(collection.Id, "SELECT * FROM documents LIMIT 3 OFFSET 2");

                if (result.Documents.Count != 3) return (false, $"Expected 3, got {result.Documents.Count}");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        #endregion

        #region Enumeration API Tests

        private static async Task<(bool, string)> TestEnumerateBasic()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("Test");
                await client.IngestDocument(collection.Id, @"{""N"":1}");
                await client.IngestDocument(collection.Id, @"{""N"":2}");

                var result = await client.Enumerate(new EnumerationQuery { CollectionId = collection.Id });

                if (result.Objects.Count != 2) return (false, $"Expected 2, got {result.Objects.Count}");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestEnumerateMaxResults()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("Test");
                for (int i = 0; i < 10; i++)
                    await client.IngestDocument(collection.Id, $@"{{""N"":{i}}}");

                var result = await client.Enumerate(new EnumerationQuery
                {
                    CollectionId = collection.Id,
                    MaxResults = 5
                });

                if (result.Objects.Count != 5) return (false, $"Expected 5, got {result.Objects.Count}");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestEnumerateSkip()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("Test");
                for (int i = 0; i < 10; i++)
                    await client.IngestDocument(collection.Id, $@"{{""N"":{i}}}");

                var result = await client.Enumerate(new EnumerationQuery
                {
                    CollectionId = collection.Id,
                    Skip = 7
                });

                if (result.Objects.Count != 3) return (false, $"Expected 3, got {result.Objects.Count}");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestEnumeratePagination()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("Test");
                for (int i = 0; i < 10; i++)
                    await client.IngestDocument(collection.Id, $@"{{""N"":{i}}}");

                // Page 1
                var page1 = await client.Enumerate(new EnumerationQuery
                {
                    CollectionId = collection.Id,
                    Skip = 0,
                    MaxResults = 3
                });

                // Page 2
                var page2 = await client.Enumerate(new EnumerationQuery
                {
                    CollectionId = collection.Id,
                    Skip = 3,
                    MaxResults = 3
                });

                if (page1.Objects.Count != 3) return (false, "Page1 count wrong");
                if (page2.Objects.Count != 3) return (false, "Page2 count wrong");

                // Ensure no overlap
                var page1Ids = page1.Objects.Select(d => d.Id).ToHashSet();
                var page2Ids = page2.Objects.Select(d => d.Id).ToHashSet();
                if (page1Ids.Intersect(page2Ids).Any()) return (false, "Pages overlap");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestEnumerateCreatedAsc()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("Test");
                await client.IngestDocument(collection.Id, @"{""N"":1}");
                await Task.Delay(10);
                await client.IngestDocument(collection.Id, @"{""N"":2}");

                var result = await client.Enumerate(new EnumerationQuery
                {
                    CollectionId = collection.Id,
                    Ordering = EnumerationOrderEnum.CreatedAscending
                });

                if (result.Objects[0].CreatedUtc > result.Objects[1].CreatedUtc)
                    return (false, "Order incorrect");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestEnumerateCreatedDesc()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("Test");
                await client.IngestDocument(collection.Id, @"{""N"":1}");
                await Task.Delay(10);
                await client.IngestDocument(collection.Id, @"{""N"":2}");

                var result = await client.Enumerate(new EnumerationQuery
                {
                    CollectionId = collection.Id,
                    Ordering = EnumerationOrderEnum.CreatedDescending
                });

                if (result.Objects[0].CreatedUtc < result.Objects[1].CreatedUtc)
                    return (false, "Order incorrect");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestEnumerateTotalRecords()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("Test");
                for (int i = 0; i < 10; i++)
                    await client.IngestDocument(collection.Id, $@"{{""N"":{i}}}");

                var result = await client.Enumerate(new EnumerationQuery
                {
                    CollectionId = collection.Id,
                    MaxResults = 3
                });

                if (result.TotalRecords != 10) return (false, $"Expected 10, got {result.TotalRecords}");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestEnumerateRecordsRemaining()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("Test");
                for (int i = 0; i < 10; i++)
                    await client.IngestDocument(collection.Id, $@"{{""N"":{i}}}");

                var result = await client.Enumerate(new EnumerationQuery
                {
                    CollectionId = collection.Id,
                    Skip = 2,
                    MaxResults = 3
                });

                if (result.RecordsRemaining != 5) return (false, $"Expected 5, got {result.RecordsRemaining}");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestEnumerateEndOfResults()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("Test");
                for (int i = 0; i < 5; i++)
                    await client.IngestDocument(collection.Id, $@"{{""N"":{i}}}");

                var result = await client.Enumerate(new EnumerationQuery
                {
                    CollectionId = collection.Id,
                    MaxResults = 10
                });

                if (!result.EndOfResults) return (false, "Expected EndOfResults=true");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestEnumerateEmpty()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("Test");

                var result = await client.Enumerate(new EnumerationQuery { CollectionId = collection.Id });

                if (result.Objects.Count != 0) return (false, $"Expected 0, got {result.Objects.Count}");
                if (result.TotalRecords != 0) return (false, "Expected TotalRecords=0");
                if (!result.EndOfResults) return (false, "Expected EndOfResults=true");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        #endregion

        #region Schema API Tests

        private static async Task<(bool, string)> TestGetSchemas()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("Test");
                await client.IngestDocument(collection.Id, @"{""Name"":""Joel""}");

                var schemas = await client.GetSchemas();

                if (schemas.Count < 1) return (false, "Expected at least 1 schema");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestGetSchemasReuse()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("Test");

                // Same structure = same schema
                var doc1 = await client.IngestDocument(collection.Id, @"{""Name"":""Joel""}");
                var doc2 = await client.IngestDocument(collection.Id, @"{""Name"":""John""}");

                if (doc1.SchemaId != doc2.SchemaId)
                    return (false, "Expected same schema for same structure");

                // Different structure = different schema
                var doc3 = await client.IngestDocument(collection.Id, @"{""Age"":30}");

                if (doc1.SchemaId == doc3.SchemaId)
                    return (false, "Expected different schema for different structure");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestGetSchemaById()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("Test");
                var doc = await client.IngestDocument(collection.Id, @"{""Name"":""Joel""}");

                var schema = await client.GetSchema(doc.SchemaId);

                if (schema == null) return (false, "Schema is null");
                if (schema.Id != doc.SchemaId) return (false, "Id mismatch");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestGetSchemaNonExistent()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var schema = await client.GetSchema("sch_nonexistent");

                if (schema != null) return (false, "Expected null");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestGetSchemaElements()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("Test");
                var doc = await client.IngestDocument(collection.Id, @"{""Name"":""Joel"",""Age"":30}");

                var elements = await client.GetSchemaElements(doc.SchemaId);

                if (elements.Count != 2) return (false, $"Expected 2 elements, got {elements.Count}");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestGetSchemaElementsKeys()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("Test");
                var doc = await client.IngestDocument(collection.Id, @"{""FirstName"":""Joel"",""LastName"":""C""}");

                var elements = await client.GetSchemaElements(doc.SchemaId);
                var keys = elements.Select(e => e.Key).ToHashSet();

                if (!keys.Contains("FirstName")) return (false, "Missing FirstName");
                if (!keys.Contains("LastName")) return (false, "Missing LastName");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestGetSchemaElementsTypes()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("Test");
                var doc = await client.IngestDocument(collection.Id, @"{""Name"":""Joel"",""Age"":30,""Active"":true}");

                var elements = await client.GetSchemaElements(doc.SchemaId);
                var typesByKey = elements.ToDictionary(e => e.Key, e => e.DataType);

                if (!typesByKey["Name"].Contains("string")) return (false, $"Name type wrong: {typesByKey["Name"]}");
                if (!typesByKey["Age"].Contains("int") && !typesByKey["Age"].Contains("number"))
                    return (false, $"Age type wrong: {typesByKey["Age"]}");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        #endregion

        #region Index API Tests

        private static async Task<(bool, string)> TestGetIndexTableMappings()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("Test");
                await client.IngestDocument(collection.Id, @"{""Name"":""Joel"",""Age"":30}");

                var mappings = await client.GetIndexTableMappings();

                if (mappings.Count < 2) return (false, $"Expected at least 2 mappings, got {mappings.Count}");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestGetIndexTableMappingByKey()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("Test");
                await client.IngestDocument(collection.Id, @"{""City"":""Austin""}");

                var mapping = await client.GetIndexTableMapping("City");

                if (mapping == null) return (false, "Mapping is null");
                if (mapping.Key != "City") return (false, $"Key mismatch: {mapping.Key}");
                if (string.IsNullOrEmpty(mapping.TableName)) return (false, "TableName is empty");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestGetIndexTableMappingNonExistent()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var mapping = await client.GetIndexTableMapping("NonExistentKey");

                if (mapping != null) return (false, "Expected null");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        #endregion

        #region Flush API Tests

        private static async Task<(bool, string)> TestFlushPersistence()
        {
            string testDir = CreateTestDir();
            try
            {
                string dbPath = Path.Combine(testDir, "lattice.db");

                // Create in-memory, add data, flush
                using (var client = new LatticeClient(new LatticeSettings
                {
                    InMemory = true,
                    DefaultDocumentsDirectory = testDir,
                    DatabaseFilename = dbPath
                }))
                {
                    var collection = await client.CreateCollection("Test");
                    await client.IngestDocument(collection.Id, @"{""Name"":""Joel""}");
                    client.Flush();
                }

                // Verify file was created
                if (!File.Exists(dbPath)) return (false, "Database file not created");

                // Open from disk and verify data
                using (var client = new LatticeClient(new LatticeSettings
                {
                    InMemory = false,
                    DefaultDocumentsDirectory = testDir,
                    DatabaseFilename = dbPath
                }))
                {
                    var collections = await client.GetCollections();
                    if (collections.Count != 1) return (false, "Data not persisted");
                }

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        #endregion

        #region Edge Case Tests

        private static async Task<(bool, string)> TestEdgeEmptyStrings()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("Test");
                var doc = await client.IngestDocument(collection.Id, @"{""Name"":""""}");

                var result = await client.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Filters = new List<SearchFilter>
                    {
                        new SearchFilter("Name", SearchConditionEnum.Equals, "")
                    }
                });

                if (result.Documents.Count != 1) return (false, $"Expected 1, got {result.Documents.Count}");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestEdgeSpecialCharacters()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("Test");
                string json = @"{""Text"":""Hello 'World' with \""quotes\"" and <special> chars & more""}";
                var doc = await client.IngestDocument(collection.Id, json);

                var retrieved = await client.GetDocument(doc.Id, includeContent: true);
                if (!retrieved.Content.Contains("Hello")) return (false, "Content corrupted");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestEdgeDeeplyNested()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("Test");
                string json = @"{""L1"":{""L2"":{""L3"":{""L4"":{""L5"":""DeepValue""}}}}}";
                var doc = await client.IngestDocument(collection.Id, json);

                var result = await client.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Filters = new List<SearchFilter>
                    {
                        new SearchFilter("L1.L2.L3.L4.L5", SearchConditionEnum.Equals, "DeepValue")
                    }
                });

                if (result.Documents.Count != 1) return (false, "Deep search failed");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestEdgeLargeArray()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("Test");

                var items = Enumerable.Range(0, 100).Select(i => $"\"{i}\"");
                string json = $@"{{""Items"":[{string.Join(",", items)}]}}";
                var doc = await client.IngestDocument(collection.Id, json);

                var result = await client.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Filters = new List<SearchFilter>
                    {
                        new SearchFilter("Items", SearchConditionEnum.Equals, "50")
                    }
                });

                if (result.Documents.Count != 1) return (false, "Array search failed");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestEdgeNumericValues()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("Test");
                string json = @"{""Int"":42,""Float"":3.14,""Negative"":-100,""Large"":9999999999}";
                var doc = await client.IngestDocument(collection.Id, json);

                var result = await client.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Filters = new List<SearchFilter>
                    {
                        new SearchFilter("Int", SearchConditionEnum.Equals, "42")
                    }
                });

                if (result.Documents.Count != 1) return (false, "Numeric search failed");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestEdgeBooleanValues()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("Test");
                await client.IngestDocument(collection.Id, @"{""Active"":true}");
                await client.IngestDocument(collection.Id, @"{""Active"":false}");

                // Boolean values are stored as lowercase "true"/"false" per JSON convention
                var result = await client.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Filters = new List<SearchFilter>
                    {
                        new SearchFilter("Active", SearchConditionEnum.Equals, "true")
                    }
                });

                if (result.Documents.Count != 1) return (false, $"Expected 1, got {result.Documents.Count}");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestEdgeNullValues()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("Test");
                var doc = await client.IngestDocument(collection.Id, @"{""Name"":null,""Age"":30}");

                var result = await client.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Filters = new List<SearchFilter>
                    {
                        new SearchFilter("Name", SearchConditionEnum.IsNull, null)
                    }
                });

                if (result.Documents.Count != 1) return (false, "Null search failed");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestEdgeUnicodeCharacters()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("Test");
                string json = @"{""Name"":""日本語テスト"",""Emoji"":""🎉""}";
                var doc = await client.IngestDocument(collection.Id, json);

                var retrieved = await client.GetDocument(doc.Id, includeContent: true);
                if (!retrieved.Content.Contains("日本語")) return (false, "Unicode not preserved");

                var result = await client.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Filters = new List<SearchFilter>
                    {
                        new SearchFilter("Name", SearchConditionEnum.Equals, "日本語テスト")
                    }
                });

                if (result.Documents.Count != 1) return (false, "Unicode search failed");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        #endregion

        #region Performance Tests

        private static async Task<(bool, string)> TestPerfIngest100()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("Test");

                var sw = Stopwatch.StartNew();
                for (int i = 0; i < 100; i++)
                {
                    await client.IngestDocument(collection.Id, $@"{{""Name"":""User{i}"",""Age"":{20 + i % 50}}}");
                }
                sw.Stop();

                double docsPerSecond = 100.0 / sw.Elapsed.TotalSeconds;
                Console.WriteLine($"           Ingestion: {docsPerSecond:F1} docs/sec ({sw.ElapsedMilliseconds}ms total)");

                // Pass if > 10 docs/sec (very conservative)
                if (docsPerSecond < 10) return (false, $"Too slow: {docsPerSecond:F1} docs/sec");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestPerfSearch100()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("Test");

                for (int i = 0; i < 100; i++)
                {
                    await client.IngestDocument(collection.Id, $@"{{""Name"":""User{i}"",""City"":""{(i % 2 == 0 ? "Austin" : "Denver")}""}}");
                }

                var sw = Stopwatch.StartNew();
                var result = await client.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Filters = new List<SearchFilter>
                    {
                        new SearchFilter("City", SearchConditionEnum.Equals, "Austin")
                    },
                    MaxResults = 100
                });
                sw.Stop();

                Console.WriteLine($"           Search: {sw.ElapsedMilliseconds}ms for {result.Documents.Count} results");

                if (result.Documents.Count != 50) return (false, $"Expected 50, got {result.Documents.Count}");
                if (sw.ElapsedMilliseconds > 5000) return (false, $"Too slow: {sw.ElapsedMilliseconds}ms");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestPerfGetDocuments100()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("Test");

                for (int i = 0; i < 100; i++)
                {
                    await client.IngestDocument(collection.Id, $@"{{""N"":{i}}}",
                        labels: new List<string> { "label" },
                        tags: new Dictionary<string, string> { { "k", "v" } });
                }

                var sw = Stopwatch.StartNew();
                var docs = await client.GetDocuments(collection.Id);
                sw.Stop();

                Console.WriteLine($"           GetDocuments: {sw.ElapsedMilliseconds}ms for {docs.Count} docs");

                if (docs.Count != 100) return (false, $"Expected 100, got {docs.Count}");
                if (sw.ElapsedMilliseconds > 5000) return (false, $"Too slow: {sw.ElapsedMilliseconds}ms");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestPerfEnumerate100()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("Test");

                for (int i = 0; i < 100; i++)
                {
                    await client.IngestDocument(collection.Id, $@"{{""N"":{i}}}");
                }

                var sw = Stopwatch.StartNew();
                var result = await client.Enumerate(new EnumerationQuery
                {
                    CollectionId = collection.Id,
                    MaxResults = 100
                });
                sw.Stop();

                Console.WriteLine($"           Enumerate: {sw.ElapsedMilliseconds}ms for {result.Objects.Count} docs");

                if (result.Objects.Count != 100) return (false, $"Expected 100, got {result.Objects.Count}");
                if (sw.ElapsedMilliseconds > 5000) return (false, $"Too slow: {sw.ElapsedMilliseconds}ms");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        #endregion

        #region Integration Tests

        private static async Task<(bool, string)> TestIntegrationFullCrud()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);

                // Create
                var collection = await client.CreateCollection("People", "People collection");
                if (collection == null) return (false, "Create collection failed");

                // Ingest
                var doc = await client.IngestDocument(collection.Id,
                    @"{""Name"":""Joel"",""Age"":40}",
                    "PersonDoc",
                    new List<string> { "employee" },
                    new Dictionary<string, string> { { "dept", "eng" } });
                if (doc == null) return (false, "Ingest failed");

                // Read
                var retrieved = await client.GetDocument(doc.Id, includeContent: true);
                if (retrieved == null) return (false, "Read failed");
                if (retrieved.Name != "PersonDoc") return (false, "Name mismatch");
                if (!retrieved.Labels.Contains("employee")) return (false, "Labels not persisted");
                if (retrieved.Tags["dept"] != "eng") return (false, "Tags not persisted");
                if (!retrieved.Content.Contains("Joel")) return (false, "Content wrong");

                // Search
                var searchResult = await client.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Filters = new List<SearchFilter>
                    {
                        new SearchFilter("Name", SearchConditionEnum.Equals, "Joel")
                    }
                });
                if (searchResult.Documents.Count != 1) return (false, "Search failed");

                // Delete document
                await client.DeleteDocument(doc.Id);
                if (await client.DocumentExists(doc.Id)) return (false, "Document delete failed");

                // Delete collection
                await client.DeleteCollection(collection.Id);
                if (await client.CollectionExists(collection.Id)) return (false, "Collection delete failed");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestIntegrationMultipleCollections()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);

                var col1 = await client.CreateCollection("Collection1");
                var col2 = await client.CreateCollection("Collection2");

                await client.IngestDocument(col1.Id, @"{""Data"":""Col1Doc""}");
                await client.IngestDocument(col2.Id, @"{""Data"":""Col2Doc""}");

                var docs1 = await client.GetDocuments(col1.Id);
                var docs2 = await client.GetDocuments(col2.Id);

                if (docs1.Count != 1) return (false, "Col1 doc count wrong");
                if (docs2.Count != 1) return (false, "Col2 doc count wrong");

                // Search in col1 shouldn't find col2 docs
                var result = await client.Search(new SearchQuery
                {
                    CollectionId = col1.Id,
                    Filters = new List<SearchFilter>
                    {
                        new SearchFilter("Data", SearchConditionEnum.Equals, "Col2Doc")
                    }
                });

                if (result.Documents.Count != 0) return (false, "Collection isolation failed");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestIntegrationSchemaSharing()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("Test");

                // Ingest docs with same structure
                var doc1 = await client.IngestDocument(collection.Id, @"{""Name"":""A"",""Value"":1}");
                var doc2 = await client.IngestDocument(collection.Id, @"{""Name"":""B"",""Value"":2}");
                var doc3 = await client.IngestDocument(collection.Id, @"{""Name"":""C"",""Value"":3}");

                // All should share same schema
                if (doc1.SchemaId != doc2.SchemaId || doc2.SchemaId != doc3.SchemaId)
                    return (false, "Schemas not shared");

                var schemas = await client.GetSchemas();
                if (schemas.Count != 1) return (false, $"Expected 1 schema, got {schemas.Count}");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        #endregion
    }
}
