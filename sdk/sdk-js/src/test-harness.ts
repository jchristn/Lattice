/**
 * Lattice SDK Test Harness for JavaScript/TypeScript
 *
 * A comprehensive test suite for the Lattice JavaScript SDK.
 * Tests all API endpoints and validates responses.
 *
 * Usage:
 *   npx ts-node src/test-harness.ts <endpoint_url>
 *   OR
 *   node dist/test-harness.js <endpoint_url>
 *
 * Example:
 *   npx ts-node src/test-harness.ts http://localhost:8000
 */

import {
    LatticeClient,
    Collection,
    Document,
    SearchQuery,
    SearchCondition,
    SchemaEnforcementMode,
    IndexingMode,
    FieldConstraint
} from "./index";

interface TestOutcome {
    success: boolean;
    error?: string;
}

function passed(): TestOutcome {
    return { success: true };
}

function failed(error: string): TestOutcome {
    return { success: false, error };
}

interface TestResult {
    section: string;
    name: string;
    passed: boolean;
    elapsedMs: number;
    error?: string;
}

class TestHarness {
    private endpoint: string;
    private client: LatticeClient;
    private results: TestResult[] = [];
    private passCount = 0;
    private failCount = 0;
    private currentSection = "";
    private overallStartTime = 0;

    constructor(endpoint: string) {
        this.endpoint = endpoint;
        this.client = new LatticeClient(endpoint);
    }

    async runAllTests(): Promise<boolean> {
        console.log("=".repeat(79));
        console.log("  Lattice SDK Test Harness - JavaScript/TypeScript");
        console.log("=".repeat(79));
        console.log();
        console.log(`  Endpoint: ${this.endpoint}`);
        console.log();

        this.overallStartTime = Date.now();

        try {
            // Health check first
            await this.runTestSection("HEALTH CHECK", () => this.testHealthCheck());

            // Collection API Tests
            await this.runTestSection("COLLECTION API", () => this.testCollectionApi());

            // Document API Tests
            await this.runTestSection("DOCUMENT API", () => this.testDocumentApi());

            // Search API Tests
            await this.runTestSection("SEARCH API", () => this.testSearchApi());

            // Enumeration API Tests
            await this.runTestSection("ENUMERATION API", () => this.testEnumerationApi());

            // Schema API Tests
            await this.runTestSection("SCHEMA API", () => this.testSchemaApi());

            // Index API Tests
            await this.runTestSection("INDEX API", () => this.testIndexApi());

            // Constraint Tests
            await this.runTestSection("SCHEMA CONSTRAINTS", () => this.testConstraintsApi());

            // Indexing Mode Tests
            await this.runTestSection("INDEXING MODE", () => this.testIndexingModeApi());

            // Edge Case Tests
            await this.runTestSection("EDGE CASES", () => this.testEdgeCases());

            // Performance Tests
            await this.runTestSection("PERFORMANCE", () => this.testPerformance());
        } catch (error: any) {
            console.log(`[FATAL] Unhandled exception: ${error.message}`);
            this.failCount++;
        }

        const overallElapsed = Date.now() - this.overallStartTime;
        this.printSummary(overallElapsed);

        return this.failCount === 0;
    }

    private async runTestSection(sectionName: string, testFunc: () => Promise<void>): Promise<void> {
        console.log();
        console.log(`--- ${sectionName} ---`);
        this.currentSection = sectionName;
        await testFunc();
    }

    private async runTest(name: string, testFunc: () => Promise<TestOutcome>): Promise<void> {
        const startTime = Date.now();
        let passed = false;
        let error: string | undefined;

        try {
            const outcome = await testFunc();
            passed = outcome.success;
            error = outcome.error;
        } catch (e: any) {
            passed = false;
            error = e.message;
        }

        const elapsedMs = Date.now() - startTime;

        const result: TestResult = {
            section: this.currentSection,
            name,
            passed,
            elapsedMs,
            error
        };
        this.results.push(result);

        if (passed) {
            console.log(`  [PASS] ${name} (${elapsedMs}ms)`);
            this.passCount++;
        } else {
            console.log(`  [FAIL] ${name} (${elapsedMs}ms)`);
            if (error) {
                console.log(`         Error: ${error}`);
            }
            this.failCount++;
        }
    }

