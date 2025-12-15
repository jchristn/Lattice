# Collection Constraints and Indexing Configuration Plan

This document outlines the implementation plan for adding schema constraints, selective indexing, and collection update capabilities to Lattice.

---

## Executive Summary

Three major features will be added:

1. **Schema Constraints**: Define and enforce document schemas at the collection level
2. **Selective Indexing**: Manually specify which fields are indexed (default: index all)
3. **Collection Updates**: APIs to modify constraints/indexes and rebuild indexes

---

## Implementation Progress

| Phase | Component | Status | Notes |
|-------|-----------|--------|-------|
| **Phase 1** | **Database and Models** | **COMPLETE** | |
| 1.1 | SchemaEnforcementMode.cs | ✅ Complete | `src/Lattice.Core/Models/SchemaEnforcementMode.cs` |
| 1.2 | IndexingMode.cs | ✅ Complete | `src/Lattice.Core/Models/IndexingMode.cs` |
| 1.3 | FieldConstraint.cs | ✅ Complete | `src/Lattice.Core/Models/FieldConstraint.cs` |
| 1.4 | IndexedField.cs | ✅ Complete | `src/Lattice.Core/Models/IndexedField.cs` |
| 1.5 | Collection.cs update | ✅ Complete | Added SchemaEnforcementMode and IndexingMode properties |
| 1.6 | SetupQueries.cs | ✅ Complete | Added fieldconstraints and indexedfields tables, updated collections table |
| 1.7 | IdGenerator.cs | ✅ Complete | Added `fco_` and `ixf_` prefixes |
| 1.8 | IFieldConstraintMethods.cs | ✅ Complete | `src/Lattice.Core/Repositories/Interfaces/` |
| 1.9 | FieldConstraintMethods.cs | ✅ Complete | `src/Lattice.Core/Repositories/Sqlite/Implementations/` |
| 1.10 | IIndexedFieldMethods.cs | ✅ Complete | `src/Lattice.Core/Repositories/Interfaces/` |
| 1.11 | IndexedFieldMethods.cs | ✅ Complete | `src/Lattice.Core/Repositories/Sqlite/Implementations/` |
| 1.12 | Converters.cs | ✅ Complete | Added FieldConstraintFromDataRow, IndexedFieldFromDataRow |
| 1.13 | SqliteRepository.cs | ✅ Complete | Added FieldConstraints and IndexedFields properties |
| 1.14 | CollectionMethods.cs | ✅ Complete | Updated Create/Update to handle new columns |
| 1.15 | IIndexMethods.cs | ✅ Complete | Added helper methods for index rebuilding |
| 1.16 | IndexMethods.cs | ✅ Complete | Implemented new helper methods |
| **Phase 2** | **Validation Engine** | **COMPLETE** | |
| 2.1 | ValidationResult.cs | ✅ Complete | `src/Lattice.Core/Validation/ValidationResult.cs` |
| 2.2 | ValidationError.cs | ✅ Complete | Included in ValidationResult.cs |
| 2.3 | SchemaValidator.cs | ✅ Complete | `src/Lattice.Core/Validation/SchemaValidator.cs` |
| 2.4 | SchemaValidationException.cs | ✅ Complete | `src/Lattice.Core/Exceptions/SchemaValidationException.cs` |
| **Phase 3** | **Client API** | **COMPLETE** | |
| 3.1 | CreateCollection() update | ✅ Complete | Added new parameters for constraints and indexing |
| 3.2 | IngestDocument() update | ✅ Complete | Added schema validation and selective indexing |
| 3.3 | GetCollectionConstraints() | ✅ Complete | New method added |
| 3.4 | GetCollectionIndexedFields() | ✅ Complete | New method added |
| 3.5 | UpdateCollectionConstraints() | ✅ Complete | New method added |
| 3.6 | UpdateCollectionIndexing() | ✅ Complete | New method added |
| 3.7 | RebuildIndexes() | ✅ Complete | New method with progress reporting |
| 3.8 | IndexRebuildResult.cs | ✅ Complete | `src/Lattice.Core/Models/IndexRebuildResult.cs` |
| 3.9 | IndexRebuildProgress.cs | ✅ Complete | `src/Lattice.Core/Models/IndexRebuildProgress.cs` |
| **Phase 4** | **REST API** | **COMPLETE** | |
| 4.1 | CreateCollectionRequest.cs | ✅ Complete | Added constraint and indexing fields |
| 4.2 | UpdateConstraintsRequest.cs | ✅ Complete | `src/Lattice.Server/Classes/UpdateConstraintsRequest.cs` |
| 4.3 | UpdateIndexingRequest.cs | ✅ Complete | `src/Lattice.Server/Classes/UpdateIndexingRequest.cs` |
| 4.4 | RebuildIndexesRequest.cs | ✅ Complete | `src/Lattice.Server/Classes/RebuildIndexesRequest.cs` |
| 4.5 | GET /constraints endpoint | ✅ Complete | Returns constraints and enforcement mode |
| 4.6 | PUT /constraints endpoint | ✅ Complete | Updates constraints and enforcement mode |
| 4.7 | GET /indexing endpoint | ✅ Complete | Returns indexing mode and indexed fields |
| 4.8 | PUT /indexing endpoint | ✅ Complete | Updates indexing mode and indexed fields |
| 4.9 | POST /indexes/rebuild endpoint | ✅ Complete | Triggers index rebuild |
| 4.10 | SchemaValidationException handling | ✅ Complete | Returns 400 with validation errors |
| **Testing** | **Automated Tests** | **COMPLETE** | |
| T.1 | Schema Constraints tests | ✅ Complete | 32+ tests covering type validation, nullable, regex, range, length, allowed values, enforcement modes, nested fields, array elements |
| T.2 | Indexing Mode tests | ✅ Complete | 10+ tests covering selective mode, none mode, all mode, nested fields, rebuild with progress |
| **Dashboard** | **UI Components** | **COMPLETE** | |
| D.1 | Schema Constraints Modal | ✅ Complete | View/edit enforcement mode and field constraints |
| D.2 | Indexing Config Modal | ✅ Complete | View/edit indexing mode and indexed fields |
| D.3 | Index Rebuild Modal | ✅ Complete | Rebuild with progress display and results |
| D.4 | Collections.jsx update | ✅ Complete | Action menu items for new features |
| D.5 | Collections.css update | ✅ Complete | Styling for new modal components |
| D.6 | api.js update | ✅ Complete | API methods for constraints, indexing, rebuild |
| **Postman** | **API Collection** | **COMPLETE** | |
| P.1 | Get Schema Constraints | ✅ Complete | GET /constraints endpoint |
| P.2 | Update Schema Constraints | ✅ Complete | POST /constraints with examples |
| P.3 | Get Indexing Config | ✅ Complete | GET /indexing endpoint |
| P.4 | Update Indexing Config | ✅ Complete | POST /indexing with examples |
| P.5 | Rebuild Indexes | ✅ Complete | POST /rebuild-indexes with options |
| P.6 | Create Collection (With Constraints) | ✅ Complete | Full example with constraints and indexing |

