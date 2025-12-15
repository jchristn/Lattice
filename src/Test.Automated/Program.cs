namespace Test.Automated
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Lattice.Core;
    using Lattice.Core.Exceptions;
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

                // ===== SCHEMA CONSTRAINTS TESTS =====
                await RunTestSection("SCHEMA CONSTRAINTS", async () =>
                {
                    // Basic constraint tests
                    await RunTest("Constraints: create collection with strict mode", TestConstraintsStrictMode);
                    await RunTest("Constraints: valid document passes validation", TestConstraintsValidDocument);
                    await RunTest("Constraints: missing required field fails", TestConstraintsMissingRequired);
                    await RunTest("Constraints: type mismatch fails", TestConstraintsTypeMismatch);
                    await RunTest("Constraints: update constraints on collection", TestConstraintsUpdate);

                    // Type validation tests
                    await RunTest("Constraints: string type validation", TestConstraintsStringType);
                    await RunTest("Constraints: integer type validation", TestConstraintsIntegerType);
                    await RunTest("Constraints: number type accepts decimals", TestConstraintsNumberType);
                    await RunTest("Constraints: boolean type validation", TestConstraintsBooleanType);
                    await RunTest("Constraints: array type validation", TestConstraintsArrayType);

                    // Nullable tests
                    await RunTest("Constraints: nullable field accepts null", TestConstraintsNullableAcceptsNull);
                    await RunTest("Constraints: non-nullable field rejects null", TestConstraintsNonNullableRejectsNull);

                    // Regex pattern tests
                    await RunTest("Constraints: regex pattern matching succeeds", TestConstraintsRegexPatternSuccess);
                    await RunTest("Constraints: regex pattern mismatch fails", TestConstraintsRegexPatternFails);
                    await RunTest("Constraints: email pattern validation", TestConstraintsEmailPattern);

                    // Range validation tests
                    await RunTest("Constraints: MinValue validation succeeds", TestConstraintsMinValueSuccess);
                    await RunTest("Constraints: MinValue validation fails", TestConstraintsMinValueFails);
                    await RunTest("Constraints: MaxValue validation succeeds", TestConstraintsMaxValueSuccess);
                    await RunTest("Constraints: MaxValue validation fails", TestConstraintsMaxValueFails);

                    // Length validation tests
                    await RunTest("Constraints: MinLength string succeeds", TestConstraintsMinLengthStringSuccess);
                    await RunTest("Constraints: MinLength string fails", TestConstraintsMinLengthStringFails);
                    await RunTest("Constraints: MaxLength string succeeds", TestConstraintsMaxLengthStringSuccess);
                    await RunTest("Constraints: MaxLength string fails", TestConstraintsMaxLengthStringFails);
                    await RunTest("Constraints: array MinLength succeeds", TestConstraintsArrayMinLengthSuccess);
                    await RunTest("Constraints: array MaxLength fails", TestConstraintsArrayMaxLengthFails);

                    // Allowed values tests
                    await RunTest("Constraints: allowed values succeeds", TestConstraintsAllowedValuesSuccess);
                    await RunTest("Constraints: allowed values fails", TestConstraintsAllowedValuesFails);

                    // Enforcement mode tests
                    await RunTest("Constraints: strict mode rejects extra fields", TestConstraintsStrictModeRejectsExtra);
                    await RunTest("Constraints: flexible mode allows extra fields", TestConstraintsFlexibleModeAllowsExtra);
                    await RunTest("Constraints: partial mode validates only specified", TestConstraintsPartialMode);
                    await RunTest("Constraints: none mode skips validation", TestConstraintsNoneMode);

                    // Nested field tests
                    await RunTest("Constraints: nested field validation", TestConstraintsNestedField);
                    await RunTest("Constraints: deeply nested field validation", TestConstraintsDeeplyNestedField);

                    // Array element type tests
                    await RunTest("Constraints: array element type validation succeeds", TestConstraintsArrayElementTypeSuccess);
                    await RunTest("Constraints: array element type validation fails", TestConstraintsArrayElementTypeFails);
                });

                // ===== INDEXING MODE TESTS =====
                await RunTestSection("INDEXING MODE", async () =>
                {
                    await RunTest("Indexing: selective mode only indexes specified fields", TestIndexingSelectiveMode);
                    await RunTest("Indexing: none mode skips indexing", TestIndexingNoneMode);
                    await RunTest("Indexing: update indexing mode", TestIndexingUpdateMode);
                    await RunTest("Indexing: rebuild indexes", TestIndexingRebuildIndexes);

                    // Additional indexing tests
                    await RunTest("Indexing: search non-indexed field returns empty", TestIndexingSearchNonIndexedField);
                    await RunTest("Indexing: all mode indexes everything", TestIndexingAllModeIndexesAll);
                    await RunTest("Indexing: nested field selective indexing", TestIndexingNestedFieldSelective);
                    await RunTest("Indexing: rebuild with progress reporting", TestIndexingRebuildWithProgress);
                    await RunTest("Indexing: rebuild drops unused indexes", TestIndexingRebuildDropsUnused);
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
                Database = new Lattice.Core.DatabaseSettings { Filename = Path.Combine(testDir, "lattice.db") }
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
                    Database = new Lattice.Core.DatabaseSettings { Filename = dbPath }
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
                    Database = new Lattice.Core.DatabaseSettings { Filename = dbPath }
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

        #region Schema Constraints Tests

        private static async Task<(bool, string)> TestConstraintsStrictMode()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Name", DataType = "string", Required = true },
                    new FieldConstraint { FieldPath = "Age", DataType = "integer", Required = true }
                };

                var collection = await client.CreateCollection(
                    "StrictTest",
                    schemaEnforcementMode: SchemaEnforcementMode.Strict,
                    fieldConstraints: constraints);

                if (collection.SchemaEnforcementMode != SchemaEnforcementMode.Strict)
                    return (false, "SchemaEnforcementMode not set correctly");

                var savedConstraints = await client.GetCollectionConstraints(collection.Id);
                if (savedConstraints.Count != 2)
                    return (false, $"Expected 2 constraints, got {savedConstraints.Count}");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestConstraintsValidDocument()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Name", DataType = "string", Required = true },
                    new FieldConstraint { FieldPath = "Age", DataType = "integer", Required = true }
                };

                var collection = await client.CreateCollection(
                    "ValidDocTest",
                    schemaEnforcementMode: SchemaEnforcementMode.Flexible,
                    fieldConstraints: constraints);

                var doc = await client.IngestDocument(collection.Id, @"{""Name"":""Joel"",""Age"":30}");
                if (doc == null) return (false, "Document should have been created");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestConstraintsMissingRequired()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Name", DataType = "string", Required = true },
                    new FieldConstraint { FieldPath = "Age", DataType = "integer", Required = true }
                };

                var collection = await client.CreateCollection(
                    "MissingFieldTest",
                    schemaEnforcementMode: SchemaEnforcementMode.Flexible,
                    fieldConstraints: constraints);

                try
                {
                    await client.IngestDocument(collection.Id, @"{""Name"":""Joel""}");
                    return (false, "Should have thrown SchemaValidationException");
                }
                catch (SchemaValidationException ex)
                {
                    if (ex.Errors.Count == 0) return (false, "Expected validation errors");
                    if (!ex.Errors.Any(e => e.ErrorCode == "MISSING_REQUIRED_FIELD"))
                        return (false, "Expected MISSING_REQUIRED_FIELD error");
                    return (true, null);
                }
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestConstraintsTypeMismatch()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Age", DataType = "integer", Required = true }
                };

                var collection = await client.CreateCollection(
                    "TypeMismatchTest",
                    schemaEnforcementMode: SchemaEnforcementMode.Flexible,
                    fieldConstraints: constraints);

                try
                {
                    await client.IngestDocument(collection.Id, @"{""Age"":""not a number""}");
                    return (false, "Should have thrown SchemaValidationException");
                }
                catch (SchemaValidationException ex)
                {
                    if (!ex.Errors.Any(e => e.ErrorCode == "TYPE_MISMATCH"))
                        return (false, "Expected TYPE_MISMATCH error");
                    return (true, null);
                }
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestConstraintsUpdate()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("UpdateConstraintsTest");

                var initial = await client.GetCollectionConstraints(collection.Id);
                if (initial.Count != 0) return (false, "Expected no initial constraints");

                var constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Email", DataType = "string", Required = true }
                };
                await client.UpdateCollectionConstraints(collection.Id, SchemaEnforcementMode.Flexible, constraints);

                var updated = await client.GetCollectionConstraints(collection.Id);
                if (updated.Count != 1) return (false, "Expected 1 constraint after update");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        // Type validation tests
        private static async Task<(bool, string)> TestConstraintsStringType()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Name", DataType = "string", Required = true }
                };

                var collection = await client.CreateCollection(
                    "StringTypeTest",
                    schemaEnforcementMode: SchemaEnforcementMode.Flexible,
                    fieldConstraints: constraints);

                // String should pass
                var doc = await client.IngestDocument(collection.Id, @"{""Name"":""Joel""}");
                if (doc == null) return (false, "String value should pass");

                // Number should fail
                try
                {
                    await client.IngestDocument(collection.Id, @"{""Name"":123}");
                    return (false, "Number for string field should fail");
                }
                catch (SchemaValidationException) { }

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestConstraintsIntegerType()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Count", DataType = "integer", Required = true }
                };

                var collection = await client.CreateCollection(
                    "IntegerTypeTest",
                    schemaEnforcementMode: SchemaEnforcementMode.Flexible,
                    fieldConstraints: constraints);

                // Integer should pass
                var doc = await client.IngestDocument(collection.Id, @"{""Count"":42}");
                if (doc == null) return (false, "Integer value should pass");

                // Decimal should fail for integer type
                try
                {
                    await client.IngestDocument(collection.Id, @"{""Count"":3.14}");
                    return (false, "Decimal for integer field should fail");
                }
                catch (SchemaValidationException) { }

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestConstraintsNumberType()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Price", DataType = "number", Required = true }
                };

                var collection = await client.CreateCollection(
                    "NumberTypeTest",
                    schemaEnforcementMode: SchemaEnforcementMode.Flexible,
                    fieldConstraints: constraints);

                // Integer should pass for number
                var doc1 = await client.IngestDocument(collection.Id, @"{""Price"":42}");
                if (doc1 == null) return (false, "Integer should pass for number type");

                // Decimal should also pass
                var doc2 = await client.IngestDocument(collection.Id, @"{""Price"":19.99}");
                if (doc2 == null) return (false, "Decimal should pass for number type");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestConstraintsBooleanType()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Active", DataType = "boolean", Required = true }
                };

                var collection = await client.CreateCollection(
                    "BooleanTypeTest",
                    schemaEnforcementMode: SchemaEnforcementMode.Flexible,
                    fieldConstraints: constraints);

                // true should pass
                var doc1 = await client.IngestDocument(collection.Id, @"{""Active"":true}");
                if (doc1 == null) return (false, "true should pass for boolean");

                // false should pass
                var doc2 = await client.IngestDocument(collection.Id, @"{""Active"":false}");
                if (doc2 == null) return (false, "false should pass for boolean");

                // String "true" should fail
                try
                {
                    await client.IngestDocument(collection.Id, @"{""Active"":""true""}");
                    return (false, "String 'true' for boolean should fail");
                }
                catch (SchemaValidationException) { }

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestConstraintsArrayType()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Tags", DataType = "array", Required = true }
                };

                var collection = await client.CreateCollection(
                    "ArrayTypeTest",
                    schemaEnforcementMode: SchemaEnforcementMode.Flexible,
                    fieldConstraints: constraints);

                // Array should pass
                var doc = await client.IngestDocument(collection.Id, @"{""Tags"":[""a"",""b"",""c""]}");
                if (doc == null) return (false, "Array value should pass");

                // String should fail
                try
                {
                    await client.IngestDocument(collection.Id, @"{""Tags"":""not an array""}");
                    return (false, "String for array field should fail");
                }
                catch (SchemaValidationException) { }

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        // Nullable tests
        private static async Task<(bool, string)> TestConstraintsNullableAcceptsNull()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "MiddleName", DataType = "string", Required = true, Nullable = true }
                };

                var collection = await client.CreateCollection(
                    "NullableTest",
                    schemaEnforcementMode: SchemaEnforcementMode.Flexible,
                    fieldConstraints: constraints);

                var doc = await client.IngestDocument(collection.Id, @"{""MiddleName"":null}");
                if (doc == null) return (false, "Null should be accepted for nullable field");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestConstraintsNonNullableRejectsNull()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Name", DataType = "string", Required = true, Nullable = false }
                };

                var collection = await client.CreateCollection(
                    "NonNullableTest",
                    schemaEnforcementMode: SchemaEnforcementMode.Flexible,
                    fieldConstraints: constraints);

                try
                {
                    await client.IngestDocument(collection.Id, @"{""Name"":null}");
                    return (false, "Null should be rejected for non-nullable field");
                }
                catch (SchemaValidationException ex)
                {
                    if (!ex.Errors.Any(e => e.ErrorCode == "NULL_NOT_ALLOWED"))
                        return (false, "Expected NULL_NOT_ALLOWED error");
                    return (true, null);
                }
            }
            finally { CleanupTestDir(testDir); }
        }

        // Regex pattern tests
        private static async Task<(bool, string)> TestConstraintsRegexPatternSuccess()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Code", DataType = "string", Required = true, RegexPattern = @"^[A-Z]{3}-\d{4}$" }
                };

                var collection = await client.CreateCollection(
                    "RegexSuccessTest",
                    schemaEnforcementMode: SchemaEnforcementMode.Flexible,
                    fieldConstraints: constraints);

                var doc = await client.IngestDocument(collection.Id, @"{""Code"":""ABC-1234""}");
                if (doc == null) return (false, "Valid pattern should pass");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestConstraintsRegexPatternFails()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Code", DataType = "string", Required = true, RegexPattern = @"^[A-Z]{3}-\d{4}$" }
                };

                var collection = await client.CreateCollection(
                    "RegexFailTest",
                    schemaEnforcementMode: SchemaEnforcementMode.Flexible,
                    fieldConstraints: constraints);

                try
                {
                    await client.IngestDocument(collection.Id, @"{""Code"":""invalid""}");
                    return (false, "Invalid pattern should fail");
                }
                catch (SchemaValidationException ex)
                {
                    if (!ex.Errors.Any(e => e.ErrorCode == "PATTERN_MISMATCH"))
                        return (false, "Expected PATTERN_MISMATCH error");
                    return (true, null);
                }
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestConstraintsEmailPattern()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Email", DataType = "string", Required = true, RegexPattern = @"^[\w\.-]+@[\w\.-]+\.\w+$" }
                };

                var collection = await client.CreateCollection(
                    "EmailPatternTest",
                    schemaEnforcementMode: SchemaEnforcementMode.Flexible,
                    fieldConstraints: constraints);

                // Valid email
                var doc = await client.IngestDocument(collection.Id, @"{""Email"":""user@example.com""}");
                if (doc == null) return (false, "Valid email should pass");

                // Invalid email
                try
                {
                    await client.IngestDocument(collection.Id, @"{""Email"":""not-an-email""}");
                    return (false, "Invalid email should fail");
                }
                catch (SchemaValidationException) { }

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        // Range validation tests
        private static async Task<(bool, string)> TestConstraintsMinValueSuccess()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Age", DataType = "integer", Required = true, MinValue = 18 }
                };

                var collection = await client.CreateCollection(
                    "MinValueSuccessTest",
                    schemaEnforcementMode: SchemaEnforcementMode.Flexible,
                    fieldConstraints: constraints);

                var doc = await client.IngestDocument(collection.Id, @"{""Age"":21}");
                if (doc == null) return (false, "Value above min should pass");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestConstraintsMinValueFails()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Age", DataType = "integer", Required = true, MinValue = 18 }
                };

                var collection = await client.CreateCollection(
                    "MinValueFailTest",
                    schemaEnforcementMode: SchemaEnforcementMode.Flexible,
                    fieldConstraints: constraints);

                try
                {
                    await client.IngestDocument(collection.Id, @"{""Age"":16}");
                    return (false, "Value below min should fail");
                }
                catch (SchemaValidationException ex)
                {
                    if (!ex.Errors.Any(e => e.ErrorCode == "VALUE_TOO_SMALL"))
                        return (false, "Expected VALUE_TOO_SMALL error");
                    return (true, null);
                }
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestConstraintsMaxValueSuccess()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Percent", DataType = "number", Required = true, MaxValue = 100 }
                };

                var collection = await client.CreateCollection(
                    "MaxValueSuccessTest",
                    schemaEnforcementMode: SchemaEnforcementMode.Flexible,
                    fieldConstraints: constraints);

                var doc = await client.IngestDocument(collection.Id, @"{""Percent"":75}");
                if (doc == null) return (false, "Value below max should pass");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestConstraintsMaxValueFails()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Percent", DataType = "number", Required = true, MaxValue = 100 }
                };

                var collection = await client.CreateCollection(
                    "MaxValueFailTest",
                    schemaEnforcementMode: SchemaEnforcementMode.Flexible,
                    fieldConstraints: constraints);

                try
                {
                    await client.IngestDocument(collection.Id, @"{""Percent"":150}");
                    return (false, "Value above max should fail");
                }
                catch (SchemaValidationException ex)
                {
                    if (!ex.Errors.Any(e => e.ErrorCode == "VALUE_TOO_LARGE"))
                        return (false, "Expected VALUE_TOO_LARGE error");
                    return (true, null);
                }
            }
            finally { CleanupTestDir(testDir); }
        }

        // Length validation tests
        private static async Task<(bool, string)> TestConstraintsMinLengthStringSuccess()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Password", DataType = "string", Required = true, MinLength = 8 }
                };

                var collection = await client.CreateCollection(
                    "MinLengthSuccessTest",
                    schemaEnforcementMode: SchemaEnforcementMode.Flexible,
                    fieldConstraints: constraints);

                var doc = await client.IngestDocument(collection.Id, @"{""Password"":""password123""}");
                if (doc == null) return (false, "String meeting minLength should pass");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestConstraintsMinLengthStringFails()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Password", DataType = "string", Required = true, MinLength = 8 }
                };

                var collection = await client.CreateCollection(
                    "MinLengthFailTest",
                    schemaEnforcementMode: SchemaEnforcementMode.Flexible,
                    fieldConstraints: constraints);

                try
                {
                    await client.IngestDocument(collection.Id, @"{""Password"":""short""}");
                    return (false, "String below minLength should fail");
                }
                catch (SchemaValidationException ex)
                {
                    if (!ex.Errors.Any(e => e.ErrorCode == "STRING_TOO_SHORT"))
                        return (false, "Expected STRING_TOO_SHORT error");
                    return (true, null);
                }
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestConstraintsMaxLengthStringSuccess()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Username", DataType = "string", Required = true, MaxLength = 20 }
                };

                var collection = await client.CreateCollection(
                    "MaxLengthSuccessTest",
                    schemaEnforcementMode: SchemaEnforcementMode.Flexible,
                    fieldConstraints: constraints);

                var doc = await client.IngestDocument(collection.Id, @"{""Username"":""joel""}");
                if (doc == null) return (false, "String within maxLength should pass");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestConstraintsMaxLengthStringFails()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Username", DataType = "string", Required = true, MaxLength = 10 }
                };

                var collection = await client.CreateCollection(
                    "MaxLengthFailTest",
                    schemaEnforcementMode: SchemaEnforcementMode.Flexible,
                    fieldConstraints: constraints);

                try
                {
                    await client.IngestDocument(collection.Id, @"{""Username"":""verylongusername""}");
                    return (false, "String exceeding maxLength should fail");
                }
                catch (SchemaValidationException ex)
                {
                    if (!ex.Errors.Any(e => e.ErrorCode == "STRING_TOO_LONG"))
                        return (false, "Expected STRING_TOO_LONG error");
                    return (true, null);
                }
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestConstraintsArrayMinLengthSuccess()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Tags", DataType = "array", Required = true, MinLength = 2 }
                };

                var collection = await client.CreateCollection(
                    "ArrayMinLengthSuccessTest",
                    schemaEnforcementMode: SchemaEnforcementMode.Flexible,
                    fieldConstraints: constraints);

                var doc = await client.IngestDocument(collection.Id, @"{""Tags"":[""a"",""b"",""c""]}");
                if (doc == null) return (false, "Array meeting minLength should pass");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestConstraintsArrayMaxLengthFails()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Tags", DataType = "array", Required = true, MaxLength = 3 }
                };

                var collection = await client.CreateCollection(
                    "ArrayMaxLengthFailTest",
                    schemaEnforcementMode: SchemaEnforcementMode.Flexible,
                    fieldConstraints: constraints);

                try
                {
                    await client.IngestDocument(collection.Id, @"{""Tags"":[""a"",""b"",""c"",""d"",""e""]}");
                    return (false, "Array exceeding maxLength should fail");
                }
                catch (SchemaValidationException ex)
                {
                    if (!ex.Errors.Any(e => e.ErrorCode == "ARRAY_TOO_LONG"))
                        return (false, "Expected ARRAY_TOO_LONG error");
                    return (true, null);
                }
            }
            finally { CleanupTestDir(testDir); }
        }

        // Allowed values tests
        private static async Task<(bool, string)> TestConstraintsAllowedValuesSuccess()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Status", DataType = "string", Required = true, AllowedValues = new List<string> { "active", "inactive", "pending" } }
                };

                var collection = await client.CreateCollection(
                    "AllowedValuesSuccessTest",
                    schemaEnforcementMode: SchemaEnforcementMode.Flexible,
                    fieldConstraints: constraints);

                var doc = await client.IngestDocument(collection.Id, @"{""Status"":""active""}");
                if (doc == null) return (false, "Value in allowed list should pass");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestConstraintsAllowedValuesFails()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Status", DataType = "string", Required = true, AllowedValues = new List<string> { "active", "inactive", "pending" } }
                };

                var collection = await client.CreateCollection(
                    "AllowedValuesFailTest",
                    schemaEnforcementMode: SchemaEnforcementMode.Flexible,
                    fieldConstraints: constraints);

                try
                {
                    await client.IngestDocument(collection.Id, @"{""Status"":""deleted""}");
                    return (false, "Value not in allowed list should fail");
                }
                catch (SchemaValidationException ex)
                {
                    if (!ex.Errors.Any(e => e.ErrorCode == "VALUE_NOT_ALLOWED"))
                        return (false, "Expected VALUE_NOT_ALLOWED error");
                    return (true, null);
                }
            }
            finally { CleanupTestDir(testDir); }
        }

        // Enforcement mode tests
        private static async Task<(bool, string)> TestConstraintsStrictModeRejectsExtra()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Name", DataType = "string", Required = true }
                };

                var collection = await client.CreateCollection(
                    "StrictModeRejectsExtraTest",
                    schemaEnforcementMode: SchemaEnforcementMode.Strict,
                    fieldConstraints: constraints);

                try
                {
                    await client.IngestDocument(collection.Id, @"{""Name"":""Joel"",""ExtraField"":""value""}");
                    return (false, "Strict mode should reject extra fields");
                }
                catch (SchemaValidationException ex)
                {
                    if (!ex.Errors.Any(e => e.ErrorCode == "UNEXPECTED_FIELD"))
                        return (false, "Expected UNEXPECTED_FIELD error");
                    return (true, null);
                }
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestConstraintsFlexibleModeAllowsExtra()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Name", DataType = "string", Required = true }
                };

                var collection = await client.CreateCollection(
                    "FlexibleModeAllowsExtraTest",
                    schemaEnforcementMode: SchemaEnforcementMode.Flexible,
                    fieldConstraints: constraints);

                var doc = await client.IngestDocument(collection.Id, @"{""Name"":""Joel"",""ExtraField"":""value""}");
                if (doc == null) return (false, "Flexible mode should allow extra fields");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestConstraintsPartialMode()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Email", DataType = "string", Required = true, RegexPattern = @"^[\w\.-]+@[\w\.-]+\.\w+$" }
                };

                var collection = await client.CreateCollection(
                    "PartialModeTest",
                    schemaEnforcementMode: SchemaEnforcementMode.Partial,
                    fieldConstraints: constraints);

                // Document without the specified field should pass in Partial mode
                var doc = await client.IngestDocument(collection.Id, @"{""Name"":""Joel"",""Age"":30}");
                if (doc == null) return (false, "Partial mode should allow missing specified fields");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestConstraintsNoneMode()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Name", DataType = "string", Required = true }
                };

                var collection = await client.CreateCollection(
                    "NoneModeTest",
                    schemaEnforcementMode: SchemaEnforcementMode.None,
                    fieldConstraints: constraints);

                // Any document should pass with None mode
                var doc = await client.IngestDocument(collection.Id, @"{""Whatever"":123}");
                if (doc == null) return (false, "None mode should skip all validation");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        // Nested field tests
        private static async Task<(bool, string)> TestConstraintsNestedField()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Address.City", DataType = "string", Required = true }
                };

                var collection = await client.CreateCollection(
                    "NestedFieldTest",
                    schemaEnforcementMode: SchemaEnforcementMode.Flexible,
                    fieldConstraints: constraints);

                var doc = await client.IngestDocument(collection.Id, @"{""Address"":{""City"":""Seattle"",""State"":""WA""}}");
                if (doc == null) return (false, "Nested field validation should work");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestConstraintsDeeplyNestedField()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Person.Contact.Address.ZipCode", DataType = "string", Required = true }
                };

                var collection = await client.CreateCollection(
                    "DeeplyNestedTest",
                    schemaEnforcementMode: SchemaEnforcementMode.Flexible,
                    fieldConstraints: constraints);

                var doc = await client.IngestDocument(collection.Id, @"{""Person"":{""Contact"":{""Address"":{""ZipCode"":""98101""}}}}");
                if (doc == null) return (false, "Deeply nested field validation should work");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        // Array element type tests
        private static async Task<(bool, string)> TestConstraintsArrayElementTypeSuccess()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Scores", DataType = "array", Required = true, ArrayElementType = "integer" }
                };

                var collection = await client.CreateCollection(
                    "ArrayElementTypeSuccessTest",
                    schemaEnforcementMode: SchemaEnforcementMode.Flexible,
                    fieldConstraints: constraints);

                var doc = await client.IngestDocument(collection.Id, @"{""Scores"":[90,85,92,88]}");
                if (doc == null) return (false, "Array with correct element type should pass");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestConstraintsArrayElementTypeFails()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Scores", DataType = "array", Required = true, ArrayElementType = "integer" }
                };

                var collection = await client.CreateCollection(
                    "ArrayElementTypeFailTest",
                    schemaEnforcementMode: SchemaEnforcementMode.Flexible,
                    fieldConstraints: constraints);

                try
                {
                    await client.IngestDocument(collection.Id, @"{""Scores"":[90,""not a number"",92]}");
                    return (false, "Array with wrong element type should fail");
                }
                catch (SchemaValidationException ex)
                {
                    if (!ex.Errors.Any(e => e.ErrorCode == "INVALID_ARRAY_ELEMENT"))
                        return (false, "Expected INVALID_ARRAY_ELEMENT error");
                    return (true, null);
                }
            }
            finally { CleanupTestDir(testDir); }
        }

        #endregion

        #region Indexing Mode Tests

        private static async Task<(bool, string)> TestIndexingSelectiveMode()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection(
                    "SelectiveIndexTest",
                    indexingMode: IndexingMode.Selective,
                    indexedFields: new List<string> { "Name" });

                if (collection.IndexingMode != IndexingMode.Selective)
                    return (false, "IndexingMode not set correctly");

                var indexedFields = await client.GetCollectionIndexedFields(collection.Id);
                if (indexedFields.Count != 1 || indexedFields[0].FieldPath != "Name")
                    return (false, "Indexed fields not set correctly");

                await client.IngestDocument(collection.Id, @"{""Name"":""Joel"",""Age"":30}");

                var nameResult = await client.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Filters = new List<SearchFilter> { new SearchFilter("Name", SearchConditionEnum.Equals, "Joel") }
                });
                if (nameResult.Documents.Count != 1)
                    return (false, "Search by Name should find document");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestIndexingNoneMode()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection(
                    "NoIndexTest",
                    indexingMode: IndexingMode.None);

                if (collection.IndexingMode != IndexingMode.None)
                    return (false, "IndexingMode not set correctly");

                var doc = await client.IngestDocument(collection.Id, @"{""Name"":""Joel"",""Age"":30}");
                if (doc == null) return (false, "Document should be created");

                var retrieved = await client.GetDocument(doc.Id);
                if (retrieved == null) return (false, "Document should be retrievable by ID");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestIndexingUpdateMode()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("UpdateIndexingTest");

                if (collection.IndexingMode != IndexingMode.All)
                    return (false, "Expected initial All mode");

                await client.UpdateCollectionIndexing(
                    collection.Id,
                    IndexingMode.Selective,
                    new List<string> { "Email" });

                var updated = await client.GetCollection(collection.Id);
                if (updated.IndexingMode != IndexingMode.Selective)
                    return (false, "IndexingMode not updated");

                var indexedFields = await client.GetCollectionIndexedFields(collection.Id);
                if (indexedFields.Count != 1) return (false, "Expected 1 indexed field");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestIndexingRebuildIndexes()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("RebuildTest");

                await client.IngestDocument(collection.Id, @"{""Name"":""Joel"",""Age"":30}");
                await client.IngestDocument(collection.Id, @"{""Name"":""John"",""Age"":25}");

                await client.UpdateCollectionIndexing(
                    collection.Id,
                    IndexingMode.Selective,
                    new List<string> { "Name" });

                var result = await client.RebuildIndexes(collection.Id, dropUnusedIndexes: true);

                if (result.DocumentsProcessed != 2)
                    return (false, $"Expected 2 documents processed, got {result.DocumentsProcessed}");

                if (!result.Success)
                    return (false, $"Rebuild failed: {string.Join(", ", result.Errors)}");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestIndexingSearchNonIndexedField()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection(
                    "SearchNonIndexedTest",
                    indexingMode: IndexingMode.Selective,
                    indexedFields: new List<string> { "Name" });

                await client.IngestDocument(collection.Id, @"{""Name"":""Joel"",""Age"":30}");

                // Search by non-indexed field should return empty
                var ageResult = await client.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Filters = new List<SearchFilter> { new SearchFilter("Age", SearchConditionEnum.Equals, "30") }
                });

                if (ageResult.Documents.Count != 0)
                    return (false, "Search by non-indexed field should return empty");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestIndexingAllModeIndexesAll()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection(
                    "AllModeTest",
                    indexingMode: IndexingMode.All);

                await client.IngestDocument(collection.Id, @"{""Name"":""Joel"",""Age"":30,""City"":""Seattle""}");

                // Search by any field should work
                var nameResult = await client.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Filters = new List<SearchFilter> { new SearchFilter("Name", SearchConditionEnum.Equals, "Joel") }
                });
                if (nameResult.Documents.Count != 1) return (false, "Name search should work");

                var ageResult = await client.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Filters = new List<SearchFilter> { new SearchFilter("Age", SearchConditionEnum.Equals, "30") }
                });
                if (ageResult.Documents.Count != 1) return (false, "Age search should work");

                var cityResult = await client.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Filters = new List<SearchFilter> { new SearchFilter("City", SearchConditionEnum.Equals, "Seattle") }
                });
                if (cityResult.Documents.Count != 1) return (false, "City search should work");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestIndexingNestedFieldSelective()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection(
                    "NestedIndexTest",
                    indexingMode: IndexingMode.Selective,
                    indexedFields: new List<string> { "Address.City" });

                await client.IngestDocument(collection.Id, @"{""Name"":""Joel"",""Address"":{""City"":""Seattle"",""State"":""WA""}}");

                // Search by indexed nested field should work
                var cityResult = await client.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Filters = new List<SearchFilter> { new SearchFilter("Address.City", SearchConditionEnum.Equals, "Seattle") }
                });
                if (cityResult.Documents.Count != 1)
                    return (false, "Search by indexed nested field should work");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestIndexingRebuildWithProgress()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("ProgressTest");

                for (int i = 0; i < 10; i++)
                {
                    await client.IngestDocument(collection.Id, $@"{{""Index"":{i},""Value"":""item{i}""}}");
                }

                var progressReports = new List<IndexRebuildProgress>();
                var progress = new Progress<IndexRebuildProgress>(p => progressReports.Add(p));

                var result = await client.RebuildIndexes(collection.Id, dropUnusedIndexes: false, progress);

                if (result.DocumentsProcessed != 10)
                    return (false, $"Expected 10 documents processed, got {result.DocumentsProcessed}");

                if (progressReports.Count == 0)
                    return (false, "Expected progress reports");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<(bool, string)> TestIndexingRebuildDropsUnused()
        {
            string testDir = CreateTestDir();
            try
            {
                using var client = CreateClient(testDir);
                var collection = await client.CreateCollection("DropUnusedTest");

                // Add documents with multiple fields (all will be indexed initially)
                await client.IngestDocument(collection.Id, @"{""Name"":""Joel"",""Age"":30,""Email"":""joel@test.com""}");

                // Change to selective mode with only Name
                await client.UpdateCollectionIndexing(
                    collection.Id,
                    IndexingMode.Selective,
                    new List<string> { "Name" });

                // Rebuild with drop unused
                var result = await client.RebuildIndexes(collection.Id, dropUnusedIndexes: true);

                if (!result.Success)
                    return (false, $"Rebuild failed: {string.Join(", ", result.Errors)}");

                // Verify Name search still works
                var nameResult = await client.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Filters = new List<SearchFilter> { new SearchFilter("Name", SearchConditionEnum.Equals, "Joel") }
                });
                if (nameResult.Documents.Count != 1)
                    return (false, "Name search should still work after rebuild");

                return (true, null);
            }
            finally { CleanupTestDir(testDir); }
        }

        #endregion
    }
}