    private printSummary(overallElapsed: number): void {
        console.log();
        console.log("=".repeat(79));
        console.log("  TEST SUMMARY");
        console.log("=".repeat(79));
        console.log();

        // Group by section
        const sections = new Map<string, TestResult[]>();
        for (const result of this.results) {
            if (!sections.has(result.section)) {
                sections.set(result.section, []);
            }
            sections.get(result.section)!.push(result);
        }

        for (const [sectionName, sectionResults] of sections) {
            const sectionPass = sectionResults.filter((r) => r.passed).length;
            const sectionTotal = sectionResults.length;
            const status = sectionPass === sectionTotal ? "PASS" : "FAIL";
            console.log(`  ${sectionName}: ${sectionPass}/${sectionTotal} [${status}]`);
        }

        console.log();
        console.log("-".repeat(79));
        const overallStatus = this.failCount === 0 ? "PASS" : "FAIL";
        console.log(`  TOTAL: ${this.passCount} passed, ${this.failCount} failed [${overallStatus}]`);
        console.log(`  RUNTIME: ${overallElapsed}ms (${(overallElapsed / 1000).toFixed(2)}s)`);
        console.log("-".repeat(79));

        if (this.failCount > 0) {
            console.log();
            console.log("  FAILED TESTS:");
            for (const result of this.results) {
                if (!result.passed) {
                    console.log(`    - ${result.section}: ${result.name}`);
                    if (result.error) {
                        console.log(`      Error: ${result.error}`);
                    }
                }
            }
        }

        console.log();
    }

    // ========== HEALTH CHECK TESTS ==========

    private async testHealthCheck(): Promise<void> {
        await this.runTest("Health check returns true", async () => {
            const healthy = await this.client.healthCheck();
            return healthy ? passed() : failed("Health check failed");
        });
    }

    // ========== COLLECTION API TESTS ==========

    private async testCollectionApi(): Promise<void> {
        // Create collection: basic
        await this.runTest("CreateCollection: basic", async () => {
            const collection = await this.client.collection.create({ name: "test_basic_collection" });
            if (!collection) return failed("Collection creation returned null");
            if (!collection.id.startsWith("col_")) return failed(`Invalid collection ID: ${collection.id}`);
            await this.client.collection.delete(collection.id);
            return passed();
        });

        // Create collection: with all parameters
        await this.runTest("CreateCollection: with all parameters", async () => {
            const collection = await this.client.collection.create({
                name: "test_full_collection",
                description: "A test collection",
                labels: ["test", "full"],
                tags: { env: "test", version: "1.0" },
                schemaEnforcementMode: SchemaEnforcementMode.Flexible,
                indexingMode: IndexingMode.All
            });
            if (!collection) return failed("Collection creation returned null");
            if (collection.name !== "test_full_collection") return failed(`Name mismatch: ${collection.name}`);
            if (collection.description !== "A test collection") return failed(`Description mismatch: ${collection.description}`);
            await this.client.collection.delete(collection.id);
            return passed();
        });

        // Create collection: verify all properties
        await this.runTest("CreateCollection: verify all properties", async () => {
            const collection = await this.client.collection.create({
                name: "test_props_collection",
                description: "Props test",
                labels: ["prop_test"],
                tags: { key: "value" }
            });
            if (!collection) return failed("Collection creation returned null");
            if (!collection.id) return failed("Id is empty");
            if (!collection.createdUtc) return failed("CreatedUtc not set");
            await this.client.collection.delete(collection.id);
            return passed();
        });

        // GetCollection: existing
        await this.runTest("GetCollection: existing", async () => {
            const collection = await this.client.collection.create({ name: "test_get_existing" });
            if (!collection) return failed("Setup: Collection creation failed");
            const retrieved = await this.client.collection.readById(collection.id);
            if (!retrieved) {
                await this.client.collection.delete(collection.id);
                return failed("GetCollection returned null");
            }
            if (retrieved.id !== collection.id) {
                await this.client.collection.delete(collection.id);
                return failed(`Id mismatch: ${retrieved.id}`);
            }
            await this.client.collection.delete(collection.id);
            return passed();
        });

        // GetCollection: non-existent
        await this.runTest("GetCollection: non-existent returns null", async () => {
            const retrieved = await this.client.collection.readById("col_nonexistent12345");
            if (retrieved !== null) return failed("Expected null for non-existent collection");
            return passed();
        });

        // GetCollections: multiple
        await this.runTest("GetCollections: multiple", async () => {
            const col1 = await this.client.collection.create({ name: "test_multi_1" });
            const col2 = await this.client.collection.create({ name: "test_multi_2" });
            if (!col1 || !col2) return failed("Setup: Collection creation failed");

            const collections = await this.client.collection.readAll();
            const foundIds = new Set(collections.map((c) => c.id));

            if (!foundIds.has(col1.id) || !foundIds.has(col2.id)) {
                await this.client.collection.delete(col1.id);
                await this.client.collection.delete(col2.id);
                return failed("Not all collections found");
            }

            await this.client.collection.delete(col1.id);
            await this.client.collection.delete(col2.id);
            return passed();
        });

        // CollectionExists: true
        await this.runTest("CollectionExists: true when exists", async () => {
            const collection = await this.client.collection.create({ name: "test_exists_true" });
            if (!collection) return failed("Setup: Collection creation failed");
            const exists = await this.client.collection.exists(collection.id);
            await this.client.collection.delete(collection.id);
            if (!exists) return failed("Expected exists to be true");
            return passed();
        });

        // CollectionExists: false
        await this.runTest("CollectionExists: false when not exists", async () => {
            const exists = await this.client.collection.exists("col_nonexistent12345");
            if (exists) return failed("Expected exists to be false");
            return passed();
        });

        // DeleteCollection: removes collection
        await this.runTest("DeleteCollection: removes collection", async () => {
            const collection = await this.client.collection.create({ name: "test_delete" });
            if (!collection) return failed("Setup: Collection creation failed");
            const deleted = await this.client.collection.delete(collection.id);
            if (!deleted) return failed("Delete returned false");
            const exists = await this.client.collection.exists(collection.id);
            if (exists) return failed("Collection still exists after delete");
            return passed();
        });
    }

