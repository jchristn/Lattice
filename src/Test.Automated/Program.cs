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
        private static DatabaseSettings _databaseSettings = null;
        private static bool _enableObjectLocking = false;

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

        static async Task Main(string[] args)
        {
            // Parse command-line arguments
            if (!ParseArguments(args, out string parseError))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: {parseError}");
                Console.ResetColor();
                Console.WriteLine();
                PrintUsage();
                Environment.Exit(1);
                return;
            }

            // Validate database connection
            if (!await ValidateDatabaseConnection())
            {
                Environment.Exit(1);
                return;
            }

            Console.WriteLine("===============================================================================");
            Console.WriteLine("  Lattice Automated Test Suite - Comprehensive");
            Console.WriteLine("===============================================================================");
            Console.WriteLine();
            PrintDatabaseInfo();

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

        private static async Task RunTest(string name, Func<Task<TestOutcome>> test)
        {
            Stopwatch sw = Stopwatch.StartNew();
            bool passed = false;
            string error = null;

            try
            {
                TestOutcome result = await test();
                passed = result.Success;
                error = result.Error;
            }
            catch (Exception ex)
            {
                passed = false;
                error = ex.Message;
            }

            sw.Stop();

            TestResult testResult = new TestResult
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
            IEnumerable<IGrouping<string, TestResult>> sections = _Results.GroupBy(r => r.Section);
            foreach (IGrouping<string, TestResult> section in sections)
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
                foreach (TestResult failed in _Results.Where(r => !r.Passed))
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

        private static bool ParseArguments(string[] args, out string error)
        {
            error = null;

            if (args.Length == 0)
            {
                error = "No database type specified.";
                return false;
            }

            // Check for --enable-locking flag anywhere in args
            List<string> argList = new List<string>(args);
            if (argList.Contains("--enable-locking"))
            {
                _enableObjectLocking = true;
                argList.Remove("--enable-locking");
                args = argList.ToArray();
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
            Console.WriteLine("  Test.Automated sqlite <filename> [--enable-locking]");
            Console.WriteLine("  Test.Automated postgresql <hostname> <port> <username> <password> <database> [--enable-locking]");
            Console.WriteLine("  Test.Automated mysql <hostname> <port> <username> <password> <database> [--enable-locking]");
            Console.WriteLine("  Test.Automated sqlserver <hostname> <port> <username> <password> <database> [--enable-locking]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --enable-locking    Enable object locking during document ingestion (disabled by default)");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  Test.Automated sqlite ./test.db");
            Console.WriteLine("  Test.Automated postgresql localhost 5432 postgres password lattice");
            Console.WriteLine("  Test.Automated mysql localhost 3306 root password lattice");
            Console.WriteLine("  Test.Automated sqlserver localhost 1433 sa password lattice");
            Console.WriteLine("  Test.Automated sqlite ./test.db --enable-locking");
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
            if (_enableObjectLocking)
            {
                Console.WriteLine("Object Locking: Enabled");
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
                using LatticeClient client = CreateClient(testDir, inMemory: false);

                // Try to create a test collection to verify the connection works
                Collection testCollection = await client.Collection.Create("__connection_test__");
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
                InMemory = inMemory,
                DefaultDocumentsDirectory = testDir,
                Database = dbSettings,
                EnableObjectLocking = _enableObjectLocking
            });
        }

        #endregion

        #region Collection API Tests

        private static async Task<TestOutcome> TestCreateCollectionBasic()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("TestCollection");

                if (collection == null) return TestOutcome.Fail("Collection is null");
                if (collection.Name != "TestCollection") return TestOutcome.Fail($"Name mismatch: {collection.Name}");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestCreateCollectionFull()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create(
                    "TestCollection",
                    "Test description",
                    testDir,
                    new List<string> { "label1", "label2" },
                    new Dictionary<string, string> { { "env", "test" }, { "version", "1.0" } }
                );

                if (collection == null) return TestOutcome.Fail("Collection is null");
                if (collection.Name != "TestCollection") return TestOutcome.Fail("Name mismatch");
                if (collection.Description != "Test description") return TestOutcome.Fail("Description mismatch");
                if (collection.DocumentsDirectory != testDir) return TestOutcome.Fail("DocumentsDirectory mismatch");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestCreateCollectionProperties()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                DateTime before = DateTime.UtcNow.AddSeconds(-1);
                Collection collection = await client.Collection.Create("TestCollection", "Desc");
                DateTime after = DateTime.UtcNow.AddSeconds(1);

                if (string.IsNullOrEmpty(collection.Id)) return TestOutcome.Fail("Id is empty");
                if (!collection.Id.StartsWith("col_")) return TestOutcome.Fail($"Id format wrong: {collection.Id}");
                if (collection.Name != "TestCollection") return TestOutcome.Fail("Name mismatch");
                if (collection.Description != "Desc") return TestOutcome.Fail("Description mismatch");
                if (collection.CreatedUtc < before || collection.CreatedUtc > after)
                    return TestOutcome.Fail($"CreatedUtc out of range: {collection.CreatedUtc}");
                if (collection.LastUpdateUtc < before || collection.LastUpdateUtc > after)
                    return TestOutcome.Fail($"LastUpdateUtc out of range: {collection.LastUpdateUtc}");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestGetCollectionExisting()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection created = await client.Collection.Create("TestCollection", "Desc");
                Collection retrieved = await client.Collection.ReadById(created.Id);

                if (retrieved == null) return TestOutcome.Fail("Retrieved collection is null");
                if (retrieved.Id != created.Id) return TestOutcome.Fail("Id mismatch");
                if (retrieved.Name != created.Name) return TestOutcome.Fail("Name mismatch");
                if (retrieved.Description != created.Description) return TestOutcome.Fail("Description mismatch");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestGetCollectionNonExistent()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection retrieved = await client.Collection.ReadById("col_nonexistent");

                if (retrieved != null) return TestOutcome.Fail("Expected null for non-existent collection");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestGetCollectionsEmpty()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                List<Collection> collections = await client.Collection.ReadAll();

                if (collections == null) return TestOutcome.Fail("Collections is null");
                if (collections.Count != 0) return TestOutcome.Fail($"Expected 0 collections, got {collections.Count}");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestGetCollectionsMultiple()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                await client.Collection.Create("Collection1");
                await client.Collection.Create("Collection2");
                await client.Collection.Create("Collection3");

                List<Collection> collections = await client.Collection.ReadAll();

                if (collections.Count != 3) return TestOutcome.Fail($"Expected 3 collections, got {collections.Count}");

                HashSet<string> names = collections.Select(c => c.Name).ToHashSet();
                if (!names.Contains("Collection1")) return TestOutcome.Fail("Missing Collection1");
                if (!names.Contains("Collection2")) return TestOutcome.Fail("Missing Collection2");
                if (!names.Contains("Collection3")) return TestOutcome.Fail("Missing Collection3");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestCollectionExistsTrue()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("TestCollection");
                bool exists = await client.Collection.Exists(collection.Id);

                if (!exists) return TestOutcome.Fail("Expected true");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestCollectionExistsFalse()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                bool exists = await client.Collection.Exists("col_nonexistent");

                if (exists) return TestOutcome.Fail("Expected false");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestDeleteCollection()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("TestCollection");
                await client.Collection.Delete(collection.Id);

                bool exists = await client.Collection.Exists(collection.Id);
                if (exists) return TestOutcome.Fail("Collection still exists after delete");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestDeleteCollectionRemovesDocs()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("TestCollection");
                Document doc = await client.Document.Ingest(collection.Id, @"{""Name"":""Test""}");

                await client.Collection.Delete(collection.Id);

                bool docExists = await client.Document.Exists(doc.Id);
                if (docExists) return TestOutcome.Fail("Document still exists after collection delete");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        #endregion

        #region Document API Tests

        private static async Task<TestOutcome> TestIngestDocumentBasic()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("TestCollection");
                Document doc = await client.Document.Ingest(collection.Id, @"{""Name"":""Joel""}");

                if (doc == null) return TestOutcome.Fail("Document is null");
                if (string.IsNullOrEmpty(doc.Id)) return TestOutcome.Fail("Document Id is empty");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestIngestDocumentWithName()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("TestCollection");
                Document doc = await client.Document.Ingest(collection.Id, @"{""Data"":""test""}", "MyDocument");

                if (doc.Name != "MyDocument") return TestOutcome.Fail($"Name mismatch: {doc.Name}");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestIngestDocumentWithLabels()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("TestCollection");
                List<string> labels = new List<string> { "important", "reviewed" };
                Document doc = await client.Document.Ingest(collection.Id, @"{""Data"":""test""}", labels: labels);

                if (doc.Labels.Count != 2) return TestOutcome.Fail($"Expected 2 labels, got {doc.Labels.Count}");
                if (!doc.Labels.Contains("important")) return TestOutcome.Fail("Missing 'important' label");
                if (!doc.Labels.Contains("reviewed")) return TestOutcome.Fail("Missing 'reviewed' label");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestIngestDocumentWithTags()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("TestCollection");
                Dictionary<string, string> tags = new Dictionary<string, string> { { "author", "Joel" }, { "status", "draft" } };
                Document doc = await client.Document.Ingest(collection.Id, @"{""Data"":""test""}", tags: tags);

                if (doc.Tags.Count != 2) return TestOutcome.Fail($"Expected 2 tags, got {doc.Tags.Count}");
                if (!doc.Tags.ContainsKey("author") || doc.Tags["author"] != "Joel")
                    return TestOutcome.Fail("Missing or wrong 'author' tag");
                if (!doc.Tags.ContainsKey("status") || doc.Tags["status"] != "draft")
                    return TestOutcome.Fail("Missing or wrong 'status' tag");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestIngestDocumentProperties()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("TestCollection");
                DateTime before = DateTime.UtcNow.AddSeconds(-1);
                Document doc = await client.Document.Ingest(collection.Id, @"{""Name"":""Joel""}");
                DateTime after = DateTime.UtcNow.AddSeconds(1);

                if (!doc.Id.StartsWith("doc_")) return TestOutcome.Fail($"Id format wrong: {doc.Id}");
                if (doc.CollectionId != collection.Id) return TestOutcome.Fail("CollectionId mismatch");
                if (string.IsNullOrEmpty(doc.SchemaId)) return TestOutcome.Fail("SchemaId is empty");
                if (doc.CreatedUtc < before || doc.CreatedUtc > after)
                    return TestOutcome.Fail($"CreatedUtc out of range");
                if (doc.LastUpdateUtc < before || doc.LastUpdateUtc > after)
                    return TestOutcome.Fail($"LastUpdateUtc out of range");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestIngestDocumentNested()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("TestCollection");
                string json = @"{""Person"":{""Name"":{""First"":""Joel"",""Last"":""Christner""},""Age"":40}}";
                Document doc = await client.Document.Ingest(collection.Id, json);

                // Verify we can search on nested fields
                SearchResult result = await client.Search.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Filters = new List<SearchFilter>
                    {
                        new SearchFilter("Person.Name.First", SearchConditionEnum.Equals, "Joel")
                    }
                });

                if (result.Documents.Count != 1) return TestOutcome.Fail("Could not search nested field");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestIngestDocumentArray()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("TestCollection");
                string json = @"{""Tags"":[""red"",""green"",""blue""]}";
                Document doc = await client.Document.Ingest(collection.Id, json);

                // Verify we can search array elements
                SearchResult result = await client.Search.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Filters = new List<SearchFilter>
                    {
                        new SearchFilter("Tags", SearchConditionEnum.Equals, "green")
                    }
                });

                if (result.Documents.Count != 1) return TestOutcome.Fail("Could not search array element");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestGetDocumentWithoutContent()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("TestCollection");
                Document ingested = await client.Document.Ingest(collection.Id, @"{""Name"":""Joel""}");
                Document retrieved = await client.Document.ReadById(ingested.Id, includeContent: false);

                if (retrieved == null) return TestOutcome.Fail("Retrieved document is null");
                if (retrieved.Content != null) return TestOutcome.Fail("Content should be null");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestGetDocumentWithContent()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("TestCollection");
                Document ingested = await client.Document.Ingest(collection.Id, @"{""Name"":""Joel""}");
                Document retrieved = await client.Document.ReadById(ingested.Id, includeContent: true);

                if (retrieved.Content == null) return TestOutcome.Fail("Content is null");
                if (!retrieved.Content.Contains("Joel")) return TestOutcome.Fail("Content doesn't contain expected value");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestGetDocumentLabels()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("TestCollection");
                List<string> labels = new List<string> { "label1", "label2" };
                Document ingested = await client.Document.Ingest(collection.Id, @"{""Data"":""test""}", labels: labels);
                Document retrieved = await client.Document.ReadById(ingested.Id);

                if (retrieved.Labels.Count != 2) return TestOutcome.Fail($"Expected 2 labels, got {retrieved.Labels.Count}");
                if (!retrieved.Labels.Contains("label1")) return TestOutcome.Fail("Missing label1");
                if (!retrieved.Labels.Contains("label2")) return TestOutcome.Fail("Missing label2");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestGetDocumentTags()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("TestCollection");
                Dictionary<string, string> tags = new Dictionary<string, string> { { "key1", "val1" }, { "key2", "val2" } };
                Document ingested = await client.Document.Ingest(collection.Id, @"{""Data"":""test""}", tags: tags);
                Document retrieved = await client.Document.ReadById(ingested.Id);

                if (retrieved.Tags.Count != 2) return TestOutcome.Fail($"Expected 2 tags, got {retrieved.Tags.Count}");
                if (retrieved.Tags["key1"] != "val1") return TestOutcome.Fail("Wrong value for key1");
                if (retrieved.Tags["key2"] != "val2") return TestOutcome.Fail("Wrong value for key2");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestGetDocumentNonExistent()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Document retrieved = await client.Document.ReadById("doc_nonexistent");

                if (retrieved != null) return TestOutcome.Fail("Expected null");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestGetDocumentsEmpty()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("TestCollection");
                List<Document> docs = await client.Document.ReadAllInCollection(collection.Id);

                if (docs.Count != 0) return TestOutcome.Fail($"Expected 0, got {docs.Count}");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestGetDocumentsMultiple()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("TestCollection");
                await client.Document.Ingest(collection.Id, @"{""Name"":""Doc1""}");
                await client.Document.Ingest(collection.Id, @"{""Name"":""Doc2""}");
                await client.Document.Ingest(collection.Id, @"{""Name"":""Doc3""}");

                List<Document> docs = await client.Document.ReadAllInCollection(collection.Id);

                if (docs.Count != 3) return TestOutcome.Fail($"Expected 3, got {docs.Count}");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestGetDocumentsWithLabelsAndTags()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("TestCollection");
                await client.Document.Ingest(collection.Id, @"{""N"":1}",
                    labels: new List<string> { "a" },
                    tags: new Dictionary<string, string> { { "k", "v" } });

                List<Document> docs = await client.Document.ReadAllInCollection(collection.Id);

                if (docs[0].Labels.Count != 1) return TestOutcome.Fail("Labels not populated");
                if (docs[0].Tags.Count != 1) return TestOutcome.Fail("Tags not populated");
                if (!docs[0].Labels.Contains("a")) return TestOutcome.Fail("Wrong label");
                if (docs[0].Tags["k"] != "v") return TestOutcome.Fail("Wrong tag");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestDocumentExistsTrue()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("TestCollection");
                Document doc = await client.Document.Ingest(collection.Id, @"{""Data"":1}");

                bool exists = await client.Document.Exists(doc.Id);
                if (!exists) return TestOutcome.Fail("Expected true");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestDocumentExistsFalse()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                bool exists = await client.Document.Exists("doc_nonexistent");

                if (exists) return TestOutcome.Fail("Expected false");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestDeleteDocument()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("TestCollection");
                Document doc = await client.Document.Ingest(collection.Id, @"{""Data"":1}");

                await client.Document.Delete(doc.Id);

                bool exists = await client.Document.Exists(doc.Id);
                if (exists) return TestOutcome.Fail("Document still exists");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestDeleteDocumentFileRemoved()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("TestCollection");
                Document doc = await client.Document.Ingest(collection.Id, @"{""Data"":1}");

                // File is stored in collection's DocumentsDirectory, not testDir directly
                string filePath = Path.Combine(collection.DocumentsDirectory, $"{doc.Id}.json");
                if (!File.Exists(filePath)) return TestOutcome.Fail("File wasn't created");

                await client.Document.Delete(doc.Id);

                if (File.Exists(filePath)) return TestOutcome.Fail("File still exists after delete");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        #endregion

        #region Search API Tests

        private static async Task<TestOutcome> TestSearchEquals()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("Test");
                await client.Document.Ingest(collection.Id, @"{""Name"":""Joel""}");
                await client.Document.Ingest(collection.Id, @"{""Name"":""John""}");
                await client.Document.Ingest(collection.Id, @"{""Name"":""Joel""}");

                SearchResult result = await client.Search.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Filters = new List<SearchFilter>
                    {
                        new SearchFilter("Name", SearchConditionEnum.Equals, "Joel")
                    }
                });

                if (result.Documents.Count != 2) return TestOutcome.Fail($"Expected 2, got {result.Documents.Count}");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestSearchNotEquals()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("Test");
                await client.Document.Ingest(collection.Id, @"{""Name"":""Joel""}");
                await client.Document.Ingest(collection.Id, @"{""Name"":""John""}");
                await client.Document.Ingest(collection.Id, @"{""Name"":""Jane""}");

                SearchResult result = await client.Search.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Filters = new List<SearchFilter>
                    {
                        new SearchFilter("Name", SearchConditionEnum.NotEquals, "Joel")
                    }
                });

                if (result.Documents.Count != 2) return TestOutcome.Fail($"Expected 2, got {result.Documents.Count}");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestSearchGreaterThan()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("Test");
                await client.Document.Ingest(collection.Id, @"{""Age"":25}");
                await client.Document.Ingest(collection.Id, @"{""Age"":30}");
                await client.Document.Ingest(collection.Id, @"{""Age"":35}");

                SearchResult result = await client.Search.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Filters = new List<SearchFilter>
                    {
                        new SearchFilter("Age", SearchConditionEnum.GreaterThan, "30")
                    }
                });

                if (result.Documents.Count != 1) return TestOutcome.Fail($"Expected 1, got {result.Documents.Count}");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestSearchGreaterThanOrEqualTo()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("Test");
                await client.Document.Ingest(collection.Id, @"{""Age"":25}");
                await client.Document.Ingest(collection.Id, @"{""Age"":30}");
                await client.Document.Ingest(collection.Id, @"{""Age"":35}");

                SearchResult result = await client.Search.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Filters = new List<SearchFilter>
                    {
                        new SearchFilter("Age", SearchConditionEnum.GreaterThanOrEqualTo, "30")
                    }
                });

                if (result.Documents.Count != 2) return TestOutcome.Fail($"Expected 2, got {result.Documents.Count}");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestSearchLessThan()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("Test");
                await client.Document.Ingest(collection.Id, @"{""Age"":25}");
                await client.Document.Ingest(collection.Id, @"{""Age"":30}");
                await client.Document.Ingest(collection.Id, @"{""Age"":35}");

                SearchResult result = await client.Search.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Filters = new List<SearchFilter>
                    {
                        new SearchFilter("Age", SearchConditionEnum.LessThan, "30")
                    }
                });

                if (result.Documents.Count != 1) return TestOutcome.Fail($"Expected 1, got {result.Documents.Count}");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestSearchLessThanOrEqualTo()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("Test");
                await client.Document.Ingest(collection.Id, @"{""Age"":25}");
                await client.Document.Ingest(collection.Id, @"{""Age"":30}");
                await client.Document.Ingest(collection.Id, @"{""Age"":35}");

                SearchResult result = await client.Search.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Filters = new List<SearchFilter>
                    {
                        new SearchFilter("Age", SearchConditionEnum.LessThanOrEqualTo, "30")
                    }
                });

                if (result.Documents.Count != 2) return TestOutcome.Fail($"Expected 2, got {result.Documents.Count}");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestSearchContains()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("Test");
                await client.Document.Ingest(collection.Id, @"{""City"":""San Francisco""}");
                await client.Document.Ingest(collection.Id, @"{""City"":""San Diego""}");
                await client.Document.Ingest(collection.Id, @"{""City"":""New York""}");

                SearchResult result = await client.Search.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Filters = new List<SearchFilter>
                    {
                        new SearchFilter("City", SearchConditionEnum.Contains, "San")
                    }
                });

                if (result.Documents.Count != 2) return TestOutcome.Fail($"Expected 2, got {result.Documents.Count}");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestSearchStartsWith()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("Test");
                await client.Document.Ingest(collection.Id, @"{""Name"":""Johnson""}");
                await client.Document.Ingest(collection.Id, @"{""Name"":""John""}");
                await client.Document.Ingest(collection.Id, @"{""Name"":""James""}");

                SearchResult result = await client.Search.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Filters = new List<SearchFilter>
                    {
                        new SearchFilter("Name", SearchConditionEnum.StartsWith, "John")
                    }
                });

                if (result.Documents.Count != 2) return TestOutcome.Fail($"Expected 2, got {result.Documents.Count}");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestSearchEndsWith()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("Test");
                await client.Document.Ingest(collection.Id, @"{""Email"":""joel@test.com""}");
                await client.Document.Ingest(collection.Id, @"{""Email"":""john@test.org""}");
                await client.Document.Ingest(collection.Id, @"{""Email"":""jane@test.com""}");

                SearchResult result = await client.Search.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Filters = new List<SearchFilter>
                    {
                        new SearchFilter("Email", SearchConditionEnum.EndsWith, ".com")
                    }
                });

                if (result.Documents.Count != 2) return TestOutcome.Fail($"Expected 2, got {result.Documents.Count}");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestSearchIsNull()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("Test");
                await client.Document.Ingest(collection.Id, @"{""Name"":""Joel"",""Email"":null}");
                await client.Document.Ingest(collection.Id, @"{""Name"":""John"",""Email"":""john@test.com""}");

                SearchResult result = await client.Search.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Filters = new List<SearchFilter>
                    {
                        new SearchFilter("Email", SearchConditionEnum.IsNull, null)
                    }
                });

                if (result.Documents.Count != 1) return TestOutcome.Fail($"Expected 1, got {result.Documents.Count}");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestSearchIsNotNull()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("Test");
                await client.Document.Ingest(collection.Id, @"{""Name"":""Joel"",""Email"":null}");
                await client.Document.Ingest(collection.Id, @"{""Name"":""John"",""Email"":""john@test.com""}");

                SearchResult result = await client.Search.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Filters = new List<SearchFilter>
                    {
                        new SearchFilter("Email", SearchConditionEnum.IsNotNull, null)
                    }
                });

                if (result.Documents.Count != 1) return TestOutcome.Fail($"Expected 1, got {result.Documents.Count}");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestSearchMultipleFilters()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("Test");
                await client.Document.Ingest(collection.Id, @"{""City"":""Austin"",""Age"":30}");
                await client.Document.Ingest(collection.Id, @"{""City"":""Austin"",""Age"":40}");
                await client.Document.Ingest(collection.Id, @"{""City"":""Denver"",""Age"":30}");

                SearchResult result = await client.Search.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Filters = new List<SearchFilter>
                    {
                        new SearchFilter("City", SearchConditionEnum.Equals, "Austin"),
                        new SearchFilter("Age", SearchConditionEnum.GreaterThan, "35")
                    }
                });

                if (result.Documents.Count != 1) return TestOutcome.Fail($"Expected 1, got {result.Documents.Count}");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestSearchByLabel()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("Test");
                await client.Document.Ingest(collection.Id, @"{""N"":1}", labels: new List<string> { "important" });
                await client.Document.Ingest(collection.Id, @"{""N"":2}", labels: new List<string> { "normal" });
                await client.Document.Ingest(collection.Id, @"{""N"":3}", labels: new List<string> { "important" });

                SearchResult result = await client.Search.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Labels = new List<string> { "important" }
                });

                if (result.Documents.Count != 2) return TestOutcome.Fail($"Expected 2, got {result.Documents.Count}");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestSearchByTag()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("Test");
                await client.Document.Ingest(collection.Id, @"{""N"":1}",
                    tags: new Dictionary<string, string> { { "status", "active" } });
                await client.Document.Ingest(collection.Id, @"{""N"":2}",
                    tags: new Dictionary<string, string> { { "status", "inactive" } });
                await client.Document.Ingest(collection.Id, @"{""N"":3}",
                    tags: new Dictionary<string, string> { { "status", "active" } });

                SearchResult result = await client.Search.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Tags = new Dictionary<string, string> { { "status", "active" } }
                });

                if (result.Documents.Count != 2) return TestOutcome.Fail($"Expected 2, got {result.Documents.Count}");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestSearchByLabelAndTag()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("Test");
                await client.Document.Ingest(collection.Id, @"{""N"":1}",
                    labels: new List<string> { "important" },
                    tags: new Dictionary<string, string> { { "status", "active" } });
                await client.Document.Ingest(collection.Id, @"{""N"":2}",
                    labels: new List<string> { "important" },
                    tags: new Dictionary<string, string> { { "status", "inactive" } });
                await client.Document.Ingest(collection.Id, @"{""N"":3}",
                    labels: new List<string> { "normal" },
                    tags: new Dictionary<string, string> { { "status", "active" } });

                SearchResult result = await client.Search.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Labels = new List<string> { "important" },
                    Tags = new Dictionary<string, string> { { "status", "active" } }
                });

                if (result.Documents.Count != 1) return TestOutcome.Fail($"Expected 1, got {result.Documents.Count}");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestSearchPaginationSkip()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("Test");
                for (int i = 0; i < 10; i++)
                    await client.Document.Ingest(collection.Id, $@"{{""N"":{i}}}");

                SearchResult result = await client.Search.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Skip = 5
                });

                if (result.Documents.Count != 5) return TestOutcome.Fail($"Expected 5, got {result.Documents.Count}");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestSearchPaginationMaxResults()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("Test");
                for (int i = 0; i < 10; i++)
                    await client.Document.Ingest(collection.Id, $@"{{""N"":{i}}}");

                SearchResult result = await client.Search.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    MaxResults = 3
                });

                if (result.Documents.Count != 3) return TestOutcome.Fail($"Expected 3, got {result.Documents.Count}");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestSearchPaginationBoth()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("Test");
                for (int i = 0; i < 10; i++)
                    await client.Document.Ingest(collection.Id, $@"{{""N"":{i}}}");

                SearchResult result = await client.Search.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Skip = 3,
                    MaxResults = 4
                });

                if (result.Documents.Count != 4) return TestOutcome.Fail($"Expected 4, got {result.Documents.Count}");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestSearchTotalRecords()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("Test");
                for (int i = 0; i < 10; i++)
                    await client.Document.Ingest(collection.Id, $@"{{""N"":{i}}}");

                SearchResult result = await client.Search.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    MaxResults = 3
                });

                if (result.TotalRecords != 10) return TestOutcome.Fail($"Expected TotalRecords=10, got {result.TotalRecords}");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestSearchRecordsRemaining()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("Test");
                for (int i = 0; i < 10; i++)
                    await client.Document.Ingest(collection.Id, $@"{{""N"":{i}}}");

                SearchResult result = await client.Search.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Skip = 2,
                    MaxResults = 3
                });

                // Total=10, Skip=2, Returned=3, Remaining=10-2-3=5
                if (result.RecordsRemaining != 5) return TestOutcome.Fail($"Expected RecordsRemaining=5, got {result.RecordsRemaining}");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestSearchEndOfResultsTrue()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("Test");
                for (int i = 0; i < 5; i++)
                    await client.Document.Ingest(collection.Id, $@"{{""N"":{i}}}");

                SearchResult result = await client.Search.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    MaxResults = 10
                });

                if (!result.EndOfResults) return TestOutcome.Fail("Expected EndOfResults=true");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestSearchEndOfResultsFalse()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("Test");
                for (int i = 0; i < 10; i++)
                    await client.Document.Ingest(collection.Id, $@"{{""N"":{i}}}");

                SearchResult result = await client.Search.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    MaxResults = 5
                });

                if (result.EndOfResults) return TestOutcome.Fail("Expected EndOfResults=false");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestSearchTimestamp()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("Test");
                await client.Document.Ingest(collection.Id, @"{""N"":1}");

                DateTime before = DateTime.UtcNow.AddSeconds(-1);
                SearchResult result = await client.Search.Search(new SearchQuery { CollectionId = collection.Id });
                DateTime after = DateTime.UtcNow.AddSeconds(1);

                if (result.Timestamp == null) return TestOutcome.Fail("Timestamp is null");
                if (result.Timestamp.Start < before || result.Timestamp.Start > after)
                    return TestOutcome.Fail("Timestamp.Start out of range");
                if (result.Timestamp.End < result.Timestamp.Start)
                    return TestOutcome.Fail("Timestamp.End < Timestamp.Start");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestSearchEmptyResults()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("Test");
                await client.Document.Ingest(collection.Id, @"{""Name"":""Joel""}");

                SearchResult result = await client.Search.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Filters = new List<SearchFilter>
                    {
                        new SearchFilter("Name", SearchConditionEnum.Equals, "NonExistent")
                    }
                });

                if (result.Documents.Count != 0) return TestOutcome.Fail($"Expected 0, got {result.Documents.Count}");
                if (result.TotalRecords != 0) return TestOutcome.Fail($"Expected TotalRecords=0");
                if (!result.EndOfResults) return TestOutcome.Fail("Expected EndOfResults=true");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestSearchWithContent()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("Test");
                await client.Document.Ingest(collection.Id, @"{""Name"":""Joel""}");

                SearchResult result = await client.Search.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    IncludeContent = true
                });

                if (result.Documents[0].Content == null) return TestOutcome.Fail("Content is null");
                if (!result.Documents[0].Content.Contains("Joel")) return TestOutcome.Fail("Content missing expected value");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestSearchWithoutContent()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("Test");
                await client.Document.Ingest(collection.Id, @"{""Name"":""Joel""}");

                SearchResult result = await client.Search.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    IncludeContent = false
                });

                if (result.Documents[0].Content != null) return TestOutcome.Fail("Content should be null");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestSearchBySqlBasic()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("Test");
                await client.Document.Ingest(collection.Id, @"{""Name"":""Joel""}");
                await client.Document.Ingest(collection.Id, @"{""Name"":""John""}");

                SearchResult result = await client.Search.SearchBySql(collection.Id, "SELECT * FROM documents WHERE Name = 'Joel'");

                if (result.Documents.Count != 1) return TestOutcome.Fail($"Expected 1, got {result.Documents.Count}");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestSearchBySqlOrderBy()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("Test");
                await client.Document.Ingest(collection.Id, @"{""N"":1}");
                await Task.Delay(10);
                await client.Document.Ingest(collection.Id, @"{""N"":2}");

                SearchResult result = await client.Search.SearchBySql(collection.Id, "SELECT * FROM documents ORDER BY createdutc ASC");

                if (result.Documents.Count != 2) return TestOutcome.Fail($"Expected 2, got {result.Documents.Count}");
                // First doc should be older
                if (result.Documents[0].CreatedUtc > result.Documents[1].CreatedUtc)
                    return TestOutcome.Fail("Order incorrect");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestSearchBySqlLimitOffset()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("Test");
                for (int i = 0; i < 10; i++)
                    await client.Document.Ingest(collection.Id, $@"{{""N"":{i}}}");

                SearchResult result = await client.Search.SearchBySql(collection.Id, "SELECT * FROM documents LIMIT 3 OFFSET 2");

                if (result.Documents.Count != 3) return TestOutcome.Fail($"Expected 3, got {result.Documents.Count}");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        #endregion

        #region Enumeration API Tests

        private static async Task<TestOutcome> TestEnumerateBasic()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("Test");
                await client.Document.Ingest(collection.Id, @"{""N"":1}");
                await client.Document.Ingest(collection.Id, @"{""N"":2}");

                EnumerationResult<Document> result = await client.Search.Enumerate(new EnumerationQuery { CollectionId = collection.Id });

                if (result.Objects.Count != 2) return TestOutcome.Fail($"Expected 2, got {result.Objects.Count}");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestEnumerateMaxResults()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("Test");
                for (int i = 0; i < 10; i++)
                    await client.Document.Ingest(collection.Id, $@"{{""N"":{i}}}");

                EnumerationResult<Document> result = await client.Search.Enumerate(new EnumerationQuery
                {
                    CollectionId = collection.Id,
                    MaxResults = 5
                });

                if (result.Objects.Count != 5) return TestOutcome.Fail($"Expected 5, got {result.Objects.Count}");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestEnumerateSkip()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("Test");
                for (int i = 0; i < 10; i++)
                    await client.Document.Ingest(collection.Id, $@"{{""N"":{i}}}");

                EnumerationResult<Document> result = await client.Search.Enumerate(new EnumerationQuery
                {
                    CollectionId = collection.Id,
                    Skip = 7
                });

                if (result.Objects.Count != 3) return TestOutcome.Fail($"Expected 3, got {result.Objects.Count}");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestEnumeratePagination()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("Test");
                for (int i = 0; i < 10; i++)
                    await client.Document.Ingest(collection.Id, $@"{{""N"":{i}}}");

                // Page 1
                EnumerationResult<Document> page1 = await client.Search.Enumerate(new EnumerationQuery
                {
                    CollectionId = collection.Id,
                    Skip = 0,
                    MaxResults = 3
                });

                // Page 2
                EnumerationResult<Document> page2 = await client.Search.Enumerate(new EnumerationQuery
                {
                    CollectionId = collection.Id,
                    Skip = 3,
                    MaxResults = 3
                });

                if (page1.Objects.Count != 3) return TestOutcome.Fail("Page1 count wrong");
                if (page2.Objects.Count != 3) return TestOutcome.Fail("Page2 count wrong");

                // Ensure no overlap
                HashSet<string> page1Ids = page1.Objects.Select(d => d.Id).ToHashSet();
                HashSet<string> page2Ids = page2.Objects.Select(d => d.Id).ToHashSet();
                if (page1Ids.Intersect(page2Ids).Any()) return TestOutcome.Fail("Pages overlap");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestEnumerateCreatedAsc()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("Test");
                await client.Document.Ingest(collection.Id, @"{""N"":1}");
                await Task.Delay(10);
                await client.Document.Ingest(collection.Id, @"{""N"":2}");

                EnumerationResult<Document> result = await client.Search.Enumerate(new EnumerationQuery
                {
                    CollectionId = collection.Id,
                    Ordering = EnumerationOrderEnum.CreatedAscending
                });

                if (result.Objects[0].CreatedUtc > result.Objects[1].CreatedUtc)
                    return TestOutcome.Fail("Order incorrect");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestEnumerateCreatedDesc()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("Test");
                await client.Document.Ingest(collection.Id, @"{""N"":1}");
                await Task.Delay(10);
                await client.Document.Ingest(collection.Id, @"{""N"":2}");

                EnumerationResult<Document> result = await client.Search.Enumerate(new EnumerationQuery
                {
                    CollectionId = collection.Id,
                    Ordering = EnumerationOrderEnum.CreatedDescending
                });

                if (result.Objects[0].CreatedUtc < result.Objects[1].CreatedUtc)
                    return TestOutcome.Fail("Order incorrect");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestEnumerateTotalRecords()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("Test");
                for (int i = 0; i < 10; i++)
                    await client.Document.Ingest(collection.Id, $@"{{""N"":{i}}}");

                EnumerationResult<Document> result = await client.Search.Enumerate(new EnumerationQuery
                {
                    CollectionId = collection.Id,
                    MaxResults = 3
                });

                if (result.TotalRecords != 10) return TestOutcome.Fail($"Expected 10, got {result.TotalRecords}");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestEnumerateRecordsRemaining()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("Test");
                for (int i = 0; i < 10; i++)
                    await client.Document.Ingest(collection.Id, $@"{{""N"":{i}}}");

                EnumerationResult<Document> result = await client.Search.Enumerate(new EnumerationQuery
                {
                    CollectionId = collection.Id,
                    Skip = 2,
                    MaxResults = 3
                });

                if (result.RecordsRemaining != 5) return TestOutcome.Fail($"Expected 5, got {result.RecordsRemaining}");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestEnumerateEndOfResults()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("Test");
                for (int i = 0; i < 5; i++)
                    await client.Document.Ingest(collection.Id, $@"{{""N"":{i}}}");

                EnumerationResult<Document> result = await client.Search.Enumerate(new EnumerationQuery
                {
                    CollectionId = collection.Id,
                    MaxResults = 10
                });

                if (!result.EndOfResults) return TestOutcome.Fail("Expected EndOfResults=true");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestEnumerateEmpty()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("Test");

                EnumerationResult<Document> result = await client.Search.Enumerate(new EnumerationQuery { CollectionId = collection.Id });

                if (result.Objects.Count != 0) return TestOutcome.Fail($"Expected 0, got {result.Objects.Count}");
                if (result.TotalRecords != 0) return TestOutcome.Fail("Expected TotalRecords=0");
                if (!result.EndOfResults) return TestOutcome.Fail("Expected EndOfResults=true");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        #endregion

        #region Schema API Tests

        private static async Task<TestOutcome> TestGetSchemas()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("Test");
                await client.Document.Ingest(collection.Id, @"{""Name"":""Joel""}");

                List<Schema> schemas = await client.Schema.ReadAll();

                if (schemas.Count < 1) return TestOutcome.Fail("Expected at least 1 schema");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestGetSchemasReuse()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("Test");

                // Same structure = same schema
                Document doc1 = await client.Document.Ingest(collection.Id, @"{""Name"":""Joel""}");
                Document doc2 = await client.Document.Ingest(collection.Id, @"{""Name"":""John""}");

                if (doc1.SchemaId != doc2.SchemaId)
                    return TestOutcome.Fail("Expected same schema for same structure");

                // Different structure = different schema
                Document doc3 = await client.Document.Ingest(collection.Id, @"{""Age"":30}");

                if (doc1.SchemaId == doc3.SchemaId)
                    return TestOutcome.Fail("Expected different schema for different structure");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestGetSchemaById()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("Test");
                Document doc = await client.Document.Ingest(collection.Id, @"{""Name"":""Joel""}");

                Schema schema = await client.Schema.ReadById(doc.SchemaId);

                if (schema == null) return TestOutcome.Fail("Schema is null");
                if (schema.Id != doc.SchemaId) return TestOutcome.Fail("Id mismatch");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestGetSchemaNonExistent()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Schema schema = await client.Schema.ReadById("sch_nonexistent");

                if (schema != null) return TestOutcome.Fail("Expected null");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestGetSchemaElements()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("Test");
                Document doc = await client.Document.Ingest(collection.Id, @"{""Name"":""Joel"",""Age"":30}");

                List<SchemaElement> elements = await client.Schema.GetElements(doc.SchemaId);

                if (elements.Count != 2) return TestOutcome.Fail($"Expected 2 elements, got {elements.Count}");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestGetSchemaElementsKeys()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("Test");
                Document doc = await client.Document.Ingest(collection.Id, @"{""FirstName"":""Joel"",""LastName"":""C""}");

                List<SchemaElement> elements = await client.Schema.GetElements(doc.SchemaId);
                HashSet<string> keys = elements.Select(e => e.Key).ToHashSet();

                if (!keys.Contains("FirstName")) return TestOutcome.Fail("Missing FirstName");
                if (!keys.Contains("LastName")) return TestOutcome.Fail("Missing LastName");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestGetSchemaElementsTypes()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("Test");
                Document doc = await client.Document.Ingest(collection.Id, @"{""Name"":""Joel"",""Age"":30,""Active"":true}");

                List<SchemaElement> elements = await client.Schema.GetElements(doc.SchemaId);
                Dictionary<string, string> typesByKey = elements.ToDictionary(e => e.Key, e => e.DataType);

                if (!typesByKey["Name"].Contains("string")) return TestOutcome.Fail($"Name type wrong: {typesByKey["Name"]}");
                if (!typesByKey["Age"].Contains("int") && !typesByKey["Age"].Contains("number"))
                    return TestOutcome.Fail($"Age type wrong: {typesByKey["Age"]}");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        #endregion

        #region Index API Tests

        private static async Task<TestOutcome> TestGetIndexTableMappings()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("Test");
                await client.Document.Ingest(collection.Id, @"{""Name"":""Joel"",""Age"":30}");

                List<IndexTableMapping> mappings = await client.Index.GetMappings();

                if (mappings.Count < 2) return TestOutcome.Fail($"Expected at least 2 mappings, got {mappings.Count}");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestGetIndexTableMappingByKey()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("Test");
                await client.Document.Ingest(collection.Id, @"{""City"":""Austin""}");

                IndexTableMapping mapping = await client.Index.GetMappingByKey("City");

                if (mapping == null) return TestOutcome.Fail("Mapping is null");
                if (mapping.Key != "City") return TestOutcome.Fail($"Key mismatch: {mapping.Key}");
                if (string.IsNullOrEmpty(mapping.TableName)) return TestOutcome.Fail("TableName is empty");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestGetIndexTableMappingNonExistent()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                IndexTableMapping mapping = await client.Index.GetMappingByKey("NonExistentKey");

                if (mapping != null) return TestOutcome.Fail("Expected null");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        #endregion

        #region Flush API Tests

        private static async Task<TestOutcome> TestFlushPersistence()
        {
            string testDir = CreateTestDir();
            try
            {
                string dbPath = Path.Combine(testDir, "lattice.db");

                // Create in-memory, add data, flush
                using (LatticeClient client = new LatticeClient(new LatticeSettings
                {
                    InMemory = true,
                    DefaultDocumentsDirectory = testDir,
                    Database = new Lattice.Core.DatabaseSettings { Filename = dbPath }
                }))
                {
                    Collection collection = await client.Collection.Create("Test");
                    await client.Document.Ingest(collection.Id, @"{""Name"":""Joel""}");
                    client.Flush();
                }

                // Verify file was created
                if (!File.Exists(dbPath)) return TestOutcome.Fail("Database file not created");

                // Open from disk and verify data
                using (LatticeClient client = new LatticeClient(new LatticeSettings
                {
                    InMemory = false,
                    DefaultDocumentsDirectory = testDir,
                    Database = new Lattice.Core.DatabaseSettings { Filename = dbPath }
                }))
                {
                    List<Collection> collections = await client.Collection.ReadAll();
                    if (collections.Count != 1) return TestOutcome.Fail("Data not persisted");
                }

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        #endregion

        #region Edge Case Tests

        private static async Task<TestOutcome> TestEdgeEmptyStrings()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("Test");
                Document doc = await client.Document.Ingest(collection.Id, @"{""Name"":""""}");

                SearchResult result = await client.Search.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Filters = new List<SearchFilter>
                    {
                        new SearchFilter("Name", SearchConditionEnum.Equals, "")
                    }
                });

                if (result.Documents.Count != 1) return TestOutcome.Fail($"Expected 1, got {result.Documents.Count}");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestEdgeSpecialCharacters()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("Test");
                string json = @"{""Text"":""Hello 'World' with \""quotes\"" and <special> chars & more""}";
                Document doc = await client.Document.Ingest(collection.Id, json);

                Document retrieved = await client.Document.ReadById(doc.Id, includeContent: true);
                if (!retrieved.Content.Contains("Hello")) return TestOutcome.Fail("Content corrupted");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestEdgeDeeplyNested()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("Test");
                string json = @"{""L1"":{""L2"":{""L3"":{""L4"":{""L5"":""DeepValue""}}}}}";
                Document doc = await client.Document.Ingest(collection.Id, json);

                SearchResult result = await client.Search.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Filters = new List<SearchFilter>
                    {
                        new SearchFilter("L1.L2.L3.L4.L5", SearchConditionEnum.Equals, "DeepValue")
                    }
                });

                if (result.Documents.Count != 1) return TestOutcome.Fail("Deep search failed");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestEdgeLargeArray()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("Test");

                IEnumerable<string> items = Enumerable.Range(0, 100).Select(i => $"\"{i}\"");
                string json = $@"{{""Items"":[{string.Join(",", items)}]}}";
                Document doc = await client.Document.Ingest(collection.Id, json);

                SearchResult result = await client.Search.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Filters = new List<SearchFilter>
                    {
                        new SearchFilter("Items", SearchConditionEnum.Equals, "50")
                    }
                });

                if (result.Documents.Count != 1) return TestOutcome.Fail("Array search failed");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestEdgeNumericValues()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("Test");
                string json = @"{""Int"":42,""Float"":3.14,""Negative"":-100,""Large"":9999999999}";
                Document doc = await client.Document.Ingest(collection.Id, json);

                SearchResult result = await client.Search.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Filters = new List<SearchFilter>
                    {
                        new SearchFilter("Int", SearchConditionEnum.Equals, "42")
                    }
                });

                if (result.Documents.Count != 1) return TestOutcome.Fail("Numeric search failed");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestEdgeBooleanValues()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("Test");
                await client.Document.Ingest(collection.Id, @"{""Active"":true}");
                await client.Document.Ingest(collection.Id, @"{""Active"":false}");

                // Boolean values are stored as lowercase "true"/"false" per JSON convention
                SearchResult result = await client.Search.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Filters = new List<SearchFilter>
                    {
                        new SearchFilter("Active", SearchConditionEnum.Equals, "true")
                    }
                });

                if (result.Documents.Count != 1) return TestOutcome.Fail($"Expected 1, got {result.Documents.Count}");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestEdgeNullValues()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("Test");
                Document doc = await client.Document.Ingest(collection.Id, @"{""Name"":null,""Age"":30}");

                SearchResult result = await client.Search.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Filters = new List<SearchFilter>
                    {
                        new SearchFilter("Name", SearchConditionEnum.IsNull, null)
                    }
                });

                if (result.Documents.Count != 1) return TestOutcome.Fail("Null search failed");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestEdgeUnicodeCharacters()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("Test");
                string json = @"{""Name"":""日本語テスト"",""Emoji"":""🎉""}";
                Document doc = await client.Document.Ingest(collection.Id, json);

                Document retrieved = await client.Document.ReadById(doc.Id, includeContent: true);
                if (!retrieved.Content.Contains("日本語")) return TestOutcome.Fail("Unicode not preserved");

                SearchResult result = await client.Search.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Filters = new List<SearchFilter>
                    {
                        new SearchFilter("Name", SearchConditionEnum.Equals, "日本語テスト")
                    }
                });

                if (result.Documents.Count != 1) return TestOutcome.Fail("Unicode search failed");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        #endregion

        #region Performance Tests

        private static async Task<TestOutcome> TestPerfIngest100()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("Test");

                Stopwatch sw = Stopwatch.StartNew();
                for (int i = 0; i < 100; i++)
                {
                    await client.Document.Ingest(collection.Id, $@"{{""Name"":""User{i}"",""Age"":{20 + i % 50}}}");
                }
                sw.Stop();

                double docsPerSecond = 100.0 / sw.Elapsed.TotalSeconds;
                Console.WriteLine($"           Ingestion: {docsPerSecond:F1} docs/sec ({sw.ElapsedMilliseconds}ms total)");

                // Pass if > 10 docs/sec (very conservative)
                if (docsPerSecond < 10) return TestOutcome.Fail($"Too slow: {docsPerSecond:F1} docs/sec");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestPerfSearch100()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("Test");

                for (int i = 0; i < 100; i++)
                {
                    await client.Document.Ingest(collection.Id, $@"{{""Name"":""User{i}"",""City"":""{(i % 2 == 0 ? "Austin" : "Denver")}""}}");
                }

                Stopwatch sw = Stopwatch.StartNew();
                SearchResult result = await client.Search.Search(new SearchQuery
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

                if (result.Documents.Count != 50) return TestOutcome.Fail($"Expected 50, got {result.Documents.Count}");
                if (sw.ElapsedMilliseconds > 5000) return TestOutcome.Fail($"Too slow: {sw.ElapsedMilliseconds}ms");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestPerfGetDocuments100()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("Test");

                for (int i = 0; i < 100; i++)
                {
                    await client.Document.Ingest(collection.Id, $@"{{""N"":{i}}}",
                        labels: new List<string> { "label" },
                        tags: new Dictionary<string, string> { { "k", "v" } });
                }

                Stopwatch sw = Stopwatch.StartNew();
                List<Document> docs = await client.Document.ReadAllInCollection(collection.Id);
                sw.Stop();

                Console.WriteLine($"           GetDocuments: {sw.ElapsedMilliseconds}ms for {docs.Count} docs");

                if (docs.Count != 100) return TestOutcome.Fail($"Expected 100, got {docs.Count}");
                if (sw.ElapsedMilliseconds > 5000) return TestOutcome.Fail($"Too slow: {sw.ElapsedMilliseconds}ms");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestPerfEnumerate100()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("Test");

                for (int i = 0; i < 100; i++)
                {
                    await client.Document.Ingest(collection.Id, $@"{{""N"":{i}}}");
                }

                Stopwatch sw = Stopwatch.StartNew();
                EnumerationResult<Document> result = await client.Search.Enumerate(new EnumerationQuery
                {
                    CollectionId = collection.Id,
                    MaxResults = 100
                });
                sw.Stop();

                Console.WriteLine($"           Enumerate: {sw.ElapsedMilliseconds}ms for {result.Objects.Count} docs");

                if (result.Objects.Count != 100) return TestOutcome.Fail($"Expected 100, got {result.Objects.Count}");
                if (sw.ElapsedMilliseconds > 5000) return TestOutcome.Fail($"Too slow: {sw.ElapsedMilliseconds}ms");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        #endregion

        #region Integration Tests

        private static async Task<TestOutcome> TestIntegrationFullCrud()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);

                // Create
                Collection collection = await client.Collection.Create("People", "People collection");
                if (collection == null) return TestOutcome.Fail("Create collection failed");

                // Ingest
                Document doc = await client.Document.Ingest(collection.Id,
                    @"{""Name"":""Joel"",""Age"":40}",
                    "PersonDoc",
                    new List<string> { "employee" },
                    new Dictionary<string, string> { { "dept", "eng" } });
                if (doc == null) return TestOutcome.Fail("Ingest failed");

                // Read
                Document retrieved = await client.Document.ReadById(doc.Id, includeContent: true);
                if (retrieved == null) return TestOutcome.Fail("Read failed");
                if (retrieved.Name != "PersonDoc") return TestOutcome.Fail("Name mismatch");
                if (!retrieved.Labels.Contains("employee")) return TestOutcome.Fail("Labels not persisted");
                if (retrieved.Tags["dept"] != "eng") return TestOutcome.Fail("Tags not persisted");
                if (!retrieved.Content.Contains("Joel")) return TestOutcome.Fail("Content wrong");

                // Search
                SearchResult searchResult = await client.Search.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Filters = new List<SearchFilter>
                    {
                        new SearchFilter("Name", SearchConditionEnum.Equals, "Joel")
                    }
                });
                if (searchResult.Documents.Count != 1) return TestOutcome.Fail("Search failed");

                // Delete document
                await client.Document.Delete(doc.Id);
                if (await client.Document.Exists(doc.Id)) return TestOutcome.Fail("Document delete failed");

                // Delete collection
                await client.Collection.Delete(collection.Id);
                if (await client.Collection.Exists(collection.Id)) return TestOutcome.Fail("Collection delete failed");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestIntegrationMultipleCollections()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);

                Collection col1 = await client.Collection.Create("Collection1");
                Collection col2 = await client.Collection.Create("Collection2");

                await client.Document.Ingest(col1.Id, @"{""Data"":""Col1Doc""}");
                await client.Document.Ingest(col2.Id, @"{""Data"":""Col2Doc""}");

                List<Document> docs1 = await client.Document.ReadAllInCollection(col1.Id);
                List<Document> docs2 = await client.Document.ReadAllInCollection(col2.Id);

                if (docs1.Count != 1) return TestOutcome.Fail("Col1 doc count wrong");
                if (docs2.Count != 1) return TestOutcome.Fail("Col2 doc count wrong");

                // Search in col1 shouldn't find col2 docs
                SearchResult result = await client.Search.Search(new SearchQuery
                {
                    CollectionId = col1.Id,
                    Filters = new List<SearchFilter>
                    {
                        new SearchFilter("Data", SearchConditionEnum.Equals, "Col2Doc")
                    }
                });

                if (result.Documents.Count != 0) return TestOutcome.Fail("Collection isolation failed");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestIntegrationSchemaSharing()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("Test");

                // Ingest docs with same structure
                Document doc1 = await client.Document.Ingest(collection.Id, @"{""Name"":""A"",""Value"":1}");
                Document doc2 = await client.Document.Ingest(collection.Id, @"{""Name"":""B"",""Value"":2}");
                Document doc3 = await client.Document.Ingest(collection.Id, @"{""Name"":""C"",""Value"":3}");

                // All should share same schema
                if (doc1.SchemaId != doc2.SchemaId || doc2.SchemaId != doc3.SchemaId)
                    return TestOutcome.Fail("Schemas not shared");

                List<Schema> schemas = await client.Schema.ReadAll();
                if (schemas.Count != 1) return TestOutcome.Fail($"Expected 1 schema, got {schemas.Count}");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        #endregion

        #region Schema Constraints Tests

        private static async Task<TestOutcome> TestConstraintsStrictMode()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                List<FieldConstraint> constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Name", DataType = "string", Required = true },
                    new FieldConstraint { FieldPath = "Age", DataType = "integer", Required = true }
                };

                Collection collection = await client.Collection.Create(
                    "StrictTest",
                    schemaEnforcementMode: SchemaEnforcementMode.Strict,
                    fieldConstraints: constraints);

                if (collection.SchemaEnforcementMode != SchemaEnforcementMode.Strict)
                    return TestOutcome.Fail("SchemaEnforcementMode not set correctly");

                List<FieldConstraint> savedConstraints = await client.Collection.GetConstraints(collection.Id);
                if (savedConstraints.Count != 2)
                    return TestOutcome.Fail($"Expected 2 constraints, got {savedConstraints.Count}");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestConstraintsValidDocument()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                List<FieldConstraint> constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Name", DataType = "string", Required = true },
                    new FieldConstraint { FieldPath = "Age", DataType = "integer", Required = true }
                };

                Collection collection = await client.Collection.Create(
                    "ValidDocTest",
                    schemaEnforcementMode: SchemaEnforcementMode.Flexible,
                    fieldConstraints: constraints);

                Document doc = await client.Document.Ingest(collection.Id, @"{""Name"":""Joel"",""Age"":30}");
                if (doc == null) return TestOutcome.Fail("Document should have been created");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestConstraintsMissingRequired()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                List<FieldConstraint> constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Name", DataType = "string", Required = true },
                    new FieldConstraint { FieldPath = "Age", DataType = "integer", Required = true }
                };

                Collection collection = await client.Collection.Create(
                    "MissingFieldTest",
                    schemaEnforcementMode: SchemaEnforcementMode.Flexible,
                    fieldConstraints: constraints);

                try
                {
                    await client.Document.Ingest(collection.Id, @"{""Name"":""Joel""}");
                    return TestOutcome.Fail("Should have thrown SchemaValidationException");
                }
                catch (SchemaValidationException ex)
                {
                    if (ex.Errors.Count == 0) return TestOutcome.Fail("Expected validation errors");
                    if (!ex.Errors.Any(e => e.ErrorCode == "MISSING_REQUIRED_FIELD"))
                        return TestOutcome.Fail("Expected MISSING_REQUIRED_FIELD error");
                    return TestOutcome.Pass();
                }
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestConstraintsTypeMismatch()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                List<FieldConstraint> constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Age", DataType = "integer", Required = true }
                };

                Collection collection = await client.Collection.Create(
                    "TypeMismatchTest",
                    schemaEnforcementMode: SchemaEnforcementMode.Flexible,
                    fieldConstraints: constraints);

                try
                {
                    await client.Document.Ingest(collection.Id, @"{""Age"":""not a number""}");
                    return TestOutcome.Fail("Should have thrown SchemaValidationException");
                }
                catch (SchemaValidationException ex)
                {
                    if (!ex.Errors.Any(e => e.ErrorCode == "TYPE_MISMATCH"))
                        return TestOutcome.Fail("Expected TYPE_MISMATCH error");
                    return TestOutcome.Pass();
                }
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestConstraintsUpdate()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("UpdateConstraintsTest");

                List<FieldConstraint> initial = await client.Collection.GetConstraints(collection.Id);
                if (initial.Count != 0) return TestOutcome.Fail("Expected no initial constraints");

                List<FieldConstraint> constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Email", DataType = "string", Required = true }
                };
                await client.Collection.UpdateConstraints(collection.Id, SchemaEnforcementMode.Flexible, constraints);

                List<FieldConstraint> updated = await client.Collection.GetConstraints(collection.Id);
                if (updated.Count != 1) return TestOutcome.Fail("Expected 1 constraint after update");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        // Type validation tests
        private static async Task<TestOutcome> TestConstraintsStringType()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                List<FieldConstraint> constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Name", DataType = "string", Required = true }
                };

                Collection collection = await client.Collection.Create(
                    "StringTypeTest",
                    schemaEnforcementMode: SchemaEnforcementMode.Flexible,
                    fieldConstraints: constraints);

                // String should pass
                Document doc = await client.Document.Ingest(collection.Id, @"{""Name"":""Joel""}");
                if (doc == null) return TestOutcome.Fail("String value should pass");

                // Number should fail
                try
                {
                    await client.Document.Ingest(collection.Id, @"{""Name"":123}");
                    return TestOutcome.Fail("Number for string field should fail");
                }
                catch (SchemaValidationException) { }

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestConstraintsIntegerType()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                List<FieldConstraint> constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Count", DataType = "integer", Required = true }
                };

                Collection collection = await client.Collection.Create(
                    "IntegerTypeTest",
                    schemaEnforcementMode: SchemaEnforcementMode.Flexible,
                    fieldConstraints: constraints);

                // Integer should pass
                Document doc = await client.Document.Ingest(collection.Id, @"{""Count"":42}");
                if (doc == null) return TestOutcome.Fail("Integer value should pass");

                // Decimal should fail for integer type
                try
                {
                    await client.Document.Ingest(collection.Id, @"{""Count"":3.14}");
                    return TestOutcome.Fail("Decimal for integer field should fail");
                }
                catch (SchemaValidationException) { }

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestConstraintsNumberType()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                List<FieldConstraint> constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Price", DataType = "number", Required = true }
                };

                Collection collection = await client.Collection.Create(
                    "NumberTypeTest",
                    schemaEnforcementMode: SchemaEnforcementMode.Flexible,
                    fieldConstraints: constraints);

                // Integer should pass for number
                Document doc1 = await client.Document.Ingest(collection.Id, @"{""Price"":42}");
                if (doc1 == null) return TestOutcome.Fail("Integer should pass for number type");

                // Decimal should also pass
                Document doc2 = await client.Document.Ingest(collection.Id, @"{""Price"":19.99}");
                if (doc2 == null) return TestOutcome.Fail("Decimal should pass for number type");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestConstraintsBooleanType()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                List<FieldConstraint> constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Active", DataType = "boolean", Required = true }
                };

                Collection collection = await client.Collection.Create(
                    "BooleanTypeTest",
                    schemaEnforcementMode: SchemaEnforcementMode.Flexible,
                    fieldConstraints: constraints);

                // true should pass
                Document doc1 = await client.Document.Ingest(collection.Id, @"{""Active"":true}");
                if (doc1 == null) return TestOutcome.Fail("true should pass for boolean");

                // false should pass
                Document doc2 = await client.Document.Ingest(collection.Id, @"{""Active"":false}");
                if (doc2 == null) return TestOutcome.Fail("false should pass for boolean");

                // String "true" should fail
                try
                {
                    await client.Document.Ingest(collection.Id, @"{""Active"":""true""}");
                    return TestOutcome.Fail("String 'true' for boolean should fail");
                }
                catch (SchemaValidationException) { }

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestConstraintsArrayType()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                List<FieldConstraint> constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Tags", DataType = "array", Required = true }
                };

                Collection collection = await client.Collection.Create(
                    "ArrayTypeTest",
                    schemaEnforcementMode: SchemaEnforcementMode.Flexible,
                    fieldConstraints: constraints);

                // Array should pass
                Document doc = await client.Document.Ingest(collection.Id, @"{""Tags"":[""a"",""b"",""c""]}");
                if (doc == null) return TestOutcome.Fail("Array value should pass");

                // String should fail
                try
                {
                    await client.Document.Ingest(collection.Id, @"{""Tags"":""not an array""}");
                    return TestOutcome.Fail("String for array field should fail");
                }
                catch (SchemaValidationException) { }

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        // Nullable tests
        private static async Task<TestOutcome> TestConstraintsNullableAcceptsNull()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                List<FieldConstraint> constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "MiddleName", DataType = "string", Required = true, Nullable = true }
                };

                Collection collection = await client.Collection.Create(
                    "NullableTest",
                    schemaEnforcementMode: SchemaEnforcementMode.Flexible,
                    fieldConstraints: constraints);

                Document doc = await client.Document.Ingest(collection.Id, @"{""MiddleName"":null}");
                if (doc == null) return TestOutcome.Fail("Null should be accepted for nullable field");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestConstraintsNonNullableRejectsNull()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                List<FieldConstraint> constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Name", DataType = "string", Required = true, Nullable = false }
                };

                Collection collection = await client.Collection.Create(
                    "NonNullableTest",
                    schemaEnforcementMode: SchemaEnforcementMode.Flexible,
                    fieldConstraints: constraints);

                try
                {
                    await client.Document.Ingest(collection.Id, @"{""Name"":null}");
                    return TestOutcome.Fail("Null should be rejected for non-nullable field");
                }
                catch (SchemaValidationException ex)
                {
                    if (!ex.Errors.Any(e => e.ErrorCode == "NULL_NOT_ALLOWED"))
                        return TestOutcome.Fail("Expected NULL_NOT_ALLOWED error");
                    return TestOutcome.Pass();
                }
            }
            finally { CleanupTestDir(testDir); }
        }

        // Regex pattern tests
        private static async Task<TestOutcome> TestConstraintsRegexPatternSuccess()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                List<FieldConstraint> constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Code", DataType = "string", Required = true, RegexPattern = @"^[A-Z]{3}-\d{4}$" }
                };

                Collection collection = await client.Collection.Create(
                    "RegexSuccessTest",
                    schemaEnforcementMode: SchemaEnforcementMode.Flexible,
                    fieldConstraints: constraints);

                Document doc = await client.Document.Ingest(collection.Id, @"{""Code"":""ABC-1234""}");
                if (doc == null) return TestOutcome.Fail("Valid pattern should pass");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestConstraintsRegexPatternFails()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                List<FieldConstraint> constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Code", DataType = "string", Required = true, RegexPattern = @"^[A-Z]{3}-\d{4}$" }
                };

                Collection collection = await client.Collection.Create(
                    "RegexFailTest",
                    schemaEnforcementMode: SchemaEnforcementMode.Flexible,
                    fieldConstraints: constraints);

                try
                {
                    await client.Document.Ingest(collection.Id, @"{""Code"":""invalid""}");
                    return TestOutcome.Fail("Invalid pattern should fail");
                }
                catch (SchemaValidationException ex)
                {
                    if (!ex.Errors.Any(e => e.ErrorCode == "PATTERN_MISMATCH"))
                        return TestOutcome.Fail("Expected PATTERN_MISMATCH error");
                    return TestOutcome.Pass();
                }
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestConstraintsEmailPattern()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                List<FieldConstraint> constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Email", DataType = "string", Required = true, RegexPattern = @"^[\w\.-]+@[\w\.-]+\.\w+$" }
                };

                Collection collection = await client.Collection.Create(
                    "EmailPatternTest",
                    schemaEnforcementMode: SchemaEnforcementMode.Flexible,
                    fieldConstraints: constraints);

                // Valid email
                Document doc = await client.Document.Ingest(collection.Id, @"{""Email"":""user@example.com""}");
                if (doc == null) return TestOutcome.Fail("Valid email should pass");

                // Invalid email
                try
                {
                    await client.Document.Ingest(collection.Id, @"{""Email"":""not-an-email""}");
                    return TestOutcome.Fail("Invalid email should fail");
                }
                catch (SchemaValidationException) { }

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        // Range validation tests
        private static async Task<TestOutcome> TestConstraintsMinValueSuccess()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                List<FieldConstraint> constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Age", DataType = "integer", Required = true, MinValue = 18 }
                };

                Collection collection = await client.Collection.Create(
                    "MinValueSuccessTest",
                    schemaEnforcementMode: SchemaEnforcementMode.Flexible,
                    fieldConstraints: constraints);

                Document doc = await client.Document.Ingest(collection.Id, @"{""Age"":21}");
                if (doc == null) return TestOutcome.Fail("Value above min should pass");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestConstraintsMinValueFails()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                List<FieldConstraint> constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Age", DataType = "integer", Required = true, MinValue = 18 }
                };

                Collection collection = await client.Collection.Create(
                    "MinValueFailTest",
                    schemaEnforcementMode: SchemaEnforcementMode.Flexible,
                    fieldConstraints: constraints);

                try
                {
                    await client.Document.Ingest(collection.Id, @"{""Age"":16}");
                    return TestOutcome.Fail("Value below min should fail");
                }
                catch (SchemaValidationException ex)
                {
                    if (!ex.Errors.Any(e => e.ErrorCode == "VALUE_TOO_SMALL"))
                        return TestOutcome.Fail("Expected VALUE_TOO_SMALL error");
                    return TestOutcome.Pass();
                }
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestConstraintsMaxValueSuccess()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                List<FieldConstraint> constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Percent", DataType = "number", Required = true, MaxValue = 100 }
                };

                Collection collection = await client.Collection.Create(
                    "MaxValueSuccessTest",
                    schemaEnforcementMode: SchemaEnforcementMode.Flexible,
                    fieldConstraints: constraints);

                Document doc = await client.Document.Ingest(collection.Id, @"{""Percent"":75}");
                if (doc == null) return TestOutcome.Fail("Value below max should pass");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestConstraintsMaxValueFails()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                List<FieldConstraint> constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Percent", DataType = "number", Required = true, MaxValue = 100 }
                };

                Collection collection = await client.Collection.Create(
                    "MaxValueFailTest",
                    schemaEnforcementMode: SchemaEnforcementMode.Flexible,
                    fieldConstraints: constraints);

                try
                {
                    await client.Document.Ingest(collection.Id, @"{""Percent"":150}");
                    return TestOutcome.Fail("Value above max should fail");
                }
                catch (SchemaValidationException ex)
                {
                    if (!ex.Errors.Any(e => e.ErrorCode == "VALUE_TOO_LARGE"))
                        return TestOutcome.Fail("Expected VALUE_TOO_LARGE error");
                    return TestOutcome.Pass();
                }
            }
            finally { CleanupTestDir(testDir); }
        }

        // Length validation tests
        private static async Task<TestOutcome> TestConstraintsMinLengthStringSuccess()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                List<FieldConstraint> constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Password", DataType = "string", Required = true, MinLength = 8 }
                };

                Collection collection = await client.Collection.Create(
                    "MinLengthSuccessTest",
                    schemaEnforcementMode: SchemaEnforcementMode.Flexible,
                    fieldConstraints: constraints);

                Document doc = await client.Document.Ingest(collection.Id, @"{""Password"":""password123""}");
                if (doc == null) return TestOutcome.Fail("String meeting minLength should pass");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestConstraintsMinLengthStringFails()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                List<FieldConstraint> constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Password", DataType = "string", Required = true, MinLength = 8 }
                };

                Collection collection = await client.Collection.Create(
                    "MinLengthFailTest",
                    schemaEnforcementMode: SchemaEnforcementMode.Flexible,
                    fieldConstraints: constraints);

                try
                {
                    await client.Document.Ingest(collection.Id, @"{""Password"":""short""}");
                    return TestOutcome.Fail("String below minLength should fail");
                }
                catch (SchemaValidationException ex)
                {
                    if (!ex.Errors.Any(e => e.ErrorCode == "STRING_TOO_SHORT"))
                        return TestOutcome.Fail("Expected STRING_TOO_SHORT error");
                    return TestOutcome.Pass();
                }
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestConstraintsMaxLengthStringSuccess()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                List<FieldConstraint> constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Username", DataType = "string", Required = true, MaxLength = 20 }
                };

                Collection collection = await client.Collection.Create(
                    "MaxLengthSuccessTest",
                    schemaEnforcementMode: SchemaEnforcementMode.Flexible,
                    fieldConstraints: constraints);

                Document doc = await client.Document.Ingest(collection.Id, @"{""Username"":""joel""}");
                if (doc == null) return TestOutcome.Fail("String within maxLength should pass");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestConstraintsMaxLengthStringFails()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                List<FieldConstraint> constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Username", DataType = "string", Required = true, MaxLength = 10 }
                };

                Collection collection = await client.Collection.Create(
                    "MaxLengthFailTest",
                    schemaEnforcementMode: SchemaEnforcementMode.Flexible,
                    fieldConstraints: constraints);

                try
                {
                    await client.Document.Ingest(collection.Id, @"{""Username"":""verylongusername""}");
                    return TestOutcome.Fail("String exceeding maxLength should fail");
                }
                catch (SchemaValidationException ex)
                {
                    if (!ex.Errors.Any(e => e.ErrorCode == "STRING_TOO_LONG"))
                        return TestOutcome.Fail("Expected STRING_TOO_LONG error");
                    return TestOutcome.Pass();
                }
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestConstraintsArrayMinLengthSuccess()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                List<FieldConstraint> constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Tags", DataType = "array", Required = true, MinLength = 2 }
                };

                Collection collection = await client.Collection.Create(
                    "ArrayMinLengthSuccessTest",
                    schemaEnforcementMode: SchemaEnforcementMode.Flexible,
                    fieldConstraints: constraints);

                Document doc = await client.Document.Ingest(collection.Id, @"{""Tags"":[""a"",""b"",""c""]}");
                if (doc == null) return TestOutcome.Fail("Array meeting minLength should pass");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestConstraintsArrayMaxLengthFails()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                List<FieldConstraint> constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Tags", DataType = "array", Required = true, MaxLength = 3 }
                };

                Collection collection = await client.Collection.Create(
                    "ArrayMaxLengthFailTest",
                    schemaEnforcementMode: SchemaEnforcementMode.Flexible,
                    fieldConstraints: constraints);

                try
                {
                    await client.Document.Ingest(collection.Id, @"{""Tags"":[""a"",""b"",""c"",""d"",""e""]}");
                    return TestOutcome.Fail("Array exceeding maxLength should fail");
                }
                catch (SchemaValidationException ex)
                {
                    if (!ex.Errors.Any(e => e.ErrorCode == "ARRAY_TOO_LONG"))
                        return TestOutcome.Fail("Expected ARRAY_TOO_LONG error");
                    return TestOutcome.Pass();
                }
            }
            finally { CleanupTestDir(testDir); }
        }

        // Allowed values tests
        private static async Task<TestOutcome> TestConstraintsAllowedValuesSuccess()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                List<FieldConstraint> constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Status", DataType = "string", Required = true, AllowedValues = new List<string> { "active", "inactive", "pending" } }
                };

                Collection collection = await client.Collection.Create(
                    "AllowedValuesSuccessTest",
                    schemaEnforcementMode: SchemaEnforcementMode.Flexible,
                    fieldConstraints: constraints);

                Document doc = await client.Document.Ingest(collection.Id, @"{""Status"":""active""}");
                if (doc == null) return TestOutcome.Fail("Value in allowed list should pass");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestConstraintsAllowedValuesFails()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                List<FieldConstraint> constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Status", DataType = "string", Required = true, AllowedValues = new List<string> { "active", "inactive", "pending" } }
                };

                Collection collection = await client.Collection.Create(
                    "AllowedValuesFailTest",
                    schemaEnforcementMode: SchemaEnforcementMode.Flexible,
                    fieldConstraints: constraints);

                try
                {
                    await client.Document.Ingest(collection.Id, @"{""Status"":""deleted""}");
                    return TestOutcome.Fail("Value not in allowed list should fail");
                }
                catch (SchemaValidationException ex)
                {
                    if (!ex.Errors.Any(e => e.ErrorCode == "VALUE_NOT_ALLOWED"))
                        return TestOutcome.Fail("Expected VALUE_NOT_ALLOWED error");
                    return TestOutcome.Pass();
                }
            }
            finally { CleanupTestDir(testDir); }
        }

        // Enforcement mode tests
        private static async Task<TestOutcome> TestConstraintsStrictModeRejectsExtra()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                List<FieldConstraint> constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Name", DataType = "string", Required = true }
                };

                Collection collection = await client.Collection.Create(
                    "StrictModeRejectsExtraTest",
                    schemaEnforcementMode: SchemaEnforcementMode.Strict,
                    fieldConstraints: constraints);

                try
                {
                    await client.Document.Ingest(collection.Id, @"{""Name"":""Joel"",""ExtraField"":""value""}");
                    return TestOutcome.Fail("Strict mode should reject extra fields");
                }
                catch (SchemaValidationException ex)
                {
                    if (!ex.Errors.Any(e => e.ErrorCode == "UNEXPECTED_FIELD"))
                        return TestOutcome.Fail("Expected UNEXPECTED_FIELD error");
                    return TestOutcome.Pass();
                }
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestConstraintsFlexibleModeAllowsExtra()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                List<FieldConstraint> constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Name", DataType = "string", Required = true }
                };

                Collection collection = await client.Collection.Create(
                    "FlexibleModeAllowsExtraTest",
                    schemaEnforcementMode: SchemaEnforcementMode.Flexible,
                    fieldConstraints: constraints);

                Document doc = await client.Document.Ingest(collection.Id, @"{""Name"":""Joel"",""ExtraField"":""value""}");
                if (doc == null) return TestOutcome.Fail("Flexible mode should allow extra fields");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestConstraintsPartialMode()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                List<FieldConstraint> constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Email", DataType = "string", Required = true, RegexPattern = @"^[\w\.-]+@[\w\.-]+\.\w+$" }
                };

                Collection collection = await client.Collection.Create(
                    "PartialModeTest",
                    schemaEnforcementMode: SchemaEnforcementMode.Partial,
                    fieldConstraints: constraints);

                // Document without the specified field should pass in Partial mode
                Document doc = await client.Document.Ingest(collection.Id, @"{""Name"":""Joel"",""Age"":30}");
                if (doc == null) return TestOutcome.Fail("Partial mode should allow missing specified fields");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestConstraintsNoneMode()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                List<FieldConstraint> constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Name", DataType = "string", Required = true }
                };

                Collection collection = await client.Collection.Create(
                    "NoneModeTest",
                    schemaEnforcementMode: SchemaEnforcementMode.None,
                    fieldConstraints: constraints);

                // Any document should pass with None mode
                Document doc = await client.Document.Ingest(collection.Id, @"{""Whatever"":123}");
                if (doc == null) return TestOutcome.Fail("None mode should skip all validation");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        // Nested field tests
        private static async Task<TestOutcome> TestConstraintsNestedField()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                List<FieldConstraint> constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Address.City", DataType = "string", Required = true }
                };

                Collection collection = await client.Collection.Create(
                    "NestedFieldTest",
                    schemaEnforcementMode: SchemaEnforcementMode.Flexible,
                    fieldConstraints: constraints);

                Document doc = await client.Document.Ingest(collection.Id, @"{""Address"":{""City"":""Seattle"",""State"":""WA""}}");
                if (doc == null) return TestOutcome.Fail("Nested field validation should work");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestConstraintsDeeplyNestedField()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                List<FieldConstraint> constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Person.Contact.Address.ZipCode", DataType = "string", Required = true }
                };

                Collection collection = await client.Collection.Create(
                    "DeeplyNestedTest",
                    schemaEnforcementMode: SchemaEnforcementMode.Flexible,
                    fieldConstraints: constraints);

                Document doc = await client.Document.Ingest(collection.Id, @"{""Person"":{""Contact"":{""Address"":{""ZipCode"":""98101""}}}}");
                if (doc == null) return TestOutcome.Fail("Deeply nested field validation should work");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        // Array element type tests
        private static async Task<TestOutcome> TestConstraintsArrayElementTypeSuccess()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                List<FieldConstraint> constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Scores", DataType = "array", Required = true, ArrayElementType = "integer" }
                };

                Collection collection = await client.Collection.Create(
                    "ArrayElementTypeSuccessTest",
                    schemaEnforcementMode: SchemaEnforcementMode.Flexible,
                    fieldConstraints: constraints);

                Document doc = await client.Document.Ingest(collection.Id, @"{""Scores"":[90,85,92,88]}");
                if (doc == null) return TestOutcome.Fail("Array with correct element type should pass");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestConstraintsArrayElementTypeFails()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                List<FieldConstraint> constraints = new List<FieldConstraint>
                {
                    new FieldConstraint { FieldPath = "Scores", DataType = "array", Required = true, ArrayElementType = "integer" }
                };

                Collection collection = await client.Collection.Create(
                    "ArrayElementTypeFailTest",
                    schemaEnforcementMode: SchemaEnforcementMode.Flexible,
                    fieldConstraints: constraints);

                try
                {
                    await client.Document.Ingest(collection.Id, @"{""Scores"":[90,""not a number"",92]}");
                    return TestOutcome.Fail("Array with wrong element type should fail");
                }
                catch (SchemaValidationException ex)
                {
                    if (!ex.Errors.Any(e => e.ErrorCode == "INVALID_ARRAY_ELEMENT"))
                        return TestOutcome.Fail("Expected INVALID_ARRAY_ELEMENT error");
                    return TestOutcome.Pass();
                }
            }
            finally { CleanupTestDir(testDir); }
        }

        #endregion

        #region Indexing Mode Tests

        private static async Task<TestOutcome> TestIndexingSelectiveMode()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create(
                    "SelectiveIndexTest",
                    indexingMode: IndexingMode.Selective,
                    indexedFields: new List<string> { "Name" });

                if (collection.IndexingMode != IndexingMode.Selective)
                    return TestOutcome.Fail("IndexingMode not set correctly");

                List<IndexedField> indexedFields = await client.Collection.GetIndexedFields(collection.Id);
                if (indexedFields.Count != 1 || indexedFields[0].FieldPath != "Name")
                    return TestOutcome.Fail("Indexed fields not set correctly");

                await client.Document.Ingest(collection.Id, @"{""Name"":""Joel"",""Age"":30}");

                SearchResult nameResult = await client.Search.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Filters = new List<SearchFilter> { new SearchFilter("Name", SearchConditionEnum.Equals, "Joel") }
                });
                if (nameResult.Documents.Count != 1)
                    return TestOutcome.Fail("Search by Name should find document");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestIndexingNoneMode()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create(
                    "NoIndexTest",
                    indexingMode: IndexingMode.None);

                if (collection.IndexingMode != IndexingMode.None)
                    return TestOutcome.Fail("IndexingMode not set correctly");

                Document doc = await client.Document.Ingest(collection.Id, @"{""Name"":""Joel"",""Age"":30}");
                if (doc == null) return TestOutcome.Fail("Document should be created");

                Document retrieved = await client.Document.ReadById(doc.Id);
                if (retrieved == null) return TestOutcome.Fail("Document should be retrievable by ID");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestIndexingUpdateMode()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("UpdateIndexingTest");

                if (collection.IndexingMode != IndexingMode.All)
                    return TestOutcome.Fail("Expected initial All mode");

                await client.Collection.UpdateIndexing(
                    collection.Id,
                    IndexingMode.Selective,
                    new List<string> { "Email" });

                Collection updated = await client.Collection.ReadById(collection.Id);
                if (updated.IndexingMode != IndexingMode.Selective)
                    return TestOutcome.Fail("IndexingMode not updated");

                List<IndexedField> indexedFields = await client.Collection.GetIndexedFields(collection.Id);
                if (indexedFields.Count != 1) return TestOutcome.Fail("Expected 1 indexed field");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestIndexingRebuildIndexes()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("RebuildTest");

                await client.Document.Ingest(collection.Id, @"{""Name"":""Joel"",""Age"":30}");
                await client.Document.Ingest(collection.Id, @"{""Name"":""John"",""Age"":25}");

                await client.Collection.UpdateIndexing(
                    collection.Id,
                    IndexingMode.Selective,
                    new List<string> { "Name" });

                IndexRebuildResult result = await client.Collection.RebuildIndexes(collection.Id, dropUnusedIndexes: true);

                if (result.DocumentsProcessed != 2)
                    return TestOutcome.Fail($"Expected 2 documents processed, got {result.DocumentsProcessed}");

                if (!result.Success)
                    return TestOutcome.Fail($"Rebuild failed: {string.Join(", ", result.Errors)}");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestIndexingSearchNonIndexedField()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create(
                    "SearchNonIndexedTest",
                    indexingMode: IndexingMode.Selective,
                    indexedFields: new List<string> { "Name" });

                await client.Document.Ingest(collection.Id, @"{""Name"":""Joel"",""Age"":30}");

                // Search by non-indexed field should return empty
                SearchResult ageResult = await client.Search.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Filters = new List<SearchFilter> { new SearchFilter("Age", SearchConditionEnum.Equals, "30") }
                });

                if (ageResult.Documents.Count != 0)
                    return TestOutcome.Fail("Search by non-indexed field should return empty");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestIndexingAllModeIndexesAll()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create(
                    "AllModeTest",
                    indexingMode: IndexingMode.All);

                await client.Document.Ingest(collection.Id, @"{""Name"":""Joel"",""Age"":30,""City"":""Seattle""}");

                // Search by any field should work
                SearchResult nameResult = await client.Search.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Filters = new List<SearchFilter> { new SearchFilter("Name", SearchConditionEnum.Equals, "Joel") }
                });
                if (nameResult.Documents.Count != 1) return TestOutcome.Fail("Name search should work");

                SearchResult ageResult = await client.Search.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Filters = new List<SearchFilter> { new SearchFilter("Age", SearchConditionEnum.Equals, "30") }
                });
                if (ageResult.Documents.Count != 1) return TestOutcome.Fail("Age search should work");

                SearchResult cityResult = await client.Search.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Filters = new List<SearchFilter> { new SearchFilter("City", SearchConditionEnum.Equals, "Seattle") }
                });
                if (cityResult.Documents.Count != 1) return TestOutcome.Fail("City search should work");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestIndexingNestedFieldSelective()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create(
                    "NestedIndexTest",
                    indexingMode: IndexingMode.Selective,
                    indexedFields: new List<string> { "Address.City" });

                await client.Document.Ingest(collection.Id, @"{""Name"":""Joel"",""Address"":{""City"":""Seattle"",""State"":""WA""}}");

                // Search by indexed nested field should work
                SearchResult cityResult = await client.Search.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Filters = new List<SearchFilter> { new SearchFilter("Address.City", SearchConditionEnum.Equals, "Seattle") }
                });
                if (cityResult.Documents.Count != 1)
                    return TestOutcome.Fail("Search by indexed nested field should work");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestIndexingRebuildWithProgress()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("ProgressTest");

                for (int i = 0; i < 10; i++)
                {
                    await client.Document.Ingest(collection.Id, $@"{{""Index"":{i},""Value"":""item{i}""}}");
                }

                List<IndexRebuildProgress> progressReports = new List<IndexRebuildProgress>();
                Progress<IndexRebuildProgress> progress = new Progress<IndexRebuildProgress>(p => progressReports.Add(p));

                IndexRebuildResult result = await client.Collection.RebuildIndexes(collection.Id, dropUnusedIndexes: false, progress);

                if (result.DocumentsProcessed != 10)
                    return TestOutcome.Fail($"Expected 10 documents processed, got {result.DocumentsProcessed}");

                if (progressReports.Count == 0)
                    return TestOutcome.Fail("Expected progress reports");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        private static async Task<TestOutcome> TestIndexingRebuildDropsUnused()
        {
            string testDir = CreateTestDir();
            try
            {
                using LatticeClient client = CreateClient(testDir);
                Collection collection = await client.Collection.Create("DropUnusedTest");

                // Add documents with multiple fields (all will be indexed initially)
                await client.Document.Ingest(collection.Id, @"{""Name"":""Joel"",""Age"":30,""Email"":""joel@test.com""}");

                // Change to selective mode with only Name
                await client.Collection.UpdateIndexing(
                    collection.Id,
                    IndexingMode.Selective,
                    new List<string> { "Name" });

                // Rebuild with drop unused
                IndexRebuildResult result = await client.Collection.RebuildIndexes(collection.Id, dropUnusedIndexes: true);

                if (!result.Success)
                    return TestOutcome.Fail($"Rebuild failed: {string.Join(", ", result.Errors)}");

                // Verify Name search still works
                SearchResult nameResult = await client.Search.Search(new SearchQuery
                {
                    CollectionId = collection.Id,
                    Filters = new List<SearchFilter> { new SearchFilter("Name", SearchConditionEnum.Equals, "Joel") }
                });
                if (nameResult.Documents.Count != 1)
                    return TestOutcome.Fail("Name search should still work after rebuild");

                return TestOutcome.Pass();
            }
            finally { CleanupTestDir(testDir); }
        }

        #endregion
    }
}
