# Lattice SDK for C#

A comprehensive REST SDK for consuming a Lattice server.

## Requirements

- .NET 8.0 or higher

## Installation

```bash
cd sdk/sdk-csharp
dotnet restore
```

## Building

```bash
dotnet build
```

## Quick Start

```csharp
using Lattice.Sdk;
using Lattice.Sdk.Models;

// Create a client
using LatticeClient client = new LatticeClient("http://localhost:8000");

// Check server health
if (await client.HealthCheckAsync())
{
    Console.WriteLine("Server is healthy");
}

// Create a collection
Collection? collection = await client.Collection.CreateAsync(
    name: "my_collection",
    description: "Test collection"
);
Console.WriteLine($"Created collection: {collection?.Id}");

// Ingest a document
Document? document = await client.Document.IngestAsync(
    collectionId: collection!.Id,
    content: new { name = "John Doe", age = 30, department = "Engineering" },
    name: "person_1",
    labels: new List<string> { "person", "employee" },
    tags: new Dictionary<string, string> { ["status"] = "active" }
);
Console.WriteLine($"Created document: {document?.Id}");

// Search for documents
SearchResult? result = await client.Search.SearchAsync(new SearchQuery
{
    CollectionId = collection.Id,
    Filters = new List<SearchFilter>
    {
        new SearchFilter("age", SearchCondition.GreaterThan, "25")
    },
    MaxResults = 10
});

Console.WriteLine($"Found {result?.TotalRecords} documents:");
foreach (Document doc in result?.Documents ?? new List<Document>())
{
    Console.WriteLine($"  - {doc.Name} ({doc.Id})");
}

// Clean up
await client.Collection.DeleteAsync(collection.Id);
```

## Running the Test Harness

The test harness provides comprehensive testing of all SDK functionality against a running Lattice server.

### Usage

```bash
cd Lattice.Sdk.Tests
dotnet run -- <endpoint_url>
```

### Example

```bash
dotnet run -- http://localhost:8000
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
  Lattice SDK Test Harness - C#
===============================================================================

  Endpoint: http://localhost:8000

--- HEALTH CHECK ---
  [PASS] Health check returns true (18ms)

--- COLLECTION API ---
  [PASS] CreateCollection: basic (42ms)
  [PASS] CreateCollection: with all parameters (48ms)
  ...

===============================================================================
  TEST SUMMARY
===============================================================================

  HEALTH CHECK: 1/1 [PASS]
  COLLECTION API: 9/9 [PASS]
  ...

-------------------------------------------------------------------------------
  TOTAL: 57 passed, 0 failed [PASS]
  RUNTIME: 4200ms (4.20s)
-------------------------------------------------------------------------------
```

## API Reference

### LatticeClient

The main client class. Initialize with the server endpoint:

```csharp
using LatticeClient client = new LatticeClient("http://localhost:8000", TimeSpan.FromSeconds(30));
```

**Parameters:**
- `baseUrl` (string): The Lattice server URL
- `timeout` (TimeSpan?): Request timeout (default: 30 seconds)

The client implements `IDisposable`, so use `using` statements or call `Dispose()`.

### Collection Methods (`client.Collection`)

| Method | Description |
|--------|-------------|
| `CreateAsync(name, ...)` | Create a new collection |
| `ReadAllAsync()` | Get all collections |
| `ReadByIdAsync(id)` | Get a collection by ID |
| `ExistsAsync(id)` | Check if a collection exists |
| `DeleteAsync(id)` | Delete a collection |
| `GetConstraintsAsync(id)` | Get field constraints |
| `UpdateConstraintsAsync(id, mode, constraints)` | Update constraints |
| `GetIndexedFieldsAsync(id)` | Get indexed fields |
| `UpdateIndexingAsync(id, mode, fields, rebuild)` | Update indexing configuration |
| `RebuildIndexesAsync(id, dropUnused)` | Rebuild collection indexes |

### Document Methods (`client.Document`)

| Method | Description |
|--------|-------------|
| `IngestAsync(collectionId, content, ...)` | Ingest a document |
| `ReadAllInCollectionAsync(collectionId, ...)` | Get all documents in a collection |
| `ReadByIdAsync(collectionId, documentId, ...)` | Get a document by ID |
| `ExistsAsync(collectionId, documentId)` | Check if a document exists |
| `DeleteAsync(collectionId, documentId)` | Delete a document |

### Search Methods (`client.Search`)