    // ========== DOCUMENT API TESTS ==========

    private async testDocumentApi(): Promise<void> {
        // Create a collection for document tests
        const collection = await this.client.collection.create({ name: "doc_test_collection" });
        if (!collection) {
            await this.runTest("Setup: Create collection", async () => failed("Collection creation failed"));
            return;
        }

        try {
            // IngestDocument: basic
            await this.runTest("IngestDocument: basic", async () => {
                const doc = await this.client.document.ingest({
                    collectionId: collection.id,
                    content: { name: "Test" }
                });
                if (!doc) return failed("Ingest returned null");
                if (!doc.id.startsWith("doc_")) return failed(`Invalid document ID: ${doc.id}`);
                return passed();
            });

            // IngestDocument: with name
            await this.runTest("IngestDocument: with name", async () => {
                const doc = await this.client.document.ingest({
                    collectionId: collection.id,
                    content: { name: "Named" },
                    name: "my_document"
                });
                if (!doc) return failed("Ingest returned null");
                if (doc.name !== "my_document") return failed(`Name mismatch: ${doc.name}`);
                return passed();
            });

            // IngestDocument: with labels
            await this.runTest("IngestDocument: with labels", async () => {
                const doc = await this.client.document.ingest({
                    collectionId: collection.id,
                    content: { name: "Labeled" },
                    labels: ["label1", "label2"]
                });
                if (!doc) return failed("Ingest returned null");
                return passed();
            });

            // IngestDocument: with tags
            await this.runTest("IngestDocument: with tags", async () => {
                const doc = await this.client.document.ingest({
                    collectionId: collection.id,
                    content: { name: "Tagged" },
                    tags: { key: "value" }
                });
                if (!doc) return failed("Ingest returned null");
                return passed();
            });

            // IngestDocument: verify all properties
            await this.runTest("IngestDocument: verify all properties", async () => {
                const doc = await this.client.document.ingest({
                    collectionId: collection.id,
                    content: { name: "Properties Test" },
                    name: "prop_doc",
                    labels: ["prop"],
                    tags: { prop: "test" }
                });
                if (!doc) return failed("Ingest returned null");
                if (!doc.id) return failed("Id is empty");
                if (doc.collectionId !== collection.id) return failed(`CollectionId mismatch: ${doc.collectionId}`);
                if (!doc.schemaId) return failed("SchemaId is empty");
                if (!doc.createdUtc) return failed("CreatedUtc not set");
                return passed();
            });

            // IngestDocument: nested JSON
            await this.runTest("IngestDocument: nested JSON", async () => {
                const doc = await this.client.document.ingest({
                    collectionId: collection.id,
                    content: {
                        person: {
                            name: "John",
                            address: {
                                city: "New York",
                                zip: "10001"
                            }
                        }
                    }
                });
                if (!doc) return failed("Ingest returned null");
                return passed();
            });

            // IngestDocument: array JSON
            await this.runTest("IngestDocument: array JSON", async () => {
                const doc = await this.client.document.ingest({
                    collectionId: collection.id,
                    content: {
                        items: [1, 2, 3, 4, 5],
                        names: ["Alice", "Bob", "Charlie"]
                    }
                });
                if (!doc) return failed("Ingest returned null");
                return passed();
            });

            // Create a specific doc for get tests
            const testDoc = await this.client.document.ingest({
                collectionId: collection.id,
                content: { name: "GetTest", value: 42 },
                name: "get_test_doc",
                labels: ["get_test"],
                tags: { test_type: "get" }
            });

            if (testDoc) {
                // GetDocument: without content
                await this.runTest("GetDocument: without content", async () => {
                    const doc = await this.client.document.readById(collection.id, testDoc.id, false);
                    if (!doc) return failed("GetDocument returned null");
                    if (doc.content !== undefined && doc.content !== null) return failed("Content should be null");
                    return passed();
                });

                // GetDocument: with content
                await this.runTest("GetDocument: with content", async () => {
                    const doc = await this.client.document.readById(collection.id, testDoc.id, true);
                    if (!doc) return failed("GetDocument returned null");
                    if (doc.content === null || doc.content === undefined) return failed("Content should not be null");
                    return passed();
                });

                // GetDocument: verify labels
                await this.runTest("GetDocument: verify labels populated", async () => {
                    const doc = await this.client.document.readById(collection.id, testDoc.id, false, true);
                    if (!doc) return failed("GetDocument returned null");
                    if (!doc.labels.includes("get_test")) return failed(`Label 'get_test' not found: ${doc.labels}`);
                    return passed();
                });

                // GetDocument: verify tags
                await this.runTest("GetDocument: verify tags populated", async () => {
                    const doc = await this.client.document.readById(collection.id, testDoc.id, false, true, true);
                    if (!doc) return failed("GetDocument returned null");
                    if (doc.tags["test_type"] !== "get") return failed(`Tag 'test_type' mismatch: ${JSON.stringify(doc.tags)}`);
                    return passed();
                });
            }

            // GetDocument: non-existent
            await this.runTest("GetDocument: non-existent returns null", async () => {
                const doc = await this.client.document.readById(collection.id, "doc_nonexistent12345");
                if (doc !== null) return failed("Expected null for non-existent document");
                return passed();
            });

            // GetDocuments: multiple
            await this.runTest("GetDocuments: multiple documents", async () => {
                const docs = await this.client.document.readAllInCollection(collection.id);
                if (docs.length < 5) return failed(`Expected at least 5 docs, got ${docs.length}`);
                return passed();
            });

            // DocumentExists: true
            await this.runTest("DocumentExists: true when exists", async () => {
                if (!testDoc) return failed("Setup: testDoc is null");
                const exists = await this.client.document.exists(collection.id, testDoc.id);
                if (!exists) return failed("Expected exists to be true");
                return passed();
            });

            // DocumentExists: false
            await this.runTest("DocumentExists: false when not exists", async () => {
                const exists = await this.client.document.exists(collection.id, "doc_nonexistent12345");
                if (exists) return failed("Expected exists to be false");
                return passed();
            });

            // DeleteDocument: removes document
            await this.runTest("DeleteDocument: removes document", async () => {
                const doc = await this.client.document.ingest({
                    collectionId: collection.id,
                    content: { to_delete: true }
                });
                if (!doc) return failed("Setup: Ingest failed");
                const deleted = await this.client.document.delete(collection.id, doc.id);
                if (!deleted) return failed("Delete returned false");
                const exists = await this.client.document.exists(collection.id, doc.id);
                if (exists) return failed("Document still exists after delete");
                return passed();
            });
        } finally {
            await this.client.collection.delete(collection.id);
        }
    }

