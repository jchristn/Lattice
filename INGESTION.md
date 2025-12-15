# Lattice Document Ingestion Pipeline

This document describes the complete process that occurs when a JSON document is ingested into Lattice, including all database operations and file system writes.

## Overview

When a document is ingested via `LatticeClient.Document.Ingest()`, Lattice performs the following high-level operations:

1. Validate the collection exists
2. Generate or find an existing schema
3. Flatten the JSON document
4. Store document metadata
5. Create/update index tables
6. Index all document values
7. Persist the raw JSON to disk

## Detailed Ingestion Steps

### Step 1: Collection Validation

```csharp
var collection = await _Repo.Collections.ReadById(collectionId, token);
if (collection == null)
    throw new ArgumentException($"Collection {collectionId} not found");
```

**Table Accessed:** `collections`

The ingestion process first verifies that the target collection exists. If not, an exception is thrown.

---

### Step 2: Schema Generation

```csharp
var schemaElements = _SchemaGenerator.ExtractElements(json);
var schemaHash = _SchemaGenerator.ComputeSchemaHash(schemaElements);
var existingSchema = await _Repo.Schemas.ReadByHash(schemaHash, token);
```

**Tables Accessed:** `schemas`, `schemaelements`

Lattice analyzes the JSON structure to extract all keys and their data types:

#### 2a. Extract Schema Elements

The `SchemaGenerator` recursively traverses the JSON and produces a list of `SchemaElement` objects:

| JSON Input | Extracted Key | Data Type |
|------------|---------------|-----------|
| `{"name": "Joel"}` | `name` | `string` |
| `{"age": 30}` | `age` | `integer` |
| `{"active": true}` | `active` | `boolean` |
| `{"person": {"first": "Joel"}}` | `person.first` | `string` |
| `{"tags": ["a", "b"]}` | `tags` | `array<string>` |

#### 2b. Compute Schema Hash

A deterministic SHA256 hash is computed from all schema elements (sorted by key) to enable schema deduplication:

```
Hash = SHA256(key1:type1;key2:type2;...)
```

#### 2c. Find or Create Schema

**If schema exists (same hash):**
- Reuse the existing schema record
- No new schema elements created

**If schema is new:**

**Table:** `schemas`
| Column | Value |
|--------|-------|
| id | `sch_` + K-sortable ID |
| hash | Computed SHA256 hash |
| createdutc | Current UTC timestamp |

**Table:** `schemaelements` (one row per unique key)
| Column | Value |
|--------|-------|
| id | `sel_` + K-sortable ID |
| schemaid | Parent schema ID |
| position | Ordinal position (0, 1, 2...) |
| key | Dot-notation key (e.g., `person.first`) |
| datatype | `string`, `integer`, `number`, `boolean`, `null`, `array<T>` |
| nullable | Whether null values were observed |
| createdutc | Current UTC timestamp |

---

### Step 3: Index Table Creation

```csharp
foreach (var element in schemaElements)
{
    var mapping = await _Repo.Indexes.GetMappingByKey(element.Key, token);
    if (mapping == null)
    {
        var tableName = HashHelper.GenerateIndexTableName(element.Key);
        mapping = new IndexTableMapping { ... };
        await _Repo.Indexes.CreateMapping(mapping, token);
        await _Repo.Indexes.CreateIndexTable(tableName, token);
    }
}
```

**Tables Accessed:** `indextablemappings`, dynamically created `index_*` tables

For each unique key in the schema, Lattice ensures an index table exists:

#### 3a. Index Table Mapping

**Table:** `indextablemappings`
| Column | Value |
|--------|-------|
| id | `itm_` + K-sortable ID |
| key | Original key name (e.g., `person.first`) |
| tablename | `idx_` + MD5 hash of key |
| createdutc | Current UTC timestamp |

Example mappings:
| Key | Table Name |
|-----|------------|
| `person.first` | `idx_a1b2c3d4e5f6...` |
| `person.last` | `idx_f6e5d4c3b2a1...` |
| `age` | `idx_9876543210ab...` |

#### 3b. Dynamic Index Table

For each new key, a dedicated index table is created:

```sql
CREATE TABLE idx_{hash} (
    id TEXT PRIMARY KEY,
    documentid TEXT NOT NULL,
    position INTEGER,          -- Array index (NULL for non-array values)
    value TEXT,                -- The actual value (as string)
    createdutc TEXT NOT NULL
)
```

This one-table-per-key design enables:
- Fast equality and range lookups
- Efficient storage (only non-null values stored)
- Independent indexing per field

---

### Step 4: Document Record Creation

```csharp
var document = new Document
{
    Id = IdGenerator.NewDocumentId(),
    CollectionId = collectionId,
    SchemaId = schema.Id,
    Name = name,
    Labels = labels ?? new List<string>(),
    Tags = tags ?? new Dictionary<string, string>()
};
document = await _Repo.Documents.Create(document, token);
```

**Table:** `documents`
| Column | Value |
|--------|-------|
| id | `doc_` + K-sortable ID |
| collectionid | Parent collection ID |
| schemaid | Associated schema ID |
| name | Optional document name |
| createdutc | Current UTC timestamp |
| lastupdateutc | Current UTC timestamp |