| Method | Description |
|--------|-------------|
| `SearchAsync(query)` | Execute a structured search |
| `SearchBySqlAsync(collectionId, expression)` | Execute a SQL-like search |
| `EnumerateAsync(query)` | Enumerate documents |

### Schema Methods (`client.Schema`)

| Method | Description |
|--------|-------------|
| `ReadAllAsync()` | Get all schemas |
| `ReadByIdAsync(id)` | Get a schema by ID |
| `GetElementsAsync(schemaId)` | Get schema elements |

### Index Methods (`client.Index`)

| Method | Description |
|--------|-------------|
| `GetMappingsAsync()` | Get index table mappings |

## Models

### Enums

All enums are serialized as lowercase strings in JSON (e.g., `"none"`, `"strict"`, `"all"`).

```csharp
// Schema enforcement modes (serialized as: "none", "strict", "flexible", "partial")
public enum SchemaEnforcementMode
{
    None,    // No validation
    Strict,  // All constraints must pass
    Flexible, // Warns but accepts documents
    Partial  // Only validates constrained fields
}

// Indexing modes (serialized as: "all", "selective", "none")
public enum IndexingMode
{
    All,       // Index all fields
    Selective, // Only index specified fields
    None       // No indexing
}

// Search condition operators
public enum SearchCondition
{
    Equals,
    NotEquals,
    GreaterThan,
    GreaterThanOrEqualTo,
    LessThan,
    LessThanOrEqualTo,
    IsNull,
    IsNotNull,
    Contains,
    StartsWith,
    EndsWith,
    Like
}

// Enumeration ordering options
public enum EnumerationOrder
{
    CreatedAscending,
    CreatedDescending,
    LastUpdateAscending,
    LastUpdateDescending,
    NameAscending,
    NameDescending
}
```

### Classes

- `Collection`, `Document`, `Schema`, `SchemaElement`
- `FieldConstraint`, `IndexedField`
- `SearchFilter`, `SearchQuery`, `SearchResult`
- `IndexRebuildResult`, `ResponseContext`, `IndexTableMapping`

## Exception Handling

```csharp
using Lattice.Sdk.Exceptions;

try
{
    SearchResult? result = await client.Search.SearchAsync(query);
}
catch (LatticeConnectionException ex)
{
    Console.WriteLine($"Connection failed: {ex.Message}");
}
catch (LatticeApiException ex)
{
    Console.WriteLine($"API error ({ex.StatusCode}): {ex.ApiErrorMessage}");
}
catch (LatticeException ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
```

## Project Structure

```
sdk-csharp/
├── Lattice.Sdk/
│   ├── LatticeClient.cs           # Main client class
│   ├── Models/
│   │   ├── Collection.cs          # Collection model
│   │   ├── Document.cs            # Document model
│   │   ├── Schema.cs              # Schema and SchemaElement models
│   │   ├── FieldConstraint.cs     # FieldConstraint and IndexedField models
│   │   ├── Search.cs              # SearchFilter, SearchQuery, SearchResult
│   │   ├── IndexRebuildResult.cs  # Index rebuild result
│   │   ├── ResponseContext.cs     # API response wrapper
│   │   └── Enums.cs               # All enumerations
│   ├── Methods/
│   │   ├── ICollectionMethods.cs  # Collection interface
│   │   ├── IDocumentMethods.cs    # Document interface
│   │   ├── ISearchMethods.cs      # Search interface
│   │   ├── ISchemaMethods.cs      # Schema interface
│   │   ├── IIndexMethods.cs       # Index interface
│   │   └── *Methods.cs            # Implementations
│   ├── Exceptions/
│   │   └── LatticeExceptions.cs   # Custom exceptions
│   └── Lattice.Sdk.csproj
├── Lattice.Sdk.Tests/
│   ├── Program.cs                 # Test harness
│   └── Lattice.Sdk.Tests.csproj
├── Lattice.Sdk.sln
└── README.md
```

## Async Support

All SDK methods are fully async and support `CancellationToken`:

```csharp
CancellationTokenSource cts = new CancellationTokenSource();
cts.CancelAfter(TimeSpan.FromSeconds(10));

try
{
    Collection? collection = await client.Collection.ReadByIdAsync(
        "col_abc123",
        cancellationToken: cts.Token
    );
}
catch (OperationCanceledException)
{
    Console.WriteLine("Operation was cancelled");
}
```