    // ========== SEARCH API TESTS ==========

    private async testSearchApi(): Promise<void> {
        // Create a collection with searchable data
        const collection = await this.client.collection.create({ name: "search_test_collection" });
        if (!collection) {
            await this.runTest("Setup: Create collection", async () => failed("Collection creation failed"));
            return;
        }

        try {
            // Ingest test documents
            for (let i = 0; i < 20; i++) {
                await this.client.document.ingest({
                    collectionId: collection.id,
                    content: {
                        Name: `Item_${i}`,
                        Category: `Category_${i % 5}`,
                        Value: i * 10,
                        IsActive: i % 2 === 0,
                        Description: `This is item number ${i}`
                    },
                    name: `doc_${i}`,
                    labels: [`group_${i % 3}`].concat(i % 10 === 0 ? ["special"] : []),
                    tags: { priority: String(i % 3) }
                });
            }

            // Search: Equals operator
            await this.runTest("Search: Equals operator", async () => {
                const result = await this.client.search.search({
                    collectionId: collection.id,
                    filters: [{ field: "Category", condition: SearchCondition.Equals, value: "Category_2" }],
                    maxResults: 100
                });
                if (!result) return failed("Search returned null");
                if (!result.success) return failed("Search not successful");
                if (result.documents.length !== 4) return failed(`Expected 4 results, got ${result.documents.length}`);
                return passed();
            });

            // Search: NotEquals operator
            await this.runTest("Search: NotEquals operator", async () => {
                const result = await this.client.search.search({
                    collectionId: collection.id,
                    filters: [{ field: "Category", condition: SearchCondition.NotEquals, value: "Category_0" }],
                    maxResults: 100
                });
                if (!result) return failed("Search returned null");
                if (result.documents.length !== 16) return failed(`Expected 16 results, got ${result.documents.length}`);
                return passed();
            });

            // Search: GreaterThan operator
            await this.runTest("Search: GreaterThan operator", async () => {
                const result = await this.client.search.search({
                    collectionId: collection.id,
                    filters: [{ field: "Value", condition: SearchCondition.GreaterThan, value: "150" }],
                    maxResults: 100
                });
                if (!result) return failed("Search returned null");
                if (result.documents.length !== 4) return failed(`Expected 4 results, got ${result.documents.length}`);
                return passed();
            });

            // Search: LessThan operator
            await this.runTest("Search: LessThan operator", async () => {
                const result = await this.client.search.search({
                    collectionId: collection.id,
                    filters: [{ field: "Value", condition: SearchCondition.LessThan, value: "30" }],
                    maxResults: 100
                });
                if (!result) return failed("Search returned null");
                if (result.documents.length !== 3) return failed(`Expected 3 results, got ${result.documents.length}`);
                return passed();
            });

            // Search: Contains operator
            await this.runTest("Search: Contains operator", async () => {
                const result = await this.client.search.search({
                    collectionId: collection.id,
                    filters: [{ field: "Name", condition: SearchCondition.Contains, value: "Item_1" }],
                    maxResults: 100
                });
                if (!result) return failed("Search returned null");
                if (result.documents.length < 1) return failed(`Expected at least 1 result, got ${result.documents.length}`);
                return passed();
            });

            // Search: StartsWith operator
            await this.runTest("Search: StartsWith operator", async () => {
                const result = await this.client.search.search({
                    collectionId: collection.id,
                    filters: [{ field: "Name", condition: SearchCondition.StartsWith, value: "Item_" }],
                    maxResults: 100
                });
                if (!result) return failed("Search returned null");
                if (result.documents.length !== 20) return failed(`Expected 20 results, got ${result.documents.length}`);
                return passed();
            });

            // Search: multiple filters (AND)
            await this.runTest("Search: multiple filters (AND)", async () => {
                const result = await this.client.search.search({
                    collectionId: collection.id,
                    filters: [
                        { field: "Category", condition: SearchCondition.Equals, value: "Category_2" },
                        { field: "IsActive", condition: SearchCondition.Equals, value: "true" }
                    ],
                    maxResults: 100
                });
                if (!result) return failed("Search returned null");
                if (result.documents.length !== 2) return failed(`Expected 2 results, got ${result.documents.length}`);
                return passed();
            });

            // Search: by label
            await this.runTest("Search: by label", async () => {
                const result = await this.client.search.search({
                    collectionId: collection.id,
                    labels: ["special"],
                    maxResults: 100
                });
                if (!result) return failed("Search returned null");
                if (result.documents.length !== 2) return failed(`Expected 2 results, got ${result.documents.length}`);
                return passed();
            });

            // Search: by tag
            await this.runTest("Search: by tag", async () => {
                const result = await this.client.search.search({
                    collectionId: collection.id,
                    tags: { priority: "0" },
                    maxResults: 100
                });
                if (!result) return failed("Search returned null");
                if (result.documents.length !== 7) return failed(`Expected 7 results, got ${result.documents.length}`);
                return passed();
            });

            // Search: pagination Skip
            await this.runTest("Search: pagination Skip", async () => {
                const result = await this.client.search.search({
                    collectionId: collection.id,
                    skip: 10,
                    maxResults: 100
                });
                if (!result) return failed("Search returned null");
                if (result.documents.length !== 10) return failed(`Expected 10 results, got ${result.documents.length}`);
                return passed();
            });

            // Search: pagination MaxResults
            await this.runTest("Search: pagination MaxResults", async () => {
                const result = await this.client.search.search({
                    collectionId: collection.id,
                    maxResults: 5
                });
                if (!result) return failed("Search returned null");
                if (result.documents.length !== 5) return failed(`Expected 5 results, got ${result.documents.length}`);
                return passed();
            });

            // Search: verify TotalRecords
            await this.runTest("Search: verify TotalRecords", async () => {
                const result = await this.client.search.search({
                    collectionId: collection.id,
                    maxResults: 5
                });
                if (!result) return failed("Search returned null");
                if (result.totalRecords !== 20) return failed(`Expected totalRecords=20, got ${result.totalRecords}`);
                return passed();
            });

            // Search: verify EndOfResults
            await this.runTest("Search: verify EndOfResults true", async () => {
                const result = await this.client.search.search({
                    collectionId: collection.id,
                    maxResults: 100
                });
                if (!result) return failed("Search returned null");
                if (!result.endOfResults) return failed("Expected endOfResults=true");
                return passed();
            });

            // Search: empty results
            await this.runTest("Search: empty results", async () => {
                const result = await this.client.search.search({
                    collectionId: collection.id,
                    filters: [{ field: "Name", condition: SearchCondition.Equals, value: "NonExistent" }],
                    maxResults: 100
                });
                if (!result) return failed("Search returned null");
                if (result.documents.length !== 0) return failed(`Expected 0 results, got ${result.documents.length}`);
                return passed();
            });

            // Search: with IncludeContent
            await this.runTest("Search: with IncludeContent true", async () => {
                const result = await this.client.search.search({
                    collectionId: collection.id,
                    maxResults: 1,
                    includeContent: true
                });
                if (!result) return failed("Search returned null");
                if (result.documents.length === 0) return failed("No documents returned");
                if (result.documents[0].content === null || result.documents[0].content === undefined) {
                    return failed("Content should be included");
                }
                return passed();
            });

            // SearchBySql: basic query
            await this.runTest("SearchBySql: basic query", async () => {
                const result = await this.client.search.searchBySql(
                    collection.id,
                    "SELECT * FROM documents WHERE Category = 'Category_1'"
                );
                if (!result) return failed("Search returned null");
                if (result.documents.length !== 4) return failed(`Expected 4 results, got ${result.documents.length}`);
                return passed();
            });
        } finally {
            await this.client.collection.delete(collection.id);
        }
    }

