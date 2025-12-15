# Lattice Query DSL (SQL-like Interface)

Lattice provides a SQL-like Domain Specific Language (DSL) for querying documents. This interface allows users familiar with SQL to search documents using intuitive syntax.

## Overview

The DSL supports a subset of SQL-like syntax designed specifically for JSON document queries:

```sql
SELECT * FROM documents WHERE field = 'value' ORDER BY createdutc DESC LIMIT 10 OFFSET 0
```

## Syntax Reference

### Basic Structure

```
SELECT * FROM documents [WHERE conditions] [ORDER BY field [ASC|DESC]] [LIMIT n] [OFFSET n]
```

**Note:** The `SELECT * FROM documents` prefix is optional in the API. You can provide just the WHERE clause and beyond.

---

## WHERE Clause

The WHERE clause filters documents based on field values. Fields are referenced using dot-notation for nested JSON properties.

### Comparison Operators

| Operator | Description | Example |
|----------|-------------|---------|
| `=` | Equals | `Person.First = 'Joel'` |
| `!=` | Not equals | `Status != 'deleted'` |
| `<>` | Not equals (alternate) | `Status <> 'deleted'` |
| `>` | Greater than | `Age > 30` |
| `>=` | Greater than or equal | `Age >= 30` |
| `<` | Less than | `Price < 100` |
| `<=` | Less than or equal | `Price <= 100` |

### String Pattern Matching

| Operator | Description | Example |
|----------|-------------|---------|
| `LIKE '%value%'` | Contains | `Name LIKE '%Smith%'` |
| `LIKE 'value%'` | Starts with | `Name LIKE 'John%'` |
| `LIKE '%value'` | Ends with | `Email LIKE '%@gmail.com'` |
| `LIKE 'value'` | Exact match | `Code LIKE 'ABC123'` |

### NULL Checks

| Operator | Description | Example |
|----------|-------------|---------|
| `IS NULL` | Field is null or missing | `MiddleName IS NULL` |
| `IS NOT NULL` | Field exists and has value | `Email IS NOT NULL` |

### Logical Operators

| Operator | Description | Example |
|----------|-------------|---------|
| `AND` | Both conditions must match | `Age > 21 AND Status = 'active'` |

**Note:** The current implementation supports `AND` logic. All conditions must be satisfied for a document to match.

---

## ORDER BY Clause

Sort results by a supported field:

```sql
ORDER BY field [ASC|DESC]
```

### Supported Sort Fields

| Field | Description |
|-------|-------------|
| `createdutc` | Document creation timestamp |
| `lastupdateutc` | Last modification timestamp |
| `name` | Document name |

### Examples

```sql
-- Newest documents first (default)
ORDER BY createdutc DESC

-- Oldest documents first
ORDER BY createdutc ASC

-- Alphabetical by name
ORDER BY name ASC

-- Recently updated first
ORDER BY lastupdateutc DESC
```

---

## LIMIT and OFFSET Clauses

Control pagination of results:

```sql
LIMIT n        -- Return at most n documents
OFFSET n       -- Skip the first n documents
```

### Examples

```sql
-- First 10 results
LIMIT 10

-- Results 11-20 (page 2)
LIMIT 10 OFFSET 10

-- Results 21-30 (page 3)
LIMIT 10 OFFSET 20
```

---

## Field References (Dot Notation)

Fields in nested JSON documents are referenced using dot notation:

### JSON Document
```json
{
  "Person": {
    "First": "Joel",
    "Last": "Christner",
    "Contact": {
      "Email": "joel@example.com"
    }
  },
  "Tags": ["developer", "admin"]
}
```

### Field References
| JSON Path | DSL Reference |
|-----------|---------------|
| `Person.First` | `Person.First` |
| `Person.Contact.Email` | `Person.Contact.Email` |
| `Tags[0]` | `Tags` (matches any array element) |

---

## Complete Examples

### Example 1: Simple Equality

```sql
SELECT * FROM documents WHERE Person.First = 'Joel'
```

