# Lattice SDK for Python

A comprehensive REST SDK for consuming a Lattice server.

## Requirements

- Python 3.8 or higher
- `requests` library

## Installation

```bash
cd sdk/sdk-python
pip install -r requirements.txt
```

Or install in development mode:

```bash
pip install -e .
```

## Quick Start

```python
from lattice_sdk import LatticeClient, SearchQuery, SearchFilter, SearchCondition

# Create a client
client = LatticeClient("http://localhost:8000")

# Check server health
if client.health_check():
    print("Server is healthy")

# Create a collection
collection = client.collection.create(
    name="my_collection",
    description="Test collection"
)
print(f"Created collection: {collection.id}")

# Ingest a document
document = client.document.ingest(
    collection_id=collection.id,
    content={"name": "John Doe", "age": 30, "department": "Engineering"},
    name="person_1",
    labels=["person", "employee"],
    tags={"status": "active"}
)
print(f"Created document: {document.id}")

# Search for documents
query = SearchQuery(
    collection_id=collection.id,
    filters=[SearchFilter("age", SearchCondition.GREATER_THAN, "25")],
    max_results=10
)
result = client.search.search(query)

print(f"Found {result.total_records} documents:")
for doc in result.documents:
    print(f"  - {doc.name} ({doc.id})")

# Clean up
client.collection.delete(collection.id)
```

## Running the Test Harness

The test harness provides comprehensive testing of all SDK functionality against a running Lattice server.

### Usage

```bash
python test_harness.py <endpoint_url>
```

### Example

```bash
python test_harness.py http://localhost:8000
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
  Lattice SDK Test Harness - Python
===============================================================================

  Endpoint: http://localhost:8000

--- HEALTH CHECK ---
  [PASS] Health check returns true (15ms)

--- COLLECTION API ---
  [PASS] CreateCollection: basic (45ms)
  [PASS] CreateCollection: with all parameters (52ms)
  ...

===============================================================================
  TEST SUMMARY
===============================================================================

  HEALTH CHECK: 1/1 [PASS]
  COLLECTION API: 9/9 [PASS]
  ...

-------------------------------------------------------------------------------
  TOTAL: 57 passed, 0 failed [PASS]
  RUNTIME: 3500ms (3.50s)
-------------------------------------------------------------------------------
```

## API Reference

### LatticeClient

The main client class. Initialize with the server endpoint:

```python
client = LatticeClient("http://localhost:8000", timeout=30)
```

**Parameters:**
- `base_url` (str): The Lattice server URL
- `timeout` (int): Request timeout in seconds (default: 30)

### Collection Methods (`client.collection`)

| Method | Description |
|--------|-------------|
| `create(name, ...)` | Create a new collection |
| `read_all()` | Get all collections |
| `read_by_id(id)` | Get a collection by ID |
| `exists(id)` | Check if a collection exists |
| `delete(id)` | Delete a collection |
| `get_constraints(id)` | Get field constraints |
| `update_constraints(id, mode, constraints)` | Update constraints |
| `get_indexed_fields(id)` | Get indexed fields |
| `update_indexing(id, mode, fields)` | Update indexing configuration |
| `rebuild_indexes(id)` | Rebuild collection indexes |

### Document Methods (`client.document`)

| Method | Description |
|--------|-------------|
| `ingest(collection_id, content, ...)` | Ingest a document |
| `read_all_in_collection(collection_id, ...)` | Get all documents in a collection |
| `read_by_id(id, ...)` | Get a document by ID |
| `exists(id)` | Check if a document exists |
| `delete(id)` | Delete a document |

### Search Methods (`client.search`)

| Method | Description |
|--------|-------------|
| `search(query)` | Execute a structured search |
| `search_by_sql(collection_id, expression)` | Execute a SQL-like search |
| `enumerate(query)` | Enumerate documents |

### Schema Methods (`client.schema`)

| Method | Description |
|--------|-------------|
| `read_all()` | Get all schemas |
| `read_by_id(id)` | Get a schema by ID |
| `get_elements(schema_id)` | Get schema elements |

### Index Methods (`client.index`)

| Method | Description |
|--------|-------------|
| `get_mappings()` | Get index table mappings |

## Models

### Enums

All enums are serialized as lowercase strings in JSON.

- `SchemaEnforcementMode`: NONE (`"none"`), STRICT (`"strict"`), FLEXIBLE (`"flexible"`), PARTIAL (`"partial"`)
- `IndexingMode`: ALL (`"all"`), SELECTIVE (`"selective"`), NONE (`"none"`)
- `SearchCondition`: EQUALS, NOT_EQUALS, GREATER_THAN, LESS_THAN, CONTAINS, STARTS_WITH, ENDS_WITH, IS_NULL, IS_NOT_NULL, LIKE
- `EnumerationOrder`: CREATED_ASCENDING, CREATED_DESCENDING, LAST_UPDATE_ASCENDING, LAST_UPDATE_DESCENDING, NAME_ASCENDING, NAME_DESCENDING

### Data Classes

- `Collection`, `Document`, `Schema`, `SchemaElement`
- `FieldConstraint`, `IndexedField`
- `SearchFilter`, `SearchQuery`, `SearchResult`
- `IndexRebuildResult`, `ResponseContext`, `IndexTableMapping`

## Exception Handling

```python
from lattice_sdk import LatticeException, LatticeConnectionError, LatticeApiError

try:
    result = client.search.search(query)
except LatticeConnectionError as e:
    print(f"Connection failed: {e}")
except LatticeApiError as e:
    print(f"API error ({e.status_code}): {e.error_message}")
except LatticeException as e:
    print(f"Error: {e}")
```