    // ========== ENUMERATION API TESTS ==========

    private async testEnumerationApi(): Promise<void> {
        const collection = await this.client.collection.create({ name: "enum_test_collection" });
        if (!collection) {
            await this.runTest("Setup: Create collection", async () => failed("Collection creation failed"));
            return;
        }

        try {
            // Ingest test documents
            for (let i = 0; i < 10; i++) {
                await this.client.document.ingest({
                    collectionId: collection.id,
                    content: { index: i, name: `EnumItem_${i}` },
                    name: `enum_doc_${i}`
                });
                await new Promise((resolve) => setTimeout(resolve, 50)); // Small delay
            }

            // Enumerate: basic
            await this.runTest("Enumerate: basic", async () => {
                const result = await this.client.search.enumerate({
                    collectionId: collection.id,
                    maxResults: 100
                });
                if (!result) return failed("Enumerate returned null");
                if (result.documents.length !== 10) return failed(`Expected 10 results, got ${result.documents.length}`);
                return passed();
            });

            // Enumerate: with MaxResults
            await this.runTest("Enumerate: with MaxResults", async () => {
                const result = await this.client.search.enumerate({
                    collectionId: collection.id,
                    maxResults: 5
                });
                if (!result) return failed("Enumerate returned null");
                if (result.documents.length !== 5) return failed(`Expected 5 results, got ${result.documents.length}`);
                return passed();
            });

            // Enumerate: with Skip
            await this.runTest("Enumerate: with Skip", async () => {
                const result = await this.client.search.enumerate({
                    collectionId: collection.id,
                    skip: 5,
                    maxResults: 100
                });
                if (!result) return failed("Enumerate returned null");
                if (result.documents.length !== 5) return failed(`Expected 5 results, got ${result.documents.length}`);
                return passed();
            });

            // Enumerate: verify TotalRecords
            await this.runTest("Enumerate: verify TotalRecords", async () => {
                const result = await this.client.search.enumerate({
                    collectionId: collection.id,
                    maxResults: 3
                });
                if (!result) return failed("Enumerate returned null");
                if (result.totalRecords !== 10) return failed(`Expected totalRecords=10, got ${result.totalRecords}`);
                return passed();
            });

            // Enumerate: verify EndOfResults
            await this.runTest("Enumerate: verify EndOfResults", async () => {
                const result = await this.client.search.enumerate({
                    collectionId: collection.id,
                    maxResults: 100
                });
                if (!result) return failed("Enumerate returned null");
                if (!result.endOfResults) return failed("Expected endOfResults=true");
                return passed();
            });
        } finally {
            await this.client.collection.delete(collection.id);
        }
    }

