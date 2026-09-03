"use strict";
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
Object.defineProperty(exports, "__esModule", { value: true });
const index_1 = require("./index");
function passed() {
    return { success: true };
}
function failed(error) {
    return { success: false, error };
}
class TestHarness {
    constructor(endpoint) {
        this.results = [];
        this.passCount = 0;
        this.failCount = 0;
        this.currentSection = "";
        this.overallStartTime = 0;
        this.endpoint = endpoint;
        this.client = new index_1.LatticeClient(endpoint);
    }
    async runAllTests() {
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
        }
        catch (error) {
            console.log(`[FATAL] Unhandled exception: ${error.message}`);
            this.failCount++;
        }
        const overallElapsed = Date.now() - this.overallStartTime;
        this.printSummary(overallElapsed);
        return this.failCount === 0;
    }
    async runTestSection(sectionName, testFunc) {
        console.log();
        console.log(`--- ${sectionName} ---`);
        this.currentSection = sectionName;
        await testFunc();
    }
    async runTest(name, testFunc) {
        const startTime = Date.now();
        let passed = false;
        let error;
        try {
            const outcome = await testFunc();
            passed = outcome.success;
            error = outcome.error;
        }
        catch (e) {
            passed = false;
            error = e.message;
        }
        const elapsedMs = Date.now() - startTime;
        const result = {
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
        }
        else {
            console.log(`  [FAIL] ${name} (${elapsedMs}ms)`);
            if (error) {
                console.log(`         Error: ${error}`);
            }
            this.failCount++;
        }
    }
    printSummary(overallElapsed) {
        console.log();
        console.log("=".repeat(79));
        console.log("  TEST SUMMARY");
        console.log("=".repeat(79));
        console.log();
        // Group by section
        const sections = new Map();
        for (const result of this.results) {
            if (!sections.has(result.section)) {
                sections.set(result.section, []);
            }
            sections.get(result.section).push(result);
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
    async testHealthCheck() {
        await this.runTest("Health check returns true", async () => {
            const healthy = await this.client.healthCheck();
            return healthy ? passed() : failed("Health check failed");
        });
    }
    // ========== COLLECTION API TESTS ==========
    async testCollectionApi() {
        // Create collection: basic
        await this.runTest("CreateCollection: basic", async () => {
            const collection = await this.client.collection.create({ name: "test_basic_collection" });
            if (!collection)
                return failed("Collection creation returned null");
            if (!collection.id.startsWith("col_"))
                return failed(`Invalid collection ID: ${collection.id}`);
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
                schemaEnforcementMode: index_1.SchemaEnforcementMode.Flexible,
                indexingMode: index_1.IndexingMode.All
            });
            if (!collection)
                return failed("Collection creation returned null");
            if (collection.name !== "test_full_collection")
                return failed(`Name mismatch: ${collection.name}`);
            if (collection.description !== "A test collection")
                return failed(`Description mismatch: ${collection.description}`);
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
            if (!collection)
                return failed("Collection creation returned null");
            if (!collection.id)
                return failed("Id is empty");
            if (!collection.createdUtc)
                return failed("CreatedUtc not set");
            await this.client.collection.delete(collection.id);
            return passed();
        });
        // GetCollection: existing
        await this.runTest("GetCollection: existing", async () => {
            const collection = await this.client.collection.create({ name: "test_get_existing" });
            if (!collection)
                return failed("Setup: Collection creation failed");
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
            if (retrieved !== null)
                return failed("Expected null for non-existent collection");
            return passed();
        });
        // GetCollections: multiple
        await this.runTest("GetCollections: multiple", async () => {
            const col1 = await this.client.collection.create({ name: "test_multi_1" });
            const col2 = await this.client.collection.create({ name: "test_multi_2" });
            if (!col1 || !col2)
                return failed("Setup: Collection creation failed");
            const collections = await this.client.collection.readAll();
            const foundIds = new Set(collections.objects.map((c) => c.id));
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
            if (!collection)
                return failed("Setup: Collection creation failed");
            const exists = await this.client.collection.exists(collection.id);
            await this.client.collection.delete(collection.id);
            if (!exists)
                return failed("Expected exists to be true");
            return passed();
        });
        // CollectionExists: false
        await this.runTest("CollectionExists: false when not exists", async () => {
            const exists = await this.client.collection.exists("col_nonexistent12345");
            if (exists)
                return failed("Expected exists to be false");
            return passed();
        });
        // DeleteCollection: removes collection
        await this.runTest("DeleteCollection: removes collection", async () => {
            const collection = await this.client.collection.create({ name: "test_delete" });
            if (!collection)
                return failed("Setup: Collection creation failed");
            const deleted = await this.client.collection.delete(collection.id);
            if (!deleted)
                return failed("Delete returned false");
            const exists = await this.client.collection.exists(collection.id);
            if (exists)
                return failed("Collection still exists after delete");
            return passed();
        });
    }
    // ========== DOCUMENT API TESTS ==========
    async testDocumentApi() {
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
                if (!doc)
                    return failed("Ingest returned null");
                if (!doc.id.startsWith("doc_"))
                    return failed(`Invalid document ID: ${doc.id}`);
                return passed();
            });
            // IngestDocument: with name
            await this.runTest("IngestDocument: with name", async () => {
                const doc = await this.client.document.ingest({
                    collectionId: collection.id,
                    content: { name: "Named" },
                    name: "my_document"
                });
                if (!doc)
                    return failed("Ingest returned null");
                if (doc.name !== "my_document")
                    return failed(`Name mismatch: ${doc.name}`);
                return passed();
            });
            // IngestDocument: with labels
            await this.runTest("IngestDocument: with labels", async () => {
                const doc = await this.client.document.ingest({
                    collectionId: collection.id,
                    content: { name: "Labeled" },
                    labels: ["label1", "label2"]
                });
                if (!doc)
                    return failed("Ingest returned null");
                return passed();
            });
            // IngestDocument: with tags
            await this.runTest("IngestDocument: with tags", async () => {
                const doc = await this.client.document.ingest({
                    collectionId: collection.id,
                    content: { name: "Tagged" },
                    tags: { key: "value" }
                });
                if (!doc)
                    return failed("Ingest returned null");
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
                if (!doc)
                    return failed("Ingest returned null");
                if (!doc.id)
                    return failed("Id is empty");
                if (doc.collectionId !== collection.id)
                    return failed(`CollectionId mismatch: ${doc.collectionId}`);
                if (!doc.schemaId)
                    return failed("SchemaId is empty");
                if (!doc.createdUtc)
                    return failed("CreatedUtc not set");
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
                if (!doc)
                    return failed("Ingest returned null");
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
                if (!doc)
                    return failed("Ingest returned null");
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
                    if (!doc)
                        return failed("GetDocument returned null");
                    if (doc.content !== undefined && doc.content !== null)
                        return failed("Content should be null");
                    return passed();
                });
                // GetDocument: with content
                await this.runTest("GetDocument: with content", async () => {
                    const doc = await this.client.document.readById(collection.id, testDoc.id, true);
                    if (!doc)
                        return failed("GetDocument returned null");
                    if (doc.content === null || doc.content === undefined)
                        return failed("Content should not be null");
                    return passed();
                });
                // GetDocument: verify labels
                await this.runTest("GetDocument: verify labels populated", async () => {
                    const doc = await this.client.document.readById(collection.id, testDoc.id, false, true);
                    if (!doc)
                        return failed("GetDocument returned null");
                    if (!doc.labels.includes("get_test"))
                        return failed(`Label 'get_test' not found: ${doc.labels}`);
                    return passed();
                });
                // GetDocument: verify tags
                await this.runTest("GetDocument: verify tags populated", async () => {
                    const doc = await this.client.document.readById(collection.id, testDoc.id, false, true, true);
                    if (!doc)
                        return failed("GetDocument returned null");
                    if (doc.tags["test_type"] !== "get")
                        return failed(`Tag 'test_type' mismatch: ${JSON.stringify(doc.tags)}`);
                    return passed();
                });
            }
            // GetDocument: non-existent
            await this.runTest("GetDocument: non-existent returns null", async () => {
                const doc = await this.client.document.readById(collection.id, "doc_nonexistent12345");
                if (doc !== null)
                    return failed("Expected null for non-existent document");
                return passed();
            });
            // GetDocuments: multiple
            await this.runTest("GetDocuments: multiple documents", async () => {
                const result = await this.client.document.readAllInCollection(collection.id);
                if (result.totalRecords < 5)
                    return failed(`Expected at least 5 docs, got ${result.totalRecords}`);
                return passed();
            });
            // DocumentExists: true
            await this.runTest("DocumentExists: true when exists", async () => {
                if (!testDoc)
                    return failed("Setup: testDoc is null");
                const exists = await this.client.document.exists(collection.id, testDoc.id);
                if (!exists)
                    return failed("Expected exists to be true");
                return passed();
            });
            // DocumentExists: false
            await this.runTest("DocumentExists: false when not exists", async () => {
                const exists = await this.client.document.exists(collection.id, "doc_nonexistent12345");
                if (exists)
                    return failed("Expected exists to be false");
                return passed();
            });
            // DeleteDocument: removes document
            await this.runTest("DeleteDocument: removes document", async () => {
                const doc = await this.client.document.ingest({
                    collectionId: collection.id,
                    content: { to_delete: true }
                });
                if (!doc)
                    return failed("Setup: Ingest failed");
                const deleted = await this.client.document.delete(collection.id, doc.id);
                if (!deleted)
                    return failed("Delete returned false");
                const exists = await this.client.document.exists(collection.id, doc.id);
                if (exists)
                    return failed("Document still exists after delete");
                return passed();
            });
            // IngestBatch: basic batch
            await this.runTest("IngestBatch: basic batch", async () => {
                const results = await this.client.document.ingestBatch(collection.id, [
                    { content: { name: "Batch1" } },
                    { content: { name: "Batch2" } },
                    { content: { name: "Batch3" } }
                ]);
                if (!results)
                    return failed("IngestBatch returned null");
                if (results.length !== 3)
                    return failed(`Expected 3 documents, got ${results.length}`);
                return passed();
            });
            // IngestBatch: with names and metadata
            await this.runTest("IngestBatch: with names and metadata", async () => {
                const results = await this.client.document.ingestBatch(collection.id, [
                    { content: { value: 1 }, name: "batch_named_1", labels: ["batch", "first"], tags: { order: "1" } },
                    { content: { value: 2 }, name: "batch_named_2", labels: ["batch", "second"], tags: { order: "2" } }
                ]);
                if (!results)
                    return failed("IngestBatch returned null");
                if (results.length !== 2)
                    return failed(`Expected 2 documents, got ${results.length}`);
                if (results[0].name !== "batch_named_1")
                    return failed(`Name mismatch: ${results[0].name}`);
                if (results[1].name !== "batch_named_2")
                    return failed(`Name mismatch: ${results[1].name}`);
                return passed();
            });
            // IngestBatch: verify documents retrievable
            await this.runTest("IngestBatch: verify documents retrievable", async () => {
                const results = await this.client.document.ingestBatch(collection.id, [{ content: { check: "retrievable" }, name: "batch_retrieve_test" }]);
                if (!results || results.length === 0)
                    return failed("IngestBatch returned null or empty");
                const retrieved = await this.client.document.readById(collection.id, results[0].id);
                if (!retrieved)
                    return failed("Could not retrieve batch-ingested document");
                if (retrieved.name !== "batch_retrieve_test")
                    return failed(`Retrieved name mismatch: ${retrieved.name}`);
                return passed();
            });
        }
        finally {
            await this.client.collection.delete(collection.id);
        }
    }
    // ========== SEARCH API TESTS ==========
    async testSearchApi() {
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
                    filters: [{ field: "Category", condition: index_1.SearchCondition.Equals, value: "Category_2" }],
                    maxResults: 100
                });
                if (!result)
                    return failed("Search returned null");
                if (!result.success)
                    return failed("Search not successful");
                if (result.documents.length !== 4)
                    return failed(`Expected 4 results, got ${result.documents.length}`);
                return passed();
            });
            // Search: NotEquals operator
            await this.runTest("Search: NotEquals operator", async () => {
                const result = await this.client.search.search({
                    collectionId: collection.id,
                    filters: [{ field: "Category", condition: index_1.SearchCondition.NotEquals, value: "Category_0" }],
                    maxResults: 100
                });
                if (!result)
                    return failed("Search returned null");
                if (result.documents.length !== 16)
                    return failed(`Expected 16 results, got ${result.documents.length}`);
                return passed();
            });
            // Search: GreaterThan operator
            await this.runTest("Search: GreaterThan operator", async () => {
                const result = await this.client.search.search({
                    collectionId: collection.id,
                    filters: [{ field: "Value", condition: index_1.SearchCondition.GreaterThan, value: "150" }],
                    maxResults: 100
                });
                if (!result)
                    return failed("Search returned null");
                if (result.documents.length !== 4)
                    return failed(`Expected 4 results, got ${result.documents.length}`);
                return passed();
            });
            // Search: LessThan operator
            await this.runTest("Search: LessThan operator", async () => {
                const result = await this.client.search.search({
                    collectionId: collection.id,
                    filters: [{ field: "Value", condition: index_1.SearchCondition.LessThan, value: "30" }],
                    maxResults: 100
                });
                if (!result)
                    return failed("Search returned null");
                if (result.documents.length !== 3)
                    return failed(`Expected 3 results, got ${result.documents.length}`);
                return passed();
            });
            // Search: Contains operator
            await this.runTest("Search: Contains operator", async () => {
                const result = await this.client.search.search({
                    collectionId: collection.id,
                    filters: [{ field: "Name", condition: index_1.SearchCondition.Contains, value: "Item_1" }],
                    maxResults: 100
                });
                if (!result)
                    return failed("Search returned null");
                if (result.documents.length < 1)
                    return failed(`Expected at least 1 result, got ${result.documents.length}`);
                return passed();
            });
            // Search: StartsWith operator
            await this.runTest("Search: StartsWith operator", async () => {
                const result = await this.client.search.search({
                    collectionId: collection.id,
                    filters: [{ field: "Name", condition: index_1.SearchCondition.StartsWith, value: "Item_" }],
                    maxResults: 100
                });
                if (!result)
                    return failed("Search returned null");
                if (result.documents.length !== 20)
                    return failed(`Expected 20 results, got ${result.documents.length}`);
                return passed();
            });
            // Search: multiple filters (AND)
            await this.runTest("Search: multiple filters (AND)", async () => {
                const result = await this.client.search.search({
                    collectionId: collection.id,
                    filters: [
                        { field: "Category", condition: index_1.SearchCondition.Equals, value: "Category_2" },
                        { field: "IsActive", condition: index_1.SearchCondition.Equals, value: "true" }
                    ],
                    maxResults: 100
                });
                if (!result)
                    return failed("Search returned null");
                if (result.documents.length !== 2)
                    return failed(`Expected 2 results, got ${result.documents.length}`);
                return passed();
            });
            // Search: by label
            await this.runTest("Search: by label", async () => {
                const result = await this.client.search.search({
                    collectionId: collection.id,
                    labels: ["special"],
                    maxResults: 100
                });
                if (!result)
                    return failed("Search returned null");
                if (result.documents.length !== 2)
                    return failed(`Expected 2 results, got ${result.documents.length}`);
                return passed();
            });
            // Search: by tag
            await this.runTest("Search: by tag", async () => {
                const result = await this.client.search.search({
                    collectionId: collection.id,
                    tags: { priority: "0" },
                    maxResults: 100
                });
                if (!result)
                    return failed("Search returned null");
                if (result.documents.length !== 7)
                    return failed(`Expected 7 results, got ${result.documents.length}`);
                return passed();
            });
            // Search: pagination Skip
            await this.runTest("Search: pagination Skip", async () => {
                const result = await this.client.search.search({
                    collectionId: collection.id,
                    skip: 10,
                    maxResults: 100
                });
                if (!result)
                    return failed("Search returned null");
                if (result.documents.length !== 10)
                    return failed(`Expected 10 results, got ${result.documents.length}`);
                return passed();
            });
            // Search: pagination MaxResults
            await this.runTest("Search: pagination MaxResults", async () => {
                const result = await this.client.search.search({
                    collectionId: collection.id,
                    maxResults: 5
                });
                if (!result)
                    return failed("Search returned null");
                if (result.documents.length !== 5)
                    return failed(`Expected 5 results, got ${result.documents.length}`);
                return passed();
            });
            // Search: verify TotalRecords
            await this.runTest("Search: verify TotalRecords", async () => {
                const result = await this.client.search.search({
                    collectionId: collection.id,
                    maxResults: 5
                });
                if (!result)
                    return failed("Search returned null");
                if (result.totalRecords !== 20)
                    return failed(`Expected totalRecords=20, got ${result.totalRecords}`);
                return passed();
            });
            // Search: verify EndOfResults
            await this.runTest("Search: verify EndOfResults true", async () => {
                const result = await this.client.search.search({
                    collectionId: collection.id,
                    maxResults: 100
                });
                if (!result)
                    return failed("Search returned null");
                if (!result.endOfResults)
                    return failed("Expected endOfResults=true");
                return passed();
            });
            // Search: empty results
            await this.runTest("Search: empty results", async () => {
                const result = await this.client.search.search({
                    collectionId: collection.id,
                    filters: [{ field: "Name", condition: index_1.SearchCondition.Equals, value: "NonExistent" }],
                    maxResults: 100
                });
                if (!result)
                    return failed("Search returned null");
                if (result.documents.length !== 0)
                    return failed(`Expected 0 results, got ${result.documents.length}`);
                return passed();
            });
            // Search: with IncludeContent
            await this.runTest("Search: with IncludeContent true", async () => {
                const result = await this.client.search.search({
                    collectionId: collection.id,
                    maxResults: 1,
                    includeContent: true
                });
                if (!result)
                    return failed("Search returned null");
                if (result.documents.length === 0)
                    return failed("No documents returned");
                if (result.documents[0].content === null || result.documents[0].content === undefined) {
                    return failed("Content should be included");
                }
                return passed();
            });
            // SearchBySql: basic query
            await this.runTest("SearchBySql: basic query", async () => {
                const result = await this.client.search.searchBySql(collection.id, "SELECT * FROM documents WHERE Category = 'Category_1'");
                if (!result)
                    return failed("Search returned null");
                if (result.documents.length !== 4)
                    return failed(`Expected 4 results, got ${result.documents.length}`);
                return passed();
            });
        }
        finally {
            await this.client.collection.delete(collection.id);
        }
    }
    // ========== ENUMERATION API TESTS ==========
    async testEnumerationApi() {
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
                if (!result)
                    return failed("Enumerate returned null");
                if (result.documents.length !== 10)
                    return failed(`Expected 10 results, got ${result.documents.length}`);
                return passed();
            });
            // Enumerate: with MaxResults
            await this.runTest("Enumerate: with MaxResults", async () => {
                const result = await this.client.search.enumerate({
                    collectionId: collection.id,
                    maxResults: 5
                });
                if (!result)
                    return failed("Enumerate returned null");
                if (result.documents.length !== 5)
                    return failed(`Expected 5 results, got ${result.documents.length}`);
                return passed();
            });
            // Enumerate: with Skip
            await this.runTest("Enumerate: with Skip", async () => {
                const result = await this.client.search.enumerate({
                    collectionId: collection.id,
                    skip: 5,
                    maxResults: 100
                });
                if (!result)
                    return failed("Enumerate returned null");
                if (result.documents.length !== 5)
                    return failed(`Expected 5 results, got ${result.documents.length}`);
                return passed();
            });
            // Enumerate: verify TotalRecords
            await this.runTest("Enumerate: verify TotalRecords", async () => {
                const result = await this.client.search.enumerate({
                    collectionId: collection.id,
                    maxResults: 3
                });
                if (!result)
                    return failed("Enumerate returned null");
                if (result.totalRecords !== 10)
                    return failed(`Expected totalRecords=10, got ${result.totalRecords}`);
                return passed();
            });
            // Enumerate: verify EndOfResults
            await this.runTest("Enumerate: verify EndOfResults", async () => {
                const result = await this.client.search.enumerate({
                    collectionId: collection.id,
                    maxResults: 100
                });
                if (!result)
                    return failed("Enumerate returned null");
                if (!result.endOfResults)
                    return failed("Expected endOfResults=true");
                return passed();
            });
        }
        finally {
            await this.client.collection.delete(collection.id);
        }
    }
    // ========== SCHEMA API TESTS ==========
    async testSchemaApi() {
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
                if (schemas.objects.length === 0)
                    return failed("No schemas returned");
                return passed();
            });
            // GetSchema: by id
            await this.runTest("GetSchema: by id", async () => {
                if (!doc1)
                    return failed("Setup: doc1 is null");
                const schema = await this.client.schema.readById(doc1.schemaId);
                if (!schema)
                    return failed("GetSchema returned null");
                if (schema.id !== doc1.schemaId)
                    return failed(`Schema ID mismatch: ${schema.id}`);
                return passed();
            });
            // GetSchema: non-existent
            await this.runTest("GetSchema: non-existent returns null", async () => {
                const schema = await this.client.schema.readById("sch_nonexistent12345");
                if (schema !== null)
                    return failed("Expected null for non-existent schema");
                return passed();
            });
            // GetSchemaElements: returns elements
            await this.runTest("GetSchemaElements: returns elements", async () => {
                if (!doc1)
                    return failed("Setup: doc1 is null");
                const elements = await this.client.schema.getElements(doc1.schemaId);
                if (elements.objects.length === 0)
                    return failed("No elements returned");
                return passed();
            });
            // GetSchemaElements: correct keys
            await this.runTest("GetSchemaElements: correct keys", async () => {
                if (!doc1)
                    return failed("Setup: doc1 is null");
                const elements = await this.client.schema.getElements(doc1.schemaId);
                const keys = new Set(elements.objects.map((e) => e.key));
                const expected = ["name", "value", "active"];
                for (const key of expected) {
                    if (!keys.has(key))
                        return failed(`Missing expected key: ${key}. Found: ${Array.from(keys)}`);
                }
                return passed();
            });
        }
        finally {
            await this.client.collection.delete(collection.id);
        }
    }
    // ========== INDEX API TESTS ==========
    async testIndexApi() {
        // GetIndexTableMappings: returns mappings
        await this.runTest("GetIndexTableMappings: returns mappings", async () => {
            const mappings = await this.client.index.getMappings();
            if (!Array.isArray(mappings.objects))
                return failed("GetMappings did not return objects array");
            return passed();
        });
    }
    // ========== CONSTRAINTS API TESTS ==========
    async testConstraintsApi() {
        // Create collection with strict mode
        await this.runTest("Constraints: create collection with strict mode", async () => {
            const constraint = {
                fieldPath: "name",
                dataType: "string",
                required: true
            };
            const collection = await this.client.collection.create({
                name: "constraints_test",
                schemaEnforcementMode: index_1.SchemaEnforcementMode.Strict,
                fieldConstraints: [constraint]
            });
            if (!collection)
                return failed("Collection creation failed");
            await this.client.collection.delete(collection.id);
            return passed();
        });
        // Update constraints
        await this.runTest("Constraints: update constraints on collection", async () => {
            const collection = await this.client.collection.create({ name: "constraints_update_test" });
            if (!collection)
                return failed("Collection creation failed");
            const constraint = {
                fieldPath: "email",
                dataType: "string",
                required: true,
                regexPattern: "^[\\w\\.-]+@[\\w\\.-]+\\.\\w+$"
            };
            const success = await this.client.collection.updateConstraints(collection.id, index_1.SchemaEnforcementMode.Strict, [constraint]);
            await this.client.collection.delete(collection.id);
            if (!success)
                return failed("Update constraints failed");
            return passed();
        });
        // Get constraints
        await this.runTest("Constraints: get constraints from collection", async () => {
            const constraint = {
                fieldPath: "test_field",
                dataType: "string",
                required: true
            };
            const collection = await this.client.collection.create({
                name: "constraints_get_test",
                schemaEnforcementMode: index_1.SchemaEnforcementMode.Strict,
                fieldConstraints: [constraint]
            });
            if (!collection)
                return failed("Collection creation failed");
            const constraints = await this.client.collection.getConstraints(collection.id);
            await this.client.collection.delete(collection.id);
            if (constraints.length === 0)
                return failed("No constraints returned");
            return passed();
        });
    }
    // ========== INDEXING MODE API TESTS ==========
    async testIndexingModeApi() {
        // Selective mode
        await this.runTest("Indexing: selective mode only indexes specified fields", async () => {
            const collection = await this.client.collection.create({
                name: "indexing_selective_test",
                indexingMode: index_1.IndexingMode.Selective,
                indexedFields: ["name", "email"]
            });
            if (!collection)
                return failed("Collection creation failed");
            await this.client.collection.delete(collection.id);
            return passed();
        });
        // None mode
        await this.runTest("Indexing: none mode skips indexing", async () => {
            const collection = await this.client.collection.create({
                name: "indexing_none_test",
                indexingMode: index_1.IndexingMode.None
            });
            if (!collection)
                return failed("Collection creation failed");
            await this.client.collection.delete(collection.id);
            return passed();
        });
        // Update indexing mode
        await this.runTest("Indexing: update indexing mode", async () => {
            const collection = await this.client.collection.create({ name: "indexing_update_test" });
            if (!collection)
                return failed("Collection creation failed");
            const success = await this.client.collection.updateIndexing(collection.id, index_1.IndexingMode.Selective, ["name"], false);
            await this.client.collection.delete(collection.id);
            if (!success)
                return failed("Update indexing failed");
            return passed();
        });
        // Rebuild indexes
        await this.runTest("Indexing: rebuild indexes", async () => {
            const collection = await this.client.collection.create({ name: "indexing_rebuild_test" });
            if (!collection)
                return failed("Collection creation failed");
            // Ingest a document first
            await this.client.document.ingest({
                collectionId: collection.id,
                content: { name: "test", value: 42 }
            });
            const result = await this.client.collection.rebuildIndexes(collection.id);
            await this.client.collection.delete(collection.id);
            if (!result)
                return failed("Rebuild indexes returned null");
            return passed();
        });
    }
    // ========== EDGE CASE TESTS ==========
    async testEdgeCases() {
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
                if (!doc)
                    return failed("Ingest failed");
                return passed();
            });
            // Special characters
            await this.runTest("Edge: special characters in values", async () => {
                const doc = await this.client.document.ingest({
                    collectionId: collection.id,
                    content: { text: 'Hello! @#$%^&*()_+-={}[]|\\:";<>?,./' }
                });
                if (!doc)
                    return failed("Ingest failed");
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
                if (!doc)
                    return failed("Ingest failed");
                return passed();
            });
            // Large array
            await this.runTest("Edge: large array in JSON", async () => {
                const doc = await this.client.document.ingest({
                    collectionId: collection.id,
                    content: { items: Array.from({ length: 100 }, (_, i) => i) }
                });
                if (!doc)
                    return failed("Ingest failed");
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
                if (!doc)
                    return failed("Ingest failed");
                return passed();
            });
            // Boolean values
            await this.runTest("Edge: boolean values", async () => {
                const doc = await this.client.document.ingest({
                    collectionId: collection.id,
                    content: { active: true, disabled: false }
                });
                if (!doc)
                    return failed("Ingest failed");
                return passed();
            });
            // Null values
            await this.runTest("Edge: null values in JSON", async () => {
                const doc = await this.client.document.ingest({
                    collectionId: collection.id,
                    content: { name: "Test", optional_field: null }
                });
                if (!doc)
                    return failed("Ingest failed");
                return passed();
            });
            // Unicode characters
            await this.runTest("Edge: unicode characters", async () => {
                const doc = await this.client.document.ingest({
                    collectionId: collection.id,
                    content: { greeting: "Hello, world!", japanese: "Hello, world!", emoji: "Hello, world!" }
                });
                if (!doc)
                    return failed("Ingest failed");
                return passed();
            });
        }
        finally {
            await this.client.collection.delete(collection.id);
        }
    }
    // ========== PERFORMANCE TESTS ==========
    async testPerformance() {
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
                    if (!doc)
                        return failed(`Failed to ingest document ${i}`);
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
                    filters: [{ field: "Category", condition: index_1.SearchCondition.Equals, value: "Category_5" }],
                    maxResults: 100
                });
                const elapsed = Date.now() - start;
                if (!result)
                    return failed("Search returned null");
                process.stdout.write(`(${elapsed}ms) `);
                return passed();
            });
            // GetDocuments for 100 documents
            await this.runTest("Perf: GetDocuments for 100 documents", async () => {
                const start = Date.now();
                const result = await this.client.document.readAllInCollection(collection.id);
                const elapsed = Date.now() - start;
                if (result.totalRecords !== 100)
                    return failed(`Expected 100 docs, got ${result.totalRecords}`);
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
                if (!result)
                    return failed("Enumerate returned null");
                if (result.documents.length !== 100)
                    return failed(`Expected 100 docs, got ${result.documents.length}`);
                process.stdout.write(`(${elapsed}ms) `);
                return passed();
            });
        }
        finally {
            await this.client.collection.delete(collection.id);
        }
    }
}
// Main entry point
async function main() {
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
//# sourceMappingURL=data:application/json;base64,eyJ2ZXJzaW9uIjozLCJmaWxlIjoidGVzdC1oYXJuZXNzLmpzIiwic291cmNlUm9vdCI6IiIsInNvdXJjZXMiOlsiLi4vc3JjL3Rlc3QtaGFybmVzcy50cyJdLCJuYW1lcyI6W10sIm1hcHBpbmdzIjoiO0FBQUE7Ozs7Ozs7Ozs7Ozs7R0FhRzs7QUFFSCxtQ0FTaUI7QUFPakIsU0FBUyxNQUFNO0lBQ1gsT0FBTyxFQUFFLE9BQU8sRUFBRSxJQUFJLEVBQUUsQ0FBQztBQUM3QixDQUFDO0FBRUQsU0FBUyxNQUFNLENBQUMsS0FBYTtJQUN6QixPQUFPLEVBQUUsT0FBTyxFQUFFLEtBQUssRUFBRSxLQUFLLEVBQUUsQ0FBQztBQUNyQyxDQUFDO0FBVUQsTUFBTSxXQUFXO0lBU2IsWUFBWSxRQUFnQjtRQU5wQixZQUFPLEdBQWlCLEVBQUUsQ0FBQztRQUMzQixjQUFTLEdBQUcsQ0FBQyxDQUFDO1FBQ2QsY0FBUyxHQUFHLENBQUMsQ0FBQztRQUNkLG1CQUFjLEdBQUcsRUFBRSxDQUFDO1FBQ3BCLHFCQUFnQixHQUFHLENBQUMsQ0FBQztRQUd6QixJQUFJLENBQUMsUUFBUSxHQUFHLFFBQVEsQ0FBQztRQUN6QixJQUFJLENBQUMsTUFBTSxHQUFHLElBQUkscUJBQWEsQ0FBQyxRQUFRLENBQUMsQ0FBQztJQUM5QyxDQUFDO0lBRUQsS0FBSyxDQUFDLFdBQVc7UUFDYixPQUFPLENBQUMsR0FBRyxDQUFDLEdBQUcsQ0FBQyxNQUFNLENBQUMsRUFBRSxDQUFDLENBQUMsQ0FBQztRQUM1QixPQUFPLENBQUMsR0FBRyxDQUFDLG9EQUFvRCxDQUFDLENBQUM7UUFDbEUsT0FBTyxDQUFDLEdBQUcsQ0FBQyxHQUFHLENBQUMsTUFBTSxDQUFDLEVBQUUsQ0FBQyxDQUFDLENBQUM7UUFDNUIsT0FBTyxDQUFDLEdBQUcsRUFBRSxDQUFDO1FBQ2QsT0FBTyxDQUFDLEdBQUcsQ0FBQyxlQUFlLElBQUksQ0FBQyxRQUFRLEVBQUUsQ0FBQyxDQUFDO1FBQzVDLE9BQU8sQ0FBQyxHQUFHLEVBQUUsQ0FBQztRQUVkLElBQUksQ0FB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aXB0L1R5cGVTY3JpcHRcbiAqXG4gKiBBIGNvbXByZWhlbnNpdmUgdGVzdCBzdWl0ZSBmb3IgdGhlIExhdHRpY2UgSmF2YVNjcmlwdCBTREsuXG4gKiBUZXN0cyBhbGwgQVBJIGVuZHBvaW50cyBhbmQgdmFsaWRhdGVzIHJlc3BvbnNlcy5cbiAqXG4gKiBVc2FnZTpcbiAqICAgbnB4IHRzLW5vZGUgc3JjL3Rlc3QtaGFybmVzcy50cyA8ZW5kcG9pbnRfdXJsPlxuICogICBPUlxuICogICBub2RlIGRpc3QvdGVzdC1oYXJuZXNzLmpzIDxlbmRwb2ludF91cmw+XG4gKlxuICogRXhhbXBsZTpcbiAqICAgbnB4IHRzLW5vZGUgc3JjL3Rlc3QtaGFybmVzcy50cyBodHRwOi8vbG9jYWxob3N0OjgwMDBcbiAqL1xuXG5pbXBvcnQge1xuICAgIExhdHRpY2VDbGllbnQsXG4gICAgQ29sbGVjdGlvbixcbiAgICBEb2N1bWVudCxcbiAgICBTZWFyY2hRdWVyeSxcbiAgICBTZWFyY2hDb25kaXRpb24sXG4gICAgU2NoZW1hRW5mb3JjZW1lbnRNb2RlLFxuICAgIEluZGV4aW5nTW9kZSxcbiAgICBGaWVsZENvbnN0cmFpbnRcbn0gZnJvbSBcIi4vaW5kZXhcIjtcblxuaW50ZXJmYWNlIFRlc3RPdXRjb21lIHtcbiAgICBzdWNjZXNzOiBib29sZWFuO1xuICAgIGVycm9yPzogc3RyaW5nO1xufVxuXG5mdW5jdGlvbiBwYXNzZWQoKTogVGVzdE91dGNvbWUge1xuICAgIHJldHVybiB7IHN1Y2Nlc3M6IHRydWUgfTtcbn1cblxuZnVuY3Rpb24gZmFpbGVkKGVycm9yOiBzdHJpbmcpOiBUZXN0T3V0Y29tZSB7XG4gICAgcmV0dXJuIHsgc3VjY2VzczogZmFsc2UsIGVycm9yIH07XG59XG5cbmludGVyZmFjZSBUZXN0UmVzdWx0IHtcbiAgICBzZWN0aW9uOiBzdHJpbmc7XG4gICAgbmFtZTogc3RyaW5nO1xuICAgIHBhc3NlZDogYm9vbGVhbjtcbiAgICBlbGFwc2VkTXM6IG51bWJlcjtcbiAgICBlcnJvcj86IHN0cmluZztcbn1cblxuY2xhc3MgVGVzdEhhcm5lc3Mge1xuICAgIHByaXZhdGUgZW5kcG9pbnQ6IHN0cmluZztcbiAgICBwcml2YXRlIGNsaWVudDogTGF0dGljZUNsaWVudDtcbiAgICBwcml2YXRlIHJlc3VsdHM6IFRlc3RSZXN1bHRbXSA9IFtdO1xuICAgIHByaXZhdGUgcGFzc0NvdW50ID0gMDtcbiAgICBwcml2YXRlIGZhaWxDb3VudCA9IDA7XG4gICAgcHJpdmF0ZSBjdXJyZW50U2VjdGlvbiA9IFwiXCI7XG4gICAgcHJpdmF0ZSBvdmVyYWxsU3RhcnRUaW1lID0gMDtcblxuICAgIGNvbnN0cnVjdG9yKGVuZHBvaW50OiBzdHJpbmcpIHtcbiAgICAgICAgdGhpcy5lbmRwb2ludCA9IGVuZHBvaW50O1xuICAgICAgICB0aGlzLmNsaWVudCA9IG5ldyBMYXR0aWNlQ2xpZW50KGVuZHBvaW50KTtcbiAgICB9XG5cbiAgICBhc3luYyBydW5BbGxUZXN0cygpOiBQcm9taXNlPGJvb2xlYW4+IHtcbiAgICAgICAgY29uc29sZS5sb2coXCI9XCIucmVwZWF0KDc5KSk7XG4gICAgICAgIGNvbnNvbGUubG9nKFwiICBMYXR0aWNlIFNESyBUZXN0IEhhcm5lc3MgLSBKYXZhU2NyaXB0L1R5cGVTY3JpcHRcIik7XG4gICAgICAgIGNvbnNvbGUubG9nKFwiPVwiLnJlcGVhdCg3OSkpO1xuICAgICAgICBjb25zb2xlLmxvZygpO1xuICAgICAgICBjb25zb2xlLmxvZyhgICBFbmRwb2ludDogJHt0aGlzLmVuZHBvaW50fWApO1xuICAgICAgICBjb25zb2xlLmxvZygpO1xuXG4gICAgICAgIHRoaXMub3ZlcmFsbFN0YXJ0VGltZSA9IERhdGUubm93KCk7XG5cbiAgICAgICAgdHJ5IHtcbiAgICAgICAgICAgIC8vIEhlYWx0aCBjaGVjayBmaXJzdFxuICAgICAgICAgICAgYXdhaXQgdGhpcy5ydW5UZXN0U2VjdGlvbihcIkhFQUxUSCBDSEVDS1wiLCAoKSA9PiB0aGlzLnRlc3RIZWFsdGhDaGVjaygpKTtcblxuICAgICAgICAgICAgLy8gQ29sbGVjdGlvbiBBUEkgVGVzdHNcbiAgICAgICAgICAgIGF3YWl0IHRoaXMucnVuVGVzdFNlY3Rpb24oXCJDT0xMRUNUSU9OIEFQSVwiLCAoKSA9PiB0aGlzLnRlc3RDb2xsZWN0aW9uQXBpKCkpO1xuXG4gICAgICAgICAgICAvLyBEb2N1bWVudCBBUEkgVGVzdHNcbiAgICAgICAgICAgIGF3YWl0IHRoaXMucnVuVGVzdFNlY3Rpb24oXCJET0NVTUVOVCBBUElcIiwgKCkgPT4gdGhpcy50ZXN0RG9jdW1lbnRBcGkoKSk7XG5cbiAgICAgICAgICAgIC8vIFNlYXJjaCBBUEkgVGVzdHNcbiAgICAgICAgICAgIGF3YWl0IHRoaXMucnVuVGVzdFNlY3Rpb24oXCJTRUFSQ0ggQVBJXCIsICgpID0+IHRoaXMudGVzdFNlYXJjaEFwaSgpKTtcblxuICAgICAgICAgICAgLy8gRW51bWVyYXRpb24gQVBJIFRlc3RzXG4gICAgICAgICAgICBhd2FpdCB0aGlzLnJ1blRlc3RTZWN0aW9uKFwiRU5VTUVSQVRJT04gQVBJXCIsICgpID0+IHRoaXMudGVzdEVudW1lcmF0aW9uQXBpKCkpO1xuXG4gICAgICAgICAgICAvLyBTY2hlbWEgQVBJIFRlc3RzXG4gICAgICAgICAgICBhd2FpdCB0aGlzLnJ1blRlc3RTZWN0aW9uKFwiU0NIRU1BIEFQSVwiLCAoKSA9PiB0aGlzLnRlc3RTY2hlbWFBcGkoKSk7XG5cbiAgICAgICAgICAgIC8vIEluZGV4IEFQSSBUZXN0c1xuICAgICAgICAgICAgYXdhaXQgdGhpcy5ydW5UZXN0U2VjdGlvbihcIklOREVYIEFQSVwiLCAoKSA9PiB0aGlzLnRlc3RJbmRleEFwaSgpKTtcblxuICAgICAgICAgICAgLy8gQ29uc3RyYWludCBUZXN0c1xuICAgICAgICAgICAgYXdhaXQgdGhpcy5ydW5UZXN0U2VjdGlvbihcIlNDSEVNQSBDT05TVFJBSU5UU1wiLCAoKSA9PiB0aGlzLnRlc3RDb25zdHJhaW50c0FwaSgpKTtcblxuICAgICAgICAgICAgLy8gSW5kZXhpbmcgTW9kZSBUZXN0c1xuICAgICAgICAgICAgYXdhaXQgdGhpcy5ydW5UZXN0U2VjdGlvbihcIklOREVYSU5HIE1PREVcIiwgKCkgPT4gdGhpcy50ZXN0SW5kZXhpbmdNb2RlQXBpKCkpO1xuXG4gICAgICAgICAgICAvLyBFZGdlIENhc2UgVGVzdHNcbiAgICAgICAgICAgIGF3YWl0IHRoaXMucnVuVGVzdFNlY3Rpb24oXCJFREdFIENBU0VTXCIsICgpID0+IHRoaXMudGVzdEVkZ2VDYXNlcygpKTtcblxuICAgICAgICAgICAgLy8gUGVyZm9ybWFuY2UgVGVzdHNcbiAgICAgICAgICAgIGF3YWl0IHRoaXMucnVuVGVzdFNlY3Rpb24oXCJQRVJGT1JNQU5DRVwiLCAoKSA9PiB0aGlzLnRlc3RQZXJmb3JtYW5jZSgpKTtcbiAgICAgICAgfSBjYXRjaCAoZXJyb3I6IGFueSkge1xuICAgICAgICAgICAgY29uc29sZS5sb2coYFtGQVRBTF0gVW5oYW5kbGVkIGV4Y2VwdGlvbjogJHtlcnJvci5tZXNzYWdlfWApO1xuICAgICAgICAgICAgdGhpcy5mYWlsQ291bnQrKztcbiAgICAgICAgfVxuXG4gICAgICAgIGNvbnN0IG92ZXJhbGxFbGFwc2VkID0gRGF0ZS5ub3coKSAtIHRoaXMub3ZlcmFsbFN0YXJ0VGltZTtcbiAgICAgICAgdGhpcy5wcmludFN1bW1hcnkob3ZlcmFsbEVsYXBzZWQpO1xuXG4gICAgICAgIHJldHVybiB0aGlzLmZhaWxDb3VudCA9PT0gMDtcbiAgICB9XG5cbiAgICBwcml2YXRlIGFzeW5jIHJ1blRlc3RTZWN0aW9uKHNlY3Rpb25OYW1lOiBzdHJpbmcsIHRlc3RGdW5jOiAoKSA9PiBQcm9taXNlPHZvaWQ+KTogUHJvbWlzZTx2b2lkPiB7XG4gICAgICAgIGNvbnNvbGUubG9nKCk7XG4gICAgICAgIGNvbnNvbGUubG9nKGAtLS0gJHtzZWN0aW9uTmFtZX0gLS0tYCk7XG4gICAgICAgIHRoaXMuY3VycmVudFNlY3Rpb24gPSBzZWN0aW9uTmFtZTtcbiAgICAgICAgYXdhaXQgdGVzdEZ1bmMoKTtcbiAgICB9XG5cbiAgICBwcml2YXRlIGFzeW5jIHJ1blRlc3QobmFtZTogc3RyaW5nLCB0ZXN0RnVuYzogKCkgPT4gUHJvbWlzZTxUZXN0T3V0Y29tZT4pOiBQcm9taXNlPHZvaWQ+IHtcbiAgICAgICAgY29uc3Qgc3RhcnRUaW1lID0gRGF0ZS5ub3coKTtcbiAgICAgICAgbGV0IHBhc3NlZCA9IGZhbHNlO1xuICAgICAgICBsZXQgZXJyb3I6IHN0cmluZyB8IHVuZGVmaW5lZDtcblxuICAgICAgICB0cnkge1xuICAgICAgICAgICAgY29uc3Qgb3V0Y29tZSA9IGF3YWl0IHRlc3RGdW5jKCk7XG4gICAgICAgICAgICBwYXNzZWQgPSBvdXRjb21lLnN1Y2Nlc3M7XG4gICAgICAgICAgICBlcnJvciA9IG91dGNvbWUuZXJyb3I7XG4gICAgICAgIH0gY2F0Y2ggKGU6IGFueSkge1xuICAgICAgICAgICAgcGFzc2VkID0gZmFsc2U7XG4gICAgICAgICAgICBlcnJvciA9IGUubWVzc2FnZTtcbiAgICAgICAgfVxuXG4gICAgICAgIGNvbnN0IGVsYXBzZWRNcyA9IERhdGUubm93KCkgLSBzdGFydFRpbWU7XG5cbiAgICAgICAgY29uc3QgcmVzdWx0OiBUZXN0UmVzdWx0ID0ge1xuICAgICAgICAgICAgc2VjdGlvbjogdGhpcy5jdXJyZW50U2VjdGlvbixcbiAgICAgICAgICAgIG5hbWUsXG4gICAgICAgICAgICBwYXNzZWQsXG4gICAgICAgICAgICBlbGFwc2VkTXMsXG4gICAgICAgICAgICBlcnJvclxuICAgICAgICB9O1xuICAgICAgICB0aGlzLnJlc3VsdHMucHVzaChyZXN1bHQpO1xuXG4gICAgICAgIGlmIChwYXNzZWQpIHtcbiAgICAgICAgICAgIGNvbnNvbGUubG9nKGAgIFtQQVNTXSAke25hbWV9ICgke2VsYXBzZWRNc31tcylgKTtcbiAgICAgICAgICAgIHRoaXMucGFzc0NvdW50Kys7XG4gICAgICAgIH0gZWxzZSB7XG4gICAgICAgICAgICBjb25zb2xlLmxvZyhgICBbRkFJTF0gJHtuYW1lfSAoJHtlbGFwc2VkTXN9bXMpYCk7XG4gICAgICAgICAgICBpZiAoZXJyb3IpIHtcbiAgICAgICAgICAgICAgICBjb25zb2xlLmxvZyhgICAgICAgICAgRXJyb3I6ICR7ZXJyb3J9YCk7XG4gICAgICAgICAgICB9XG4gICAgICAgICAgICB0aGlzLmZhaWxDb3VudCsrO1xuICAgICAgICB9XG4gICAgfVxuXG4gICAgcHJpdmF0ZSBwcmludFN1bW1hcnkob3ZlcmFsbEVsYXBzZWQ6IG51bWJlcik6IHZvaWQge1xuICAgICAgICBjb25zb2xlLmxvZygpO1xuICAgICAgICBjb25zb2xlLmxvZyhcIj1cIi5yZXBlYXQoNzkpKTtcbiAgICAgICAgY29uc29sZS5sb2coXCIgIFRFU1QgU1VNTUFSWVwiKTtcbiAgICAgICAgY29uc29sZS5sb2coXCI9XCIucmVwZWF0KDc5KSk7XG4gICAgICAgIGNvbnNvbGUubG9nKCk7XG5cbiAgICAgICAgLy8gR3JvdXAgYnkgc2VjdGlvblxuICAgICAgICBjb25zdCBzZWN0aW9ucyA9IG5ldyBNYXA8c3RyaW5nLCBUZXN0UmVzdWx0W10+KCk7XG4gICAgICAgIGZvciAoY29uc3QgcmVzdWx0IG9mIHRoaXMucmVzdWx0cykge1xuICAgICAgICAgICAgaWYgKCFzZWN0aW9ucy5oYXMocmVzdWx0LnNlY3Rpb24pKSB7XG4gICAgICAgICAgICAgICAgc2VjdGlvbnMuc2V0KHJlc3VsdC5zZWN0aW9uLCBbXSk7XG4gICAgICAgICAgICB9XG4gICAgICAgICAgICBzZWN0aW9ucy5nZXQocmVzdWx0LnNlY3Rpb24pIS5wdXNoKHJlc3VsdCk7XG4gICAgICAgIH1cblxuICAgICAgICBmb3IgKGNvbnN0IFtzZWN0aW9uTmFtZSwgc2VjdGlvblJlc3VsdHNdIG9mIHNlY3Rpb25zKSB7XG4gICAgICAgICAgICBjb25zdCBzZWN0aW9uUGFzcyA9IHNlY3Rpb25SZXN1bHRzLmZpbHRlcigocikgPT4gci5wYXNzZWQpLmxlbmd0aDtcbiAgICAgICAgICAgIGNvbnN0IHNlY3Rpb25Ub3RhbCA9IHNlY3Rpb25SZXN1bHRzLmxlbmd0aDtcbiAgICAgICAgICAgIGNvbnN0IHN0YXR1cyA9IHNlY3Rpb25QYXNzID09PSBzZWN0aW9uVG90YWwgPyBcIlBBU1NcIiA6IFwiRkFJTFwiO1xuICAgICAgICAgICAgY29uc29sZS5sb2coYCAgJHtzZWN0aW9uTmFtZX06ICR7c2VjdGlvblBhc3N9LyR7c2VjdGlvblRvdGFsfSBbJHtzdGF0dXN9XWApO1xuICAgICAgICB9XG5cbiAgICAgICAgY29uc29sZS5sb2coKTtcbiAgICAgICAgY29uc29sZS5sb2coXCItXCIucmVwZWF0KDc5KSk7XG4gICAgICAgIGNvbnN0IG92ZXJhbGxTdGF0dXMgPSB0aGlzLmZhaWxDb3VudCA9PT0gMCA/IFwiUEFTU1wiIDogXCJGQUlMXCI7XG4gICAgICAgIGNvbnNvbGUubG9nKGAgIFRPVEFMOiAke3RoaXMucGFzc0NvdW50fSBwYXNzZWQsICR7dGhpcy5mYWlsQ291bnR9IGZhaWxlZCBbJHtvdmVyYWxsU3RhdHVzfV1gKTtcbiAgICAgICAgY29uc29sZS5sb2coYCAgUlVOVElNRTogJHtvdmVyYWxsRWxhcHNlZH1tcyAoJHsob3ZlcmFsbEVsYXBzZWQgLyAxMDAwKS50b0ZpeGVkKDIpfXMpYCk7XG4gICAgICAgIGNvbnNvbGUubG9nKFwiLVwiLnJlcGVhdCg3OSkpO1xuXG4gICAgICAgIGlmICh0aGlzLmZhaWxDb3VudCA+IDApIHtcbiAgICAgICAgICAgIGNvbnNvbGUubG9nKCk7XG4gICAgICAgICAgICBjb25zb2xlLmxvZyhcIiAgRkFJTEVEIFRFU1RTOlwiKTtcbiAgICAgICAgICAgIGZvciAoY29uc3QgcmVzdWx0IG9mIHRoaXMucmVzdWx0cykge1xuICAgICAgICAgICAgICAgIGlmICghcmVzdWx0LnBhc3NlZCkge1xuICAgICAgICAgICAgICAgICAgICBjb25zb2xlLmxvZyhgICAgIC0gJHtyZXN1bHQuc2VjdGlvbn06ICR7cmVzdWx0Lm5hbWV9YCk7XG4gICAgICAgICAgICAgICAgICAgIGlmIChyZXN1bHQuZXJyb3IpIHtcbiAgICAgICAgICAgICAgICAgICAgICAgIGNvbnNvbGUubG9nKGAgICAgICBFcnJvcjogJHtyZXN1bHQuZXJyb3J9YCk7XG4gICAgICAgICAgICAgICAgICAgIH1cbiAgICAgICAgICAgICAgICB9XG4gICAgICAgICAgICB9XG4gICAgICAgIH1cblxuICAgICAgICBjb25zb2xlLmxvZygpO1xuICAgIH1cblxuICAgIC8vID09PT09PT09PT0gSEVBTFRIIENIRUNLIFRFU1RTID09PT09PT09PT1cblxuICAgIHByaXZhdGUgYXN5bmMgdGVzdEhlYWx0aENoZWNrKCk6IFByb21pc2U8dm9pZD4ge1xuICAgICAgICBhd2FpdCB0aGlzLnJ1blRlc3QoXCJIZWFsdGggY2hlY2sgcmV0dXJucyB0cnVlXCIsIGFzeW5jICgpID0+IHtcbiAgICAgICAgICAgIGNvbnN0IGhlYWx0aHkgPSBhd2FpdCB0aGlzLmNsaWVudC5oZWFsdGhDaGVjaygpO1xuICAgICAgICAgICAgcmV0dXJuIGhlYWx0aHkgPyBwYXNzZWQoKSA6IGZhaWxlZChcIkhlYWx0aCBjaGVjayBmYWlsZWRcIik7XG4gICAgICAgIH0pO1xuICAgIH1cblxuICAgIC8vID09PT09PT09PT0gQ09MTEVDVElPTiBBUEkgVEVTVFMgPT09PT09PT09PVxuXG4gICAgcHJpdmF0ZSBhc3luYyB0ZXN0Q29sbGVjdGlvbkFwaSgpOiBQcm9taXNlPHZvaWQ+IHtcbiAgICAgICAgLy8gQ3JlYXRlIGNvbGxlY3Rpb246IGJhc2ljXG4gICAgICAgIGF3YWl0IHRoaXMucnVuVGVzdChcIkNyZWF0ZUNvbGxlY3Rpb246IGJhc2ljXCIsIGFzeW5jICgpID0+IHtcbiAgICAgICAgICAgIGNvbnN0IGNvbGxlY3Rpb24gPSBhd2FpdCB0aGlzLmNsaWVudC5jb2xsZWN0aW9uLmNyZWF0ZSh7IG5hbWU6IFwidGVzdF9iYXNpY19jb2xsZWN0aW9uXCIgfSk7XG4gICAgICAgICAgICBpZiAoIWNvbGxlY3Rpb24pIHJldHVybiBmYWlsZWQoXCJDb2xsZWN0aW9uIGNyZWF0aW9uIHJldHVybmVkIG51bGxcIik7XG4gICAgICAgICAgICBpZiAoIWNvbGxlY3Rpb24uaWQuc3RhcnRzV2l0aChcImNvbF9cIikpIHJldHVybiBmYWlsZWQoYEludmFsaWQgY29sbGVjdGlvbiBJRDogJHtjb2xsZWN0aW9uLmlkfWApO1xuICAgICAgICAgICAgYXdhaXQgdGhpcy5jbGllbnQuY29sbGVjdGlvbi5kZWxldGUoY29sbGVjdGlvbi5pZCk7XG4gICAgICAgICAgICByZXR1cm4gcGFzc2VkKCk7XG4gICAgICAgIH0pO1xuXG4gICAgICAgIC8vIENyZWF0ZSBjb2xsZWN0aW9uOiB3aXRoIGFsbCBwYXJhbWV0ZXJzXG4gICAgICAgIGF3YWl0IHRoaXMucnVuVGVzdChcIkNyZWF0ZUNvbGxlY3Rpb246IHdpdGggYWxsIHBhcmFtZXRlcnNcIiwgYXN5bmMgKCkgPT4ge1xuICAgICAgICAgICAgY29uc3QgY29sbGVjdGlvbiA9IGF3YWl0IHRoaXMuY2xpZW50LmNvbGxlY3Rpb24uY3JlYXRlKHtcbiAgICAgICAgICAgICAgICBuYW1lOiBcInRlc3RfZnVsbF9jb2xsZWN0aW9uXCIsXG4gICAgICAgICAgICAgICAgZGVzY3JpcHRpb246IFwiQSB0ZXN0IGNvbGxlY3Rpb25cIixcbiAgICAgICAgICAgICAgICBsYWJlbHM6IFtcInRlc3RcIiwgXCJmdWxsXCJdLFxuICAgICAgICAgICAgICAgIHRhZ3M6IHsgZW52OiBcInRlc3RcIiwgdmVyc2lvbjogXCIxLjBcIiB9LFxuICAgICAgICAgICAgICAgIHNjaGVtYUVuZm9yY2VtZW50TW9kZTogU2NoZW1hRW5mb3JjZW1lbnRNb2RlLkZsZXhpYmxlLFxuICAgICAgICAgICAgICAgIGluZGV4aW5nTW9kZTogSW5kZXhpbmdNb2RlLkFsbFxuICAgICAgICAgICAgfSk7XG4gICAgICAgICAgICBpZiAoIWNvbGxlY3Rpb24pIHJldHVybiBmYWlsZWQoXCJDb2xsZWN0aW9uIGNyZWF0aW9uIHJldHVybmVkIG51bGxcIik7XG4gICAgICAgICAgICBpZiAoY29sbGVjdGlvbi5uYW1lICE9PSBcInRlc3RfZnVsbF9jb2xsZWN0aW9uXCIpIHJldHVybiBmYWlsZWQoYE5hbWUgbWlzbWF0Y2g6ICR7Y29sbGVjdGlvbi5uYW1lfWApO1xuICAgICAgICAgICAgaWYgKGNvbGxlY3Rpb24uZGVzY3JpcHRpb24gIT09IFwiQSB0ZXN0IGNvbGxlY3Rpb25cIikgcmV0dXJuIGZhaWxlZChgRGVzY3JpcHRpb24gbWlzbWF0Y2g6ICR7Y29sbGVjdGlvbi5kZXNjcmlwdGlvbn1gKTtcbiAgICAgICAgICAgIGF3YWl0IHRoaXMuY2xpZW50LmNvbGxlY3Rpb24uZGVsZXRlKGNvbGxlY3Rpb24uaWQpO1xuICAgICAgICAgICAgcmV0dXJuIHBhc3NlZCgpO1xuICAgICAgICB9KTtcblxuICAgICAgICAvLyBDcmVhdGUgY29sbGVjdGlvbjogdmVyaWZ5IGFsbCBwcm9wZXJ0aWVzXG4gICAgICAgIGF3YWl0IHRoaXMucnVuVGVzdChcIkNyZWF0ZUNvbGxlY3Rpb246IHZlcmlmeSBhbGwgcHJvcGVydGllc1wiLCBhc3luYyAoKSA9PiB7XG4gICAgICAgICAgICBjb25zdCBjb2xsZWN0aW9uID0gYXdhaXQgdGhpcy5jbGllbnQuY29sbGVjdGlvbi5jcmVhdGUoe1xuICAgICAgICAgICAgICAgIG5hbWU6IFwidGVzdF9wcm9wc19jb2xsZWN0aW9uXCIsXG4gICAgICAgICAgICAgICAgZGVzY3JpcHRpb246IFwiUHJvcHMgdGVzdFwiLFxuICAgICAgICAgICAgICAgIGxhYmVsczogW1wicHJvcF90ZXN0XCJdLFxuICAgICAgICAgICAgICAgIHRhZ3M6IHsga2V5OiBcInZhbHVlXCIgfVxuICAgICAgICAgICAgfSk7XG4gICAgICAgICAgICBpZiAoIWNvbGxlY3Rpb24pIHJldHVybiBmYWlsZWQoXCJDb2xsZWN0aW9uIGNyZWF0aW9uIHJldHVybmVkIG51bGxcIik7XG4gICAgICAgICAgICBpZiAoIWNvbGxlY3Rpb24uaWQpIHJldHVybiBmYWlsZWQoXCJJZCBpcyBlbXB0eVwiKTtcbiAgICAgICAgICAgIGlmICghY29sbGVjdGlvbi5jcmVhdGVkVXRjKSByZXR1cm4gZmFpbGVkKFwiQ3JlYXRlZFV0YyBub3Qgc2V0XCIpO1xuICAgICAgICAgICAgYXdhaXQgdGhpcy5jbGllbnQuY29sbGVjdGlvbi5kZWxldGUoY29sbGVjdGlvbi5pZCk7XG4gICAgICAgICAgICByZXR1cm4gcGFzc2VkKCk7XG4gICAgICAgIH0pO1xuXG4gICAgICAgIC8vIEdldENvbGxlY3Rpb246IGV4aXN0aW5nXG4gICAgICAgIGF3YWl0IHRoaXMucnVuVGVzdChcIkdldENvbGxlY3Rpb246IGV4aXN0aW5nXCIsIGFzeW5jICgpID0+IHtcbiAgICAgICAgICAgIGNvbnN0IGNvbGxlY3Rpb24gPSBhd2FpdCB0aGlzLmNsaWVudC5jb2xsZWN0aW9uLmNyZWF0ZSh7IG5hbWU6IFwidGVzdF9nZXRfZXhpc3RpbmdcIiB9KTtcbiAgICAgICAgICAgIGlmICghY29sbGVjdGlvbikgcmV0dXJuIGZhaWxlZChcIlNldHVwOiBDb2xsZWN0aW9uIGNyZWF0aW9uIGZhaWxlZFwiKTtcbiAgICAgICAgICAgIGNvbnN0IHJldHJpZXZlZCA9IGF3YWl0IHRoaXMuY2xpZW50LmNvbGxlY3Rpb24ucmVhZEJ5SWQoY29sbGVjdGlvbi5pZCk7XG4gICAgICAgICAgICBpZiAoIXJldHJpZXZlZCkge1xuICAgICAgICAgICAgICAgIGF3YWl0IHRoaXMuY2xpZW50LmNvbGxlY3Rpb24uZGVsZXRlKGNvbGxlY3Rpb24uaWQpO1xuICAgICAgICAgICAgICAgIHJldHVybiBmYWlsZWQoXCJHZXRDb2xsZWN0aW9uIHJldHVybmVkIG51bGxcIik7XG4gICAgICAgICAgICB9XG4gICAgICAgICAgICBpZiAocmV0cmlldmVkLmlkICE9PSBjb2xsZWN0aW9uLmlkKSB7XG4gICAgICAgICAgICAgICAgYXdhaXQgdGhpcy5jbGllbnQuY29sbGVjdGlvbi5kZWxldGUoY29sbGVjdGlvbi5pZCk7XG4gICAgICAgICAgICAgICAgcmV0dXJuIGZhaWxlZChgSWQgbWlzbWF0Y2g6ICR7cmV0cmlldmVkLmlkfWApO1xuICAgICAgICAgICAgfVxuICAgICAgICAgICAgYXdhaXQgdGhpcy5jbGllbnQuY29sbGVjdGlvbi5kZWxldGUoY29sbGVjdGlvbi5pZCk7XG4gICAgICAgICAgICByZXR1cm4gcGFzc2VkKCk7XG4gICAgICAgIH0pO1xuXG4gICAgICAgIC8vIEdldENvbGxlY3Rpb246IG5vbi1leGlzdGVudFxuICAgICAgICBhd2FpdCB0aGlzLnJ1blRlc3QoXCJHZXRDb2xsZWN0aW9uOiBub24tZXhpc3RlbnQgcmV0dXJucyBudWxsXCIsIGFzeW5jICgpID0+IHtcbiAgICAgICAgICAgIGNvbnN0IHJldHJpZXZlZCA9IGF3YWl0IHRoaXMuY2xpZW50LmNvbGxlY3Rpb24ucmVhZEJ5SWQoXCJjb2xfbm9uZXhpc3RlbnQxMjM0NVwiKTtcbiAgICAgICAgICAgIGlmIChyZXRyaWV2ZWQgIT09IG51bGwpIHJldHVybiBmYWlsZWQoXCJFeHBlY3RlZCBudWxsIGZvciBub24tZXhpc3RlbnQgY29sbGVjdGlvblwiKTtcbiAgICAgICAgICAgIHJldHVybiBwYXNzZWQoKTtcbiAgICAgICAgfSk7XG5cbiAgICAgICAgLy8gR2V0Q29sbGVjdGlvbnM6IG11bHRpcGxlXG4gICAgICAgIGF3YWl0IHRoaXMucnVuVGVzdChcIkdldENvbGxlY3Rpb25zOiBtdWx0aXBsZVwiLCBhc3luYyAoKSA9PiB7XG4gICAgICAgICAgICBjb25zdCBjb2wxID0gYXdhaXQgdGhpcy5jbGllbnQuY29sbGVjdGlvbi5jcmVhdGUoeyBuYW1lOiBcInRlc3RfbXVsdGlfMVwiIH0pO1xuICAgICAgICAgICAgY29uc3QgY29sMiA9IGF3YWl0IHRoaXMuY2xpZW50LmNvbGxlY3Rpb24uY3JlYXRlKHsgbmFtZTogXCJ0ZXN0X211bHRpXzJcIiB9KTtcbiAgICAgICAgICAgIGlmICghY29sMSB8fCAhY29sMikgcmV0dXJuIGZhaWxlZChcIlNldHVwOiBDb2xsZWN0aW9uIGNyZWF0aW9uIGZhaWxlZFwiKTtcblxuICAgICAgICAgICAgY29uc3QgY29sbGVjdGlvbnMgPSBhd2FpdCB0aGlzLmNsaWVudC5jb2xsZWN0aW9uLnJlYWRBbGwoKTtcbiAgICAgICAgICAgIGNvbnN0IGZvdW5kSWRzID0gbmV3IFNldChjb2xsZWN0aW9ucy5vYmplY3RzLm1hcCgoYykgPT4gYy5pZCkpO1xuXG4gICAgICAgICAgICBpZiAoIWZvdW5kSWRzLmhhcyhjb2wxLmlkKSB8fCAhZm91bmRJZHMuaGFzKGNvbDIuaWQpKSB7XG4gICAgICAgICAgICAgICAgYXdhaXQgdGhpcy5jbGllbnQuY29sbGVjdGlvbi5kZWxldGUoY29sMS5pZCk7XG4gICAgICAgICAgICAgICAgYXdhaXQgdGhpcy5jbGllbnQuY29sbGVjdGlvbi5kZWxldGUoY29sMi5pZCk7XG4gICAgICAgICAgICAgICAgcmV0dXJuIGZhaWxlZChcIk5vdCBhbGwgY29sbGVjdGlvbnMgZm91bmRcIik7XG4gICAgICAgICAgICB9XG5cbiAgICAgICAgICAgIGF3YWl0IHRoaXMuY2xpZW50LmNvbGxlY3Rpb24uZGVsZXRlKGNvbDEuaWQpO1xuICAgICAgICAgICAgYXdhaXQgdGhpcy5jbGllbnQuY29sbGVjdGlvbi5kZWxldGUoY29sMi5pZCk7XG4gICAgICAgICAgICByZXR1cm4gcGFzc2VkKCk7XG4gICAgICAgIH0pO1xuXG4gICAgICAgIC8vIENvbGxlY3Rpb25FeGlzdHM6IHRydWVcbiAgICAgICAgYXdhaXQgdGhpcy5ydW5UZXN0KFwiQ29sbGVjdGlvbkV4aXN0czogdHJ1ZSB3aGVuIGV4aXN0c1wiLCBhc3luYyAoKSA9PiB7XG4gICAgICAgICAgICBjb25zdCBjb2xsZWN0aW9uID0gYXdhaXQgdGhpcy5jbGllbnQuY29sbGVjdGlvbi5jcmVhdGUoeyBuYW1lOiBcInRlc3RfZXhpc3RzX3RydWVcIiB9KTtcbiAgICAgICAgICAgIGlmICghY29sbGVjdGlvbikgcmV0dXJuIGZhaWxlZChcIlNldHVwOiBDb2xsZWN0aW9uIGNyZWF0aW9uIGZhaWxlZFwiKTtcbiAgICAgICAgICAgIGNvbnN0IGV4aXN0cyA9IGF3YWl0IHRoaXMuY2xpZW50LmNvbGxlY3Rpb24uZXhpc3RzKGNvbGxlY3Rpb24uaWQpO1xuICAgICAgICAgICAgYXdhaXQgdGhpcy5jbGllbnQuY29sbGVjdGlvbi5kZWxldGUoY29sbGVjdGlvbi5pZCk7XG4gICAgICAgICAgICBpZiAoIWV4aXN0cykgcmV0dXJuIGZhaWxlZChcIkV4cGVjdGVkIGV4aXN0cyB0byBiZSB0cnVlXCIpO1xuICAgICAgICAgICAgcmV0dXJuIHBhc3NlZCgpO1xuICAgICAgICB9KTtcblxuICAgICAgICAvLyBDb2xsZWN0aW9uRXhpc3RzOiBmYWxzZVxuICAgICAgICBhd2FpdCB0aGlzLnJ1blRlc3QoXCJDb2xsZWN0aW9uRXhpc3RzOiBmYWxzZSB3aGVuIG5vdCBleGlzdHNcIiwgYXN5bmMgKCkgPT4ge1xuICAgICAgICAgICAgY29uc3QgZXhpc3RzID0gYXdhaXQgdGhpcy5jbGllbnQuY29sbGVjdGlvbi5leGlzdHMoXCJjb2xfbm9uZXhpc3RlbnQxMjM0NVwiKTtcbiAgICAgICAgICAgIGlmIChleGlzdHMpIHJldHVybiBmYWlsZWQoXCJFeHBlY3RlZCBleGlzdHMgdG8gYmUgZmFsc2VcIik7XG4gICAgICAgICAgICByZXR1cm4gcGFzc2VkKCk7XG4gICAgICAgIH0pO1xuXG4gICAgICAgIC8vIERlbGV0ZUNvbGxlY3Rpb246IHJlbW92ZXMgY29sbGVjdGlvblxuICAgICAgICBhd2FpdCB0aGlzLnJ1blRlc3QoXCJEZWxldGVDb2xsZWN0aW9uOiByZW1vdmVzIGNvbGxlY3Rpb25cIiwgYXN5bmMgKCkgPT4ge1xuICAgICAgICAgICAgY29uc3QgY29sbGVjdGlvbiA9IGF3YWl0IHRoaXMuY2xpZW50LmNvbGxlY3Rpb24uY3JlYXRlKHsgbmFtZTogXCJ0ZXN0X2RlbGV0ZVwiIH0pO1xuICAgICAgICAgICAgaWYgKCFjb2xsZWN0aW9uKSByZXR1cm4gZmFpbGVkKFwiU2V0dXA6IENvbGxlY3Rpb24gY3JlYXRpb24gZmFpbGVkXCIpO1xuICAgICAgICAgICAgY29uc3QgZGVsZXRlZCA9IGF3YWl0IHRoaXMuY2xpZW50LmNvbGxlY3Rpb24uZGVsZXRlKGNvbGxlY3Rpb24uaWQpO1xuICAgICAgICAgICAgaWYgKCFkZWxldGVkKSByZXR1cm4gZmFpbGVkKFwiRGVsZXRlIHJldHVybmVkIGZhbHNlXCIpO1xuICAgICAgICAgICAgY29uc3QgZXhpc3RzID0gYXdhaXQgdGhpcy5jbGllbnQuY29sbGVjdGlvbi5leGlzdHMoY29sbGVjdGlvbi5pZCk7XG4gICAgICAgICAgICBpZiAoZXhpc3RzKSByZXR1cm4gZmFpbGVkKFwiQ29sbGVjdGlvbiBzdGlsbCBleGlzdHMgYWZ0ZXIgZGVsZXRlXCIpO1xuICAgICAgICAgICAgcmV0dXJuIHBhc3NlZCgpO1xuICAgICAgICB9KTtcbiAgICB9XG5cbiAgICAvLyA9PT09PT09PT09IERPQ1VNRU5UIEFQSSBURVNUUyA9PT09PT09PT09XG5cbiAgICBwcml2YXRlIGFzeW5jIHRlc3REb2N1bWVudEFwaSgpOiBQcm9taXNlPHZvaWQ+IHtcbiAgICAgICAgLy8gQ3JlYXRlIGEgY29sbGVjdGlvbiBmb3IgZG9jdW1lbnQgdGVzdHNcbiAgICAgICAgY29uc3QgY29sbGVjdGlvbiA9IGF3YWl0IHRoaXMuY2xpZW50LmNvbGxlY3Rpb24uY3JlYXRlKHsgbmFtZTogXCJkb2NfdGVzdF9jb2xsZWN0aW9uXCIgfSk7XG4gICAgICAgIGlmICghY29sbGVjdGlvbikge1xuICAgICAgICAgICAgYXdhaXQgdGhpcy5ydW5UZXN0KFwiU2V0dXA6IENyZWF0ZSBjb2xsZWN0aW9uXCIsIGFzeW5jICgpID0+IGZhaWxlZChcIkNvbGxlY3Rpb24gY3JlYXRpb24gZmFpbGVkXCIpKTtcbiAgICAgICAgICAgIHJldHVybjtcbiAgICAgICAgfVxuXG4gICAgICAgIHRyeSB7XG4gICAgICAgICAgICAvLyBJbmdlc3REb2N1bWVudDogYmFzaWNcbiAgICAgICAgICAgIGF3YWl0IHRoaXMucnVuVGVzdChcIkluZ2VzdERvY3VtZW50OiBiYXNpY1wiLCBhc3luYyAoKSA9PiB7XG4gICAgICAgICAgICAgICAgY29uc3QgZG9jID0gYXdhaXQgdGhpcy5jbGllbnQuZG9jdW1lbnQuaW5nZXN0KHtcbiAgICAgICAgICAgICAgICAgICAgY29sbGVjdGlvbklkOiBjb2xsZWN0aW9uLmlkLFxuICAgICAgICAgICAgICAgICAgICBjb250ZW50OiB7IG5hbWU6IFwiVGVzdFwiIH1cbiAgICAgICAgICAgICAgICB9KTtcbiAgICAgICAgICAgICAgICBpZiAoIWRvYykgcmV0dXJuIGZhaWxlZChcIkluZ2VzdCByZXR1cm5lZCBudWxsXCIpO1xuICAgICAgICAgICAgICAgIGlmICghZG9jLmlkLnN0YXJ0c1dpdGgoXCJkb2NfXCIpKSByZXR1cm4gZmFpbGVkKGBJbnZhbGlkIGRvY3VtZW50IElEOiAke2RvYy5pZH1gKTtcbiAgICAgICAgICAgICAgICByZXR1cm4gcGFzc2VkKCk7XG4gICAgICAgICAgICB9KTtcblxuICAgICAgICAgICAgLy8gSW5nZXN0RG9jdW1lbnQ6IHdpdGggbmFtZVxuICAgICAgICAgICAgYXdhaXQgdGhpcy5ydW5UZXN0KFwiSW5nZXN0RG9jdW1lbnQ6IHdpdGggbmFtZVwiLCBhc3luYyAoKSA9PiB7XG4gICAgICAgICAgICAgICAgY29uc3QgZG9jID0gYXdhaXQgdGhpcy5jbGllbnQuZG9jdW1lbnQuaW5nZXN0KHtcbiAgICAgICAgICAgICAgICAgICAgY29sbGVjdGlvbklkOiBjb2xsZWN0aW9uLmlkLFxuICAgICAgICAgICAgICAgICAgICBjb250ZW50OiB7IG5hbWU6IFwiTmFtZWRcIiB9LFxuICAgICAgICAgICAgICAgICAgICBuYW1lOiBcIm15X2RvY3VtZW50XCJcbiAgICAgICAgICAgICAgICB9KTtcbiAgICAgICAgICAgICAgICBpZiAoIWRvYykgcmV0dXJuIGZhaWxlZChcIkluZ2VzdCByZXR1cm5lZCBudWxsXCIpO1xuICAgICAgICAgICAgICAgIGlmIChkb2MubmFtZSAhPT0gXCJteV9kb2N1bWVudFwiKSByZXR1cm4gZmFpbGVkKGBOYW1lIG1pc21hdGNoOiAke2RvYy5uYW1lfWApO1xuICAgICAgICAgICAgICAgIHJldHVybiBwYXNzZWQoKTtcbiAgICAgICAgICAgIH0pO1xuXG4gICAgICAgICAgICAvLyBJbmdlc3REb2N1bWVudDogd2l0aCBsYWJlbHNcbiAgICAgICAgICAgIGF3YWl0IHRoaXMucnVuVGVzdChcIkluZ2VzdERvY3VtZW50OiB3aXRoIGxhYmVsc1wiLCBhc3luYyAoKSA9PiB7XG4gICAgICAgICAgICAgICAgY29uc3QgZG9jID0gYXdhaXQgdGhpcy5jbGllbnQuZG9jdW1lbnQuaW5nZXN0KHtcbiAgICAgICAgICAgICAgICAgICAgY29sbGVjdGlvbklkOiBjb2xsZWN0aW9uLmlkLFxuICAgICAgICAgICAgICAgICAgICBjb250ZW50OiB7IG5hbWU6IFwiTGFiZWxlZFwiIH0sXG4gICAgICAgICAgICAgICAgICAgIGxhYmVsczogW1wibGFiZWwxXCIsIFwibGFiZWwyXCJdXG4gICAgICAgICAgICAgICAgfSk7XG4gICAgICAgICAgICAgICAgaWYgKCFkb2MpIHJldHVybiBmYWlsZWQoXCJJbmdlc3QgcmV0dXJuZWQgbnVsbFwiKTtcbiAgICAgICAgICAgICAgICByZXR1cm4gcGFzc2VkKCk7XG4gICAgICAgICAgICB9KTtcblxuICAgICAgICAgICAgLy8gSW5nZXN0RG9jdW1lbnQ6IHdpdGggdGFnc1xuICAgICAgICAgICAgYXdhaXQgdGhpcy5ydW5UZXN0KFwiSW5nZXN0RG9jdW1lbnQ6IHdpdGggdGFnc1wiLCBhc3luYyAoKSA9PiB7XG4gICAgICAgICAgICAgICAgY29uc3QgZG9jID0gYXdhaXQgdGhpcy5jbGllbnQuZG9jdW1lbnQuaW5nZXN0KHtcbiAgICAgICAgICAgICAgICAgICAgY29sbGVjdGlvbklkOiBjb2xsZWN0aW9uLmlkLFxuICAgICAgICAgICAgICAgICAgICBjb250ZW50OiB7IG5hbWU6IFwiVGFnZ2VkXCIgfSxcbiAgICAgICAgICAgICAgICAgICAgdGFnczogeyBrZXk6IFwidmFsdWVcIiB9XG4gICAgICAgICAgICAgICAgfSk7XG4gICAgICAgICAgICAgICAgaWYgKCFkb2MpIHJldHVybiBmYWlsZWQoXCJJbmdlc3QgcmV0dXJuZWQgbnVsbFwiKTtcbiAgICAgICAgICAgICAgICByZXR1cm4gcGFzc2VkKCk7XG4gICAgICAgICAgICB9KTtcblxuICAgICAgICAgICAgLy8gSW5nZXN0RG9jdW1lbnQ6IHZlcmlmeSBhbGwgcHJvcGVydGllc1xuICAgICAgICAgICAgYXdhaXQgdGhpcy5ydW5UZXN0KFwiSW5nZXN0RG9jdW1lbnQ6IHZlcmlmeSBhbGwgcHJvcGVydGllc1wiLCBhc3luYyAoKSA9PiB7XG4gICAgICAgICAgICAgICAgY29uc3QgZG9jID0gYXdhaXQgdGhpcy5jbGllbnQuZG9jdW1lbnQuaW5nZXN0KHtcbiAgICAgICAgICAgICAgICAgICAgY29sbGVjdGlvbklkOiBjb2xsZWN0aW9uLmlkLFxuICAgICAgICAgICAgICAgICAgICBjb250ZW50OiB7IG5hbWU6IFwiUHJvcGVydGllcyBUZXN0XCIgfSxcbiAgICAgICAgICAgICAgICAgICAgbmFtZTogXCJwcm9wX2RvY1wiLFxuICAgICAgICAgICAgICAgICAgICBsYWJlbHM6IFtcInByb3BcIl0sXG4gICAgICAgICAgICAgICAgICAgIHRhZ3M6IHsgcHJvcDogXCJ0ZXN0XCIgfVxuICAgICAgICAgICAgICAgIH0pO1xuICAgICAgICAgICAgICAgIGlmICghZG9jKSByZXR1cm4gZmFpbGVkKFwiSW5nZXN0IHJldHVybmVkIG51bGxcIik7XG4gICAgICAgICAgICAgICAgaWYgKCFkb2MuaWQpIHJldHVybiBmYWlsZWQoXCJJZCBpcyBlbXB0eVwiKTtcbiAgICAgICAgICAgICAgICBpZiAoZG9jLmNvbGxlY3Rpb25JZCAhPT0gY29sbGVjdGlvbi5pZCkgcmV0dXJuIGZhaWxlZChgQ29sbGVjdGlvbklkIG1pc21hdGNoOiAke2RvYy5jb2xsZWN0aW9uSWR9YCk7XG4gICAgICAgICAgICAgICAgaWYgKCFkb2Muc2NoZW1hSWQpIHJldHVybiBmYWlsZWQoXCJTY2hlbWFJZCBpcyBlbXB0eVwiKTtcbiAgICAgICAgICAgICAgICBpZiAoIWRvYy5jcmVhdGVkVXRjKSByZXR1cm4gZmFpbGVkKFwiQ3JlYXRlZFV0YyBub3Qgc2V0XCIpO1xuICAgICAgICAgICAgICAgIHJldHVybiBwYXNzZWQoKTtcbiAgICAgICAgICAgIH0pO1xuXG4gICAgICAgICAgICAvLyBJbmdlc3REb2N1bWVudDogbmVzdGVkIEpTT05cbiAgICAgICAgICAgIGF3YWl0IHRoaXMucnVuVGVzdChcIkluZ2VzdERvY3VtZW50OiBuZXN0ZWQgSlNPTlwiLCBhc3luYyAoKSA9PiB7XG4gICAgICAgICAgICAgICAgY29uc3QgZG9jID0gYXdhaXQgdGhpcy5jbGllbnQuZG9jdW1lbnQuaW5nZXN0KHtcbiAgICAgICAgICAgICAgICAgICAgY29sbGVjdGlvbklkOiBjb2xsZWN0aW9uLmlkLFxuICAgICAgICAgICAgICAgICAgICBjb250ZW50OiB7XG4gICAgICAgICAgICAgICAgICAgICAgICBwZXJzb246IHtcbiAgICAgICAgICAgICAgICAgICAgICAgICAgICBuYW1lOiBcIkpvaG5cIixcbiAgICAgICAgICAgICAgICAgICAgICAgICAgICBhZGRyZXNzOiB7XG4gICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIGNpdHk6IFwiTmV3IFlvcmtcIixcbiAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgemlwOiBcIjEwMDAxXCJcbiAgICAgICAgICAgICAgICAgICAgICAgICAgICB9XG4gICAgICAgICAgICAgICAgICAgICAgICB9XG4gICAgICAgICAgICAgICAgICAgIH1cbiAgICAgICAgICAgICAgICB9KTtcbiAgICAgICAgICAgICAgICBpZiAoIWRvYykgcmV0dXJuIGZhaWxlZChcIkluZ2VzdCByZXR1cm5lZCBudWxsXCIpO1xuICAgICAgICAgICAgICAgIHJldHVybiBwYXNzZWQoKTtcbiAgICAgICAgICAgIH0pO1xuXG4gICAgICAgICAgICAvLyBJbmdlc3REb2N1bWVudDogYXJyYXkgSlNPTlxuICAgICAgICAgICAgYXdhaXQgdGhpcy5ydW5UZXN0KFwiSW5nZXN0RG9jdW1lbnQ6IGFycmF5IEpTT05cIiwgYXN5bmMgKCkgPT4ge1xuICAgICAgICAgICAgICAgIGNvbnN0IGRvYyA9IGF3YWl0IHRoaXMuY2xpZW50LmRvY3VtZW50LmluZ2VzdCh7XG4gICAgICAgICAgICAgICAgICAgIGNvbGxlY3Rpb25JZDogY29sbGVjdGlvbi5pZCxcbiAgICAgICAgICAgICAgICAgICAgY29udGVudDoge1xuICAgICAgICAgICAgICAgICAgICAgICAgaXRlbXM6IFsxLCAyLCAzLCA0LCA1XSxcbiAgICAgICAgICAgICAgICAgICAgICAgIG5hbWVzOiBbXCJBbGljZVwiLCBcIkJvYlwiLCBcIkNoYXJsaWVcIl1cbiAgICAgICAgICAgICAgICAgICAgfVxuICAgICAgICAgICAgICAgIH0pO1xuICAgICAgICAgICAgICAgIGlmICghZG9jKSByZXR1cm4gZmFpbGVkKFwiSW5nZXN0IHJldHVybmVkIG51bGxcIik7XG4gICAgICAgICAgICAgICAgcmV0dXJuIHBhc3NlZCgpO1xuICAgICAgICAgICAgfSk7XG5cbiAgICAgICAgICAgIC8vIENyZWF0ZSBhIHNwZWNpZmljIGRvYyBmb3IgZ2V0IHRlc3RzXG4gICAgICAgICAgICBjb25zdCB0ZXN0RG9jID0gYXdhaXQgdGhpcy5jbGllbnQuZG9jdW1lbnQuaW5nZXN0KHtcbiAgICAgICAgICAgICAgICBjb2xsZWN0aW9uSWQ6IGNvbGxlY3Rpb24uaWQsXG4gICAgICAgICAgICAgICAgY29udGVudDogeyBuYW1lOiBcIkdldFRlc3RcIiwgdmFsdWU6IDQyIH0sXG4gICAgICAgICAgICAgICAgbmFtZTogXCJnZXRfdGVzdF9kb2NcIixcbiAgICAgICAgICAgICAgICBsYWJlbHM6IFtcImdldF90ZXN0XCJdLFxuICAgICAgICAgICAgICAgIHRhZ3M6IHsgdGVzdF90eXBlOiBcImdldFwiIH1cbiAgICAgICAgICAgIH0pO1xuXG4gICAgICAgICAgICBpZiAodGVzdERvYykge1xuICAgICAgICAgICAgICAgIC8vIEdldERvY3VtZW50OiB3aXRob3V0IGNvbnRlbnRcbiAgICAgICAgICAgICAgICBhd2FpdCB0aGlzLnJ1blRlc3QoXCJHZXREb2N1bWVudDogd2l0aG91dCBjb250ZW50XCIsIGFzeW5jICgpID0+IHtcbiAgICAgICAgICAgICAgICAgICAgY29uc3QgZG9jID0gYXdhaXQgdGhpcy5jbGllbnQuZG9jdW1lbnQucmVhZEJ5SWQoY29sbGVjdGlvbi5pZCwgdGVzdERvYy5pZCwgZmFsc2UpO1xuICAgICAgICAgICAgICAgICAgICBpZiAoIWRvYykgcmV0dXJuIGZhaWxlZChcIkdldERvY3VtZW50IHJldHVybmVkIG51bGxcIik7XG4gICAgICAgICAgICAgICAgICAgIGlmIChkb2MuY29udGVudCAhPT0gdW5kZWZpbmVkICYmIGRvYy5jb250ZW50ICE9PSBudWxsKSByZXR1cm4gZmFpbGVkKFwiQ29udGVudCBzaG91bGQgYmUgbnVsbFwiKTtcbiAgICAgICAgICAgICAgICAgICAgcmV0dXJuIHBhc3NlZCgpO1xuICAgICAgICAgICAgICAgIH0pO1xuXG4gICAgICAgICAgICAgICAgLy8gR2V0RG9jdW1lbnQ6IHdpdGggY29udGVudFxuICAgICAgICAgICAgICAgIGF3YWl0IHRoaXMucnVuVGVzdChcIkdldERvY3VtZW50OiB3aXRoIGNvbnRlbnRcIiwgYXN5bmMgKCkgPT4ge1xuICAgICAgICAgICAgICAgICAgICBjb25zdCBkb2MgPSBhd2FpdCB0aGlzLmNsaWVudC5kb2N1bWVudC5yZWFkQnlJZChjb2xsZWN0aW9uLmlkLCB0ZXN0RG9jLmlkLCB0cnVlKTtcbiAgICAgICAgICAgICAgICAgICAgaWYgKCFkb2MpIHJldHVybiBmYWlsZWQoXCJHZXREb2N1bWVudCByZXR1cm5lZCBudWxsXCIpO1xuICAgICAgICAgICAgICAgICAgICBpZiAoZG9jLmNvbnRlbnQgPT09IG51bGwgfHwgZG9jLmNvbnRlbnQgPT09IHVuZGVmaW5lZCkgcmV0dXJuIGZhaWxlZChcIkNvbnRlbnQgc2hvdWxkIG5vdCBiZSBudWxsXCIpO1xuICAgICAgICAgICAgICAgICAgICByZXR1cm4gcGFzc2VkKCk7XG4gICAgICAgICAgICAgICAgfSk7XG5cbiAgICAgICAgICAgICAgICAvLyBHZXREb2N1bWVudDogdmVyaWZ5IGxhYmVsc1xuICAgICAgICAgICAgICAgIGF3YWl0IHRoaXMucnVuVGVzdChcIkdldERvY3VtZW50OiB2ZXJpZnkgbGFiZWxzIHBvcHVsYXRlZFwiLCBhc3luYyAoKSA9PiB7XG4gICAgICAgICAgICAgICAgICAgIGNvbnN0IGRvYyA9IGF3YWl0IHRoaXMuY2xpZW50LmRvY3VtZW50LnJlYWRCeUlkKGNvbGxlY3Rpb24uaWQsIHRlc3REb2MuaWQsIGZhbHNlLCB0cnVlKTtcbiAgICAgICAgICAgICAgICAgICAgaWYgKCFkb2MpIHJldHVybiBmYWlsZWQoXCJHZXREb2N1bWVudCByZXR1cm5lZCBudWxsXCIpO1xuICAgICAgICAgICAgICAgICAgICBpZiAoIWRvYy5sYWJlbHMuaW5jbHVkZXMoXCJnZXRfdGVzdFwiKSkgcmV0dXJuIGZhaWxlZChgTGFiZWwgJ2dldF90ZXN0JyBub3QgZm91bmQ6ICR7ZG9jLmxhYmVsc31gKTtcbiAgICAgICAgICAgICAgICAgICAgcmV0dXJuIHBhc3NlZCgpO1xuICAgICAgICAgICAgICAgIH0pO1xuXG4gICAgICAgICAgICAgICAgLy8gR2V0RG9jdW1lbnQ6IHZlcmlmeSB0YWdzXG4gICAgICAgICAgICAgICAgYXdhaXQgdGhpcy5ydW5UZXN0KFwiR2V0RG9jdW1lbnQ6IHZlcmlmeSB0YWdzIHBvcHVsYXRlZFwiLCBhc3luYyAoKSA9PiB7XG4gICAgICAgICAgICAgICAgICAgIGNvbnN0IGRvYyA9IGF3YWl0IHRoaXMuY2xpZW50LmRvY3VtZW50LnJlYWRCeUlkKGNvbGxlY3Rpb24uaWQsIHRlc3REb2MuaWQsIGZhbHNlLCB0cnVlLCB0cnVlKTtcbiAgICAgICAgICAgICAgICAgICAgaWYgKCFkb2MpIHJldHVybiBmYWlsZWQoXCJHZXREb2N1bWVudCByZXR1cm5lZCBudWxsXCIpO1xuICAgICAgICAgICAgICAgICAgICBpZiAoZG9jLnRhZ3NbXCJ0ZXN0X3R5cGVcIl0gIT09IFwiZ2V0XCIpIHJldHVybiBmYWlsZWQoYFRhZyAndGVzdF90eXBlJyBtaXNtYXRjaDogJHtKU09OLnN0cmluZ2lmeShkb2MudGFncyl9YCk7XG4gICAgICAgICAgICAgICAgICAgIHJldHVybiBwYXNzZWQoKTtcbiAgICAgICAgICAgICAgICB9KTtcbiAgICAgICAgICAgIH1cblxuICAgICAgICAgICAgLy8gR2V0RG9jdW1lbnQ6IG5vbi1leGlzdGVudFxuICAgICAgICAgICAgYXdhaXQgdGhpcy5ydW5UZXN0KFwiR2V0RG9jdW1lbnQ6IG5vbi1leGlzdGVudCByZXR1cm5zIG51bGxcIiwgYXN5bmMgKCkgPT4ge1xuICAgICAgICAgICAgICAgIGNvbnN0IGRvYyA9IGF3YWl0IHRoaXMuY2xpZW50LmRvY3VtZW50LnJlYWRCeUlkKGNvbGxlY3Rpb24uaWQsIFwiZG9jX25vbmV4aXN0ZW50MTIzNDVcIik7XG4gICAgICAgICAgICAgICAgaWYgKGRvYyAhPT0gbnVsbCkgcmV0dXJuIGZhaWxlZChcIkV4cGVjdGVkIG51bGwgZm9yIG5vbi1leGlzdGVudCBkb2N1bWVudFwiKTtcbiAgICAgICAgICAgICAgICByZXR1cm4gcGFzc2VkKCk7XG4gICAgICAgICAgICB9KTtcblxuICAgICAgICAgICAgLy8gR2V0RG9jdW1lbnRzOiBtdWx0aXBsZVxuICAgICAgICAgICAgYXdhaXQgdGhpcy5ydW5UZXN0KFwiR2V0RG9jdW1lbnRzOiBtdWx0aXBsZSBkb2N1bWVudHNcIiwgYXN5bmMgKCkgPT4ge1xuICAgICAgICAgICAgICAgIGNvbnN0IHJlc3VsdCA9IGF3YWl0IHRoaXMuY2xpZW50LmRvY3VtZW50LnJlYWRBbGxJbkNvbGxlY3Rpb24oY29sbGVjdGlvbi5pZCk7XG4gICAgICAgICAgICAgICAgaWYgKHJlc3VsdC50b3RhbFJlY29yZHMgPCA1KSByZXR1cm4gZmFpbGVkKGBFeHBlY3RlZCBhdCBsZWFzdCA1IGRvY3MsIGdvdCAke3Jlc3VsdC50b3RhbFJlY29yZHN9YCk7XG4gICAgICAgICAgICAgICAgcmV0dXJuIHBhc3NlZCgpO1xuICAgICAgICAgICAgfSk7XG5cbiAgICAgICAgICAgIC8vIERvY3VtZW50RXhpc3RzOiB0cnVlXG4gICAgICAgICAgICBhd2FpdCB0aGlzLnJ1blRlc3QoXCJEb2N1bWVudEV4aXN0czogdHJ1ZSB3aGVuIGV4aXN0c1wiLCBhc3luYyAoKSA9PiB7XG4gICAgICAgICAgICAgICAgaWYgKCF0ZXN0RG9jKSByZXR1cm4gZmFpbGVkKFwiU2V0dXA6IHRlc3REb2MgaXMgbnVsbFwiKTtcbiAgICAgICAgICAgICAgICBjb25zdCBleGlzdHMgPSBhd2FpdCB0aGlzLmNsaWVudC5kb2N1bWVudC5leGlzdHMoY29sbGVjdGlvbi5pZCwgdGVzdERvYy5pZCk7XG4gICAgICAgICAgICAgICAgaWYgKCFleGlzdHMpIHJldHVybiBmYWlsZWQoXCJFeHBlY3RlZCBleGlzdHMgdG8gYmUgdHJ1ZVwiKTtcbiAgICAgICAgICAgICAgICByZXR1cm4gcGFzc2VkKCk7XG4gICAgICAgICAgICB9KTtcblxuICAgICAgICAgICAgLy8gRG9jdW1lbnRFeGlzdHM6IGZhbHNlXG4gICAgICAgICAgICBhd2FpdCB0aGlzLnJ1blRlc3QoXCJEb2N1bWVudEV4aXN0czogZmFsc2Ugd2hlbiBub3QgZXhpc3RzXCIsIGFzeW5jICgpID0+IHtcbiAgICAgICAgICAgICAgICBjb25zdCBleGlzdHMgPSBhd2FpdCB0aGlzLmNsaWVudC5kb2N1bWVudC5leGlzdHMoY29sbGVjdGlvbi5pZCwgXCJkb2Nfbm9uZXhpc3RlbnQxMjM0NVwiKTtcbiAgICAgICAgICAgICAgICBpZiAoZXhpc3RzKSByZXR1cm4gZmFpbGVkKFwiRXhwZWN0ZWQgZXhpc3RzIHRvIGJlIGZhbHNlXCIpO1xuICAgICAgICAgICAgICAgIHJldHVybiBwYXNzZWQoKTtcbiAgICAgICAgICAgIH0pO1xuXG4gICAgICAgICAgICAvLyBEZWxldGVEb2N1bWVudDogcmVtb3ZlcyBkb2N1bWVudFxuICAgICAgICAgICAgYXdhaXQgdGhpcy5ydW5UZXN0KFwiRGVsZXRlRG9jdW1lbnQ6IHJlbW92ZXMgZG9jdW1lbnRcIiwgYXN5bmMgKCkgPT4ge1xuICAgICAgICAgICAgICAgIGNvbnN0IGRvYyA9IGF3YWl0IHRoaXMuY2xpZW50LmRvY3VtZW50LmluZ2VzdCh7XG4gICAgICAgICAgICAgICAgICAgIGNvbGxlY3Rpb25JZDogY29sbGVjdGlvbi5pZCxcbiAgICAgICAgICAgICAgICAgICAgY29udGVudDogeyB0b19kZWxldGU6IHRydWUgfVxuICAgICAgICAgICAgICAgIH0pO1xuICAgICAgICAgICAgICAgIGlmICghZG9jKSByZXR1cm4gZmFpbGVkKFwiU2V0dXA6IEluZ2VzdCBmYWlsZWRcIik7XG4gICAgICAgICAgICAgICAgY29uc3QgZGVsZXRlZCA9IGF3YWl0IHRoaXMuY2xpZW50LmRvY3VtZW50LmRlbGV0ZShjb2xsZWN0aW9uLmlkLCBkb2MuaWQpO1xuICAgICAgICAgICAgICAgIGlmICghZGVsZXRlZCkgcmV0dXJuIGZhaWxlZChcIkRlbGV0ZSByZXR1cm5lZCBmYWxzZVwiKTtcbiAgICAgICAgICAgICAgICBjb25zdCBleGlzdHMgPSBhd2FpdCB0aGlzLmNsaWVudC5kb2N1bWVudC5leGlzdHMoY29sbGVjdGlvbi5pZCwgZG9jLmlkKTtcbiAgICAgICAgICAgICAgICBpZiAoZXhpc3RzKSByZXR1cm4gZmFpbGVkKFwiRG9jdW1lbnQgc3RpbGwgZXhpc3RzIGFmdGVyIGRlbGV0ZVwiKTtcbiAgICAgICAgICAgICAgICByZXR1cm4gcGFzc2VkKCk7XG4gICAgICAgICAgICB9KTtcblxuICAgICAgICAgICAgLy8gSW5nZXN0QmF0Y2g6IGJhc2ljIGJhdGNoXG4gICAgICAgICAgICBhd2FpdCB0aGlzLnJ1blRlc3QoXCJJbmdlc3RCYXRjaDogYmFzaWMgYmF0Y2hcIiwgYXN5bmMgKCkgPT4ge1xuICAgICAgICAgICAgICAgIGNvbnN0IHJlc3VsdHMgPSBhd2FpdCB0aGlzLmNsaWVudC5kb2N1bWVudC5pbmdlc3RCYXRjaChcbiAgICAgICAgICAgICAgICAgICAgY29sbGVjdGlvbi5pZCxcbiAgICAgICAgICAgICAgICAgICAgW1xuICAgICAgICAgICAgICAgICAgICAgICAgeyBjb250ZW50OiB7IG5hbWU6IFwiQmF0Y2gxXCIgfSB9LFxuICAgICAgICAgICAgICAgICAgICAgICAgeyBjb250ZW50OiB7IG5hbWU6IFwiQmF0Y2gyXCIgfSB9LFxuICAgICAgICAgICAgICAgICAgICAgICAgeyBjb250ZW50OiB7IG5hbWU6IFwiQmF0Y2gzXCIgfSB9XG4gICAgICAgICAgICAgICAgICAgIF1cbiAgICAgICAgICAgICAgICApO1xuICAgICAgICAgICAgICAgIGlmICghcmVzdWx0cykgcmV0dXJuIGZhaWxlZChcIkluZ2VzdEJhdGNoIHJldHVybmVkIG51bGxcIik7XG4gICAgICAgICAgICAgICAgaWYgKHJlc3VsdHMubGVuZ3RoICE9PSAzKSByZXR1cm4gZmFpbGVkKGBFeHBlY3RlZCAzIGRvY3VtZW50cywgZ290ICR7cmVzdWx0cy5sZW5ndGh9YCk7XG4gICAgICAgICAgICAgICAgcmV0dXJuIHBhc3NlZCgpO1xuICAgICAgICAgICAgfSk7XG5cbiAgICAgICAgICAgIC8vIEluZ2VzdEJhdGNoOiB3aXRoIG5hbWVzIGFuZCBtZXRhZGF0YVxuICAgICAgICAgICAgYXdhaXQgdGhpcy5ydW5UZXN0KFwiSW5nZXN0QmF0Y2g6IHdpdGggbmFtZXMgYW5kIG1ldGFkYXRhXCIsIGFzeW5jICgpID0+IHtcbiAgICAgICAgICAgICAgICBjb25zdCByZXN1bHRzID0gYXdhaXQgdGhpcy5jbGllbnQuZG9jdW1lbnQuaW5nZXN0QmF0Y2goXG4gICAgICAgICAgICAgICAgICAgIGNvbGxlY3Rpb24uaWQsXG4gICAgICAgICAgICAgICAgICAgIFtcbiAgICAgICAgICAgICAgICAgICAgICAgIHsgY29udGVudDogeyB2YWx1ZTogMSB9LCBuYW1lOiBcImJhdGNoX25hbWVkXzFcIiwgbGFiZWxzOiBbXCJiYXRjaFwiLCBcImZpcnN0XCJdLCB0YWdzOiB7IG9yZGVyOiBcIjFcIiB9IH0sXG4gICAgICAgICAgICAgICAgICAgICAgICB7IGNvbnRlbnQ6IHsgdmFsdWU6IDIgfSwgbmFtZTogXCJiYXRjaF9uYW1lZF8yXCIsIGxhYmVsczogW1wiYmF0Y2hcIiwgXCJzZWNvbmRcIl0sIHRhZ3M6IHsgb3JkZXI6IFwiMlwiIH0gfVxuICAgICAgICAgICAgICAgICAgICBdXG4gICAgICAgICAgICAgICAgKTtcbiAgICAgICAgICAgICAgICBpZiAoIXJlc3VsdHMpIHJldHVybiBmYWlsZWQoXCJJbmdlc3RCYXRjaCByZXR1cm5lZCBudWxsXCIpO1xuICAgICAgICAgICAgICAgIGlmIChyZXN1bHRzLmxlbmd0aCAhPT0gMikgcmV0dXJuIGZhaWxlZChgRXhwZWN0ZWQgMiBkb2N1bWVudHMsIGdvdCAke3Jlc3VsdHMubGVuZ3RofWApO1xuICAgICAgICAgICAgICAgIGlmIChyZXN1bHRzWzBdLm5hbWUgIT09IFwiYmF0Y2hfbmFtZWRfMVwiKSByZXR1cm4gZmFpbGVkKGBOYW1lIG1pc21hdGNoOiAke3Jlc3VsdHNbMF0ubmFtZX1gKTtcbiAgICAgICAgICAgICAgICBpZiAocmVzdWx0c1sxXS5uYW1lICE9PSBcImJhdGNoX25hbWVkXzJcIikgcmV0dXJuIGZhaWxlZChgTmFtZSBtaXNtYXRjaDogJHtyZXN1bHRzWzFdLm5hbWV9YCk7XG4gICAgICAgICAgICAgICAgcmV0dXJuIHBhc3NlZCgpO1xuICAgICAgICAgICAgfSk7XG5cbiAgICAgICAgICAgIC8vIEluZ2VzdEJhdGNoOiB2ZXJpZnkgZG9jdW1lbnRzIHJldHJpZXZhYmxlXG4gICAgICAgICAgICBhd2FpdCB0aGlzLnJ1blRlc3QoXCJJbmdlc3RCYXRjaDogdmVyaWZ5IGRvY3VtZW50cyByZXRyaWV2YWJsZVwiLCBhc3luYyAoKSA9PiB7XG4gICAgICAgICAgICAgICAgY29uc3QgcmVzdWx0cyA9IGF3YWl0IHRoaXMuY2xpZW50LmRvY3VtZW50LmluZ2VzdEJhdGNoKFxuICAgICAgICAgICAgICAgICAgICBjb2xsZWN0aW9uLmlkLFxuICAgICAgICAgICAgICAgICAgICBbeyBjb250ZW50OiB7IGNoZWNrOiBcInJldHJpZXZhYmxlXCIgfSwgbmFtZTogXCJiYXRjaF9yZXRyaWV2ZV90ZXN0XCIgfV1cbiAgICAgICAgICAgICAgICApO1xuICAgICAgICAgICAgICAgIGlmICghcmVzdWx0cyB8fCByZXN1bHRzLmxlbmd0aCA9PT0gMCkgcmV0dXJuIGZhaWxlZChcIkluZ2VzdEJhdGNoIHJldHVybmVkIG51bGwgb3IgZW1wdHlcIik7XG4gICAgICAgICAgICAgICAgY29uc3QgcmV0cmlldmVkID0gYXdhaXQgdGhpcy5jbGllbnQuZG9jdW1lbnQucmVhZEJ5SWQoY29sbGVjdGlvbi5pZCwgcmVzdWx0c1swXS5pZCk7XG4gICAgICAgICAgICAgICAgaWYgKCFyZXRyaWV2ZWQpIHJldHVybiBmYWlsZWQoXCJDb3VsZCBub3QgcmV0cmlldmUgYmF0Y2gtaW5nZXN0ZWQgZG9jdW1lbnRcIik7XG4gICAgICAgICAgICAgICAgaWYgKHJldHJpZXZlZC5uYW1lICE9PSBcImJhdGNoX3JldHJpZXZlX3Rlc3RcIikgcmV0dXJuIGZhaWxlZChgUmV0cmlldmVkIG5hbWUgbWlzbWF0Y2g6ICR7cmV0cmlldmVkLm5hbWV9YCk7XG4gICAgICAgICAgICAgICAgcmV0dXJuIHBhc3NlZCgpO1xuICAgICAgICAgICAgfSk7XG4gICAgICAgIH0gZmluYWxseSB7XG4gICAgICAgICAgICBhd2FpdCB0aGlzLmNsaWVudC5jb2xsZWN0aW9uLmRlbGV0ZShjb2xsZWN0aW9uLmlkKTtcbiAgICAgICAgfVxuICAgIH1cblxuICAgIC8vID09PT09PT09PT0gU0VBUkNIIEFQSSBURVNUUyA9PT09PT09PT09XG5cbiAgICBwcml2YXRlIGFzeW5jIHRlc3RTZWFyY2hBcGkoKTogUHJvbWlzZTx2b2lkPiB7XG4gICAgICAgIC8vIENyZWF0ZSBhIGNvbGxlY3Rpb24gd2l0aCBzZWFyY2hhYmxlIGRhdGFcbiAgICAgICAgY29uc3QgY29sbGVjdGlvbiA9IGF3YWl0IHRoaXMuY2xpZW50LmNvbGxlY3Rpb24uY3JlYXRlKHsgbmFtZTogXCJzZWFyY2hfdGVzdF9jb2xsZWN0aW9uXCIgfSk7XG4gICAgICAgIGlmICghY29sbGVjdGlvbikge1xuICAgICAgICAgICAgYXdhaXQgdGhpcy5ydW5UZXN0KFwiU2V0dXA6IENyZWF0ZSBjb2xsZWN0aW9uXCIsIGFzeW5jICgpID0+IGZhaWxlZChcIkNvbGxlY3Rpb24gY3JlYXRpb24gZmFpbGVkXCIpKTtcbiAgICAgICAgICAgIHJldHVybjtcbiAgICAgICAgfVxuXG4gICAgICAgIHRyeSB7XG4gICAgICAgICAgICAvLyBJbmdlc3QgdGVzdCBkb2N1bWVudHNcbiAgICAgICAgICAgIGZvciAobGV0IGkgPSAwOyBpIDwgMjA7IGkrKykge1xuICAgICAgICAgICAgICAgIGF3YWl0IHRoaXMuY2xpZW50LmRvY3VtZW50LmluZ2VzdCh7XG4gICAgICAgICAgICAgICAgICAgIGNvbGxlY3Rpb25JZDogY29sbGVjdGlvbi5pZCxcbiAgICAgICAgICAgICAgICAgICAgY29udGVudDoge1xuICAgICAgICAgICAgICAgICAgICAgICAgTmFtZTogYEl0ZW1fJHtpfWAsXG4gICAgICAgICAgICAgICAgICAgICAgICBDYXRlZ29yeTogYENhdGVnb3J5XyR7aSAlIDV9YCxcbiAgICAgICAgICAgICAgICAgICAgICAgIFZhbHVlOiBpICogMTAsXG4gICAgICAgICAgICAgICAgICAgICAgICBJc0FjdGl2ZTogaSAlIDIgPT09IDAsXG4gICAgICAgICAgICAgICAgICAgICAgICBEZXNjcmlwdGlvbjogYFRoaXMgaXMgaXRlbSBudW1iZXIgJHtpfWBcbiAgICAgICAgICAgICAgICAgICAgfSxcbiAgICAgICAgICAgICAgICAgICAgbmFtZTogYGRvY18ke2l9YCxcbiAgICAgICAgICAgICAgICAgICAgbGFiZWxzOiBbYGdyb3VwXyR7aSAlIDN9YF0uY29uY2F0KGkgJSAxMCA9PT0gMCA/IFtcInNwZWNpYWxcIl0gOiBbXSksXG4gICAgICAgICAgICAgICAgICAgIHRhZ3M6IHsgcHJpb3JpdHk6IFN0cmluZyhpICUgMykgfVxuICAgICAgICAgICAgICAgIH0pO1xuICAgICAgICAgICAgfVxuXG4gICAgICAgICAgICAvLyBTZWFyY2g6IEVxdWFscyBvcGVyYXRvclxuICAgICAgICAgICAgYXdhaXQgdGhpcy5ydW5UZXN0KFwiU2VhcmNoOiBFcXVhbHMgb3BlcmF0b3JcIiwgYXN5bmMgKCkgPT4ge1xuICAgICAgICAgICAgICAgIGNvbnN0IHJlc3VsdCA9IGF3YWl0IHRoaXMuY2xpZW50LnNlYXJjaC5zZWFyY2goe1xuICAgICAgICAgICAgICAgICAgICBjb2xsZWN0aW9uSWQ6IGNvbGxlY3Rpb24uaWQsXG4gICAgICAgICAgICAgICAgICAgIGZpbHRlcnM6IFt7IGZpZWxkOiBcIkNhdGVnb3J5XCIsIGNvbmRpdGlvbjogU2VhcmNoQ29uZGl0aW9uLkVxdWFscywgdmFsdWU6IFwiQ2F0ZWdvcnlfMlwiIH1dLFxuICAgICAgICAgICAgICAgICAgICBtYXhSZXN1bHRzOiAxMDBcbiAgICAgICAgICAgICAgICB9KTtcbiAgICAgICAgICAgICAgICBpZiAoIXJlc3VsdCkgcmV0dXJuIGZhaWxlZChcIlNlYXJjaCByZXR1cm5lZCBudWxsXCIpO1xuICAgICAgICAgICAgICAgIGlmICghcmVzdWx0LnN1Y2Nlc3MpIHJldHVybiBmYWlsZWQoXCJTZWFyY2ggbm90IHN1Y2Nlc3NmdWxcIik7XG4gICAgICAgICAgICAgICAgaWYgKHJlc3VsdC5kb2N1bWVudHMubGVuZ3RoICE9PSA0KSByZXR1cm4gZmFpbGVkKGBFeHBlY3RlZCA0IHJlc3VsdHMsIGdvdCAke3Jlc3VsdC5kb2N1bWVudHMubGVuZ3RofWApO1xuICAgICAgICAgICAgICAgIHJldHVybiBwYXNzZWQoKTtcbiAgICAgICAgICAgIH0pO1xuXG4gICAgICAgICAgICAvLyBTZWFyY2g6IE5vdEVxdWFscyBvcGVyYXRvclxuICAgICAgICAgICAgYXdhaXQgdGhpcy5ydW5UZXN0KFwiU2VhcmNoOiBOb3RFcXVhbHMgb3BlcmF0b3JcIiwgYXN5bmMgKCkgPT4ge1xuICAgICAgICAgICAgICAgIGNvbnN0IHJlc3VsdCA9IGF3YWl0IHRoaXMuY2xpZW50LnNlYXJjaC5zZWFyY2goe1xuICAgICAgICAgICAgICAgICAgICBjb2xsZWN0aW9uSWQ6IGNvbGxlY3Rpb24uaWQsXG4gICAgICAgICAgICAgICAgICAgIGZpbHRlcnM6IFt7IGZpZWxkOiBcIkNhdGVnb3J5XCIsIGNvbmRpdGlvbjogU2VhcmNoQ29uZGl0aW9uLk5vdEVxdWFscywgdmFsdWU6IFwiQ2F0ZWdvcnlfMFwiIH1dLFxuICAgICAgICAgICAgICAgICAgICBtYXhSZXN1bHRzOiAxMDBcbiAgICAgICAgICAgICAgICB9KTtcbiAgICAgICAgICAgICAgICBpZiAoIXJlc3VsdCkgcmV0dXJuIGZhaWxlZChcIlNlYXJjaCByZXR1cm5lZCBudWxsXCIpO1xuICAgICAgICAgICAgICAgIGlmIChyZXN1bHQuZG9jdW1lbnRzLmxlbmd0aCAhPT0gMTYpIHJldHVybiBmYWlsZWQoYEV4cGVjdGVkIDE2IHJlc3VsdHMsIGdvdCAke3Jlc3VsdC5kb2N1bWVudHMubGVuZ3RofWApO1xuICAgICAgICAgICAgICAgIHJldHVybiBwYXNzZWQoKTtcbiAgICAgICAgICAgIH0pO1xuXG4gICAgICAgICAgICAvLyBTZWFyY2g6IEdyZWF0ZXJUaGFuIG9wZXJhdG9yXG4gICAgICAgICAgICBhd2FpdCB0aGlzLnJ1blRlc3QoXCJTZWFyY2g6IEdyZWF0ZXJUaGFuIG9wZXJhdG9yXCIsIGFzeW5jICgpID0+IHtcbiAgICAgICAgICAgICAgICBjb25zdCByZXN1bHQgPSBhd2FpdCB0aGlzLmNsaWVudC5zZWFyY2guc2VhcmNoKHtcbiAgICAgICAgICAgICAgICAgICAgY29sbGVjdGlvbklkOiBjb2xsZWN0aW9uLmlkLFxuICAgICAgICAgICAgICAgICAgICBmaWx0ZXJzOiBbeyBmaWVsZDogXCJWYWx1ZVwiLCBjb25kaXRpb246IFNlYXJjaENvbmRpdGlvbi5HcmVhdGVyVGhhbiwgdmFsdWU6IFwiMTUwXCIgfV0sXG4gICAgICAgICAgICAgICAgICAgIG1heFJlc3VsdHM6IDEwMFxuICAgICAgICAgICAgICAgIH0pO1xuICAgICAgICAgICAgICAgIGlmICghcmVzdWx0KSByZXR1cm4gZmFpbGVkKFwiU2VhcmNoIHJldHVybmVkIG51bGxcIik7XG4gICAgICAgICAgICAgICAgaWYgKHJlc3VsdC5kb2N1bWVudHMubGVuZ3RoICE9PSA0KSByZXR1cm4gZmFpbGVkKGBFeHBlY3RlZCA0IHJlc3VsdHMsIGdvdCAke3Jlc3VsdC5kb2N1bWVudHMubGVuZ3RofWApO1xuICAgICAgICAgICAgICAgIHJldHVybiBwYXNzZWQoKTtcbiAgICAgICAgICAgIH0pO1xuXG4gICAgICAgICAgICAvLyBTZWFyY2g6IExlc3NUaGFuIG9wZXJhdG9yXG4gICAgICAgICAgICBhd2FpdCB0aGlzLnJ1blRlc3QoXCJTZWFyY2g6IExlc3NUaGFuIG9wZXJhdG9yXCIsIGFzeW5jICgpID0+IHtcbiAgICAgICAgICAgICAgICBjb25zdCByZXN1bHQgPSBhd2FpdCB0aGlzLmNsaWVudC5zZWFyY2guc2VhcmNoKHtcbiAgICAgICAgICAgICAgICAgICAgY29sbGVjdGlvbklkOiBjb2xsZWN0aW9uLmlkLFxuICAgICAgICAgICAgICAgICAgICBmaWx0ZXJzOiBbeyBmaWVsZDogXCJWYWx1ZVwiLCBjb25kaXRpb246IFNlYXJjaENvbmRpdGlvbi5MZXNzVGhhbiwgdmFsdWU6IFwiMzBcIiB9XSxcbiAgICAgICAgICAgICAgICAgICAgbWF4UmVzdWx0czogMTAwXG4gICAgICAgICAgICAgICAgfSk7XG4gICAgICAgICAgICAgICAgaWYgKCFyZXN1bHQpIHJldHVybiBmYWlsZWQoXCJTZWFyY2ggcmV0dXJuZWQgbnVsbFwiKTtcbiAgICAgICAgICAgICAgICBpZiAocmVzdWx0LmRvY3VtZW50cy5sZW5ndGggIT09IDMpIHJldHVybiBmYWlsZWQoYEV4cGVjdGVkIDMgcmVzdWx0cywgZ290ICR7cmVzdWx0LmRvY3VtZW50cy5sZW5ndGh9YCk7XG4gICAgICAgICAgICAgICAgcmV0dXJuIHBhc3NlZCgpO1xuICAgICAgICAgICAgfSk7XG5cbiAgICAgICAgICAgIC8vIFNlYXJjaDogQ29udGFpbnMgb3BlcmF0b3JcbiAgICAgICAgICAgIGF3YWl0IHRoaXMucnVuVGVzdChcIlNlYXJjaDogQ29udGFpbnMgb3BlcmF0b3JcIiwgYXN5bmMgKCkgPT4ge1xuICAgICAgICAgICAgICAgIGNvbnN0IHJlc3VsdCA9IGF3YWl0IHRoaXMuY2xpZW50LnNlYXJjaC5zZWFyY2goe1xuICAgICAgICAgICAgICAgICAgICBjb2xsZWN0aW9uSWQ6IGNvbGxlY3Rpb24uaWQsXG4gICAgICAgICAgICAgICAgICAgIGZpbHRlcnM6IFt7IGZpZWxkOiBcIk5hbWVcIiwgY29uZGl0aW9uOiBTZWFyY2hDb25kaXRpb24uQ29udGFpbnMsIHZhbHVlOiBcIkl0ZW1fMVwiIH1dLFxuICAgICAgICAgICAgICAgICAgICBtYXhSZXN1bHRzOiAxMDBcbiAgICAgICAgICAgICAgICB9KTtcbiAgICAgICAgICAgICAgICBpZiAoIXJlc3VsdCkgcmV0dXJuIGZhaWxlZChcIlNlYXJjaCByZXR1cm5lZCBudWxsXCIpO1xuICAgICAgICAgICAgICAgIGlmIChyZXN1bHQuZG9jdW1lbnRzLmxlbmd0aCA8IDEpIHJldHVybiBmYWlsZWQoYEV4cGVjdGVkIGF0IGxlYXN0IDEgcmVzdWx0LCBnb3QgJHtyZXN1bHQuZG9jdW1lbnRzLmxlbmd0aH1gKTtcbiAgICAgICAgICAgICAgICByZXR1cm4gcGFzc2VkKCk7XG4gICAgICAgICAgICB9KTtcblxuICAgICAgICAgICAgLy8gU2VhcmNoOiBTdGFydHNXaXRoIG9wZXJhdG9yXG4gICAgICAgICAgICBhd2FpdCB0aGlzLnJ1blRlc3QoXCJTZWFyY2g6IFN0YXJ0c1dpdGggb3BlcmF0b3JcIiwgYXN5bmMgKCkgPT4ge1xuICAgICAgICAgICAgICAgIGNvbnN0IHJlc3VsdCA9IGF3YWl0IHRoaXMuY2xpZW50LnNlYXJjaC5zZWFyY2goe1xuICAgICAgICAgICAgICAgICAgICBjb2xsZWN0aW9uSWQ6IGNvbGxlY3Rpb24uaWQsXG4gICAgICAgICAgICAgICAgICAgIGZpbHRlcnM6IFt7IGZpZWxkOiBcIk5hbWVcIiwgY29uZGl0aW9uOiBTZWFyY2hDb25kaXRpb24uU3RhcnRzV2l0aCwgdmFsdWU6IFwiSXRlbV9cIiB9XSxcbiAgICAgICAgICAgICAgICAgICAgbWF4UmVzdWx0czogMTAwXG4gICAgICAgICAgICAgICAgfSk7XG4gICAgICAgICAgICAgICAgaWYgKCFyZXN1bHQpIHJldHVybiBmYWlsZWQoXCJTZWFyY2ggcmV0dXJuZWQgbnVsbFwiKTtcbiAgICAgICAgICAgICAgICBpZiAocmVzdWx0LmRvY3VtZW50cy5sZW5ndGggIT09IDIwKSByZXR1cm4gZmFpbGVkKGBFeHBlY3RlZCAyMCByZXN1bHRzLCBnb3QgJHtyZXN1bHQuZG9jdW1lbnRzLmxlbmd0aH1gKTtcbiAgICAgICAgICAgICAgICByZXR1cm4gcGFzc2VkKCk7XG4gICAgICAgICAgICB9KTtcblxuICAgICAgICAgICAgLy8gU2VhcmNoOiBtdWx0aXBsZSBmaWx0ZXJzIChBTkQpXG4gICAgICAgICAgICBhd2FpdCB0aGlzLnJ1blRlc3QoXCJTZWFyY2g6IG11bHRpcGxlIGZpbHRlcnMgKEFORClcIiwgYXN5bmMgKCkgPT4ge1xuICAgICAgICAgICAgICAgIGNvbnN0IHJlc3VsdCA9IGF3YWl0IHRoaXMuY2xpZW50LnNlYXJjaC5zZWFyY2goe1xuICAgICAgICAgICAgICAgICAgICBjb2xsZWN0aW9uSWQ6IGNvbGxlY3Rpb24uaWQsXG4gICAgICAgICAgICAgICAgICAgIGZpbHRlcnM6IFtcbiAgICAgICAgICAgICAgICAgICAgICAgIHsgZmllbGQ6IFwiQ2F0ZWdvcnlcIiwgY29uZGl0aW9uOiBTZWFyY2hDb25kaXRpb24uRXF1YWxzLCB2YWx1ZTogXCJDYXRlZ29yeV8yXCIgfSxcbiAgICAgICAgICAgICAgICAgICAgICAgIHsgZmllbGQ6IFwiSXNBY3RpdmVcIiwgY29uZGl0aW9uOiBTZWFyY2hDb25kaXRpb24uRXF1YWxzLCB2YWx1ZTogXCJ0cnVlXCIgfVxuICAgICAgICAgICAgICAgICAgICBdLFxuICAgICAgICAgICAgICAgICAgICBtYXhSZXN1bHRzOiAxMDBcbiAgICAgICAgICAgICAgICB9KTtcbiAgICAgICAgICAgICAgICBpZiAoIXJlc3VsdCkgcmV0dXJuIGZhaWxlZChcIlNlYXJjaCByZXR1cm5lZCBudWxsXCIpO1xuICAgICAgICAgICAgICAgIGlmIChyZXN1bHQuZG9jdW1lbnRzLmxlbmd0aCAhPT0gMikgcmV0dXJuIGZhaWxlZChgRXhwZWN0ZWQgMiByZXN1bHRzLCBnb3QgJHtyZXN1bHQuZG9jdW1lbnRzLmxlbmd0aH1gKTtcbiAgICAgICAgICAgICAgICByZXR1cm4gcGFzc2VkKCk7XG4gICAgICAgICAgICB9KTtcblxuICAgICAgICAgICAgLy8gU2VhcmNoOiBieSBsYWJlbFxuICAgICAgICAgICAgYXdhaXQgdGhpcy5ydW5UZXN0KFwiU2VhcmNoOiBieSBsYWJlbFwiLCBhc3luYyAoKSA9PiB7XG4gICAgICAgICAgICAgICAgY29uc3QgcmVzdWx0ID0gYXdhaXQgdGhpcy5jbGllbnQuc2VhcmNoLnNlYXJjaCh7XG4gICAgICAgICAgICAgICAgICAgIGNvbGxlY3Rpb25JZDogY29sbGVjdGlvbi5pZCxcbiAgICAgICAgICAgICAgICAgICAgbGFiZWxzOiBbXCJzcGVjaWFsXCJdLFxuICAgICAgICAgICAgICAgICAgICBtYXhSZXN1bHRzOiAxMDBcbiAgICAgICAgICAgICAgICB9KTtcbiAgICAgICAgICAgICAgICBpZiAoIXJlc3VsdCkgcmV0dXJuIGZhaWxlZChcIlNlYXJjaCByZXR1cm5lZCBudWxsXCIpO1xuICAgICAgICAgICAgICAgIGlmIChyZXN1bHQuZG9jdW1lbnRzLmxlbmd0aCAhPT0gMikgcmV0dXJuIGZhaWxlZChgRXhwZWN0ZWQgMiByZXN1bHRzLCBnb3QgJHtyZXN1bHQuZG9jdW1lbnRzLmxlbmd0aH1gKTtcbiAgICAgICAgICAgICAgICByZXR1cm4gcGFzc2VkKCk7XG4gICAgICAgICAgICB9KTtcblxuICAgICAgICAgICAgLy8gU2VhcmNoOiBieSB0YWdcbiAgICAgICAgICAgIGF3YWl0IHRoaXMucnVuVGVzdChcIlNlYXJjaDogYnkgdGFnXCIsIGFzeW5jICgpID0+IHtcbiAgICAgICAgICAgICAgICBjb25zdCByZXN1bHQgPSBhd2FpdCB0aGlzLmNsaWVudC5zZWFyY2guc2VhcmNoKHtcbiAgICAgICAgICAgICAgICAgICAgY29sbGVjdGlvbklkOiBjb2xsZWN0aW9uLmlkLFxuICAgICAgICAgICAgICAgICAgICB0YWdzOiB7IHByaW9yaXR5OiBcIjBcIiB9LFxuICAgICAgICAgICAgICAgICAgICBtYXhSZXN1bHRzOiAxMDBcbiAgICAgICAgICAgICAgICB9KTtcbiAgICAgICAgICAgICAgICBpZiAoIXJlc3VsdCkgcmV0dXJuIGZhaWxlZChcIlNlYXJjaCByZXR1cm5lZCBudWxsXCIpO1xuICAgICAgICAgICAgICAgIGlmIChyZXN1bHQuZG9jdW1lbnRzLmxlbmd0aCAhPT0gNykgcmV0dXJuIGZhaWxlZChgRXhwZWN0ZWQgNyByZXN1bHRzLCBnb3QgJHtyZXN1bHQuZG9jdW1lbnRzLmxlbmd0aH1gKTtcbiAgICAgICAgICAgICAgICByZXR1cm4gcGFzc2VkKCk7XG4gICAgICAgICAgICB9KTtcblxuICAgICAgICAgICAgLy8gU2VhcmNoOiBwYWdpbmF0aW9uIFNraXBcbiAgICAgICAgICAgIGF3YWl0IHRoaXMucnVuVGVzdChcIlNlYXJjaDogcGFnaW5hdGlvbiBTa2lwXCIsIGFzeW5jICgpID0+IHtcbiAgICAgICAgICAgICAgICBjb25zdCByZXN1bHQgPSBhd2FpdCB0aGlzLmNsaWVudC5zZWFyY2guc2VhcmNoKHtcbiAgICAgICAgICAgICAgICAgICAgY29sbGVjdGlvbklkOiBjb2xsZWN0aW9uLmlkLFxuICAgICAgICAgICAgICAgICAgICBza2lwOiAxMCxcbiAgICAgICAgICAgICAgICAgICAgbWF4UmVzdWx0czogMTAwXG4gICAgICAgICAgICAgICAgfSk7XG4gICAgICAgICAgICAgICAgaWYgKCFyZXN1bHQpIHJldHVybiBmYWlsZWQoXCJTZWFyY2ggcmV0dXJuZWQgbnVsbFwiKTtcbiAgICAgICAgICAgICAgICBpZiAocmVzdWx0LmRvY3VtZW50cy5sZW5ndGggIT09IDEwKSByZXR1cm4gZmFpbGVkKGBFeHBlY3RlZCAxMCByZXN1bHRzLCBnb3QgJHtyZXN1bHQuZG9jdW1lbnRzLmxlbmd0aH1gKTtcbiAgICAgICAgICAgICAgICByZXR1cm4gcGFzc2VkKCk7XG4gICAgICAgICAgICB9KTtcblxuICAgICAgICAgICAgLy8gU2VhcmNoOiBwYWdpbmF0aW9uIE1heFJlc3VsdHNcbiAgICAgICAgICAgIGF3YWl0IHRoaXMucnVuVGVzdChcIlNlYXJjaDogcGFnaW5hdGlvbiBNYXhSZXN1bHRzXCIsIGFzeW5jICgpID0+IHtcbiAgICAgICAgICAgICAgICBjb25zdCByZXN1bHQgPSBhd2FpdCB0aGlzLmNsaWVudC5zZWFyY2guc2VhcmNoKHtcbiAgICAgICAgICAgICAgICAgICAgY29sbGVjdGlvbklkOiBjb2xsZWN0aW9uLmlkLFxuICAgICAgICAgICAgICAgICAgICBtYXhSZXN1bHRzOiA1XG4gICAgICAgICAgICAgICAgfSk7XG4gICAgICAgICAgICAgICAgaWYgKCFyZXN1bHQpIHJldHVybiBmYWlsZWQoXCJTZWFyY2ggcmV0dXJuZWQgbnVsbFwiKTtcbiAgICAgICAgICAgICAgICBpZiAocmVzdWx0LmRvY3VtZW50cy5sZW5ndGggIT09IDUpIHJldHVybiBmYWlsZWQoYEV4cGVjdGVkIDUgcmVzdWx0cywgZ290ICR7cmVzdWx0LmRvY3VtZW50cy5sZW5ndGh9YCk7XG4gICAgICAgICAgICAgICAgcmV0dXJuIHBhc3NlZCgpO1xuICAgICAgICAgICAgfSk7XG5cbiAgICAgICAgICAgIC8vIFNlYXJjaDogdmVyaWZ5IFRvdGFsUmVjb3Jkc1xuICAgICAgICAgICAgYXdhaXQgdGhpcy5ydW5UZXN0KFwiU2VhcmNoOiB2ZXJpZnkgVG90YWxSZWNvcmRzXCIsIGFzeW5jICgpID0+IHtcbiAgICAgICAgICAgICAgICBjb25zdCByZXN1bHQgPSBhd2FpdCB0aGlzLmNsaWVudC5zZWFyY2guc2VhcmNoKHtcbiAgICAgICAgICAgICAgICAgICAgY29sbGVjdGlvbklkOiBjb2xsZWN0aW9uLmlkLFxuICAgICAgICAgICAgICAgICAgICBtYXhSZXN1bHRzOiA1XG4gICAgICAgICAgICAgICAgfSk7XG4gICAgICAgICAgICAgICAgaWYgKCFyZXN1bHQpIHJldHVybiBmYWlsZWQoXCJTZWFyY2ggcmV0dXJuZWQgbnVsbFwiKTtcbiAgICAgICAgICAgICAgICBpZiAocmVzdWx0LnRvdGFsUmVjb3JkcyAhPT0gMjApIHJldHVybiBmYWlsZWQoYEV4cGVjdGVkIHRvdGFsUmVjb3Jkcz0yMCwgZ290ICR7cmVzdWx0LnRvdGFsUmVjb3Jkc31gKTtcbiAgICAgICAgICAgICAgICByZXR1cm4gcGFzc2VkKCk7XG4gICAgICAgICAgICB9KTtcblxuICAgICAgICAgICAgLy8gU2VhcmNoOiB2ZXJpZnkgRW5kT2ZSZXN1bHRzXG4gICAgICAgICAgICBhd2FpdCB0aGlzLnJ1blRlc3QoXCJTZWFyY2g6IHZlcmlmeSBFbmRPZlJlc3VsdHMgdHJ1ZVwiLCBhc3luYyAoKSA9PiB7XG4gICAgICAgICAgICAgICAgY29uc3QgcmVzdWx0ID0gYXdhaXQgdGhpcy5jbGllbnQuc2VhcmNoLnNlYXJjaCh7XG4gICAgICAgICAgICAgICAgICAgIGNvbGxlY3Rpb25JZDogY29sbGVjdGlvbi5pZCxcbiAgICAgICAgICAgICAgICAgICAgbWF4UmVzdWx0czogMTAwXG4gICAgICAgICAgICAgICAgfSk7XG4gICAgICAgICAgICAgICAgaWYgKCFyZXN1bHQpIHJldHVybiBmYWlsZWQoXCJTZWFyY2ggcmV0dXJuZWQgbnVsbFwiKTtcbiAgICAgICAgICAgICAgICBpZiAoIXJlc3VsdC5lbmRPZlJlc3VsdHMpIHJldHVybiBmYWlsZWQoXCJFeHBlY3RlZCBlbmRPZlJlc3VsdHM9dHJ1ZVwiKTtcbiAgICAgICAgICAgICAgICByZXR1cm4gcGFzc2VkKCk7XG4gICAgICAgICAgICB9KTtcblxuICAgICAgICAgICAgLy8gU2VhcmNoOiBlbXB0eSByZXN1bHRzXG4gICAgICAgICAgICBhd2FpdCB0aGlzLnJ1blRlc3QoXCJTZWFyY2g6IGVtcHR5IHJlc3VsdHNcIiwgYXN5bmMgKCkgPT4ge1xuICAgICAgICAgICAgICAgIGNvbnN0IHJlc3VsdCA9IGF3YWl0IHRoaXMuY2xpZW50LnNlYXJjaC5zZWFyY2goe1xuICAgICAgICAgICAgICAgICAgICBjb2xsZWN0aW9uSWQ6IGNvbGxlY3Rpb24uaWQsXG4gICAgICAgICAgICAgICAgICAgIGZpbHRlcnM6IFt7IGZpZWxkOiBcIk5hbWVcIiwgY29uZGl0aW9uOiBTZWFyY2hDb25kaXRpb24uRXF1YWxzLCB2YWx1ZTogXCJOb25FeGlzdGVudFwiIH1dLFxuICAgICAgICAgICAgICAgICAgICBtYXhSZXN1bHRzOiAxMDBcbiAgICAgICAgICAgICAgICB9KTtcbiAgICAgICAgICAgICAgICBpZiAoIXJlc3VsdCkgcmV0dXJuIGZhaWxlZChcIlNlYXJjaCByZXR1cm5lZCBudWxsXCIpO1xuICAgICAgICAgICAgICAgIGlmIChyZXN1bHQuZG9jdW1lbnRzLmxlbmd0aCAhPT0gMCkgcmV0dXJuIGZhaWxlZChgRXhwZWN0ZWQgMCByZXN1bHRzLCBnb3QgJHtyZXN1bHQuZG9jdW1lbnRzLmxlbmd0aH1gKTtcbiAgICAgICAgICAgICAgICByZXR1cm4gcGFzc2VkKCk7XG4gICAgICAgICAgICB9KTtcblxuICAgICAgICAgICAgLy8gU2VhcmNoOiB3aXRoIEluY2x1ZGVDb250ZW50XG4gICAgICAgICAgICBhd2FpdCB0aGlzLnJ1blRlc3QoXCJTZWFyY2g6IHdpdGggSW5jbHVkZUNvbnRlbnQgdHJ1ZVwiLCBhc3luYyAoKSA9PiB7XG4gICAgICAgICAgICAgICAgY29uc3QgcmVzdWx0ID0gYXdhaXQgdGhpcy5jbGllbnQuc2VhcmNoLnNlYXJjaCh7XG4gICAgICAgICAgICAgICAgICAgIGNvbGxlY3Rpb25JZDogY29sbGVjdGlvbi5pZCxcbiAgICAgICAgICAgICAgICAgICAgbWF4UmVzdWx0czogMSxcbiAgICAgICAgICAgICAgICAgICAgaW5jbHVkZUNvbnRlbnQ6IHRydWVcbiAgICAgICAgICAgICAgICB9KTtcbiAgICAgICAgICAgICAgICBpZiAoIXJlc3VsdCkgcmV0dXJuIGZhaWxlZChcIlNlYXJjaCByZXR1cm5lZCBudWxsXCIpO1xuICAgICAgICAgICAgICAgIGlmIChyZXN1bHQuZG9jdW1lbnRzLmxlbmd0aCA9PT0gMCkgcmV0dXJuIGZhaWxlZChcIk5vIGRvY3VtZW50cyByZXR1cm5lZFwiKTtcbiAgICAgICAgICAgICAgICBpZiAocmVzdWx0LmRvY3VtZW50c1swXS5jb250ZW50ID09PSBudWxsIHx8IHJlc3VsdC5kb2N1bWVudHNbMF0uY29udGVudCA9PT0gdW5kZWZpbmVkKSB7XG4gICAgICAgICAgICAgICAgICAgIHJldHVybiBmYWlsZWQoXCJDb250ZW50IHNob3VsZCBiZSBpbmNsdWRlZFwiKTtcbiAgICAgICAgICAgICAgICB9XG4gICAgICAgICAgICAgICAgcmV0dXJuIHBhc3NlZCgpO1xuICAgICAgICAgICAgfSk7XG5cbiAgICAgICAgICAgIC8vIFNlYXJjaEJ5U3FsOiBiYXNpYyBxdWVyeVxuICAgICAgICAgICAgYXdhaXQgdGhpcy5ydW5UZXN0KFwiU2VhcmNoQnlTcWw6IGJhc2ljIHF1ZXJ5XCIsIGFzeW5jICgpID0+IHtcbiAgICAgICAgICAgICAgICBjb25zdCByZXN1bHQgPSBhd2FpdCB0aGlzLmNsaWVudC5zZWFyY2guc2VhcmNoQnlTcWwoXG4gICAgICAgICAgICAgICAgICAgIGNvbGxlY3Rpb24uaWQsXG4gICAgICAgICAgICAgICAgICAgIFwiU0VMRUNUICogRlJPTSBkb2N1bWVudHMgV0hFUkUgQ2F0ZWdvcnkgPSAnQ2F0ZWdvcnlfMSdcIlxuICAgICAgICAgICAgICAgICk7XG4gICAgICAgICAgICAgICAgaWYgKCFyZXN1bHQpIHJldHVybiBmYWlsZWQoXCJTZWFyY2ggcmV0dXJuZWQgbnVsbFwiKTtcbiAgICAgICAgICAgICAgICBpZiAocmVzdWx0LmRvY3VtZW50cy5sZW5ndGggIT09IDQpIHJldHVybiBmYWlsZWQoYEV4cGVjdGVkIDQgcmVzdWx0cywgZ290ICR7cmVzdWx0LmRvY3VtZW50cy5sZW5ndGh9YCk7XG4gICAgICAgICAgICAgICAgcmV0dXJuIHBhc3NlZCgpO1xuICAgICAgICAgICAgfSk7XG4gICAgICAgIH0gZmluYWxseSB7XG4gICAgICAgICAgICBhd2FpdCB0aGlzLmNsaWVudC5jb2xsZWN0aW9uLmRlbGV0ZShjb2xsZWN0aW9uLmlkKTtcbiAgICAgICAgfVxuICAgIH1cblxuICAgIC8vID09PT09PT09PT0gRU5VTUVSQVRJT04gQVBJIFRFU1RTID09PT09PT09PT1cblxuICAgIHByaXZhdGUgYXN5bmMgdGVzdEVudW1lcmF0aW9uQXBpKCk6IFByb21pc2U8dm9pZD4ge1xuICAgICAgICBjb25zdCBjb2xsZWN0aW9uID0gYXdhaXQgdGhpcy5jbGllbnQuY29sbGVjdGlvbi5jcmVhdGUoeyBuYW1lOiBcImVudW1fdGVzdF9jb2xsZWN0aW9uXCIgfSk7XG4gICAgICAgIGlmICghY29sbGVjdGlvbikge1xuICAgICAgICAgICAgYXdhaXQgdGhpcy5ydW5UZXN0KFwiU2V0dXA6IENyZWF0ZSBjb2xsZWN0aW9uXCIsIGFzeW5jICgpID0+IGZhaWxlZChcIkNvbGxlY3Rpb24gY3JlYXRpb24gZmFpbGVkXCIpKTtcbiAgICAgICAgICAgIHJldHVybjtcbiAgICAgICAgfVxuXG4gICAgICAgIHRyeSB7XG4gICAgICAgICAgICAvLyBJbmdlc3QgdGVzdCBkb2N1bWVudHNcbiAgICAgICAgICAgIGZvciAobGV0IGkgPSAwOyBpIDwgMTA7IGkrKykge1xuICAgICAgICAgICAgICAgIGF3YWl0IHRoaXMuY2xpZW50LmRvY3VtZW50LmluZ2VzdCh7XG4gICAgICAgICAgICAgICAgICAgIGNvbGxlY3Rpb25JZDogY29sbGVjdGlvbi5pZCxcbiAgICAgICAgICAgICAgICAgICAgY29udGVudDogeyBpbmRleDogaSwgbmFtZTogYEVudW1JdGVtXyR7aX1gIH0sXG4gICAgICAgICAgICAgICAgICAgIG5hbWU6IGBlbnVtX2RvY18ke2l9YFxuICAgICAgICAgICAgICAgIH0pO1xuICAgICAgICAgICAgICAgIGF3YWl0IG5ldyBQcm9taXNlKChyZXNvbHZlKSA9PiBzZXRUaW1lb3V0KHJlc29sdmUsIDUwKSk7IC8vIFNtYWxsIGRlbGF5XG4gICAgICAgICAgICB9XG5cbiAgICAgICAgICAgIC8vIEVudW1lcmF0ZTogYmFzaWNcbiAgICAgICAgICAgIGF3YWl0IHRoaXMucnVuVGVzdChcIkVudW1lcmF0ZTogYmFzaWNcIiwgYXN5bmMgKCkgPT4ge1xuICAgICAgICAgICAgICAgIGNvbnN0IHJlc3VsdCA9IGF3YWl0IHRoaXMuY2xpZW50LnNlYXJjaC5lbnVtZXJhdGUoe1xuICAgICAgICAgICAgICAgICAgICBjb2xsZWN0aW9uSWQ6IGNvbGxlY3Rpb24uaWQsXG4gICAgICAgICAgICAgICAgICAgIG1heFJlc3VsdHM6IDEwMFxuICAgICAgICAgICAgICAgIH0pO1xuICAgICAgICAgICAgICAgIGlmICghcmVzdWx0KSByZXR1cm4gZmFpbGVkKFwiRW51bWVyYXRlIHJldHVybmVkIG51bGxcIik7XG4gICAgICAgICAgICAgICAgaWYgKHJlc3VsdC5kb2N1bWVudHMubGVuZ3RoICE9PSAxMCkgcmV0dXJuIGZhaWxlZChgRXhwZWN0ZWQgMTAgcmVzdWx0cywgZ290ICR7cmVzdWx0LmRvY3VtZW50cy5sZW5ndGh9YCk7XG4gICAgICAgICAgICAgICAgcmV0dXJuIHBhc3NlZCgpO1xuICAgICAgICAgICAgfSk7XG5cbiAgICAgICAgICAgIC8vIEVudW1lcmF0ZTogd2l0aCBNYXhSZXN1bHRzXG4gICAgICAgICAgICBhd2FpdCB0aGlzLnJ1blRlc3QoXCJFbnVtZXJhdGU6IHdpdGggTWF4UmVzdWx0c1wiLCBhc3luYyAoKSA9PiB7XG4gICAgICAgICAgICAgICAgY29uc3QgcmVzdWx0ID0gYXdhaXQgdGhpcy5jbGllbnQuc2VhcmNoLmVudW1lcmF0ZSh7XG4gICAgICAgICAgICAgICAgICAgIGNvbGxlY3Rpb25JZDogY29sbGVjdGlvbi5pZCxcbiAgICAgICAgICAgICAgICAgICAgbWF4UmVzdWx0czogNVxuICAgICAgICAgICAgICAgIH0pO1xuICAgICAgICAgICAgICAgIGlmICghcmVzdWx0KSByZXR1cm4gZmFpbGVkKFwiRW51bWVyYXRlIHJldHVybmVkIG51bGxcIik7XG4gICAgICAgICAgICAgICAgaWYgKHJlc3VsdC5kb2N1bWVudHMubGVuZ3RoICE9PSA1KSByZXR1cm4gZmFpbGVkKGBFeHBlY3RlZCA1IHJlc3VsdHMsIGdvdCAke3Jlc3VsdC5kb2N1bWVudHMubGVuZ3RofWApO1xuICAgICAgICAgICAgICAgIHJldHVybiBwYXNzZWQoKTtcbiAgICAgICAgICAgIH0pO1xuXG4gICAgICAgICAgICAvLyBFbnVtZXJhdGU6IHdpdGggU2tpcFxuICAgICAgICAgICAgYXdhaXQgdGhpcy5ydW5UZXN0KFwiRW51bWVyYXRlOiB3aXRoIFNraXBcIiwgYXN5bmMgKCkgPT4ge1xuICAgICAgICAgICAgICAgIGNvbnN0IHJlc3VsdCA9IGF3YWl0IHRoaXMuY2xpZW50LnNlYXJjaC5lbnVtZXJhdGUoe1xuICAgICAgICAgICAgICAgICAgICBjb2xsZWN0aW9uSWQ6IGNvbGxlY3Rpb24uaWQsXG4gICAgICAgICAgICAgICAgICAgIHNraXA6IDUsXG4gICAgICAgICAgICAgICAgICAgIG1heFJlc3VsdHM6IDEwMFxuICAgICAgICAgICAgICAgIH0pO1xuICAgICAgICAgICAgICAgIGlmICghcmVzdWx0KSByZXR1cm4gZmFpbGVkKFwiRW51bWVyYXRlIHJldHVybmVkIG51bGxcIik7XG4gICAgICAgICAgICAgICAgaWYgKHJlc3VsdC5kb2N1bWVudHMubGVuZ3RoICE9PSA1KSByZXR1cm4gZmFpbGVkKGBFeHBlY3RlZCA1IHJlc3VsdHMsIGdvdCAke3Jlc3VsdC5kb2N1bWVudHMubGVuZ3RofWApO1xuICAgICAgICAgICAgICAgIHJldHVybiBwYXNzZWQoKTtcbiAgICAgICAgICAgIH0pO1xuXG4gICAgICAgICAgICAvLyBFbnVtZXJhdGU6IHZlcmlmeSBUb3RhbFJlY29yZHNcbiAgICAgICAgICAgIGF3YWl0IHRoaXMucnVuVGVzdChcIkVudW1lcmF0ZTogdmVyaWZ5IFRvdGFsUmVjb3Jkc1wiLCBhc3luYyAoKSA9PiB7XG4gICAgICAgICAgICAgICAgY29uc3QgcmVzdWx0ID0gYXdhaXQgdGhpcy5jbGllbnQuc2VhcmNoLmVudW1lcmF0ZSh7XG4gICAgICAgICAgICAgICAgICAgIGNvbGxlY3Rpb25JZDogY29sbGVjdGlvbi5pZCxcbiAgICAgICAgICAgICAgICAgICAgbWF4UmVzdWx0czogM1xuICAgICAgICAgICAgICAgIH0pO1xuICAgICAgICAgICAgICAgIGlmICghcmVzdWx0KSByZXR1cm4gZmFpbGVkKFwiRW51bWVyYXRlIHJldHVybmVkIG51bGxcIik7XG4gICAgICAgICAgICAgICAgaWYgKHJlc3VsdC50b3RhbFJlY29yZHMgIT09IDEwKSByZXR1cm4gZmFpbGVkKGBFeHBlY3RlZCB0b3RhbFJlY29yZHM9MTAsIGdvdCAke3Jlc3VsdC50b3RhbFJlY29yZHN9YCk7XG4gICAgICAgICAgICAgICAgcmV0dXJuIHBhc3NlZCgpO1xuICAgICAgICAgICAgfSk7XG5cbiAgICAgICAgICAgIC8vIEVudW1lcmF0ZTogdmVyaWZ5IEVuZE9mUmVzdWx0c1xuICAgICAgICAgICAgYXdhaXQgdGhpcy5ydW5UZXN0KFwiRW51bWVyYXRlOiB2ZXJpZnkgRW5kT2ZSZXN1bHRzXCIsIGFzeW5jICgpID0+IHtcbiAgICAgICAgICAgICAgICBjb25zdCByZXN1bHQgPSBhd2FpdCB0aGlzLmNsaWVudC5zZWFyY2guZW51bWVyYXRlKHtcbiAgICAgICAgICAgICAgICAgICAgY29sbGVjdGlvbklkOiBjb2xsZWN0aW9uLmlkLFxuICAgICAgICAgICAgICAgICAgICBtYXhSZXN1bHRzOiAxMDBcbiAgICAgICAgICAgICAgICB9KTtcbiAgICAgICAgICAgICAgICBpZiAoIXJlc3VsdCkgcmV0dXJuIGZhaWxlZChcIkVudW1lcmF0ZSByZXR1cm5lZCBudWxsXCIpO1xuICAgICAgICAgICAgICAgIGlmICghcmVzdWx0LmVuZE9mUmVzdWx0cykgcmV0dXJuIGZhaWxlZChcIkV4cGVjdGVkIGVuZE9mUmVzdWx0cz10cnVlXCIpO1xuICAgICAgICAgICAgICAgIHJldHVybiBwYXNzZWQoKTtcbiAgICAgICAgICAgIH0pO1xuICAgICAgICB9IGZpbmFsbHkge1xuICAgICAgICAgICAgYXdhaXQgdGhpcy5jbGllbnQuY29sbGVjdGlvbi5kZWxldGUoY29sbGVjdGlvbi5pZCk7XG4gICAgICAgIH1cbiAgICB9XG5cbiAgICAvLyA9PT09PT09PT09IFNDSEVNQSBBUEkgVEVTVFMgPT09PT09PT09PVxuXG4gICAgcHJpdmF0ZSBhc3luYyB0ZXN0U2NoZW1hQXBpKCk6IFByb21pc2U8dm9pZD4ge1xuICAgICAgICBjb25zdCBjb2xsZWN0aW9uID0gYXdhaXQgdGhpcy5jbGllbnQuY29sbGVjdGlvbi5jcmVhdGUoeyBuYW1lOiBcInNjaGVtYV90ZXN0X2NvbGxlY3Rpb25cIiB9KTtcbiAgICAgICAgaWYgKCFjb2xsZWN0aW9uKSB7XG4gICAgICAgICAgICBhd2FpdCB0aGlzLnJ1blRlc3QoXCJTZXR1cDogQ3JlYXRlIGNvbGxlY3Rpb25cIiwgYXN5bmMgKCkgPT4gZmFpbGVkKFwiQ29sbGVjdGlvbiBjcmVhdGlvbiBmYWlsZWRcIikpO1xuICAgICAgICAgICAgcmV0dXJuO1xuICAgICAgICB9XG5cbiAgICAgICAgdHJ5IHtcbiAgICAgICAgICAgIC8vIEluZ2VzdCBkb2N1bWVudHMgdG8gY3JlYXRlIHNjaGVtYXNcbiAgICAgICAgICAgIGNvbnN0IGRvYzEgPSBhd2FpdCB0aGlzLmNsaWVudC5kb2N1bWVudC5pbmdlc3Qoe1xuICAgICAgICAgICAgICAgIGNvbGxlY3Rpb25JZDogY29sbGVjdGlvbi5pZCxcbiAgICAgICAgICAgICAgICBjb250ZW50OiB7IG5hbWU6IFwiVGVzdFwiLCB2YWx1ZTogNDIsIGFjdGl2ZTogdHJ1ZSB9XG4gICAgICAgICAgICB9KTtcblxuICAgICAgICAgICAgLy8gR2V0U2NoZW1hczogcmV0dXJucyBzY2hlbWFzXG4gICAgICAgICAgICBhd2FpdCB0aGlzLnJ1blRlc3QoXCJHZXRTY2hlbWFzOiByZXR1cm5zIHNjaGVtYXNcIiwgYXN5bmMgKCkgPT4ge1xuICAgICAgICAgICAgICAgIGNvbnN0IHNjaGVtYXMgPSBhd2FpdCB0aGlzLmNsaWVudC5zY2hlbWEucmVhZEFsbCgpO1xuICAgICAgICAgICAgICAgIGlmIChzY2hlbWFzLm9iamVjdHMubGVuZ3RoID09PSAwKSByZXR1cm4gZmFpbGVkKFwiTm8gc2NoZW1hcyByZXR1cm5lZFwiKTtcbiAgICAgICAgICAgICAgICByZXR1cm4gcGFzc2VkKCk7XG4gICAgICAgICAgICB9KTtcblxuICAgICAgICAgICAgLy8gR2V0U2NoZW1hOiBieSBpZFxuICAgICAgICAgICAgYXdhaXQgdGhpcy5ydW5UZXN0KFwiR2V0U2NoZW1hOiBieSBpZFwiLCBhc3luYyAoKSA9PiB7XG4gICAgICAgICAgICAgICAgaWYgKCFkb2MxKSByZXR1cm4gZmFpbGVkKFwiU2V0dXA6IGRvYzEgaXMgbnVsbFwiKTtcbiAgICAgICAgICAgICAgICBjb25zdCBzY2hlbWEgPSBhd2FpdCB0aGlzLmNsaWVudC5zY2hlbWEucmVhZEJ5SWQoZG9jMS5zY2hlbWFJZCk7XG4gICAgICAgICAgICAgICAgaWYgKCFzY2hlbWEpIHJldHVybiBmYWlsZWQoXCJHZXRTY2hlbWEgcmV0dXJuZWQgbnVsbFwiKTtcbiAgICAgICAgICAgICAgICBpZiAoc2NoZW1hLmlkICE9PSBkb2MxLnNjaGVtYUlkKSByZXR1cm4gZmFpbGVkKGBTY2hlbWEgSUQgbWlzbWF0Y2g6ICR7c2NoZW1hLmlkfWApO1xuICAgICAgICAgICAgICAgIHJldHVybiBwYXNzZWQoKTtcbiAgICAgICAgICAgIH0pO1xuXG4gICAgICAgICAgICAvLyBHZXRTY2hlbWE6IG5vbi1leGlzdGVudFxuICAgICAgICAgICAgYXdhaXQgdGhpcy5ydW5UZXN0KFwiR2V0U2NoZW1hOiBub24tZXhpc3RlbnQgcmV0dXJucyBudWxsXCIsIGFzeW5jICgpID0+IHtcbiAgICAgICAgICAgICAgICBjb25zdCBzY2hlbWEgPSBhd2FpdCB0aGlzLmNsaWVudC5zY2hlbWEucmVhZEJ5SWQoXCJzY2hfbm9uZXhpc3RlbnQxMjM0NVwiKTtcbiAgICAgICAgICAgICAgICBpZiAoc2NoZW1hICE9PSBudWxsKSByZXR1cm4gZmFpbGVkKFwiRXhwZWN0ZWQgbnVsbCBmb3Igbm9uLWV4aXN0ZW50IHNjaGVtYVwiKTtcbiAgICAgICAgICAgICAgICByZXR1cm4gcGFzc2VkKCk7XG4gICAgICAgICAgICB9KTtcblxuICAgICAgICAgICAgLy8gR2V0U2NoZW1hRWxlbWVudHM6IHJldHVybnMgZWxlbWVudHNcbiAgICAgICAgICAgIGF3YWl0IHRoaXMucnVuVGVzdChcIkdldFNjaGVtYUVsZW1lbnRzOiByZXR1cm5zIGVsZW1lbnRzXCIsIGFzeW5jICgpID0+IHtcbiAgICAgICAgICAgICAgICBpZiAoIWRvYzEpIHJldHVybiBmYWlsZWQoXCJTZXR1cDogZG9jMSBpcyBudWxsXCIpO1xuICAgICAgICAgICAgICAgIGNvbnN0IGVsZW1lbnRzID0gYXdhaXQgdGhpcy5jbGllbnQuc2NoZW1hLmdldEVsZW1lbnRzKGRvYzEuc2NoZW1hSWQpO1xuICAgICAgICAgICAgICAgIGlmIChlbGVtZW50cy5vYmplY3RzLmxlbmd0aCA9PT0gMCkgcmV0dXJuIGZhaWxlZChcIk5vIGVsZW1lbnRzIHJldHVybmVkXCIpO1xuICAgICAgICAgICAgICAgIHJldHVybiBwYXNzZWQoKTtcbiAgICAgICAgICAgIH0pO1xuXG4gICAgICAgICAgICAvLyBHZXRTY2hlbWFFbGVtZW50czogY29ycmVjdCBrZXlzXG4gICAgICAgICAgICBhd2FpdCB0aGlzLnJ1blRlc3QoXCJHZXRTY2hlbWFFbGVtZW50czogY29ycmVjdCBrZXlzXCIsIGFzeW5jICgpID0+IHtcbiAgICAgICAgICAgICAgICBpZiAoIWRvYzEpIHJldHVybiBmYWlsZWQoXCJTZXR1cDogZG9jMSBpcyBudWxsXCIpO1xuICAgICAgICAgICAgICAgIGNvbnN0IGVsZW1lbnRzID0gYXdhaXQgdGhpcy5jbGllbnQuc2NoZW1hLmdldEVsZW1lbnRzKGRvYzEuc2NoZW1hSWQpO1xuICAgICAgICAgICAgICAgIGNvbnN0IGtleXMgPSBuZXcgU2V0KGVsZW1lbnRzLm9iamVjdHMubWFwKChlKSA9PiBlLmtleSkpO1xuICAgICAgICAgICAgICAgIGNvbnN0IGV4cGVjdGVkID0gW1wibmFtZVwiLCBcInZhbHVlXCIsIFwiYWN0aXZlXCJdO1xuICAgICAgICAgICAgICAgIGZvciAoY29uc3Qga2V5IG9mIGV4cGVjdGVkKSB7XG4gICAgICAgICAgICAgICAgICAgIGlmICgha2V5cy5oYXMoa2V5KSkgcmV0dXJuIGZhaWxlZChgTWlzc2luZyBleHBlY3RlZCBrZXk6ICR7a2V5fS4gRm91bmQ6ICR7QXJyYXkuZnJvbShrZXlzKX1gKTtcbiAgICAgICAgICAgICAgICB9XG4gICAgICAgICAgICAgICAgcmV0dXJuIHBhc3NlZCgpO1xuICAgICAgICAgICAgfSk7XG4gICAgICAgIH0gZmluYWxseSB7XG4gICAgICAgICAgICBhd2FpdCB0aGlzLmNsaWVudC5jb2xsZWN0aW9uLmRlbGV0ZShjb2xsZWN0aW9uLmlkKTtcbiAgICAgICAgfVxuICAgIH1cblxuICAgIC8vID09PT09PT09PT0gSU5ERVggQVBJIFRFU1RTID09PT09PT09PT1cblxuICAgIHByaXZhdGUgYXN5bmMgdGVzdEluZGV4QXBpKCk6IFByb21pc2U8dm9pZD4ge1xuICAgICAgICAvLyBHZXRJbmRleFRhYmxlTWFwcGluZ3M6IHJldHVybnMgbWFwcGluZ3NcbiAgICAgICAgYXdhaXQgdGhpcy5ydW5UZXN0KFwiR2V0SW5kZXhUYWJsZU1hcHBpbmdzOiByZXR1cm5zIG1hcHBpbmdzXCIsIGFzeW5jICgpID0+IHtcbiAgICAgICAgICAgIGNvbnN0IG1hcHBpbmdzID0gYXdhaXQgdGhpcy5jbGllbnQuaW5kZXguZ2V0TWFwcGluZ3MoKTtcbiAgICAgICAgICAgIGlmICghQXJyYXkuaXNBcnJheShtYXBwaW5ncy5vYmplY3RzKSkgcmV0dXJuIGZhaWxlZChcIkdldE1hcHBpbmdzIGRpZCBub3QgcmV0dXJuIG9iamVjdHMgYXJyYXlcIik7XG4gICAgICAgICAgICByZXR1cm4gcGFzc2VkKCk7XG4gICAgICAgIH0pO1xuICAgIH1cblxuICAgIC8vID09PT09PT09PT0gQ09OU1RSQUlOVFMgQVBJIFRFU1RTID09PT09PT09PT1cblxuICAgIHByaXZhdGUgYXN5bmMgdGVzdENvbnN0cmFpbnRzQXBpKCk6IFByb21pc2U8dm9pZD4ge1xuICAgICAgICAvLyBDcmVhdGUgY29sbGVjdGlvbiB3aXRoIHN0cmljdCBtb2RlXG4gICAgICAgIGF3YWl0IHRoaXMucnVuVGVzdChcIkNvbnN0cmFpbnRzOiBjcmVhdGUgY29sbGVjdGlvbiB3aXRoIHN0cmljdCBtb2RlXCIsIGFzeW5jICgpID0+IHtcbiAgICAgICAgICAgIGNvbnN0IGNvbnN0cmFpbnQ6IEZpZWxkQ29uc3RyYWludCA9IHtcbiAgICAgICAgICAgICAgICBmaWVsZFBhdGg6IFwibmFtZVwiLFxuICAgICAgICAgICAgICAgIGRhdGFUeXBlOiBcInN0cmluZ1wiLFxuICAgICAgICAgICAgICAgIHJlcXVpcmVkOiB0cnVlXG4gICAgICAgICAgICB9O1xuICAgICAgICAgICAgY29uc3QgY29sbGVjdGlvbiA9IGF3YWl0IHRoaXMuY2xpZW50LmNvbGxlY3Rpb24uY3JlYXRlKHtcbiAgICAgICAgICAgICAgICBuYW1lOiBcImNvbnN0cmFpbnRzX3Rlc3RcIixcbiAgICAgICAgICAgICAgICBzY2hlbWFFbmZvcmNlbWVudE1vZGU6IFNjaGVtYUVuZm9yY2VtZW50TW9kZS5TdHJpY3QsXG4gICAgICAgICAgICAgICAgZmllbGRDb25zdHJhaW50czogW2NvbnN0cmFpbnRdXG4gICAgICAgICAgICB9KTtcbiAgICAgICAgICAgIGlmICghY29sbGVjdGlvbikgcmV0dXJuIGZhaWxlZChcIkNvbGxlY3Rpb24gY3JlYXRpb24gZmFpbGVkXCIpO1xuICAgICAgICAgICAgYXdhaXQgdGhpcy5jbGllbnQuY29sbGVjdGlvbi5kZWxldGUoY29sbGVjdGlvbi5pZCk7XG4gICAgICAgICAgICByZXR1cm4gcGFzc2VkKCk7XG4gICAgICAgIH0pO1xuXG4gICAgICAgIC8vIFVwZGF0ZSBjb25zdHJhaW50c1xuICAgICAgICBhd2FpdCB0aGlzLnJ1blRlc3QoXCJDb25zdHJhaW50czogdXBkYXRlIGNvbnN0cmFpbnRzIG9uIGNvbGxlY3Rpb25cIiwgYXN5bmMgKCkgPT4ge1xuICAgICAgICAgICAgY29uc3QgY29sbGVjdGlvbiA9IGF3YWl0IHRoaXMuY2xpZW50LmNvbGxlY3Rpb24uY3JlYXRlKHsgbmFtZTogXCJjb25zdHJhaW50c191cGRhdGVfdGVzdFwiIH0pO1xuICAgICAgICAgICAgaWYgKCFjb2xsZWN0aW9uKSByZXR1cm4gZmFpbGVkKFwiQ29sbGVjdGlvbiBjcmVhdGlvbiBmYWlsZWRcIik7XG5cbiAgICAgICAgICAgIGNvbnN0IGNvbnN0cmFpbnQ6IEZpZWxkQ29uc3RyYWludCA9IHtcbiAgICAgICAgICAgICAgICBmaWVsZFBhdGg6IFwiZW1haWxcIixcbiAgICAgICAgICAgICAgICBkYXRhVHlwZTogXCJzdHJpbmdcIixcbiAgICAgICAgICAgICAgICByZXF1aXJlZDogdHJ1ZSxcbiAgICAgICAgICAgICAgICByZWdleFBhdHRlcm46IFwiXltcXFxcd1xcXFwuLV0rQFtcXFxcd1xcXFwuLV0rXFxcXC5cXFxcdyskXCJcbiAgICAgICAgICAgIH07XG4gICAgICAgICAgICBjb25zdCBzdWNjZXNzID0gYXdhaXQgdGhpcy5jbGllbnQuY29sbGVjdGlvbi51cGRhdGVDb25zdHJhaW50cyhcbiAgICAgICAgICAgICAgICBjb2xsZWN0aW9uLmlkLFxuICAgICAgICAgICAgICAgIFNjaGVtYUVuZm9yY2VtZW50TW9kZS5TdHJpY3QsXG4gICAgICAgICAgICAgICAgW2NvbnN0cmFpbnRdXG4gICAgICAgICAgICApO1xuICAgICAgICAgICAgYXdhaXQgdGhpcy5jbGllbnQuY29sbGVjdGlvbi5kZWxldGUoY29sbGVjdGlvbi5pZCk7XG5cbiAgICAgICAgICAgIGlmICghc3VjY2VzcykgcmV0dXJuIGZhaWxlZChcIlVwZGF0ZSBjb25zdHJhaW50cyBmYWlsZWRcIik7XG4gICAgICAgICAgICByZXR1cm4gcGFzc2VkKCk7XG4gICAgICAgIH0pO1xuXG4gICAgICAgIC8vIEdldCBjb25zdHJhaW50c1xuICAgICAgICBhd2FpdCB0aGlzLnJ1blRlc3QoXCJDb25zdHJhaW50czogZ2V0IGNvbnN0cmFpbnRzIGZyb20gY29sbGVjdGlvblwiLCBhc3luYyAoKSA9PiB7XG4gICAgICAgICAgICBjb25zdCBjb25zdHJhaW50OiBGaWVsZENvbnN0cmFpbnQgPSB7XG4gICAgICAgICAgICAgICAgZmllbGRQYXRoOiBcInRlc3RfZmllbGRcIixcbiAgICAgICAgICAgICAgICBkYXRhVHlwZTogXCJzdHJpbmdcIixcbiAgICAgICAgICAgICAgICByZXF1aXJlZDogdHJ1ZVxuICAgICAgICAgICAgfTtcbiAgICAgICAgICAgIGNvbnN0IGNvbGxlY3Rpb24gPSBhd2FpdCB0aGlzLmNsaWVudC5jb2xsZWN0aW9uLmNyZWF0ZSh7XG4gICAgICAgICAgICAgICAgbmFtZTogXCJjb25zdHJhaW50c19nZXRfdGVzdFwiLFxuICAgICAgICAgICAgICAgIHNjaGVtYUVuZm9yY2VtZW50TW9kZTogU2NoZW1hRW5mb3JjZW1lbnRNb2RlLlN0cmljdCxcbiAgICAgICAgICAgICAgICBmaWVsZENvbnN0cmFpbnRzOiBbY29uc3RyYWludF1cbiAgICAgICAgICAgIH0pO1xuICAgICAgICAgICAgaWYgKCFjb2xsZWN0aW9uKSByZXR1cm4gZmFpbGVkKFwiQ29sbGVjdGlvbiBjcmVhdGlvbiBmYWlsZWRcIik7XG5cbiAgICAgICAgICAgIGNvbnN0IGNvbnN0cmFpbnRzID0gYXdhaXQgdGhpcy5jbGllbnQuY29sbGVjdGlvbi5nZXRDb25zdHJhaW50cyhjb2xsZWN0aW9uLmlkKTtcbiAgICAgICAgICAgIGF3YWl0IHRoaXMuY2xpZW50LmNvbGxlY3Rpb24uZGVsZXRlKGNvbGxlY3Rpb24uaWQpO1xuXG4gICAgICAgICAgICBpZiAoY29uc3RyYWludHMubGVuZ3RoID09PSAwKSByZXR1cm4gZmFpbGVkKFwiTm8gY29uc3RyYWludHMgcmV0dXJuZWRcIik7XG4gICAgICAgICAgICByZXR1cm4gcGFzc2VkKCk7XG4gICAgICAgIH0pO1xuICAgIH1cblxuICAgIC8vID09PT09PT09PT0gSU5ERVhJTkcgTU9ERSBBUEkgVEVTVFMgPT09PT09PT09PVxuXG4gICAgcHJpdmF0ZSBhc3luYyB0ZXN0SW5kZXhpbmdNb2RlQXBpKCk6IFByb21pc2U8dm9pZD4ge1xuICAgICAgICAvLyBTZWxlY3RpdmUgbW9kZVxuICAgICAgICBhd2FpdCB0aGlzLnJ1blRlc3QoXCJJbmRleGluZzogc2VsZWN0aXZlIG1vZGUgb25seSBpbmRleGVzIHNwZWNpZmllZCBmaWVsZHNcIiwgYXN5bmMgKCkgPT4ge1xuICAgICAgICAgICAgY29uc3QgY29sbGVjdGlvbiA9IGF3YWl0IHRoaXMuY2xpZW50LmNvbGxlY3Rpb24uY3JlYXRlKHtcbiAgICAgICAgICAgICAgICBuYW1lOiBcImluZGV4aW5nX3NlbGVjdGl2ZV90ZXN0XCIsXG4gICAgICAgICAgICAgICAgaW5kZXhpbmdNb2RlOiBJbmRleGluZ01vZGUuU2VsZWN0aXZlLFxuICAgICAgICAgICAgICAgIGluZGV4ZWRGaWVsZHM6IFtcIm5hbWVcIiwgXCJlbWFpbFwiXVxuICAgICAgICAgICAgfSk7XG4gICAgICAgICAgICBpZiAoIWNvbGxlY3Rpb24pIHJldHVybiBmYWlsZWQoXCJDb2xsZWN0aW9uIGNyZWF0aW9uIGZhaWxlZFwiKTtcbiAgICAgICAgICAgIGF3YWl0IHRoaXMuY2xpZW50LmNvbGxlY3Rpb24uZGVsZXRlKGNvbGxlY3Rpb24uaWQpO1xuICAgICAgICAgICAgcmV0dXJuIHBhc3NlZCgpO1xuICAgICAgICB9KTtcblxuICAgICAgICAvLyBOb25lIG1vZGVcbiAgICAgICAgYXdhaXQgdGhpcy5ydW5UZXN0KFwiSW5kZXhpbmc6IG5vbmUgbW9kZSBza2lwcyBpbmRleGluZ1wiLCBhc3luYyAoKSA9PiB7XG4gICAgICAgICAgICBjb25zdCBjb2xsZWN0aW9uID0gYXdhaXQgdGhpcy5jbGllbnQuY29sbGVjdGlvbi5jcmVhdGUoe1xuICAgICAgICAgICAgICAgIG5hbWU6IFwiaW5kZXhpbmdfbm9uZV90ZXN0XCIsXG4gICAgICAgICAgICAgICAgaW5kZXhpbmdNb2RlOiBJbmRleGluZ01vZGUuTm9uZVxuICAgICAgICAgICAgfSk7XG4gICAgICAgICAgICBpZiAoIWNvbGxlY3Rpb24pIHJldHVybiBmYWlsZWQoXCJDb2xsZWN0aW9uIGNyZWF0aW9uIGZhaWxlZFwiKTtcbiAgICAgICAgICAgIGF3YWl0IHRoaXMuY2xpZW50LmNvbGxlY3Rpb24uZGVsZXRlKGNvbGxlY3Rpb24uaWQpO1xuICAgICAgICAgICAgcmV0dXJuIHBhc3NlZCgpO1xuICAgICAgICB9KTtcblxuICAgICAgICAvLyBVcGRhdGUgaW5kZXhpbmcgbW9kZVxuICAgICAgICBhd2FpdCB0aGlzLnJ1blRlc3QoXCJJbmRleGluZzogdXBkYXRlIGluZGV4aW5nIG1vZGVcIiwgYXN5bmMgKCkgPT4ge1xuICAgICAgICAgICAgY29uc3QgY29sbGVjdGlvbiA9IGF3YWl0IHRoaXMuY2xpZW50LmNvbGxlY3Rpb24uY3JlYXRlKHsgbmFtZTogXCJpbmRleGluZ191cGRhdGVfdGVzdFwiIH0pO1xuICAgICAgICAgICAgaWYgKCFjb2xsZWN0aW9uKSByZXR1cm4gZmFpbGVkKFwiQ29sbGVjdGlvbiBjcmVhdGlvbiBmYWlsZWRcIik7XG5cbiAgICAgICAgICAgIGNvbnN0IHN1Y2Nlc3MgPSBhd2FpdCB0aGlzLmNsaWVudC5jb2xsZWN0aW9uLnVwZGF0ZUluZGV4aW5nKFxuICAgICAgICAgICAgICAgIGNvbGxlY3Rpb24uaWQsXG4gICAgICAgICAgICAgICAgSW5kZXhpbmdNb2RlLlNlbGVjdGl2ZSxcbiAgICAgICAgICAgICAgICBbXCJuYW1lXCJdLFxuICAgICAgICAgICAgICAgIGZhbHNlXG4gICAgICAgICAgICApO1xuICAgICAgICAgICAgYXdhaXQgdGhpcy5jbGllbnQuY29sbGVjdGlvbi5kZWxldGUoY29sbGVjdGlvbi5pZCk7XG5cbiAgICAgICAgICAgIGlmICghc3VjY2VzcykgcmV0dXJuIGZhaWxlZChcIlVwZGF0ZSBpbmRleGluZyBmYWlsZWRcIik7XG4gICAgICAgICAgICByZXR1cm4gcGFzc2VkKCk7XG4gICAgICAgIH0pO1xuXG4gICAgICAgIC8vIFJlYnVpbGQgaW5kZXhlc1xuICAgICAgICBhd2FpdCB0aGlzLnJ1blRlc3QoXCJJbmRleGluZzogcmVidWlsZCBpbmRleGVzXCIsIGFzeW5jICgpID0+IHtcbiAgICAgICAgICAgIGNvbnN0IGNvbGxlY3Rpb24gPSBhd2FpdCB0aGlzLmNsaWVudC5jb2xsZWN0aW9uLmNyZWF0ZSh7IG5hbWU6IFwiaW5kZXhpbmdfcmVidWlsZF90ZXN0XCIgfSk7XG4gICAgICAgICAgICBpZiAoIWNvbGxlY3Rpb24pIHJldHVybiBmYWlsZWQoXCJDb2xsZWN0aW9uIGNyZWF0aW9uIGZhaWxlZFwiKTtcblxuICAgICAgICAgICAgLy8gSW5nZXN0IGEgZG9jdW1lbnQgZmlyc3RcbiAgICAgICAgICAgIGF3YWl0IHRoaXMuY2xpZW50LmRvY3VtZW50LmluZ2VzdCh7XG4gICAgICAgICAgICAgICAgY29sbGVjdGlvbklkOiBjb2xsZWN0aW9uLmlkLFxuICAgICAgICAgICAgICAgIGNvbnRlbnQ6IHsgbmFtZTogXCJ0ZXN0XCIsIHZhbHVlOiA0MiB9XG4gICAgICAgICAgICB9KTtcblxuICAgICAgICAgICAgY29uc3QgcmVzdWx0ID0gYXdhaXQgdGhpcy5jbGllbnQuY29sbGVjdGlvbi5yZWJ1aWxkSW5kZXhlcyhjb2xsZWN0aW9uLmlkKTtcbiAgICAgICAgICAgIGF3YWl0IHRoaXMuY2xpZW50LmNvbGxlY3Rpb24uZGVsZXRlKGNvbGxlY3Rpb24uaWQpO1xuXG4gICAgICAgICAgICBpZiAoIXJlc3VsdCkgcmV0dXJuIGZhaWxlZChcIlJlYnVpbGQgaW5kZXhlcyByZXR1cm5lZCBudWxsXCIpO1xuICAgICAgICAgICAgcmV0dXJuIHBhc3NlZCgpO1xuICAgICAgICB9KTtcbiAgICB9XG5cbiAgICAvLyA9PT09PT09PT09IEVER0UgQ0FTRSBURVNUUyA9PT09PT09PT09XG5cbiAgICBwcml2YXRlIGFzeW5jIHRlc3RFZGdlQ2FzZXMoKTogUHJvbWlzZTx2b2lkPiB7XG4gICAgICAgIGNvbnN0IGNvbGxlY3Rpb24gPSBhd2FpdCB0aGlzLmNsaWVudC5jb2xsZWN0aW9uLmNyZWF0ZSh7IG5hbWU6IFwiZWRnZV9jYXNlX2NvbGxlY3Rpb25cIiB9KTtcbiAgICAgICAgaWYgKCFjb2xsZWN0aW9uKSB7XG4gICAgICAgICAgICBhd2FpdCB0aGlzLnJ1blRlc3QoXCJTZXR1cDogQ3JlYXRlIGNvbGxlY3Rpb25cIiwgYXN5bmMgKCkgPT4gZmFpbGVkKFwiQ29sbGVjdGlvbiBjcmVhdGlvbiBmYWlsZWRcIikpO1xuICAgICAgICAgICAgcmV0dXJuO1xuICAgICAgICB9XG5cbiAgICAgICAgdHJ5IHtcbiAgICAgICAgICAgIC8vIEVtcHR5IHN0cmluZyB2YWx1ZXNcbiAgICAgICAgICAgIGF3YWl0IHRoaXMucnVuVGVzdChcIkVkZ2U6IGVtcHR5IHN0cmluZyB2YWx1ZXMgaW4gSlNPTlwiLCBhc3luYyAoKSA9PiB7XG4gICAgICAgICAgICAgICAgY29uc3QgZG9jID0gYXdhaXQgdGhpcy5jbGllbnQuZG9jdW1lbnQuaW5nZXN0KHtcbiAgICAgICAgICAgICAgICAgICAgY29sbGVjdGlvbklkOiBjb2xsZWN0aW9uLmlkLFxuICAgICAgICAgICAgICAgICAgICBjb250ZW50OiB7IG5hbWU6IFwiXCIsIGRlc2NyaXB0aW9uOiBcIlwiIH1cbiAgICAgICAgICAgICAgICB9KTtcbiAgICAgICAgICAgICAgICBpZiAoIWRvYykgcmV0dXJuIGZhaWxlZChcIkluZ2VzdCBmYWlsZWRcIik7XG4gICAgICAgICAgICAgICAgcmV0dXJuIHBhc3NlZCgpO1xuICAgICAgICAgICAgfSk7XG5cbiAgICAgICAgICAgIC8vIFNwZWNpYWwgY2hhcmFjdGVyc1xuICAgICAgICAgICAgYXdhaXQgdGhpcy5ydW5UZXN0KFwiRWRnZTogc3BlY2lhbCBjaGFyYWN0ZXJzIGluIHZhbHVlc1wiLCBhc3luYyAoKSA9PiB7XG4gICAgICAgICAgICAgICAgY29uc3QgZG9jID0gYXdhaXQgdGhpcy5jbGllbnQuZG9jdW1lbnQuaW5nZXN0KHtcbiAgICAgICAgICAgICAgICAgICAgY29sbGVjdGlvbklkOiBjb2xsZWN0aW9uLmlkLFxuICAgICAgICAgICAgICAgICAgICBjb250ZW50OiB7IHRleHQ6ICdIZWxsbyEgQCMkJV4mKigpXystPXt9W118XFxcXDpcIjs8Pj8sLi8nIH1cbiAgICAgICAgICAgICAgICB9KTtcbiAgICAgICAgICAgICAgICBpZiAoIWRvYykgcmV0dXJuIGZhaWxlZChcIkluZ2VzdCBmYWlsZWRcIik7XG4gICAgICAgICAgICAgICAgcmV0dXJuIHBhc3NlZCgpO1xuICAgICAgICAgICAgfSk7XG5cbiAgICAgICAgICAgIC8vIERlZXBseSBuZXN0ZWQgSlNPTlxuICAgICAgICAgICAgYXdhaXQgdGhpcy5ydW5UZXN0KFwiRWRnZTogZGVlcGx5IG5lc3RlZCBKU09OICg1IGxldmVscylcIiwgYXN5bmMgKCkgPT4ge1xuICAgICAgICAgICAgICAgIGNvbnN0IGRvYyA9IGF3YWl0IHRoaXMuY2xpZW50LmRvY3VtZW50LmluZ2VzdCh7XG4gICAgICAgICAgICAgICAgICAgIGNvbGxlY3Rpb25JZDogY29sbGVjdGlvbi5pZCxcbiAgICAgICAgICAgICAgICAgICAgY29udGVudDoge1xuICAgICAgICAgICAgICAgICAgICAgICAgbGV2ZWwxOiB7XG4gICAgICAgICAgICAgICAgICAgICAgICAgICAgbGV2ZWwyOiB7XG4gICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgIGxldmVsMzoge1xuICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgbGV2ZWw0OiB7XG4gICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgbGV2ZWw1OiBcImRlZXAgdmFsdWVcIlxuICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgfVxuICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICB9XG4gICAgICAgICAgICAgICAgICAgICAgICAgICAgfVxuICAgICAgICAgICAgICAgICAgICAgICAgfVxuICAgICAgICAgICAgICAgICAgICB9XG4gICAgICAgICAgICAgICAgfSk7XG4gICAgICAgICAgICAgICAgaWYgKCFkb2MpIHJldHVybiBmYWlsZWQoXCJJbmdlc3QgZmFpbGVkXCIpO1xuICAgICAgICAgICAgICAgIHJldHVybiBwYXNzZWQoKTtcbiAgICAgICAgICAgIH0pO1xuXG4gICAgICAgICAgICAvLyBMYXJnZSBhcnJheVxuICAgICAgICAgICAgYXdhaXQgdGhpcy5ydW5UZXN0KFwiRWRnZTogbGFyZ2UgYXJyYXkgaW4gSlNPTlwiLCBhc3luYyAoKSA9PiB7XG4gICAgICAgICAgICAgICAgY29uc3QgZG9jID0gYXdhaXQgdGhpcy5jbGllbnQuZG9jdW1lbnQuaW5nZXN0KHtcbiAgICAgICAgICAgICAgICAgICAgY29sbGVjdGlvbklkOiBjb2xsZWN0aW9uLmlkLFxuICAgICAgICAgICAgICAgICAgICBjb250ZW50OiB7IGl0ZW1zOiBBcnJheS5mcm9tKHsgbGVuZ3RoOiAxMDAgfSwgKF8sIGkpID0+IGkpIH1cbiAgICAgICAgICAgICAgICB9KTtcbiAgICAgICAgICAgICAgICBpZiAoIWRvYykgcmV0dXJuIGZhaWxlZChcIkluZ2VzdCBmYWlsZWRcIik7XG4gICAgICAgICAgICAgICAgcmV0dXJuIHBhc3NlZCgpO1xuICAgICAgICAgICAgfSk7XG5cbiAgICAgICAgICAgIC8vIE51bWVyaWMgdmFsdWVzXG4gICAgICAgICAgICBhd2FpdCB0aGlzLnJ1blRlc3QoXCJFZGdlOiBudW1lcmljIHZhbHVlc1wiLCBhc3luYyAoKSA9PiB7XG4gICAgICAgICAgICAgICAgY29uc3QgZG9jID0gYXdhaXQgdGhpcy5jbGllbnQuZG9jdW1lbnQuaW5nZXN0KHtcbiAgICAgICAgICAgICAgICAgICAgY29sbGVjdGlvbklkOiBjb2xsZWN0aW9uLmlkLFxuICAgICAgICAgICAgICAgICAgICBjb250ZW50OiB7XG4gICAgICAgICAgICAgICAgICAgICAgICBpbnRlZ2VyOiA0MixcbiAgICAgICAgICAgICAgICAgICAgICAgIGZsb2F0OiAzLjE0MTU5LFxuICAgICAgICAgICAgICAgICAgICAgICAgbmVnYXRpdmU6IC0xMDAsXG4gICAgICAgICAgICAgICAgICAgICAgICB6ZXJvOiAwLFxuICAgICAgICAgICAgICAgICAgICAgICAgbGFyZ2U6IDk5OTk5OTk5OTlcbiAgICAgICAgICAgICAgICAgICAgfVxuICAgICAgICAgICAgICAgIH0pO1xuICAgICAgICAgICAgICAgIGlmICghZG9jKSByZXR1cm4gZmFpbGVkKFwiSW5nZXN0IGZhaWxlZFwiKTtcbiAgICAgICAgICAgICAgICByZXR1cm4gcGFzc2VkKCk7XG4gICAgICAgICAgICB9KTtcblxuICAgICAgICAgICAgLy8gQm9vbGVhbiB2YWx1ZXNcbiAgICAgICAgICAgIGF3YWl0IHRoaXMucnVuVGVzdChcIkVkZ2U6IGJvb2xlYW4gdmFsdWVzXCIsIGFzeW5jICgpID0+IHtcbiAgICAgICAgICAgICAgICBjb25zdCBkb2MgPSBhd2FpdCB0aGlzLmNsaWVudC5kb2N1bWVudC5pbmdlc3Qoe1xuICAgICAgICAgICAgICAgICAgICBjb2xsZWN0aW9uSWQ6IGNvbGxlY3Rpb24uaWQsXG4gICAgICAgICAgICAgICAgICAgIGNvbnRlbnQ6IHsgYWN0aXZlOiB0cnVlLCBkaXNhYmxlZDogZmFsc2UgfVxuICAgICAgICAgICAgICAgIH0pO1xuICAgICAgICAgICAgICAgIGlmICghZG9jKSByZXR1cm4gZmFpbGVkKFwiSW5nZXN0IGZhaWxlZFwiKTtcbiAgICAgICAgICAgICAgICByZXR1cm4gcGFzc2VkKCk7XG4gICAgICAgICAgICB9KTtcblxuICAgICAgICAgICAgLy8gTnVsbCB2YWx1ZXNcbiAgICAgICAgICAgIGF3YWl0IHRoaXMucnVuVGVzdChcIkVkZ2U6IG51bGwgdmFsdWVzIGluIEpTT05cIiwgYXN5bmMgKCkgPT4ge1xuICAgICAgICAgICAgICAgIGNvbnN0IGRvYyA9IGF3YWl0IHRoaXMuY2xpZW50LmRvY3VtZW50LmluZ2VzdCh7XG4gICAgICAgICAgICAgICAgICAgIGNvbGxlY3Rpb25JZDogY29sbGVjdGlvbi5pZCxcbiAgICAgICAgICAgICAgICAgICAgY29udGVudDogeyBuYW1lOiBcIlRlc3RcIiwgb3B0aW9uYWxfZmllbGQ6IG51bGwgfVxuICAgICAgICAgICAgICAgIH0pO1xuICAgICAgICAgICAgICAgIGlmICghZG9jKSByZXR1cm4gZmFpbGVkKFwiSW5nZXN0IGZhaWxlZFwiKTtcbiAgICAgICAgICAgICAgICByZXR1cm4gcGFzc2VkKCk7XG4gICAgICAgICAgICB9KTtcblxuICAgICAgICAgICAgLy8gVW5pY29kZSBjaGFyYWN0ZXJzXG4gICAgICAgICAgICBhd2FpdCB0aGlzLnJ1blRlc3QoXCJFZGdlOiB1bmljb2RlIGNoYXJhY3RlcnNcIiwgYXN5bmMgKCkgPT4ge1xuICAgICAgICAgICAgICAgIGNvbnN0IGRvYyA9IGF3YWl0IHRoaXMuY2xpZW50LmRvY3VtZW50LmluZ2VzdCh7XG4gICAgICAgICAgICAgICAgICAgIGNvbGxlY3Rpb25JZDogY29sbGVjdGlvbi5pZCxcbiAgICAgICAgICAgICAgICAgICAgY29udGVudDogeyBncmVldGluZzogXCJIZWxsbywgd29ybGQhXCIsIGphcGFuZXNlOiBcIkhlbGxvLCB3b3JsZCFcIiwgZW1vamk6IFwiSGVsbG8sIHdvcmxkIVwiIH1cbiAgICAgICAgICAgICAgICB9KTtcbiAgICAgICAgICAgICAgICBpZiAoIWRvYykgcmV0dXJuIGZhaWxlZChcIkluZ2VzdCBmYWlsZWRcIik7XG4gICAgICAgICAgICAgICAgcmV0dXJuIHBhc3NlZCgpO1xuICAgICAgICAgICAgfSk7XG4gICAgICAgIH0gZmluYWxseSB7XG4gICAgICAgICAgICBhd2FpdCB0aGlzLmNsaWVudC5jb2xsZWN0aW9uLmRlbGV0ZShjb2xsZWN0aW9uLmlkKTtcbiAgICAgICAgfVxuICAgIH1cblxuICAgIC8vID09PT09PT09PT0gUEVSRk9STUFOQ0UgVEVTVFMgPT09PT09PT09PVxuXG4gICAgcHJpdmF0ZSBhc3luYyB0ZXN0UGVyZm9ybWFuY2UoKTogUHJvbWlzZTx2b2lkPiB7XG4gICAgICAgIGNvbnN0IGNvbGxlY3Rpb24gPSBhd2FpdCB0aGlzLmNsaWVudC5jb2xsZWN0aW9uLmNyZWF0ZSh7IG5hbWU6IFwicGVyZl90ZXN0X2NvbGxlY3Rpb25cIiB9KTtcbiAgICAgICAgaWYgKCFjb2xsZWN0aW9uKSB7XG4gICAgICAgICAgICBhd2FpdCB0aGlzLnJ1blRlc3QoXCJTZXR1cDogQ3JlYXRlIGNvbGxlY3Rpb25cIiwgYXN5bmMgKCkgPT4gZmFpbGVkKFwiQ29sbGVjdGlvbiBjcmVhdGlvbiBmYWlsZWRcIikpO1xuICAgICAgICAgICAgcmV0dXJuO1xuICAgICAgICB9XG5cbiAgICAgICAgdHJ5IHtcbiAgICAgICAgICAgIC8vIEluZ2VzdCAxMDAgZG9jdW1lbnRzXG4gICAgICAgICAgICBhd2FpdCB0aGlzLnJ1blRlc3QoXCJQZXJmOiBpbmdlc3QgMTAwIGRvY3VtZW50c1wiLCBhc3luYyAoKSA9PiB7XG4gICAgICAgICAgICAgICAgY29uc3Qgc3RhcnQgPSBEYXRlLm5vdygpO1xuICAgICAgICAgICAgICAgIGZvciAobGV0IGkgPSAwOyBpIDwgMTAwOyBpKyspIHtcbiAgICAgICAgICAgICAgICAgICAgY29uc3QgZG9jID0gYXdhaXQgdGhpcy5jbGllbnQuZG9jdW1lbnQuaW5nZXN0KHtcbiAgICAgICAgICAgICAgICAgICAgICAgIGNvbGxlY3Rpb25JZDogY29sbGVjdGlvbi5pZCxcbiAgICAgICAgICAgICAgICAgICAgICAgIGNvbnRlbnQ6IHtcbiAgICAgICAgICAgICAgICAgICAgICAgICAgICBOYW1lOiBgUGVyZkl0ZW1fJHtpfWAsXG4gICAgICAgICAgICAgICAgICAgICAgICAgICAgQ2F0ZWdvcnk6IGBDYXRlZ29yeV8ke2kgJSAxMH1gLFxuICAgICAgICAgICAgICAgICAgICAgICAgICAgIFZhbHVlOiBpICogMTBcbiAgICAgICAgICAgICAgICAgICAgICAgIH0sXG4gICAgICAgICAgICAgICAgICAgICAgICBuYW1lOiBgcGVyZl9kb2NfJHtpfWBcbiAgICAgICAgICAgICAgICAgICAgfSk7XG4gICAgICAgICAgICAgICAgICAgIGlmICghZG9jKSByZXR1cm4gZmFpbGVkKGBGYWlsZWQgdG8gaW5nZXN0IGRvY3VtZW50ICR7aX1gKTtcbiAgICAgICAgICAgICAgICB9XG4gICAgICAgICAgICAgICAgY29uc3QgZWxhcHNlZCA9IERhdGUubm93KCkgLSBzdGFydDtcbiAgICAgICAgICAgICAgICBjb25zdCByYXRlID0gKDEwMCAvIGVsYXBzZWQpICogMTAwMDtcbiAgICAgICAgICAgICAgICBwcm9jZXNzLnN0ZG91dC53cml0ZShgKCR7cmF0ZS50b0ZpeGVkKDEpfSBkb2NzL3NlYykgYCk7XG4gICAgICAgICAgICAgICAgcmV0dXJuIHBhc3NlZCgpO1xuICAgICAgICAgICAgfSk7XG5cbiAgICAgICAgICAgIC8vIFNlYXJjaCBpbiAxMDAgZG9jdW1lbnRzXG4gICAgICAgICAgICBhd2FpdCB0aGlzLnJ1blRlc3QoXCJQZXJmOiBzZWFyY2ggaW4gMTAwIGRvY3VtZW50c1wiLCBhc3luYyAoKSA9PiB7XG4gICAgICAgICAgICAgICAgY29uc3Qgc3RhcnQgPSBEYXRlLm5vdygpO1xuICAgICAgICAgICAgICAgIGNvbnN0IHJlc3VsdCA9IGF3YWl0IHRoaXMuY2xpZW50LnNlYXJjaC5zZWFyY2goe1xuICAgICAgICAgICAgICAgICAgICBjb2xsZWN0aW9uSWQ6IGNvbGxlY3Rpb24uaWQsXG4gICAgICAgICAgICAgICAgICAgIGZpbHRlcnM6IFt7IGZpZWxkOiBcIkNhdGVnb3J5XCIsIGNvbmRpdGlvbjogU2VhcmNoQ29uZGl0aW9uLkVxdWFscywgdmFsdWU6IFwiQ2F0ZWdvcnlfNVwiIH1dLFxuICAgICAgICAgICAgICAgICAgICBtYXhSZXN1bHRzOiAxMDBcbiAgICAgICAgICAgICAgICB9KTtcbiAgICAgICAgICAgICAgICBjb25zdCBlbGFwc2VkID0gRGF0ZS5ub3coKSAtIHN0YXJ0O1xuICAgICAgICAgICAgICAgIGlmICghcmVzdWx0KSByZXR1cm4gZmFpbGVkKFwiU2VhcmNoIHJldHVybmVkIG51bGxcIik7XG4gICAgICAgICAgICAgICAgcHJvY2Vzcy5zdGRvdXQud3JpdGUoYCgke2VsYXBzZWR9bXMpIGApO1xuICAgICAgICAgICAgICAgIHJldHVybiBwYXNzZWQoKTtcbiAgICAgICAgICAgIH0pO1xuXG4gICAgICAgICAgICAvLyBHZXREb2N1bWVudHMgZm9yIDEwMCBkb2N1bWVudHNcbiAgICAgICAgICAgIGF3YWl0IHRoaXMucnVuVGVzdChcIlBlcmY6IEdldERvY3VtZW50cyBmb3IgMTAwIGRvY3VtZW50c1wiLCBhc3luYyAoKSA9PiB7XG4gICAgICAgICAgICAgICAgY29uc3Qgc3RhcnQgPSBEYXRlLm5vdygpO1xuICAgICAgICAgICAgICAgIGNvbnN0IHJlc3VsdCA9IGF3YWl0IHRoaXMuY2xpZW50LmRvY3VtZW50LnJlYWRBbGxJbkNvbGxlY3Rpb24oY29sbGVjdGlvbi5pZCk7XG4gICAgICAgICAgICAgICAgY29uc3QgZWxhcHNlZCA9IERhdGUubm93KCkgLSBzdGFydDtcbiAgICAgICAgICAgICAgICBpZiAocmVzdWx0LnRvdGFsUmVjb3JkcyAhPT0gMTAwKSByZXR1cm4gZmFpbGVkKGBFeHBlY3RlZCAxMDAgZG9jcywgZ290ICR7cmVzdWx0LnRvdGFsUmVjb3Jkc31gKTtcbiAgICAgICAgICAgICAgICBwcm9jZXNzLnN0ZG91dC53cml0ZShgKCR7ZWxhcHNlZH1tcykgYCk7XG4gICAgICAgICAgICAgICAgcmV0dXJuIHBhc3NlZCgpO1xuICAgICAgICAgICAgfSk7XG5cbiAgICAgICAgICAgIC8vIEVudW1lcmF0ZSAxMDAgZG9jdW1lbnRzXG4gICAgICAgICAgICBhd2FpdCB0aGlzLnJ1blRlc3QoXCJQZXJmOiBlbnVtZXJhdGUgMTAwIGRvY3VtZW50c1wiLCBhc3luYyAoKSA9PiB7XG4gICAgICAgICAgICAgICAgY29uc3Qgc3RhcnQgPSBEYXRlLm5vdygpO1xuICAgICAgICAgICAgICAgIGNvbnN0IHJlc3VsdCA9IGF3YWl0IHRoaXMuY2xpZW50LnNlYXJjaC5lbnVtZXJhdGUoe1xuICAgICAgICAgICAgICAgICAgICBjb2xsZWN0aW9uSWQ6IGNvbGxlY3Rpb24uaWQsXG4gICAgICAgICAgICAgICAgICAgIG1heFJlc3VsdHM6IDEwMFxuICAgICAgICAgICAgICAgIH0pO1xuICAgICAgICAgICAgICAgIGNvbnN0IGVsYXBzZWQgPSBEYXRlLm5vdygpIC0gc3RhcnQ7XG4gICAgICAgICAgICAgICAgaWYgKCFyZXN1bHQpIHJldHVybiBmYWlsZWQoXCJFbnVtZXJhdGUgcmV0dXJuZWQgbnVsbFwiKTtcbiAgICAgICAgICAgICAgICBpZiAocmVzdWx0LmRvY3VtZW50cy5sZW5ndGggIT09IDEwMCkgcmV0dXJuIGZhaWxlZChgRXhwZWN0ZWQgMTAwIGRvY3MsIGdvdCAke3Jlc3VsdC5kb2N1bWVudHMubGVuZ3RofWApO1xuICAgICAgICAgICAgICAgIHByb2Nlc3Muc3Rkb3V0LndyaXRlKGAoJHtlbGFwc2VkfW1zKSBgKTtcbiAgICAgICAgICAgICAgICByZXR1cm4gcGFzc2VkKCk7XG4gICAgICAgICAgICB9KTtcbiAgICAgICAgfSBmaW5hbGx5IHtcbiAgICAgICAgICAgIGF3YWl0IHRoaXMuY2xpZW50LmNvbGxlY3Rpb24uZGVsZXRlKGNvbGxlY3Rpb24uaWQpO1xuICAgICAgICB9XG4gICAgfVxufVxuXG4vLyBNYWluIGVudHJ5IHBvaW50XG5hc3luYyBmdW5jdGlvbiBtYWluKCk6IFByb21pc2U8dm9pZD4ge1xuICAgIGNvbnN0IGFyZ3MgPSBwcm9jZXNzLmFyZ3Yuc2xpY2UoMik7XG5cbiAgICBpZiAoYXJncy5sZW5ndGggPCAxKSB7XG4gICAgICAgIGNvbnNvbGUubG9nKFwiVXNhZ2U6IG5weCB0cy1ub2RlIHNyYy90ZXN0LWhhcm5lc3MudHMgPGVuZHBvaW50X3VybD5cIik7XG4gICAgICAgIGNvbnNvbGUubG9nKFwiRXhhbXBsZTogbnB4IHRzLW5vZGUgc3JjL3Rlc3QtaGFybmVzcy50cyBodHRwOi8vbG9jYWxob3N0OjgwMDBcIik7XG4gICAgICAgIHByb2Nlc3MuZXhpdCgxKTtcbiAgICB9XG5cbiAgICBjb25zdCBlbmRwb2ludCA9IGFyZ3NbMF07XG5cbiAgICBjb25zb2xlLmxvZyhgQ29ubmVjdGluZyB0byBMYXR0aWNlIHNlcnZlciBhdDogJHtlbmRwb2ludH1gKTtcbiAgICBjb25zb2xlLmxvZygpO1xuXG4gICAgY29uc3QgaGFybmVzcyA9IG5ldyBUZXN0SGFybmVzcyhlbmRwb2ludCk7XG4gICAgY29uc3Qgc3VjY2VzcyA9IGF3YWl0IGhhcm5lc3MucnVuQWxsVGVzdHMoKTtcblxuICAgIHByb2Nlc3MuZXhpdChzdWNjZXNzID8gMCA6IDEpO1xufVxuXG5tYWluKCkuY2F0Y2goKGVycm9yKSA9PiB7XG4gICAgY29uc29sZS5lcnJvcihcIkZhdGFsIGVycm9yOlwiLCBlcnJvcik7XG4gICAgcHJvY2Vzcy5leGl0KDEpO1xufSk7XG4iXX0=