### Build Status
- **Solution builds successfully** with 0 errors (warnings only)
- All new code follows existing patterns and conventions
- **42 automated tests** covering all validation scenarios
- **Dashboard fully functional** with constraints and indexing management
- **Postman collection updated** with 7 new API request examples

---

## Part 1: Schema Constraints

### 1.1 Overview

Allow users to define a schema when creating a collection. Documents that don't match the schema will be rejected during ingestion with a descriptive error.

### 1.2 Model Changes

#### Update `Collection.cs`
**Location:** `src/Lattice.Core/Models/Collection.cs`

Add a new property to the existing `Collection` model:

```csharp
public class Collection
{
    // Existing properties...
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string DocumentsDirectory { get; set; }
    public List<string> Labels { get; set; }
    public Dictionary<string, string> Tags { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime LastUpdateUtc { get; set; }

    // NEW PROPERTIES
    public SchemaEnforcementMode SchemaEnforcementMode { get; set; } = SchemaEnforcementMode.None;
    public IndexingMode IndexingMode { get; set; } = IndexingMode.All;
}
```

#### `SchemaEnforcementMode.cs` (new file)
**Location:** `src/Lattice.Core/Models/SchemaEnforcementMode.cs`

```csharp
public enum SchemaEnforcementMode
{
    /// <summary>No schema enforcement (current behavior)</summary>
    None = 0,

    /// <summary>Document must have exactly the defined fields, no more, no less</summary>
    Strict = 1,

    /// <summary>Document must have required fields but may have additional fields</summary>
    Flexible = 2,

    /// <summary>Only validate specified fields, ignore others entirely</summary>
    Partial = 3
}
```

#### `IndexingMode.cs` (new file)
**Location:** `src/Lattice.Core/Models/IndexingMode.cs`

```csharp
public enum IndexingMode
{
    /// <summary>Index all fields (current default behavior)</summary>
    All = 0,

    /// <summary>Only index explicitly specified fields</summary>
    Selective = 1,

    /// <summary>Do not create any indexes (document store only)</summary>
    None = 2
}
```

#### `FieldConstraint.cs` (new file)
**Location:** `src/Lattice.Core/Models/FieldConstraint.cs`

```csharp
public class FieldConstraint
{
    public string Id { get; set; }                    // fco_{id}
    public string CollectionId { get; set; }          // Foreign key to collections
    public string FieldPath { get; set; }             // Dot-notation: "Person.Address.City"
    public string DataType { get; set; }              // "string", "integer", "number", "boolean", "array", "object"
    public bool Required { get; set; }                // Field must be present
    public bool Nullable { get; set; }                // Field can be null
    public string RegexPattern { get; set; }          // Validation pattern for strings
    public decimal? MinValue { get; set; }            // For numbers
    public decimal? MaxValue { get; set; }            // For numbers
    public int? MinLength { get; set; }               // For strings/arrays
    public int? MaxLength { get; set; }               // For strings/arrays
    public List<string> AllowedValues { get; set; }   // Enum-like constraint
    public string ArrayElementType { get; set; }      // For array fields
    public DateTime CreatedUtc { get; set; }
    public DateTime LastUpdateUtc { get; set; }
}
```

#### `IndexedField.cs` (new file)
**Location:** `src/Lattice.Core/Models/IndexedField.cs`

```csharp
public class IndexedField
{
    public string Id { get; set; }                    // ixf_{id}
    public string CollectionId { get; set; }          // Foreign key to collections
    public string FieldPath { get; set; }             // Dot-notation path
    public DateTime CreatedUtc { get; set; }
    public DateTime LastUpdateUtc { get; set; }
}
```

### 1.3 Database Schema Changes

#### Modify `collections` table

Add two new columns to the existing `collections` table:

```sql
ALTER TABLE collections ADD COLUMN schemaenforcementmode INTEGER NOT NULL DEFAULT 0;
ALTER TABLE collections ADD COLUMN indexingmode INTEGER NOT NULL DEFAULT 0;
```

For new installations, update the CREATE TABLE statement in `SetupQueries.cs`:

```sql
CREATE TABLE IF NOT EXISTS collections (
    id TEXT PRIMARY KEY,
    name TEXT NOT NULL,
    description TEXT,
    documentsdirectory TEXT,
    schemaenforcementmode INTEGER NOT NULL DEFAULT 0,  -- NEW
    indexingmode INTEGER NOT NULL DEFAULT 0,           -- NEW
    createdutc TEXT NOT NULL,
    lastupdateutc TEXT NOT NULL
);
```

#### New `fieldconstraints` table

```sql
CREATE TABLE IF NOT EXISTS fieldconstraints (
    id TEXT PRIMARY KEY,
    collectionid TEXT NOT NULL,
    fieldpath TEXT NOT NULL,
    datatype TEXT,
    required INTEGER NOT NULL DEFAULT 0,
    nullable INTEGER NOT NULL DEFAULT 1,
    regexpattern TEXT,
    minvalue REAL,
    maxvalue REAL,
    minlength INTEGER,
    maxlength INTEGER,
    allowedvalues TEXT,              -- JSON array stored as text
    arrayelementtype TEXT,
    createdutc TEXT NOT NULL,
    lastupdateutc TEXT NOT NULL,
    FOREIGN KEY (collectionid) REFERENCES collections(id) ON DELETE CASCADE
);

CREATE INDEX idx_fieldconstraints_collectionid ON fieldconstraints(collectionid);
CREATE UNIQUE INDEX idx_fieldconstraints_collectionid_fieldpath ON fieldconstraints(collectionid, fieldpath);
```

#### New `indexedfields` table

```sql
CREATE TABLE IF NOT EXISTS indexedfields (
    id TEXT PRIMARY KEY,
    collectionid TEXT NOT NULL,
    fieldpath TEXT NOT NULL,
    createdutc TEXT NOT NULL,
    lastupdateutc TEXT NOT NULL,
    FOREIGN KEY (collectionid) REFERENCES collections(id) ON DELETE CASCADE
);

CREATE INDEX idx_indexedfields_collectionid ON indexedfields(collectionid);
CREATE UNIQUE INDEX idx_indexedfields_collectionid_fieldpath ON indexedfields(collectionid, fieldpath);
```

### 1.4 Repository Interfaces

#### `IFieldConstraintMethods.cs`
**Location:** `src/Lattice.Core/Repositories/Interfaces/IFieldConstraintMethods.cs`

```csharp
public interface IFieldConstraintMethods
{
    Task<List<FieldConstraint>> CreateMany(List<FieldConstraint> constraints, CancellationToken token = default);
    Task<List<FieldConstraint>> ReadByCollectionId(string collectionId, CancellationToken token = default);
    Task DeleteByCollectionId(string collectionId, CancellationToken token = default);
}
```