---

### Step 5: Labels and Tags

If the document has labels or tags, they are stored in separate tables:

**Table:** `labels` (one row per label)
| Column | Value |
|--------|-------|
| id | `lbl_` + K-sortable ID |
| documentid | Parent document ID |
| labelvalue | The label string |
| createdutc | Current UTC timestamp |

**Table:** `tags` (one row per key-value pair, unified for collections and documents)
| Column | Value |
|--------|-------|
| id | `tag_` + K-sortable ID |
| collectionid | Parent collection ID (NULL for document tags) |
| documentid | Parent document ID (NULL for collection tags) |
| key | Tag key |
| value | Tag value |
| createdutc | Current UTC timestamp |

---

### Step 6: JSON Flattening and Value Indexing

```csharp
var flattenedValues = _JsonFlattener.Flatten(json);
var valuesByKey = flattenedValues.GroupBy(v => v.Key);

foreach (var group in valuesByKey)
{
    var mapping = await _Repo.Indexes.GetMappingByKey(group.Key, token);
    var values = group.Select(v => new DocumentValue { ... }).ToList();
    await _Repo.Indexes.InsertValues(mapping.TableName, values, token);
}
```

**Tables Accessed:** All relevant `index_*` tables

#### 6a. JSON Flattening

The `JsonFlattener` converts nested JSON into flat key-value pairs (preserving original casing):

**Input:**
```json
{
  "Person": {
    "First": "Joel",
    "Last": "Christner",
    "Addresses": [
      {"City": "San Jose"},
      {"City": "Austin"}
    ]
  },
  "Age": 47
}
```

**Output (FlattenedValue objects):**
| Key | Position | Value | DataType |
|-----|----------|-------|----------|
| `Person.First` | null | `Joel` | string |
| `Person.Last` | null | `Christner` | string |
| `Person.Addresses.City` | 0 | `San Jose` | string |
| `Person.Addresses.City` | 1 | `Austin` | string |
| `Age` | null | `47` | integer |

#### 6b. Index Table Inserts

For each flattened value, a row is inserted into the appropriate index table:

**Table:** `idx_{hash_of_Person.First}`
| id | documentid | position | value | createdutc |
|----|------------|----------|-------|------------|
| val_xxx | doc_abc123 | NULL | Joel | 2024-01-15T... |

**Table:** `idx_{hash_of_Person.Addresses.City}`
| id | documentid | position | value | createdutc |
|----|------------|----------|-------|------------|
| val_yyy | doc_abc123 | 0 | San Jose | 2024-01-15T... |
| val_zzz | doc_abc123 | 1 | Austin | 2024-01-15T... |

---

### Step 7: Raw JSON Persistence

```csharp
string documentPath = Path.Combine(collection.DocumentsDirectory, $"{document.Id}.json");
await File.WriteAllTextAsync(documentPath, json, token);
```

**File System:** `{collection.DocumentsDirectory}/{document.Id}.json`

The original JSON document is saved to disk with the document ID as the filename. This allows:
- Full document retrieval without reconstruction
- Preservation of original formatting
- Efficient storage (database only holds indexed values)

---

## Complete Asset Summary

For a single document ingestion, the following assets may be created:

### Database Tables

| Table | Records Created | Condition |
|-------|-----------------|-----------|
| `schemas` | 0-1 | Only if new schema |
| `schemaelements` | 0-N | Only if new schema (N = unique keys) |
| `indextablemappings` | 0-N | Only for new keys |
| `idx_*` (new tables) | 0-N | Only for new keys |
| `documents` | 1 | Always |
| `labels` | 0-N | If labels provided |
| `tags` | 0-N | If tags provided |
| `idx_*` (value inserts) | N | Always (N = flattened values) |

### File System

| Path | Content |
|------|---------|
| `{documentsDirectory}/{documentId}.json` | Raw JSON document |

---

## Example: Full Ingestion Trace

**Input:**
```csharp
await client.Document.Ingest(
    collectionId: "col_abc123",
    json: """{"Person": {"First": "Joel"}, "Active": true}""",
    name: "Joel's Record",
    labels: new List<string> { "employee" },
    tags: new Dictionary<string, string> { { "dept", "engineering" } }
);
```

**Database Operations:**

1. SELECT from `collections` WHERE id = 'col_abc123'
2. Compute schema hash for keys: `Person.First`, `Active`
3. SELECT from `schemas` WHERE hash = '{computed_hash}'
4. (If new schema) INSERT into `schemas`
5. (If new schema) INSERT into `schemaelements` (2 rows)
6. SELECT from `indextablemappings` WHERE key = 'Person.First'
7. (If new key) INSERT into `indextablemappings`
8. (If new key) CREATE TABLE `idx_{hash}`
9. Repeat steps 6-8 for 'Active'
10. INSERT into `documents`
11. INSERT into `labels` (1 row)
12. INSERT into `tags` (1 row)
13. INSERT into `idx_{Person.First_hash}` (1 row)
14. INSERT into `idx_{Active_hash}` (1 row)

**File System:**
```
{documentsDirectory}/doc_xyz789.json
```

Content:
```json
{"Person": {"First": "Joel"}, "Active": true}
```