Finds all documents where the `Person.First` field equals "Joel".

### Example 2: Multiple Conditions

```sql
SELECT * FROM documents
WHERE Person.First = 'Joel'
AND Status = 'active'
AND Age >= 21
```

Finds documents matching all three conditions.

### Example 3: Pattern Matching

```sql
SELECT * FROM documents
WHERE Email LIKE '%@company.com'
AND Department LIKE 'Engineering%'
```

Finds documents with company emails in Engineering departments.

### Example 4: NULL Handling

```sql
SELECT * FROM documents
WHERE Manager IS NOT NULL
AND TerminationDate IS NULL
```

Finds active employees with assigned managers.

### Example 5: Sorted and Paginated

```sql
SELECT * FROM documents
WHERE Category = 'electronics'
ORDER BY createdutc DESC
LIMIT 20
OFFSET 40
```

Gets page 3 (20 items per page) of electronics documents, newest first.

### Example 6: Comparison Operators

```sql
SELECT * FROM documents
WHERE Price >= 100
AND Price <= 500
AND Rating > 4
```

Finds mid-range products with good ratings.

---

## API Usage

### REST API

**Endpoint:** `POST /v1.0/collections/{collectionId}/documents/search`

**Request Body (SQL Expression):**
```json
{
  "sqlExpression": "SELECT * FROM documents WHERE Person.First = 'Joel' ORDER BY createdutc DESC LIMIT 10"
}
```

**Request Body (Structured Filters):**
```json
{
  "filters": [
    { "field": "Person.First", "condition": "Equals", "value": "Joel" }
  ],
  "ordering": "CreatedDescending",
  "maxResults": 10,
  "includeContent": false,
  "includeLabels": true,
  "includeTags": true
}
```

### C# Client

```csharp
// Using SQL expression
var result = await client.Search.SearchBySql(
    collectionId: "col_abc123",
    sql: "SELECT * FROM documents WHERE Person.First = 'Joel'"
);

// Using structured query
var query = new SearchQuery
{
    CollectionId = "col_abc123",
    Filters = new List<SearchFilter>
    {
        new SearchFilter("Person.First", SearchConditionEnum.Equals, "Joel")
    },
    Ordering = EnumerationOrderEnum.CreatedDescending,
    MaxResults = 10,
    IncludeContent = false,  // Include raw JSON content (default: false)
    IncludeLabels = true,    // Include document labels (default: true)
    IncludeTags = true       // Include document tags (default: true)
};
var result = await client.Search.Search(query);

// Performance optimization: exclude labels/tags when not needed
var fastQuery = new SearchQuery
{
    CollectionId = "col_abc123",
    MaxResults = 100,
    IncludeLabels = false,   // Skip loading labels for faster queries
    IncludeTags = false      // Skip loading tags for faster queries
};
var fastResult = await client.Search.Search(fastQuery);
```

### Enumeration

```csharp
// Enumerate documents in a collection
var enumQuery = new EnumerationQuery
{
    CollectionId = "col_abc123",
    MaxResults = 100,
    Ordering = EnumerationOrderEnum.CreatedDescending,
    IncludeLabels = true,    // Include document labels (default: true)
    IncludeTags = true       // Include document tags (default: true)
};
var enumResult = await client.Search.Enumerate(enumQuery);

// Performance optimization: enumerate without labels/tags
var fastEnumQuery = new EnumerationQuery
{
    CollectionId = "col_abc123",
    MaxResults = 1000,
    IncludeLabels = false,
    IncludeTags = false
};
var fastEnumResult = await client.Search.Enumerate(fastEnumQuery);
```

### Document Retrieval