#### `IIndexedFieldMethods.cs`
**Location:** `src/Lattice.Core/Repositories/Interfaces/IIndexedFieldMethods.cs`

```csharp
public interface IIndexedFieldMethods
{
    Task<List<IndexedField>> CreateMany(List<IndexedField> fields, CancellationToken token = default);
    Task<List<IndexedField>> ReadByCollectionId(string collectionId, CancellationToken token = default);
    Task DeleteByCollectionId(string collectionId, CancellationToken token = default);
}
```

### 1.5 Schema Validation Engine

#### `SchemaValidator.cs` (new file)
**Location:** `src/Lattice.Core/Validation/SchemaValidator.cs`

```csharp
public interface ISchemaValidator
{
    ValidationResult Validate(
        string json,
        SchemaEnforcementMode mode,
        List<FieldConstraint> fieldConstraints);
}

public class SchemaValidator : ISchemaValidator
{
    // Implementation details below
}
```

**Validation Logic:**

1. **Parse JSON** - Use `JsonDocument.Parse()` to parse the incoming document
2. **Extract Fields** - Use existing `JsonFlattener` to get all field paths and values
3. **Check Required Fields** - Ensure all `Required=true` fields are present
4. **Check Extra Fields** (Strict mode) - Reject documents with fields not in schema
5. **Validate Each Field**:
   - Type checking (string, integer, number, boolean, array, object)
   - Null checking (if `Nullable=false` and value is null, reject)
   - Regex pattern matching for strings
   - Range validation for numbers (MinValue, MaxValue)
   - Length validation for strings/arrays (MinLength, MaxLength)
   - Enum validation (AllowedValues)
   - Array element type validation

#### `ValidationResult.cs` (new file)
**Location:** `src/Lattice.Core/Validation/ValidationResult.cs`

```csharp
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<ValidationError> Errors { get; set; } = new();
}

public class ValidationError
{
    public string FieldPath { get; set; }
    public string ErrorCode { get; set; }
    public string Message { get; set; }
    public object ActualValue { get; set; }
    public object ExpectedValue { get; set; }
}
```

**Error Codes:**
- `MISSING_REQUIRED_FIELD` - Required field not present
- `UNEXPECTED_FIELD` - Field not in schema (Strict mode)
- `TYPE_MISMATCH` - Value type doesn't match expected type
- `NULL_NOT_ALLOWED` - Null value in non-nullable field
- `PATTERN_MISMATCH` - String doesn't match regex pattern
- `VALUE_TOO_SMALL` - Number below MinValue
- `VALUE_TOO_LARGE` - Number above MaxValue
- `STRING_TOO_SHORT` - String below MinLength
- `STRING_TOO_LONG` - String above MaxLength
- `ARRAY_TOO_SHORT` - Array below MinLength
- `ARRAY_TOO_LONG` - Array above MaxLength
- `VALUE_NOT_ALLOWED` - Value not in AllowedValues list
- `INVALID_ARRAY_ELEMENT` - Array element doesn't match expected type

### 1.6 Exception Types

#### `SchemaValidationException.cs` (new file)
**Location:** `src/Lattice.Core/Exceptions/SchemaValidationException.cs`

```csharp
public class SchemaValidationException : Exception
{
    public string CollectionId { get; }
    public List<ValidationError> Errors { get; }

    public SchemaValidationException(string collectionId, List<ValidationError> errors)
        : base($"Document failed schema validation for collection {collectionId}: {FormatErrors(errors)}")
    {
        CollectionId = collectionId;
        Errors = errors;
    }

    private static string FormatErrors(List<ValidationError> errors)
    {
        return string.Join("; ", errors.Select(e => $"{e.FieldPath}: {e.Message}"));
    }
}
```

### 1.7 API Changes

#### Update `CreateCollection` Method
**Location:** `src/Lattice.Core/LatticeClient.cs`

```csharp
public async Task<Collection> CreateCollection(
    string name,
    string description = null,
    string documentsDirectory = null,
    List<string> labels = null,
    Dictionary<string, string> tags = null,
    SchemaEnforcementMode schemaEnforcementMode = SchemaEnforcementMode.None,  // NEW
    List<FieldConstraint> fieldConstraints = null,                              // NEW
    IndexingMode indexingMode = IndexingMode.All,                               // NEW
    List<string> indexedFields = null,                                          // NEW
    CancellationToken token = default)
```

**Implementation Steps:**
1. Create collection with new `SchemaEnforcementMode` and `IndexingMode` columns
2. If `fieldConstraints` is provided:
   - Generate IDs for each FieldConstraint
   - Set CollectionId on each FieldConstraint
   - Insert FieldConstraint records
3. If `indexedFields` is provided and `indexingMode == Selective`:
   - Create IndexedField records for each field path

#### Update `IngestDocument` Method
**Location:** `src/Lattice.Core/LatticeClient.cs`

**Implementation Steps (insert at beginning of method):**
1. Check collection's `SchemaEnforcementMode`
2. If `SchemaEnforcementMode != None`:
   - Load FieldConstraints for collection
   - Call `SchemaValidator.Validate(json, mode, fieldConstraints)`
   - If `!result.IsValid`, throw `SchemaValidationException`
3. Check collection's `IndexingMode`
4. Filter fields for indexing based on mode and `indexedfields` table
5. Proceed with existing ingestion logic

### 1.8 REST API Changes

#### Create Collection Request Update
**Location:** `src/Lattice.Server/Classes/CreateCollectionRequest.cs`

```csharp
public class CreateCollectionRequest
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string DocumentsDirectory { get; set; }
    public List<string> Labels { get; set; }
    public Dictionary<string, string> Tags { get; set; }

    // NEW PROPERTIES
    public string SchemaEnforcementMode { get; set; }        // "None", "Strict", "Flexible", "Partial"
    public List<FieldConstraintRequest> FieldConstraints { get; set; }
    public string IndexingMode { get; set; }                 // "All", "Selective", "None"
    public List<string> IndexedFields { get; set; }
}

public class FieldConstraintRequest
{
    public string FieldPath { get; set; }
    public string DataType { get; set; }
    public bool Required { get; set; }
    public bool Nullable { get; set; } = true;
    public string RegexPattern { get; set; }
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }
    public List<string> AllowedValues { get; set; }
    public string ArrayElementType { get; set; }
}
```

#### New Endpoints

```
GET /v1.0/collections/{collectionId}/constraints
```
Returns the schema enforcement mode and field constraints for a collection.

```
GET /v1.0/collections/{collectionId}/indexes
```
Returns the indexing mode and list of indexed fields for a collection.

---

## Part 2: Indexing Logic Changes

### 2.1 Update `IngestDocument` Method
**Location:** `src/Lattice.Core/LatticeClient.cs`