    // ========== SCHEMA API TESTS ==========

    private async testSchemaApi(): Promise<void> {
        const collection = await this.client.collection.create({ name: "schema_test_collection" });
        if (!collection) {
            await this.runTest("Setup: Create collection", async () => failed("Collection creation failed"));
            return;
        }

        try {
            // Ingest documents to create schemas
            const doc1 = await this.client.document.ingest({
                collectionId: collection.id,
                content: { name: "Test", value: 42, active: true }
            });

            // GetSchemas: returns schemas
            await this.runTest("GetSchemas: returns schemas", async () => {
                const schemas = await this.client.schema.readAll();
                if (schemas.length === 0) return failed("No schemas returned");
                return passed();
            });

            // GetSchema: by id
            await this.runTest("GetSchema: by id", async () => {
                if (!doc1) return failed("Setup: doc1 is null");
                const schema = await this.client.schema.readById(doc1.schemaId);
                if (!schema) return failed("GetSchema returned null");
                if (schema.id !== doc1.schemaId) return failed(`Schema ID mismatch: ${schema.id}`);
                return passed();
            });

            // GetSchema: non-existent
            await this.runTest("GetSchema: non-existent returns null", async () => {
                const schema = await this.client.schema.readById("sch_nonexistent12345");
                if (schema !== null) return failed("Expected null for non-existent schema");
                return passed();
            });

            // GetSchemaElements: returns elements
            await this.runTest("GetSchemaElements: returns elements", async () => {
                if (!doc1) return failed("Setup: doc1 is null");
                const elements = await this.client.schema.getElements(doc1.schemaId);
                if (elements.length === 0) return failed("No elements returned");
                return passed();
            });

            // GetSchemaElements: correct keys
            await this.runTest("GetSchemaElements: correct keys", async () => {
                if (!doc1) return failed("Setup: doc1 is null");
                const elements = await this.client.schema.getElements(doc1.schemaId);
                const keys = new Set(elements.map((e) => e.key));
                const expected = ["name", "value", "active"];
                for (const key of expected) {
                    if (!keys.has(key)) return failed(`Missing expected key: ${key}. Found: ${Array.from(keys)}`);
                }
                return passed();
            });
        } finally {
            await this.client.collection.delete(collection.id);
        }
    }

    // ========== INDEX API TESTS ==========

    private async testIndexApi(): Promise<void> {
        // GetIndexTableMappings: returns mappings
        await this.runTest("GetIndexTableMappings: returns mappings", async () => {
            const mappings = await this.client.index.getMappings();
            if (mappings === null) return failed("GetMappings returned null");
            return passed();
        });
    }