```csharp
// Get a single document with all data
var doc = await client.Document.ReadById(
    id: "doc_abc123",
    includeContent: true,    // Include raw JSON content (default: false)
    includeLabels: true,     // Include document labels (default: true)
    includeTags: true        // Include document tags (default: true)
);

// Performance optimization: get document metadata only
var fastDoc = await client.Document.ReadById(
    id: "doc_abc123",
    includeContent: false,
    includeLabels: false,
    includeTags: false
);

// Get all documents in a collection
var docs = await client.Document.ReadAllInCollection(
    collectionId: "col_abc123",
    includeLabels: true,     // Include document labels (default: true)
    includeTags: true        // Include document tags (default: true)
);
```

---

## Performance Parameters

The following parameters control what data is included in query results. Excluding unnecessary data can significantly improve query performance.

| Parameter | Default | Description |
|-----------|---------|-------------|
| `includeContent` | `false` | Include raw JSON document content in results |
| `includeLabels` | `true` | Include document labels in results |
| `includeTags` | `true` | Include document tags in results |

### Performance Impact

Setting `includeLabels` and `includeTags` to `false` eliminates additional database queries per document, providing significant speedups:

| Operation | With Labels/Tags | Without | Speedup |
|-----------|-----------------|---------|---------|
| GetDocument (1000 docs) | ~210 ops/sec | ~1,040 ops/sec | **5x** |
| GetDocuments bulk (5000) | ~157ms | ~57ms | **2.8x** |
| Enumerate (5000 docs) | ~13.5ms | ~5.0ms | **2.7x** |

**Best Practice:** Set `includeLabels` and `includeTags` to `false` when you only need document metadata without labels or tags.

---

## Search Conditions Reference

The following search conditions are available:

| Enum Value | SQL Equivalent | Description |
|------------|----------------|-------------|
| `Equals` | `=` | Exact match |
| `NotEquals` | `!=`, `<>` | Not equal |
| `GreaterThan` | `>` | Greater than |
| `GreaterThanOrEqualTo` | `>=` | Greater than or equal |
| `LessThan` | `<` | Less than |
| `LessThanOrEqualTo` | `<=` | Less than or equal |
| `IsNull` | `IS NULL` | Field is null/missing |
| `IsNotNull` | `IS NOT NULL` | Field has a value |
| `Contains` | `LIKE '%x%'` | Contains substring |
| `StartsWith` | `LIKE 'x%'` | Starts with |
| `EndsWith` | `LIKE '%x'` | Ends with |
| `Like` | `LIKE 'x'` | Pattern match |

---

## How Queries Are Executed

1. **Parse SQL**: The `SqlParser` converts the SQL string into a `SearchQuery` object
2. **Filter Processing**: Each filter is applied to the corresponding index table
3. **Intersection**: Results from multiple filters are intersected (AND logic)
4. **Collection Filter**: Results are filtered to the specified collection
5. **Ordering**: Documents are sorted according to the ORDER BY clause
6. **Pagination**: OFFSET and LIMIT are applied
7. **Document Loading**: Full documents are retrieved for matching IDs

### Index Table Queries

For a filter like `Person.First = 'Joel'`:

1. Look up `Person.First` in `indextablemappings` to get the table name
2. Query the index table:
   ```sql
   SELECT DISTINCT documentid
   FROM index_{hash}
   WHERE value = @value
   ```
3. Return the set of matching document IDs

---

## Limitations

1. **AND only**: Currently, only AND logic is supported (no OR)
2. **No subqueries**: Nested queries are not supported
3. **No JOINs**: Documents are queried independently
4. **No aggregations**: COUNT, SUM, AVG, etc. are not available
5. **No GROUP BY**: Grouping is not supported
6. **Limited ORDER BY**: Only `createdutc`, `lastupdateutc`, and `name` fields
7. **String comparison**: All values are stored as strings; numeric comparisons are lexicographic

---

## Best Practices

1. **Use specific fields**: Query on indexed fields for best performance
2. **Limit results**: Always use LIMIT to avoid retrieving large result sets
3. **Prefer equality**: Equality checks are faster than range or pattern queries
4. **Combine filters**: Multiple filters narrow results quickly via intersection
5. **Index key fields**: Ensure commonly queried fields are in your JSON structure