**Current Flow (simplified):**
```
1. Generate schema from JSON
2. Flatten JSON to key-value pairs
3. For EACH key: create/get index table mapping
4. Insert ALL values into index tables
```

**New Flow:**
```
1. Validate against schema (if SchemaEnforcementMode != None)
2. Generate schema from JSON
3. Flatten JSON to key-value pairs
4. FILTER flattened values based on IndexingMode:
   - If Mode == All: keep all (current behavior)
   - If Mode == Selective: keep only fields in indexedfields table
   - If Mode == None: skip all indexing
5. For each FILTERED key: create/get index table mapping
6. Insert FILTERED values into index tables
```

**Implementation:**

```csharp
private async Task<Dictionary<string, List<FlattenedValue>>> FilterValuesForIndexing(
    Dictionary<string, List<FlattenedValue>> valuesByKey,
    Collection collection,
    CancellationToken token)
{
    if (collection.IndexingMode == IndexingMode.All)
    {
        return valuesByKey; // Current behavior
    }

    if (collection.IndexingMode == IndexingMode.None)
    {
        return new Dictionary<string, List<FlattenedValue>>(); // No indexing
    }

    // Selective mode: filter to only specified fields
    var indexedFields = await _repo.IndexedFields.ReadByCollectionId(collection.Id, token);
    var indexedPaths = new HashSet<string>(
        indexedFields.Select(f => f.FieldPath),
        StringComparer.OrdinalIgnoreCase);

    return valuesByKey
        .Where(kvp => indexedPaths.Contains(kvp.Key))
        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
}
```

---

## Part 3: Collection Updates and Index Rebuilding

### 3.1 Overview

Allow users to:
1. Update schema constraints on an existing collection
2. Update indexed fields on an existing collection
3. Rebuild indexes for a collection (necessary after changing indexed fields)

### 3.2 New Client API Methods

**Location:** `src/Lattice.Core/LatticeClient.cs`

#### Update Collection Constraints

```csharp
/// <summary>
/// Updates the schema constraints for a collection.
/// Existing documents are NOT re-validated. New documents will be validated against the new schema.
/// </summary>
public async Task<Collection> UpdateCollectionConstraints(
    string collectionId,
    SchemaEnforcementMode schemaEnforcementMode,
    List<FieldConstraint> fieldConstraints = null,
    CancellationToken token = default)
{
    // Implementation:
    // 1. Validate collection exists
    // 2. Update collection's SchemaEnforcementMode
    // 3. Delete existing FieldConstraints for collection
    // 4. Create new FieldConstraints (if provided)
    // 5. Return updated Collection
}
```

#### Update Collection Indexing

```csharp
/// <summary>
/// Updates the indexing configuration for a collection.
/// NOTE: Does not automatically rebuild indexes. Call RebuildIndexes() after this.
/// </summary>
public async Task<Collection> UpdateCollectionIndexing(
    string collectionId,
    IndexingMode indexingMode,
    List<string> indexedFields = null,
    CancellationToken token = default)
{
    // Implementation:
    // 1. Validate collection exists
    // 2. Update collection's IndexingMode
    // 3. Delete existing IndexedFields for collection
    // 4. Create new IndexedFields (if provided and mode is Selective)
    // 5. Return updated Collection
}
```

#### Rebuild Indexes

```csharp
/// <summary>
/// Rebuilds all indexes for a collection based on current IndexingMode.
/// This is a potentially long-running operation for large collections.
/// </summary>
public async Task<IndexRebuildResult> RebuildIndexes(
    string collectionId,
    bool dropUnusedIndexes = true,
    IProgress<IndexRebuildProgress> progress = null,
    CancellationToken token = default)
```

### 3.3 Index Rebuild Models

#### `IndexRebuildResult.cs` (new file)
**Location:** `src/Lattice.Core/Models/IndexRebuildResult.cs`

```csharp
public class IndexRebuildResult
{
    public string CollectionId { get; set; }
    public int DocumentsProcessed { get; set; }
    public int IndexesCreated { get; set; }
    public int IndexesDropped { get; set; }
    public int ValuesInserted { get; set; }
    public TimeSpan Duration { get; set; }
    public List<string> Errors { get; set; } = new();
    public bool Success => Errors.Count == 0;
}
```

#### `IndexRebuildProgress.cs` (new file)
**Location:** `src/Lattice.Core/Models/IndexRebuildProgress.cs`

```csharp
public class IndexRebuildProgress
{
    public int TotalDocuments { get; set; }
    public int ProcessedDocuments { get; set; }
    public string CurrentPhase { get; set; }  // "Scanning", "Dropping", "Clearing", "Indexing"
    public double PercentComplete => TotalDocuments > 0
        ? (double)ProcessedDocuments / TotalDocuments * 100
        : 0;
}
```

### 3.4 Rebuild Algorithm

```csharp
public async Task<IndexRebuildResult> RebuildIndexes(
    string collectionId,
    bool dropUnusedIndexes = true,
    IProgress<IndexRebuildProgress> progress = null,
    CancellationToken token = default)
{
    var result = new IndexRebuildResult { CollectionId = collectionId };
    var stopwatch = Stopwatch.StartNew();

    // 1. Load collection
    var collection = await GetCollection(collectionId, token);
    if (collection == null)
        throw new ArgumentException($"Collection {collectionId} not found");

    // 2. Get all documents in collection
    var documents = await _repo.Documents.ReadByCollectionId(collectionId, token);

    progress?.Report(new IndexRebuildProgress
    {
        TotalDocuments = documents.Count,
        CurrentPhase = "Scanning"
    });

    // 3. If dropping unused indexes, identify and drop tables
    if (dropUnusedIndexes && collection.IndexingMode == IndexingMode.Selective)
    {
        progress?.Report(new IndexRebuildProgress
        {
            TotalDocuments = documents.Count,
            CurrentPhase = "Dropping"
        });

        var indexedFields = await _repo.IndexedFields.ReadByCollectionId(collectionId, token);
        var indexedPaths = new HashSet<string>(indexedFields.Select(f => f.FieldPath));

        // Get all index tables used by this collection
        var collectionIndexTables = await _repo.Indexes.GetIndexTablesForCollection(collectionId, token);

        foreach (var tableName in collectionIndexTables)
        {
            var mapping = await _repo.Indexes.GetMappingByTableName(tableName, token);
            if (mapping != null && !indexedPaths.Contains(mapping.Key))
            {
                await _repo.Indexes.DeleteValuesFromTable(tableName, collectionId, token);
                result.IndexesDropped++;
            }
        }
    }

    // 4. Clear existing values for this collection from relevant index tables
    progress?.Report(new IndexRebuildProgress
    {
        TotalDocuments = documents.Count,
        CurrentPhase = "Clearing"
    });

    await _repo.Indexes.DeleteValuesByCollectionId(collectionId, token);

    // 5. Re-index each document
    progress?.Report(new IndexRebuildProgress
    {
        TotalDocuments = documents.Count,
        CurrentPhase = "Indexing"
    });

    for (int i = 0; i < documents.Count; i++)
    {
        token.ThrowIfCancellationRequested();

        var doc = documents[i];
        try
        {
            // Load raw JSON
            var jsonPath = Path.Combine(collection.DocumentsDirectory, $"{doc.Id}.json");
            var json = await File.ReadAllTextAsync(jsonPath, token);

            // Flatten and filter
            var flattened = _flattener.Flatten(json);
            var valuesByKey = GroupByKey(flattened);
            var filteredValues = await FilterValuesForIndexing(valuesByKey, collection, token);

            // Create index tables if needed and insert values
            var documentValues = await CreateAndInsertValues(doc.Id, doc.SchemaId, filteredValues, token);

            result.ValuesInserted += documentValues;
            result.DocumentsProcessed++;

            progress?.Report(new IndexRebuildProgress
            {
                TotalDocuments = documents.Count,
                ProcessedDocuments = i + 1,
                CurrentPhase = "Indexing"
            });
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Document {doc.Id}: {ex.Message}");
        }
    }

    result.Duration = stopwatch.Elapsed;
    return result;
}
```

