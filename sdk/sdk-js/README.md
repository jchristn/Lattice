# Lattice SDK for JavaScript/TypeScript

A comprehensive REST SDK for consuming a Lattice server.

## Requirements

- Node.js 18.0.0 or higher
- TypeScript 5.0+ (for TypeScript usage)

## Installation

```bash
cd sdk/sdk-js
npm install
```

## Building

```bash
npm run build
```

This compiles TypeScript to JavaScript in the `dist/` directory.

## Quick Start

### TypeScript

```typescript
import { LatticeClient, SearchQuery, SearchCondition } from "./src";

async function main() {
    // Create a client
    const client = new LatticeClient("http://localhost:8000");

    // Check server health
    if (await client.healthCheck()) {
        console.log("Server is healthy");
    }

    // Create a collection
    const collection = await client.collection.create({
        name: "my_collection",
        description: "Test collection"
    });
    console.log(`Created collection: ${collection?.id}`);

    // Ingest a document
    const document = await client.document.ingest({
        collectionId: collection!.id,
        content: { name: "John Doe", age: 30, department: "Engineering" },
        name: "person_1",
        labels: ["person", "employee"],
        tags: { status: "active" }
    });
    console.log(`Created document: ${document?.id}`);

    // Search for documents
    const query: SearchQuery = {
        collectionId: collection!.id,
        filters: [{ field: "age", condition: SearchCondition.GreaterThan, value: "25" }],
        maxResults: 10
    };
    const result = await client.search.search(query);

    console.log(`Found ${result?.totalRecords} documents:`);
    for (const doc of result?.documents || []) {
        console.log(`  - ${doc.name} (${doc.id})`);
    }

    // Clean up
    await client.collection.delete(collection!.id);
}

main().catch(console.error);
```

### JavaScript (after building)

```javascript
const { LatticeClient, SearchCondition } = require("./dist");

async function main() {
    const client = new LatticeClient("http://localhost:8000");

    const collection = await client.collection.create({ name: "my_collection" });
    console.log(`Created collection: ${collection.id}`);

    // ... rest of your code
}

main().catch(console.error);
```

## Running the Test Harness

The test harness provides comprehensive testing of all SDK functionality against a running Lattice server.

### Using ts-node (Development)

```bash
npx ts-node src/test-harness.ts <endpoint_url>
```

### Example

```bash
npx ts-node src/test-harness.ts http://localhost:8000
```

### Using Compiled JavaScript

```bash
npm run build
node dist/test-harness.js http://localhost:8000
```

### Test Output

The test harness will:
- Run tests organized by API section (Collection, Document, Search, etc.)
- Display PASS/FAIL for each test with response times
- Show a summary at the end with total pass/fail counts
- List all failed tests with error messages

Example output:
```
===============================================================================
  Lattice SDK Test Harness - JavaScript/TypeScript
===============================================================================

  Endpoint: http://localhost:8000

--- HEALTH CHECK ---
  [PASS] Health check returns true (12ms)

--- COLLECTION API ---
  [PASS] CreateCollection: basic (38ms)
  [PASS] CreateCollection: with all parameters (45ms)
  ...

===============================================================================
  TEST SUMMARY
===============================================================================

  HEALTH CHECK: 1/1 [PASS]
  COLLECTION API: 9/9 [PASS]
  ...

-------------------------------------------------------------------------------
  TOTAL: 57 passed, 0 failed [PASS]
  RUNTIME: 3200ms (3.20s)
-------------------------------------------------------------------------------
```

## API Reference

### LatticeClient

The main client class. Initialize with the server endpoint:

```typescript
const client = new LatticeClient("http://localhost:8000", 30000);
```

**Parameters:**
- `baseUrl` (string): The Lattice server URL
- `timeout` (number): Request timeout in milliseconds (default: 30000)

### Collection Methods (`client.collection`)