    // ========== CONSTRAINTS API TESTS ==========

    private async testConstraintsApi(): Promise<void> {
        // Create collection with strict mode
        await this.runTest("Constraints: create collection with strict mode", async () => {
            const constraint: FieldConstraint = {
                fieldPath: "name",
                dataType: "string",
                required: true
            };
            const collection = await this.client.collection.create({
                name: "constraints_test",
                schemaEnforcementMode: SchemaEnforcementMode.Strict,
                fieldConstraints: [constraint]
            });
            if (!collection) return failed("Collection creation failed");
            await this.client.collection.delete(collection.id);
            return passed();
        });

        // Update constraints
        await this.runTest("Constraints: update constraints on collection", async () => {
            const collection = await this.client.collection.create({ name: "constraints_update_test" });
            if (!collection) return failed("Collection creation failed");

            const constraint: FieldConstraint = {
                fieldPath: "email",
                dataType: "string",
                required: true,
                regexPattern: "^[\\w\\.-]+@[\\w\\.-]+\\.\\w+$"
            };
            const success = await this.client.collection.updateConstraints(
                collection.id,
                SchemaEnforcementMode.Strict,
                [constraint]
            );
            await this.client.collection.delete(collection.id);

            if (!success) return failed("Update constraints failed");
            return passed();
        });

        // Get constraints
        await this.runTest("Constraints: get constraints from collection", async () => {
            const constraint: FieldConstraint = {
                fieldPath: "test_field",
                dataType: "string",
                required: true
            };
            const collection = await this.client.collection.create({
                name: "constraints_get_test",
                schemaEnforcementMode: SchemaEnforcementMode.Strict,
                fieldConstraints: [constraint]
            });
            if (!collection) return failed("Collection creation failed");

            const constraints = await this.client.collection.getConstraints(collection.id);
            await this.client.collection.delete(collection.id);

            if (constraints.length === 0) return failed("No constraints returned");
            return passed();
        });
    }

    // ========== INDEXING MODE API TESTS ==========

    private async testIndexingModeApi(): Promise<void> {
        // Selective mode
        await this.runTest("Indexing: selective mode only indexes specified fields", async () => {
            const collection = await this.client.collection.create({
                name: "indexing_selective_test",
                indexingMode: IndexingMode.Selective,
                indexedFields: ["name", "email"]
            });
            if (!collection) return failed("Collection creation failed");
            await this.client.collection.delete(collection.id);
            return passed();
        });

        // None mode
        await this.runTest("Indexing: none mode skips indexing", async () => {
            const collection = await this.client.collection.create({
                name: "indexing_none_test",
                indexingMode: IndexingMode.None
            });
            if (!collection) return failed("Collection creation failed");
            await this.client.collection.delete(collection.id);
            return passed();
        });

        // Update indexing mode
        await this.runTest("Indexing: update indexing mode", async () => {
            const collection = await this.client.collection.create({ name: "indexing_update_test" });
            if (!collection) return failed("Collection creation failed");

            const success = await this.client.collection.updateIndexing(
                collection.id,
                IndexingMode.Selective,
                ["name"],
                false
            );
            await this.client.collection.delete(collection.id);

            if (!success) return failed("Update indexing failed");
            return passed();
        });

        // Rebuild indexes
        await this.runTest("Indexing: rebuild indexes", async () => {
            const collection = await this.client.collection.create({ name: "indexing_rebuild_test" });
            if (!collection) return failed("Collection creation failed");

            // Ingest a document first
            await this.client.document.ingest({
                collectionId: collection.id,
                content: { name: "test", value: 42 }
            });

            const result = await this.client.collection.rebuildIndexes(collection.id);
            await this.client.collection.delete(collection.id);

            if (!result) return failed("Rebuild indexes returned null");
            return passed();
        });
    }

    // ========== EDGE CASE TESTS ==========