### 3.5 Helper Methods for Index Management

**Location:** `src/Lattice.Core/Repositories/Sqlite/Implementations/IndexMethods.cs`

```csharp
/// <summary>
/// Deletes all values from index tables for documents in a collection.
/// </summary>
public async Task DeleteValuesByCollectionId(string collectionId, CancellationToken token = default)

/// <summary>
/// Deletes values from a specific index table for a collection.
/// </summary>
public async Task DeleteValuesFromTable(string tableName, string collectionId, CancellationToken token = default)

/// <summary>
/// Gets all index table names that contain values for documents in a collection.
/// </summary>
public async Task<List<string>> GetIndexTablesForCollection(string collectionId, CancellationToken token = default)

/// <summary>
/// Gets mapping by table name.
/// </summary>
public async Task<IndexTableMapping> GetMappingByTableName(string tableName, CancellationToken token = default)
```

### 3.6 REST API Endpoints

**Update Collection Constraints:**
```
PUT /v1.0/collections/{collectionId}/constraints
```
Request body:
```json
{
    "schemaEnforcementMode": "Flexible",
    "fieldConstraints": [
        {
            "fieldPath": "email",
            "dataType": "string",
            "required": true
        }
    ]
}
```

**Update Collection Indexing:**
```
PUT /v1.0/collections/{collectionId}/indexes
```
Request body:
```json
{
    "indexingMode": "Selective",
    "indexedFields": ["email", "username", "createdAt"]
}
```

**Rebuild Indexes:**
```
POST /v1.0/collections/{collectionId}/indexes/rebuild
```
Request body:
```json
{
    "dropUnusedIndexes": true
}
```
Response:
```json
{
    "collectionId": "col_xxx",
    "documentsProcessed": 1500,
    "indexesCreated": 12,
    "indexesDropped": 3,
    "valuesInserted": 45000,
    "durationMs": 3500,
    "success": true,
    "errors": []
}
```

---

## Part 4: Implementation Order

### Phase 1: Database and Models

1. **Database Changes**
   - Add `schemaenforcementmode` column to `collections` table in `SetupQueries.cs`
   - Add `indexingmode` column to `collections` table in `SetupQueries.cs`
   - Add `fieldconstraints` table to `SetupQueries.cs`
   - Add `indexedfields` table to `SetupQueries.cs`

2. **Models**
   - Create `SchemaEnforcementMode.cs` enum
   - Create `IndexingMode.cs` enum
   - Create `FieldConstraint.cs`
   - Create `IndexedField.cs`
   - Update `Collection.cs` with new properties
   - Update `IdGenerator.cs` with new prefixes (`fco_`, `ixf_`)

3. **Repository Layer**
   - Create `IFieldConstraintMethods.cs` and `FieldConstraintMethods.cs`
   - Create `IIndexedFieldMethods.cs` and `IndexedFieldMethods.cs`
   - Update `ICollectionMethods.cs` to handle new columns
   - Update `CollectionMethods.cs` for new columns
   - Add converters to `Converters.cs`
   - Update `SqliteRepository.cs` with new method interfaces

### Phase 2: Validation Engine

1. **Validation**
   - Create `ValidationResult.cs` and `ValidationError.cs`
   - Create `ISchemaValidator.cs` interface
   - Create `SchemaValidator.cs` implementation
   - Create `SchemaValidationException.cs`

### Phase 3: Client API

1. **Create Operations**
   - Update `CreateCollection()` signature and implementation

2. **Ingest Operations**
   - Update `IngestDocument()` to validate schema
   - Update `IngestDocument()` to filter indexed fields
   - Add `FilterValuesForIndexing()` method

3. **Update Operations**
   - Add `UpdateCollectionConstraints()` method
   - Add `UpdateCollectionIndexing()` method

4. **Rebuild Operations**
   - Create `IndexRebuildResult.cs`
   - Create `IndexRebuildProgress.cs`
   - Add `RebuildIndexes()` method
   - Add helper methods to `IndexMethods.cs`

### Phase 4: REST API

1. **Request/Response Classes**
   - Update `CreateCollectionRequest.cs`
   - Create `UpdateConstraintsRequest.cs`
   - Create `UpdateIndexingRequest.cs`
   - Create `RebuildIndexesRequest.cs`
   - Create `IndexRebuildResponse.cs`

2. **Endpoints**
   - Update collection creation endpoint
   - Add `GET /constraints` endpoint
   - Add `PUT /constraints` endpoint
   - Add `GET /indexes` endpoint
   - Add `PUT /indexes` endpoint
   - Add `POST /indexes/rebuild` endpoint

---

## Part 5: Test Projects

### 5.1 Unit Tests
**Location:** `src/Test.Automated/`

#### Schema Validation Tests
Create `SchemaValidatorTests.cs`:

```csharp
[TestClass]
public class SchemaValidatorTests
{
    // Type validation
    [TestMethod] public void Validate_StringField_WithStringValue_Succeeds()
    [TestMethod] public void Validate_StringField_WithIntegerValue_Fails()
    [TestMethod] public void Validate_IntegerField_WithIntegerValue_Succeeds()
    [TestMethod] public void Validate_IntegerField_WithDecimalValue_Fails()
    [TestMethod] public void Validate_NumberField_WithDecimalValue_Succeeds()
    [TestMethod] public void Validate_BooleanField_WithBooleanValue_Succeeds()
    [TestMethod] public void Validate_ArrayField_WithArrayValue_Succeeds()

    // Required field tests
    [TestMethod] public void Validate_RequiredField_Missing_Fails()
    [TestMethod] public void Validate_RequiredField_Present_Succeeds()
    [TestMethod] public void Validate_OptionalField_Missing_Succeeds()

    // Nullable tests
    [TestMethod] public void Validate_NullableField_WithNull_Succeeds()
    [TestMethod] public void Validate_NonNullableField_WithNull_Fails()

    // Regex pattern tests
    [TestMethod] public void Validate_PatternField_MatchingValue_Succeeds()
    [TestMethod] public void Validate_PatternField_NonMatchingValue_Fails()
    [TestMethod] public void Validate_EmailPattern_ValidEmail_Succeeds()
    [TestMethod] public void Validate_EmailPattern_InvalidEmail_Fails()

    // Range tests
    [TestMethod] public void Validate_MinValue_ValueAbove_Succeeds()
    [TestMethod] public void Validate_MinValue_ValueBelow_Fails()
    [TestMethod] public void Validate_MaxValue_ValueBelow_Succeeds()
    [TestMethod] public void Validate_MaxValue_ValueAbove_Fails()
    [TestMethod] public void Validate_MinMaxRange_ValueInRange_Succeeds()

    // Length tests
    [TestMethod] public void Validate_MinLength_StringAbove_Succeeds()
    [TestMethod] public void Validate_MinLength_StringBelow_Fails()
    [TestMethod] public void Validate_MaxLength_StringBelow_Succeeds()
    [TestMethod] public void Validate_MaxLength_StringAbove_Fails()
    [TestMethod] public void Validate_ArrayMinLength_Succeeds()
    [TestMethod] public void Validate_ArrayMaxLength_Fails()

    // Allowed values tests
    [TestMethod] public void Validate_AllowedValues_ValueInList_Succeeds()
    [TestMethod] public void Validate_AllowedValues_ValueNotInList_Fails()

    // Enforcement mode tests
    [TestMethod] public void Validate_StrictMode_ExtraField_Fails()
    [TestMethod] public void Validate_FlexibleMode_ExtraField_Succeeds()
    [TestMethod] public void Validate_PartialMode_OnlyValidatesSpecifiedFields()
    [TestMethod] public void Validate_NoneMode_SkipsValidation()

    // Nested field tests
    [TestMethod] public void Validate_NestedField_ValidValue_Succeeds()
    [TestMethod] public void Validate_NestedField_InvalidValue_Fails()
    [TestMethod] public void Validate_DeeplyNestedField_Succeeds()

    // Array element tests
    [TestMethod] public void Validate_ArrayElementType_AllMatch_Succeeds()
    [TestMethod] public void Validate_ArrayElementType_OneMismatch_Fails()
}
```

#### Field Constraint Repository Tests
Create `FieldConstraintMethodsTests.cs`:

```csharp
[TestClass]
public class FieldConstraintMethodsTests
{
    [TestMethod] public void CreateMany_ValidConstraints_Succeeds()
    [TestMethod] public void ReadByCollectionId_ReturnsConstraints()
    [TestMethod] public void DeleteByCollectionId_RemovesAllConstraints()
}
```

#### Indexed Field Repository Tests
Create `IndexedFieldMethodsTests.cs`:

```csharp
[TestClass]
public class IndexedFieldMethodsTests
{
    [TestMethod] public void CreateMany_ValidFields_Succeeds()
    [TestMethod] public void ReadByCollectionId_ReturnsFields()
    [TestMethod] public void DeleteByCollectionId_RemovesAllFields()
}
```

### 5.2 Integration Tests
**Location:** `src/Test.Automated/`

#### Collection Constraint Integration Tests
Create `CollectionConstraintIntegrationTests.cs`:

```csharp
[TestClass]
public class CollectionConstraintIntegrationTests
{
    // Collection creation with constraints
    [TestMethod] public void CreateCollection_WithSchemaConstraints_StoresConstraints()
    [TestMethod] public void CreateCollection_WithIndexingMode_StoresMode()
    [TestMethod] public void CreateCollection_WithSelectiveIndexing_StoresIndexedFields()

    // Document ingestion with validation
    [TestMethod] public void IngestDocument_ValidDocument_Succeeds()
    [TestMethod] public void IngestDocument_InvalidDocument_ThrowsSchemaValidationException()
    [TestMethod] public void IngestDocument_MissingRequiredField_ThrowsWithCorrectError()
    [TestMethod] public void IngestDocument_TypeMismatch_ThrowsWithCorrectError()
    [TestMethod] public void IngestDocument_PatternMismatch_ThrowsWithCorrectError()

    // Selective indexing
    [TestMethod] public void IngestDocument_SelectiveIndexing_OnlyIndexesSpecifiedFields()
    [TestMethod] public void IngestDocument_NoIndexing_CreatesNoIndexEntries()
    [TestMethod] public void IngestDocument_AllIndexing_IndexesAllFields()

    // Search with selective indexing
    [TestMethod] public void Search_OnIndexedField_ReturnsResults()
    [TestMethod] public void Search_OnNonIndexedField_ReturnsEmpty()

    // Update constraints
    [TestMethod] public void UpdateCollectionConstraints_ChangesEnforcementMode()
    [TestMethod] public void UpdateCollectionConstraints_ReplacesFieldConstraints()
    [TestMethod] public void UpdateCollectionIndexing_ChangesIndexingMode()
    [TestMethod] public void UpdateCollectionIndexing_ReplacesIndexedFields()

    // Index rebuild
    [TestMethod] public void RebuildIndexes_ReindexesAllDocuments()
    [TestMethod] public void RebuildIndexes_DropsUnusedIndexes()
    [TestMethod] public void RebuildIndexes_ReportsProgress()
    [TestMethod] public void RebuildIndexes_HandlesLargeCollections()
    [TestMethod] public void RebuildIndexes_CanBeCancelled()
}
```

### 5.3 Performance Tests
**Location:** `src/Test.Throughput/`

Add to existing throughput tests:

```csharp
[TestMethod] public void Throughput_IngestWithValidation_MeasuresOverhead()
[TestMethod] public void Throughput_IngestWithSelectiveIndexing_MeasuresImprovement()
[TestMethod] public void Throughput_RebuildIndexes_LargeCollection()
```

---

## Part 6: Dashboard Updates

### 6.1 Collection Detail View
**Location:** `dashboard/src/`

Update the collection detail page to display and edit constraints:

#### New Components

1. **SchemaConstraintsPanel.tsx**
   - Display current `SchemaEnforcementMode`
   - List all `FieldConstraints` in a table
   - Edit mode to modify constraints
   - Add/remove field constraints

2. **IndexingConfigPanel.tsx**
   - Display current `IndexingMode`
   - List all indexed fields (when mode is Selective)
   - Edit mode to change mode and fields

3. **IndexRebuildDialog.tsx**
   - Confirmation dialog for rebuild
   - Checkbox for "Drop unused indexes"
   - Progress bar during rebuild
   - Display results when complete

#### UI Layout