| Method | Description |
|--------|-------------|
| `create(options)` | Create a new collection |
| `readAll()` | Get all collections |
| `readById(id)` | Get a collection by ID |
| `exists(id)` | Check if a collection exists |
| `delete(id)` | Delete a collection |
| `getConstraints(id)` | Get field constraints |
| `updateConstraints(id, mode, constraints)` | Update constraints |
| `getIndexedFields(id)` | Get indexed fields |
| `updateIndexing(id, mode, fields, rebuild)` | Update indexing configuration |
| `rebuildIndexes(id, dropUnused)` | Rebuild collection indexes |

### Document Methods (`client.document`)

| Method | Description |
|--------|-------------|
| `ingest(options)` | Ingest a document |
| `readAllInCollection(collectionId, ...)` | Get all documents in a collection |
| `readById(id, ...)` | Get a document by ID |
| `exists(id)` | Check if a document exists |
| `delete(id)` | Delete a document |

### Search Methods (`client.search`)

| Method | Description |
|--------|-------------|
| `search(query)` | Execute a structured search |
| `searchBySql(collectionId, expression)` | Execute a SQL-like search |
| `enumerate(query)` | Enumerate documents |

### Schema Methods (`client.schema`)

| Method | Description |
|--------|-------------|
| `readAll()` | Get all schemas |
| `readById(id)` | Get a schema by ID |
| `getElements(schemaId)` | Get schema elements |

### Index Methods (`client.index`)

| Method | Description |
|--------|-------------|
| `getMappings()` | Get index table mappings |

## Types

### Enums

All enums are serialized as lowercase strings in JSON.

```typescript
// Schema enforcement modes
enum SchemaEnforcementMode {
    None = "none",       // No validation
    Strict = "strict",   // All constraints must pass
    Flexible = "flexible", // Warns but accepts documents
    Partial = "partial"  // Only validates constrained fields
}

// Indexing modes
enum IndexingMode {
    All = "all",         // Index all fields
    Selective = "selective", // Only index specified fields
    None = "none"        // No indexing
}

// Search condition operators
enum SearchCondition {
    Equals = "Equals",
    NotEquals = "NotEquals",
    GreaterThan = "GreaterThan",
    GreaterThanOrEqualTo = "GreaterThanOrEqualTo",
    LessThan = "LessThan",
    LessThanOrEqualTo = "LessThanOrEqualTo",
    IsNull = "IsNull",
    IsNotNull = "IsNotNull",
    Contains = "Contains",
    StartsWith = "StartsWith",
    EndsWith = "EndsWith",
    Like = "Like"
}

// Enumeration ordering options
enum EnumerationOrder {
    CreatedAscending = "CreatedAscending",
    CreatedDescending = "CreatedDescending",
    LastUpdateAscending = "LastUpdateAscending",
    LastUpdateDescending = "LastUpdateDescending",
    NameAscending = "NameAscending",
    NameDescending = "NameDescending"
}
```

### Interfaces

- `Collection`, `Document`, `Schema`, `SchemaElement`
- `FieldConstraint`, `IndexedField`
- `SearchFilter`, `SearchQuery`, `SearchResult`
- `IndexRebuildResult`, `ResponseContext`, `IndexTableMapping`
- `CreateCollectionOptions`, `IngestDocumentOptions`

## Exception Handling

```typescript
import { LatticeError, LatticeConnectionError, LatticeApiError } from "./src";

try {
    const result = await client.search.search(query);
} catch (error) {
    if (error instanceof LatticeConnectionError) {
        console.error(`Connection failed: ${error.message}`);
    } else if (error instanceof LatticeApiError) {
        console.error(`API error (${error.statusCode}): ${error.errorMessage}`);
    } else if (error instanceof LatticeError) {
        console.error(`Error: ${error.message}`);
    }
}
```

## Project Structure

```
sdk-js/
├── src/
│   ├── index.ts           # Main exports
│   ├── client.ts          # LatticeClient implementation
│   ├── models.ts          # Data models and type definitions
│   ├── exceptions.ts      # Custom exceptions
│   └── test-harness.ts    # Comprehensive test suite
├── dist/                  # Compiled JavaScript (after build)
├── package.json
├── tsconfig.json
└── README.md
```