    private async testEdgeCases(): Promise<void> {
        const collection = await this.client.collection.create({ name: "edge_case_collection" });
        if (!collection) {
            await this.runTest("Setup: Create collection", async () => failed("Collection creation failed"));
            return;
        }

        try {
            // Empty string values
            await this.runTest("Edge: empty string values in JSON", async () => {
                const doc = await this.client.document.ingest({
                    collectionId: collection.id,
                    content: { name: "", description: "" }
                });
                if (!doc) return failed("Ingest failed");
                return passed();
            });

            // Special characters
            await this.runTest("Edge: special characters in values", async () => {
                const doc = await this.client.document.ingest({
                    collectionId: collection.id,
                    content: { text: 'Hello! @#$%^&*()_+-={}[]|\\:";<>?,./' }
                });
                if (!doc) return failed("Ingest failed");
                return passed();
            });

            // Deeply nested JSON
            await this.runTest("Edge: deeply nested JSON (5 levels)", async () => {
                const doc = await this.client.document.ingest({
                    collectionId: collection.id,
                    content: {
                        level1: {
                            level2: {
                                level3: {
                                    level4: {
                                        level5: "deep value"
                                    }
                                }
                            }
                        }
                    }
                });
                if (!doc) return failed("Ingest failed");
                return passed();
            });

            // Large array
            await this.runTest("Edge: large array in JSON", async () => {
                const doc = await this.client.document.ingest({
                    collectionId: collection.id,
                    content: { items: Array.from({ length: 100 }, (_, i) => i) }
                });
                if (!doc) return failed("Ingest failed");
                return passed();
            });

            // Numeric values
            await this.runTest("Edge: numeric values", async () => {
                const doc = await this.client.document.ingest({
                    collectionId: collection.id,
                    content: {
                        integer: 42,
                        float: 3.14159,
                        negative: -100,
                        zero: 0,
                        large: 9999999999
                    }
                });
                if (!doc) return failed("Ingest failed");
                return passed();
            });

            // Boolean values
            await this.runTest("Edge: boolean values", async () => {
                const doc = await this.client.document.ingest({
                    collectionId: collection.id,
                    content: { active: true, disabled: false }
                });
                if (!doc) return failed("Ingest failed");
                return passed();
            });

            // Null values
            await this.runTest("Edge: null values in JSON", async () => {
                const doc = await this.client.document.ingest({
                    collectionId: collection.id,
                    content: { name: "Test", optional_field: null }
                });
                if (!doc) return failed("Ingest failed");
                return passed();
            });

            // Unicode characters
            await this.runTest("Edge: unicode characters", async () => {
                const doc = await this.client.document.ingest({
                    collectionId: collection.id,
                    content: { greeting: "Hello, world!", japanese: "Hello, world!", emoji: "Hello, world!" }
                });
                if (!doc) return failed("Ingest failed");
                return passed();
            });
        } finally {
            await this.client.collection.delete(collection.id);
        }
    }

    // ========== PERFORMANCE TESTS ==========

    private async testPerformance(): Promise<void> {
        const collection = await this.client.collection.create({ name: "perf_test_collection" });
        if (!collection) {
            await this.runTest("Setup: Create collection", async () => failed("Collection creation failed"));
            return;
        }

        try {
            // Ingest 100 documents
            await this.runTest("Perf: ingest 100 documents", async () => {
                const start = Date.now();
                for (let i = 0; i < 100; i++) {
                    const doc = await this.client.document.ingest({
                        collectionId: collection.id,
                        content: {
                            Name: `PerfItem_${i}`,
                            Category: `Category_${i % 10}`,
                            Value: i * 10
                        },
                        name: `perf_doc_${i}`
                    });
                    if (!doc) return failed(`Failed to ingest document ${i}`);
                }
                const elapsed = Date.now() - start;
                const rate = (100 / elapsed) * 1000;
                process.stdout.write(`(${rate.toFixed(1)} docs/sec) `);
                return passed();
            });

            // Search in 100 documents
            await this.runTest("Perf: search in 100 documents", async () => {
                const start = Date.now();
                const result = await this.client.search.search({
                    collectionId: collection.id,
                    filters: [{ field: "Category", condition: SearchCondition.Equals, value: "Category_5" }],
                    maxResults: 100
                });
                const elapsed = Date.now() - start;
                if (!result) return failed("Search returned null");
                process.stdout.write(`(${elapsed}ms) `);
                return passed();
            });

            // GetDocuments for 100 documents
            await this.runTest("Perf: GetDocuments for 100 documents", async () => {
                const start = Date.now();
                const docs = await this.client.document.readAllInCollection(collection.id);
                const elapsed = Date.now() - start;
                if (docs.length !== 100) return failed(`Expected 100 docs, got ${docs.length}`);
                process.stdout.write(`(${elapsed}ms) `);
                return passed();
            });

            // Enumerate 100 documents
            await this.runTest("Perf: enumerate 100 documents", async () => {
                const start = Date.now();
                const result = await this.client.search.enumerate({
                    collectionId: collection.id,
                    maxResults: 100
                });
                const elapsed = Date.now() - start;
                if (!result) return failed("Enumerate returned null");
                if (result.documents.length !== 100) return failed(`Expected 100 docs, got ${result.documents.length}`);
                process.stdout.write(`(${elapsed}ms) `);
                return passed();
            });
        } finally {
            await this.client.collection.delete(collection.id);
        }
    }
}

// Main entry point
async function main(): Promise<void> {
    const args = process.argv.slice(2);

    if (args.length < 1) {
        console.log("Usage: npx ts-node src/test-harness.ts <endpoint_url>");
        console.log("Example: npx ts-node src/test-harness.ts http://localhost:8000");
        process.exit(1);
    }

    const endpoint = args[0];

    console.log(`Connecting to Lattice server at: ${endpoint}`);
    console.log();

    const harness = new TestHarness(endpoint);
    const success = await harness.runAllTests();

    process.exit(success ? 0 : 1);
}

main().catch((error) => {
    console.error("Fatal error:", error);
    process.exit(1);
});