```
Collection: {name}
├── Overview Tab (existing)
├── Documents Tab (existing)
├── Schema Tab (existing)
├── Constraints Tab (NEW)
│   ├── Schema Enforcement
│   │   ├── Mode: [Dropdown: None/Strict/Flexible/Partial]
│   │   └── Field Constraints Table
│   │       ├── [Add Constraint Button]
│   │       └── Rows: FieldPath | DataType | Required | Nullable | Pattern | Min | Max | Actions
│   └── [Save Changes Button]
└── Indexing Tab (NEW)
    ├── Indexing Mode: [Dropdown: All/Selective/None]
    ├── Indexed Fields (visible when Selective)
    │   ├── [Add Field Button]
    │   └── List of field paths with remove buttons
    ├── [Save Changes Button]
    └── [Rebuild Indexes Button]
```

#### API Integration

Update `dashboard/src/services/api.ts`:

```typescript
// New API methods
getCollectionConstraints(collectionId: string): Promise<CollectionConstraints>
updateCollectionConstraints(collectionId: string, request: UpdateConstraintsRequest): Promise<Collection>
getCollectionIndexing(collectionId: string): Promise<CollectionIndexing>
updateCollectionIndexing(collectionId: string, request: UpdateIndexingRequest): Promise<Collection>
rebuildIndexes(collectionId: string, dropUnused: boolean): Promise<IndexRebuildResult>
```

#### New TypeScript Types

```typescript
// types.ts
interface FieldConstraint {
    id: string;
    fieldPath: string;
    dataType: string;
    required: boolean;
    nullable: boolean;
    regexPattern?: string;
    minValue?: number;
    maxValue?: number;
    minLength?: number;
    maxLength?: number;
    allowedValues?: string[];
    arrayElementType?: string;
}

interface CollectionConstraints {
    schemaEnforcementMode: 'None' | 'Strict' | 'Flexible' | 'Partial';
    fieldConstraints: FieldConstraint[];
}

interface CollectionIndexing {
    indexingMode: 'All' | 'Selective' | 'None';
    indexedFields: string[];
}

interface IndexRebuildResult {
    collectionId: string;
    documentsProcessed: number;
    indexesCreated: number;
    indexesDropped: number;
    valuesInserted: number;
    durationMs: number;
    success: boolean;
    errors: string[];
}
```

### 6.2 Collection Creation Dialog

Update the "Create Collection" dialog to include:

1. **Schema Enforcement Section**
   - Dropdown for enforcement mode
   - Expandable section to add field constraints

2. **Indexing Section**
   - Dropdown for indexing mode
   - Text area or tag input for indexed field paths (when Selective)

---

## Part 7: Postman Collection Updates

### 7.1 Update Existing Requests

**Location:** `Lattice.postman_collection.json`

Update "Create Collection" request to include new fields in example body:

```json
{
    "name": "users",
    "description": "User accounts",
    "schemaEnforcementMode": "Flexible",
    "fieldConstraints": [
        {
            "fieldPath": "email",
            "dataType": "string",
            "required": true,
            "regexPattern": "^[\\w\\.-]+@[\\w\\.-]+\\.\\w+$"
        },
        {
            "fieldPath": "age",
            "dataType": "integer",
            "required": false,
            "minValue": 0,
            "maxValue": 150
        }
    ],
    "indexingMode": "Selective",
    "indexedFields": ["email", "username", "createdAt"]
}
```

### 7.2 New Requests

Add new folder "Collection Constraints & Indexing" with these requests:

#### Get Collection Constraints
```
GET {{baseUrl}}/v1.0/collections/{{collectionId}}/constraints
```

#### Update Collection Constraints
```
PUT {{baseUrl}}/v1.0/collections/{{collectionId}}/constraints
Content-Type: application/json

{
    "schemaEnforcementMode": "Strict",
    "fieldConstraints": [
        {
            "fieldPath": "email",
            "dataType": "string",
            "required": true,
            "regexPattern": "^[\\w\\.-]+@[\\w\\.-]+\\.\\w+$"
        }
    ]
}
```

#### Get Collection Indexing
```
GET {{baseUrl}}/v1.0/collections/{{collectionId}}/indexes
```

#### Update Collection Indexing
```
PUT {{baseUrl}}/v1.0/collections/{{collectionId}}/indexes
Content-Type: application/json

{
    "indexingMode": "Selective",
    "indexedFields": ["email", "username", "status", "createdAt"]
}
```

#### Rebuild Indexes
```
POST {{baseUrl}}/v1.0/collections/{{collectionId}}/indexes/rebuild
Content-Type: application/json

{
    "dropUnusedIndexes": true
}
```

#### Test Schema Validation - Valid Document
```
PUT {{baseUrl}}/v1.0/collections/{{collectionId}}/documents
Content-Type: application/json

{
    "name": "valid-user",
    "json": {
        "email": "user@example.com",
        "age": 25
    }
}
```

#### Test Schema Validation - Invalid Document (Missing Required)
```
PUT {{baseUrl}}/v1.0/collections/{{collectionId}}/documents
Content-Type: application/json

{
    "name": "invalid-user",
    "json": {
        "age": 25
    }
}
```
Expected: 400 Bad Request with validation error

#### Test Schema Validation - Invalid Document (Pattern Mismatch)
```
PUT {{baseUrl}}/v1.0/collections/{{collectionId}}/documents
Content-Type: application/json

{
    "name": "invalid-email-user",
    "json": {
        "email": "not-an-email",
        "age": 25
    }
}
```
Expected: 400 Bad Request with PATTERN_MISMATCH error

### 7.3 Postman Test Scripts

Add test scripts to validate responses:

```javascript
// Test for successful constraint update
pm.test("Status code is 200", function () {
    pm.response.to.have.status(200);
});

pm.test("Schema enforcement mode is updated", function () {
    var jsonData = pm.response.json();
    pm.expect(jsonData.schemaEnforcementMode).to.eql("Strict");
});

// Test for validation failure
pm.test("Status code is 400 for invalid document", function () {
    pm.response.to.have.status(400);
});

pm.test("Error contains validation details", function () {
    var jsonData = pm.response.json();
    pm.expect(jsonData.errors).to.be.an('array');
    pm.expect(jsonData.errors[0].errorCode).to.exist;
});

// Test for index rebuild
pm.test("Rebuild completed successfully", function () {
    var jsonData = pm.response.json();
    pm.expect(jsonData.success).to.be.true;
    pm.expect(jsonData.documentsProcessed).to.be.at.least(0);
});
```

---

## Part 8: Usage Examples

### 8.1 Creating a Collection with Schema Constraint

```csharp
var collection = await client.Collection.Create(
    name: "users",
    schemaEnforcementMode: SchemaEnforcementMode.Flexible,
    fieldConstraints: new List<FieldConstraint>
    {
        new FieldConstraint
        {
            FieldPath = "email",
            DataType = "string",
            Required = true,
            RegexPattern = @"^[\w\.-]+@[\w\.-]+\.\w+$"
        },
        new FieldConstraint
        {
            FieldPath = "age",
            DataType = "integer",
            Required = false,
            MinValue = 0,
            MaxValue = 150
        },
        new FieldConstraint
        {
            FieldPath = "status",
            DataType = "string",
            Required = true,
            AllowedValues = new List<string> { "active", "inactive", "pending" }
        }
    });
```

### 8.2 Creating a Collection with Selective Indexing

```csharp
var collection = await client.Collection.Create(
    name: "orders",
    indexingMode: IndexingMode.Selective,
    indexedFields: new List<string>
    {
        "orderId",
        "customerId",
        "status",
        "createdAt",
        "items.productId"
    });
```

### 8.3 Updating and Rebuilding

```csharp
// Change indexed fields
await client.Collection.UpdateIndexing(
    collectionId: "col_abc123",
    indexingMode: IndexingMode.Selective,
    indexedFields: new List<string> { "email", "username", "lastLogin" });

// Rebuild indexes with progress reporting
var progress = new Progress<IndexRebuildProgress>(p =>
{
    Console.WriteLine($"Rebuild: {p.CurrentPhase} - {p.PercentComplete:F1}%");
});

var result = await client.Collection.RebuildIndexes(
    collectionId: "col_abc123",
    dropUnusedIndexes: true,
    progress: progress);

Console.WriteLine($"Rebuilt {result.DocumentsProcessed} documents in {result.Duration.TotalSeconds}s");
```

---

## Part 9: Edge Cases and Considerations

### 9.1 Schema Validation Edge Cases

1. **Nested object validation**: The validator must correctly traverse nested objects using dot notation
2. **Array element validation**: When `ArrayElementType` is specified, all elements must match
3. **Null handling**: Distinguish between missing field and field with null value
4. **Type coercion**: Do NOT accept `"123"` for integer fields - require strict types
5. **Unicode in regex**: Ensure regex patterns handle Unicode correctly

### 9.2 Indexing Edge Cases

1. **Wildcard patterns**: NOT supported in initial implementation - require explicit field paths
2. **Array position indexing**: Use existing flattening behavior (`items.name` for all array elements)
3. **Mixed content collections**: Handle documents with different schemas gracefully
4. **Large arrays**: Existing behavior applies - no special limits

### 9.3 Index Rebuild Considerations

1. **Concurrent access**: Warn users that searches may return incomplete results during rebuild
2. **Large collections**: Process in batches of 1000 documents to manage memory
3. **Disk space**: Warn that rebuild may temporarily increase disk usage
4. **Atomicity**: Each document is processed independently - partial failures don't roll back

### 9.4 Performance Considerations

1. **Constraint caching**: Cache FieldConstraints per collection in memory (invalidate on update)
2. **IndexedFields caching**: Cache IndexedFields per collection in memory (invalidate on update)
3. **Batch validation**: For bulk imports, load constraints once and reuse
4. **Async rebuild**: Use `IProgress<T>` for progress reporting without blocking

---

## Part 10: Migration Strategy

For existing databases:

1. **Schema migration**: Add new columns with ALTER TABLE (SQLite supports this)
2. **Default values**: All existing collections get `SchemaEnforcementMode.None` and `IndexingMode.All`
3. **No breaking changes**: All existing APIs continue to work unchanged
4. **Opt-in features**: Schema validation and selective indexing only apply when explicitly configured

---

## Part 11: Files to Create/Modify Summary

### New Files (12)
| File | Location |
|------|----------|
| `SchemaEnforcementMode.cs` | `src/Lattice.Core/Models/` |
| `IndexingMode.cs` | `src/Lattice.Core/Models/` |
| `FieldConstraint.cs` | `src/Lattice.Core/Models/` |
| `IndexedField.cs` | `src/Lattice.Core/Models/` |
| `IndexRebuildResult.cs` | `src/Lattice.Core/Models/` |
| `IndexRebuildProgress.cs` | `src/Lattice.Core/Models/` |
| `ValidationResult.cs` | `src/Lattice.Core/Validation/` |
| `SchemaValidator.cs` | `src/Lattice.Core/Validation/` |
| `SchemaValidationException.cs` | `src/Lattice.Core/Exceptions/` |
| `IFieldConstraintMethods.cs` | `src/Lattice.Core/Repositories/Interfaces/` |
| `FieldConstraintMethods.cs` | `src/Lattice.Core/Repositories/Sqlite/Implementations/` |
| `IIndexedFieldMethods.cs` | `src/Lattice.Core/Repositories/Interfaces/` |
| `IndexedFieldMethods.cs` | `src/Lattice.Core/Repositories/Sqlite/Implementations/` |

### Modified Files (10)
| File | Changes |
|------|---------|
| `Collection.cs` | Add `SchemaEnforcementMode` and `IndexingMode` properties |
| `SetupQueries.cs` | Add new columns and tables |
| `IdGenerator.cs` | Add `fco_` and `ixf_` prefixes |
| `Converters.cs` | Add converters for new models |
| `SqliteRepository.cs` | Add new method interfaces |
| `CollectionMethods.cs` | Handle new columns in CRUD |
| `IndexMethods.cs` | Add helper methods for rebuild |
| `LatticeClient.cs` | Update CreateCollection, IngestDocument, add new methods |
| `CreateCollectionRequest.cs` | Add new request properties |
| `RestServiceHandler.cs` | Add new endpoints |

### Test Files (3)
| File | Location |
|------|----------|
| `SchemaValidatorTests.cs` | `src/Test.Automated/` |
| `FieldConstraintMethodsTests.cs` | `src/Test.Automated/` |
| `CollectionConstraintIntegrationTests.cs` | `src/Test.Automated/` |

### Dashboard Files (4)
| File | Location |
|------|----------|
| `SchemaConstraintsPanel.tsx` | `dashboard/src/components/` |
| `IndexingConfigPanel.tsx` | `dashboard/src/components/` |
| `IndexRebuildDialog.tsx` | `dashboard/src/components/` |
| `types.ts` | `dashboard/src/` (update) |

### Other Files (1)
| File | Changes |
|------|---------|
| `Lattice.postman_collection.json` | Add new requests and examples |

---

## Part 12: Future Enhancements (Out of Scope)

These are explicitly NOT part of this implementation:

1. **Schema versioning**: Track schema changes over time
2. **Schema migration tools**: Automatically update documents to new schema
3. **Composite indexes**: Index multiple fields together
4. **Full-text search indexes**: Specialized text search indexing
5. **Geospatial indexes**: Location-based indexing
6. **TTL indexes**: Automatic document expiration
7. **Unique constraints**: Ensure field uniqueness across collection
8. **Foreign key constraints**: Reference validation across collections
9. **Wildcard index patterns**: e.g., `items.*` to index all nested fields
