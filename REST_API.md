# Lattice REST API Reference

Comprehensive reference for the Lattice Server REST API. Lattice is a JSON document store with automatic schema detection, full-text indexing, and flexible search capabilities.

---

## Table of Contents

- [Base URL](#base-url)
- [Authentication](#authentication)
- [Response Format](#response-format)
- [Error Handling](#error-handling)
- [CORS](#cors)
- [Endpoints](#endpoints)
  - [Health](#health)
    - [GET / -- Root Health Check](#get----root-health-check)
    - [GET /v1.0/health -- Versioned Health Check](#get-v10health--versioned-health-check)
  - [Collections](#collections)
    - [PUT /v1.0/collections -- Create Collection](#put-v10collections--create-collection)
    - [GET /v1.0/collections -- List Collections](#get-v10collections--list-collections)
    - [GET /v1.0/collections/{collectionId} -- Get Collection](#get-v10collectionscollectionid--get-collection)
    - [HEAD /v1.0/collections/{collectionId} -- Check Collection Exists](#head-v10collectionscollectionid--check-collection-exists)
    - [DELETE /v1.0/collections/{collectionId} -- Delete Collection](#delete-v10collectionscollectionid--delete-collection)
    - [GET /v1.0/collections/{collectionId}/constraints -- Get Constraints](#get-v10collectionscollectionidconstraints--get-constraints)
    - [PUT /v1.0/collections/{collectionId}/constraints -- Update Constraints](#put-v10collectionscollectionidconstraints--update-constraints)
    - [GET /v1.0/collections/{collectionId}/indexing -- Get Indexing Config](#get-v10collectionscollectionidindexing--get-indexing-config)
    - [PUT /v1.0/collections/{collectionId}/indexing -- Update Indexing Config](#put-v10collectionscollectionidindexing--update-indexing-config)
    - [POST /v1.0/collections/{collectionId}/indexes/rebuild -- Rebuild Indexes](#post-v10collectionscollectionidindexesrebuild--rebuild-indexes)
  - [Documents](#documents)
    - [GET /v1.0/collections/{collectionId}/documents -- List Documents](#get-v10collectionscollectioniddocuments--list-documents)
    - [PUT /v1.0/collections/{collectionId}/documents -- Create Document](#put-v10collectionscollectioniddocuments--create-document)
    - [PUT /v1.0/collections/{collectionId}/documents/batch -- Batch Ingest Documents](#put-v10collectionscollectioniddocumentsbatch--batch-ingest-documents)
    - [GET /v1.0/collections/{collectionId}/documents/{documentId} -- Get Document](#get-v10collectionscollectioniddocumentsdocumentid--get-document)
    - [HEAD /v1.0/collections/{collectionId}/documents/{documentId} -- Check Document Exists](#head-v10collectionscollectioniddocumentsdocumentid--check-document-exists)
    - [DELETE /v1.0/collections/{collectionId}/documents/{documentId} -- Delete Document](#delete-v10collectionscollectioniddocumentsdocumentid--delete-document)
  - [Search](#search)
    - [POST /v1.0/collections/{collectionId}/documents/search -- Search Documents](#post-v10collectionscollectioniddocumentssearch--search-documents)
  - [Schemas](#schemas)
    - [GET /v1.0/schemas -- List Schemas](#get-v10schemas--list-schemas)
    - [GET /v1.0/schemas/{schemaId} -- Get Schema](#get-v10schemasschemaId--get-schema)
    - [GET /v1.0/schemas/{schemaId}/elements -- Get Schema Elements](#get-v10schemasschemaidelements--get-schema-elements)
  - [Index Tables](#index-tables)
    - [GET /v1.0/tables -- List Index Tables](#get-v10tables--list-index-tables)
    - [GET /v1.0/tables/{tableName}/entries -- Get Index Entries](#get-v10tablestablenameentries--get-index-entries)
- [Data Models](#data-models)
- [Key Behaviors](#key-behaviors)

---

## Base URL

```
http://localhost:8000
```

The hostname and port are configurable in the server settings. SSL/TLS is supported via PFX certificate configuration.

---

## Authentication

The Lattice API does not currently enforce authentication. All endpoints are accessible without credentials.

---

## Response Format

All successful and error responses (except raw content retrieval -- see note below) are wrapped in a standard envelope:

```json
{
  "success": true,
  "statusCode": 200,
  "errorMessage": null,
  "data": { },
  "processingTimeMs": 12.34,
  "guid": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "timestampUtc": "2024-01-15T12:00:00.000Z"
}
```

| Field | Type | Description |
|-------|------|-------------|
| `success` | boolean | Whether the operation completed successfully. |
| `statusCode` | integer | HTTP status code of the response. |
| `errorMessage` | string or null | Error description when `success` is `false`. Null on success. |
| `data` | object, array, or null | The response payload. Varies by endpoint. |
| `processingTimeMs` | number | Server-side processing time in milliseconds. |
| `guid` | string | Unique identifier for this response (UUID v4). |
| `timestampUtc` | string | ISO 8601 UTC timestamp of the response. |

**Exception:** When retrieving a document with `includeContent=true`, the response is the raw JSON document content returned directly, **not** wrapped in the standard envelope.

---

## Error Handling

When an error occurs, the response envelope contains details about the failure:

```json
{
  "success": false,
  "statusCode": 400,
  "errorMessage": "Name is required for collection creation",
  "data": null,
  "processingTimeMs": 1.23,
  "guid": "...",
  "timestampUtc": "..."
}
```

Schema validation errors include structured detail in the `data` field:

```json
{
  "success": false,
  "statusCode": 400,
  "errorMessage": "Schema validation failed",
  "data": {
    "errors": [
      "Field 'email' is required but missing",
      "Field 'age' must be of type integer"
    ]
  }
}
```

### HTTP Status Codes

| Code | Meaning | When Used |
|------|---------|-----------|
| 200 | OK | Successful read, update, or delete operations. |
| 201 | Created | Successful creation of a collection or document. |
| 400 | Bad Request | Missing required fields, invalid JSON, validation errors. |
| 404 | Not Found | Collection, document, schema, or table not found. |
| 409 | Conflict | Object lock held -- concurrent write to the same document name. |
| 500 | Internal Server Error | Unexpected server-side failure. |

---

## CORS

The server includes full CORS support. Preflight (`OPTIONS`) requests are handled automatically with the following headers:

- `Access-Control-Allow-Methods: OPTIONS, HEAD, GET, PUT, POST, DELETE`
- `Access-Control-Allow-Headers: *, Content-Type, X-Requested-With`
- `Access-Control-Allow-Origin: *`

---

## Endpoints

### Health

#### GET / -- Root Health Check

Returns the health status of the Lattice server.

**cURL:**

```bash
curl http://localhost:8000/
```

**Response (200 OK):**

```json
{
  "success": true,
  "statusCode": 200,
  "data": {
    "status": "healthy",
    "version": "1.0.0",
    "timestamp": "2024-01-15T12:00:00.000Z"
  },
  "processingTimeMs": 0.52,
  "guid": "...",
  "timestampUtc": "2024-01-15T12:00:00.000Z"
}
```

---

#### GET /v1.0/health -- Versioned Health Check

Identical to the root health check, available under the versioned API prefix.

**cURL:**

```bash
curl http://localhost:8000/v1.0/health
```

**Response:** Same as `GET /`.

---

### Collections

#### PUT /v1.0/collections -- Create Collection

Creates a new collection with optional schema constraints and indexing configuration.

**cURL:**

```bash
curl -X PUT http://localhost:8000/v1.0/collections \
  -H "Content-Type: application/json" \
  -d '{
    "name": "customers",
    "description": "Customer records",
    "labels": ["production", "crm"],
    "tags": {
      "department": "sales",
      "region": "us-east"
    },
    "schemaEnforcementMode": "none",
    "indexingMode": "all"
  }'
```

**Request Body:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `name` | string | **Yes** | Name of the collection. Must be unique. |
| `description` | string | No | Human-readable description. |
| `documentsDirectory` | string | No | Custom filesystem directory for document storage. |
| `labels` | string[] | No | Array of string labels for categorization. |
| `tags` | object | No | Key-value string pairs for metadata. |
| `schemaEnforcementMode` | string | No | Schema validation mode. One of: `none`, `strict`, `warn`. Default: `none`. |
| `fieldConstraints` | FieldConstraint[] | No | Array of field-level validation rules. See [FieldConstraint](#fieldconstraint). |
| `indexingMode` | string | No | Indexing strategy. One of: `all`, `selective`. Default: `all`. |
| `indexedFields` | string[] | No | Fields to index when `indexingMode` is `selective`. |

**Response (201 Created):**

```json
{
  "success": true,
  "statusCode": 201,
  "data": {
    "id": "d4e5f6a7-b8c9-0123-4567-89abcdef0123",
    "name": "customers",
    "description": "Customer records",
    "documentsDirectory": null,
    "labels": ["production", "crm"],
    "tags": {
      "department": "sales",
      "region": "us-east"
    },
    "createdUtc": "2024-01-15T12:00:00.000Z",
    "lastUpdateUtc": "2024-01-15T12:00:00.000Z",
    "schemaEnforcementMode": "none",
    "indexingMode": "all"
  },
  "processingTimeMs": 15.78,
  "guid": "...",
  "timestampUtc": "2024-01-15T12:00:00.000Z"
}
```

**Errors:**

- `400 Bad Request` -- `name` is missing or empty.

---

#### GET /v1.0/collections -- List Collections

Retrieves all collections.

**cURL:**

```bash
curl http://localhost:8000/v1.0/collections
```

**Response (200 OK):**

```json
{
  "success": true,
  "statusCode": 200,
  "data": [
    {
      "id": "d4e5f6a7-b8c9-0123-4567-89abcdef0123",
      "name": "customers",
      "description": "Customer records",
      "documentsDirectory": null,
      "labels": ["production", "crm"],
      "tags": { "department": "sales" },
      "createdUtc": "2024-01-15T12:00:00.000Z",
      "lastUpdateUtc": "2024-01-15T12:00:00.000Z",
      "schemaEnforcementMode": "none",
      "indexingMode": "all"
    }
  ],
  "processingTimeMs": 3.21,
  "guid": "...",
  "timestampUtc": "2024-01-15T12:00:00.000Z"
}
```

---

#### GET /v1.0/collections/{collectionId} -- Get Collection

Retrieves a specific collection by its ID.

**Path Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `collectionId` | string (UUID) | The unique identifier of the collection. |

**cURL:**

```bash
curl http://localhost:8000/v1.0/collections/d4e5f6a7-b8c9-0123-4567-89abcdef0123
```

**Response (200 OK):**

```json
{
  "success": true,
  "statusCode": 200,
  "data": {
    "id": "d4e5f6a7-b8c9-0123-4567-89abcdef0123",
    "name": "customers",
    "description": "Customer records",
    "documentsDirectory": null,
    "labels": ["production", "crm"],
    "tags": { "department": "sales" },
    "createdUtc": "2024-01-15T12:00:00.000Z",
    "lastUpdateUtc": "2024-01-15T12:00:00.000Z",
    "schemaEnforcementMode": "none",
    "indexingMode": "all"
  }
}
```

**Errors:**

- `404 Not Found` -- Collection with the given ID does not exist.

---

#### HEAD /v1.0/collections/{collectionId} -- Check Collection Exists

Checks whether a collection exists. Returns `200` if found, `404` if not. No response body is returned.

**Path Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `collectionId` | string (UUID) | The unique identifier of the collection. |

**cURL:**

```bash
curl -I http://localhost:8000/v1.0/collections/d4e5f6a7-b8c9-0123-4567-89abcdef0123
```

**Response:** `200 OK` (no body) or `404 Not Found` (no body).

---

#### DELETE /v1.0/collections/{collectionId} -- Delete Collection

Deletes a collection and **all of its documents** (cascade delete).

**Path Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `collectionId` | string (UUID) | The unique identifier of the collection. |

**cURL:**

```bash
curl -X DELETE http://localhost:8000/v1.0/collections/d4e5f6a7-b8c9-0123-4567-89abcdef0123
```

**Response (200 OK):**

```json
{
  "success": true,
  "statusCode": 200,
  "data": null,
  "processingTimeMs": 42.10,
  "guid": "...",
  "timestampUtc": "2024-01-15T12:00:00.000Z"
}
```

**Errors:**

- `404 Not Found` -- Collection does not exist.

---

#### GET /v1.0/collections/{collectionId}/constraints -- Get Constraints

Retrieves the schema enforcement mode and field constraints for a collection.

**Path Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `collectionId` | string (UUID) | The unique identifier of the collection. |

**cURL:**

```bash
curl http://localhost:8000/v1.0/collections/d4e5f6a7-b8c9-0123-4567-89abcdef0123/constraints
```

**Response (200 OK):**

```json
{
  "success": true,
  "statusCode": 200,
  "data": {
    "schemaEnforcementMode": "strict",
    "fieldConstraints": [
      {
        "id": "...",
        "collectionId": "d4e5f6a7-...",
        "fieldPath": "email",
        "dataType": "string",
        "required": true,
        "nullable": false,
        "regexPattern": "^[^@]+@[^@]+\\.[^@]+$",
        "minLength": 5,
        "maxLength": 255,
        "allowedValues": null,
        "arrayElementType": null,
        "minValue": null,
        "maxValue": null
      }
    ]
  }
}
```

**Errors:**

- `404 Not Found` -- Collection does not exist.

---

#### PUT /v1.0/collections/{collectionId}/constraints -- Update Constraints

Updates the schema enforcement mode and/or field constraints for a collection.

**Path Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `collectionId` | string (UUID) | The unique identifier of the collection. |

**cURL:**

```bash
curl -X PUT http://localhost:8000/v1.0/collections/d4e5f6a7-b8c9-0123-4567-89abcdef0123/constraints \
  -H "Content-Type: application/json" \
  -d '{
    "schemaEnforcementMode": "strict",
    "fieldConstraints": [
      {
        "fieldPath": "email",
        "dataType": "string",
        "required": true,
        "nullable": false,
        "regexPattern": "^[^@]+@[^@]+\\.[^@]+$",
        "minLength": 5,
        "maxLength": 255
      },
      {
        "fieldPath": "age",
        "dataType": "integer",
        "required": false,
        "nullable": true,
        "minValue": 0,
        "maxValue": 150
      }
    ]
  }'
```

**Request Body:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `schemaEnforcementMode` | string | No | One of: `none`, `strict`, `warn`. |
| `fieldConstraints` | FieldConstraint[] | No | Array of field validation rules. |

**Response (200 OK):** Updated constraints object (same structure as GET).

**Errors:**

- `404 Not Found` -- Collection does not exist.

---

#### GET /v1.0/collections/{collectionId}/indexing -- Get Indexing Config

Retrieves the current indexing mode and indexed fields for a collection.

**Path Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `collectionId` | string (UUID) | The unique identifier of the collection. |

**cURL:**

```bash
curl http://localhost:8000/v1.0/collections/d4e5f6a7-b8c9-0123-4567-89abcdef0123/indexing
```

**Response (200 OK):**

```json
{
  "success": true,
  "statusCode": 200,
  "data": {
    "indexingMode": "selective",
    "indexedFields": ["name", "email", "address.city"]
  }
}
```

**Errors:**

- `404 Not Found` -- Collection does not exist.

---

#### PUT /v1.0/collections/{collectionId}/indexing -- Update Indexing Config

Updates the indexing configuration for a collection.

**Path Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `collectionId` | string (UUID) | The unique identifier of the collection. |

**cURL:**

```bash
curl -X PUT http://localhost:8000/v1.0/collections/d4e5f6a7-b8c9-0123-4567-89abcdef0123/indexing \
  -H "Content-Type: application/json" \
  -d '{
    "indexingMode": "selective",
    "indexedFields": ["name", "email", "address.city"],
    "rebuildIndexes": true
  }'
```

**Request Body:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `indexingMode` | string | No | One of: `all`, `selective`. |
| `indexedFields` | string[] | No | Field paths to index (used with `selective` mode). |
| `rebuildIndexes` | boolean | No | If `true`, triggers an immediate index rebuild. |

**Response (200 OK):** Updated indexing configuration.

**Errors:**

- `404 Not Found` -- Collection does not exist.

---

#### POST /v1.0/collections/{collectionId}/indexes/rebuild -- Rebuild Indexes

Triggers a full rebuild of all indexes for a collection. This is useful after changing indexing configuration or if indexes become out of sync.

**Path Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `collectionId` | string (UUID) | The unique identifier of the collection. |

**cURL:**

```bash
curl -X POST http://localhost:8000/v1.0/collections/d4e5f6a7-b8c9-0123-4567-89abcdef0123/indexes/rebuild \
  -H "Content-Type: application/json" \
  -d '{
    "dropUnusedIndexes": true
  }'
```

**Request Body:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `dropUnusedIndexes` | boolean | No | If `true`, drops index tables that are no longer needed. |

**Response (200 OK):**

```json
{
  "success": true,
  "statusCode": 200,
  "data": {
    "collectionId": "d4e5f6a7-...",
    "documentsProcessed": 150,
    "indexesCreated": 12,
    "indexesDropped": 3,
    "valuesInserted": 1800,
    "duration": "00:00:02.3456789",
    "durationMs": 2345.68,
    "errors": [],
    "success": true
  }
}
```

**Errors:**

- `404 Not Found` -- Collection does not exist.

---

### Documents

#### GET /v1.0/collections/{collectionId}/documents -- List Documents

Retrieves metadata for all documents in a collection.

**Path Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `collectionId` | string (UUID) | The unique identifier of the collection. |

**cURL:**

```bash
curl http://localhost:8000/v1.0/collections/d4e5f6a7-b8c9-0123-4567-89abcdef0123/documents
```

**Response (200 OK):**

```json
{
  "success": true,
  "statusCode": 200,
  "data": [
    {
      "id": "a1b2c3d4-...",
      "collectionId": "d4e5f6a7-...",
      "schemaId": "f0e1d2c3-...",
      "name": "john-doe",
      "labels": ["active"],
      "tags": { "source": "import" },
      "createdUtc": "2024-01-15T12:00:00.000Z",
      "lastUpdateUtc": "2024-01-15T12:00:00.000Z",
      "contentLength": 256,
      "sha256Hash": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"
    }
  ]
}
```

**Errors:**

- `404 Not Found` -- Collection does not exist.

---

#### PUT /v1.0/collections/{collectionId}/documents -- Create Document

Creates (ingests) a new JSON document into a collection. The document content is automatically analyzed for schema detection and indexed.

**Path Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `collectionId` | string (UUID) | The unique identifier of the collection. |

**cURL:**

```bash
curl -X PUT http://localhost:8000/v1.0/collections/d4e5f6a7-b8c9-0123-4567-89abcdef0123/documents \
  -H "Content-Type: application/json" \
  -d '{
    "name": "john-doe",
    "labels": ["active", "verified"],
    "tags": {
      "source": "web-form",
      "importBatch": "2024-01"
    },
    "content": {
      "firstName": "John",
      "lastName": "Doe",
      "email": "john@example.com",
      "age": 30,
      "address": {
        "street": "123 Main St",
        "city": "Springfield",
        "state": "IL",
        "zip": "62704"
      },
      "interests": ["hiking", "photography"]
    }
  }'
```

**Request Body:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `content` | object | **Yes** | The JSON document content. Must be a valid JSON object. |
| `name` | string | No | Optional human-readable name for the document. |
| `labels` | string[] | No | Array of string labels for categorization. |
| `tags` | object | No | Key-value string pairs for metadata. |

**Response (201 Created):**

```json
{
  "success": true,
  "statusCode": 201,
  "data": {
    "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "collectionId": "d4e5f6a7-...",
    "schemaId": "f0e1d2c3-...",
    "name": "john-doe",
    "labels": ["active", "verified"],
    "tags": {
      "source": "web-form",
      "importBatch": "2024-01"
    },
    "createdUtc": "2024-01-15T12:00:00.000Z",
    "lastUpdateUtc": "2024-01-15T12:00:00.000Z",
    "contentLength": 256,
    "sha256Hash": "a1b2c3..."
  }
}
```

**Errors:**

- `400 Bad Request` -- `content` is missing or not a valid JSON object; schema validation failure.
- `404 Not Found` -- Collection does not exist.
- `409 Conflict` -- Object lock held on the same document name (concurrent write).

---

#### PUT /v1.0/collections/{collectionId}/documents/batch -- Batch Ingest Documents

Ingests multiple documents into a collection in a single batch operation. Each document is processed individually with its own schema detection and indexing.

**Path Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `collectionId` | string (UUID) | The unique identifier of the collection. |

**cURL:**

```bash
curl -X PUT http://localhost:8000/v1.0/collections/d4e5f6a7-b8c9-0123-4567-89abcdef0123/documents/batch \
  -H "Content-Type: application/json" \
  -d '{
    "documents": [
      {
        "name": "john-doe",
        "labels": ["active"],
        "content": {
          "firstName": "John",
          "lastName": "Doe",
          "email": "john@example.com"
        }
      },
      {
        "name": "jane-smith",
        "labels": ["active"],
        "content": {
          "firstName": "Jane",
          "lastName": "Smith",
          "email": "jane@example.com"
        }
      }
    ]
  }'
```

**Request Body:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `documents` | array | **Yes** | Array of document objects to ingest. |
| `documents[].content` | object | **Yes** | The JSON document content for each document. |
| `documents[].name` | string | No | Optional name for each document. |
| `documents[].labels` | string[] | No | Labels for each document. |
| `documents[].tags` | object | No | Tags for each document. |

**Response (201 Created):**

```json
{
  "success": true,
  "statusCode": 201,
  "data": [
    {
      "id": "a1b2c3d4-...",
      "collectionId": "d4e5f6a7-...",
      "schemaId": "f0e1d2c3-...",
      "name": "john-doe",
      "labels": ["active"],
      "tags": null,
      "createdUtc": "2024-01-15T12:00:00.000Z",
      "lastUpdateUtc": "2024-01-15T12:00:00.000Z",
      "contentLength": 128,
      "sha256Hash": "..."
    },
    {
      "id": "b2c3d4e5-...",
      "collectionId": "d4e5f6a7-...",
      "schemaId": "f0e1d2c3-...",
      "name": "jane-smith",
      "labels": ["active"],
      "tags": null,
      "createdUtc": "2024-01-15T12:00:00.000Z",
      "lastUpdateUtc": "2024-01-15T12:00:00.000Z",
      "contentLength": 130,
      "sha256Hash": "..."
    }
  ]
}
```

**Errors:**

- `400 Bad Request` -- `documents` array is missing or empty; a document is missing `content`.
- `404 Not Found` -- Collection does not exist.

---

#### GET /v1.0/collections/{collectionId}/documents/{documentId} -- Get Document

Retrieves a specific document by its ID. By default, returns document metadata only. Use the `includeContent` query parameter to retrieve the raw JSON content.

**Path Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `collectionId` | string (UUID) | The unique identifier of the collection. |
| `documentId` | string (UUID) | The unique identifier of the document. |

**Query Parameters:**

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `includeContent` | boolean | `false` | If `true`, returns the raw JSON document content directly (not wrapped in the standard response envelope). |

**cURL (metadata only):**

```bash
curl http://localhost:8000/v1.0/collections/d4e5f6a7-.../documents/a1b2c3d4-...
```

**Response (200 OK -- metadata):**

```json
{
  "success": true,
  "statusCode": 200,
  "data": {
    "id": "a1b2c3d4-...",
    "collectionId": "d4e5f6a7-...",
    "schemaId": "f0e1d2c3-...",
    "name": "john-doe",
    "labels": ["active", "verified"],
    "tags": { "source": "web-form" },
    "createdUtc": "2024-01-15T12:00:00.000Z",
    "lastUpdateUtc": "2024-01-15T12:00:00.000Z",
    "contentLength": 256,
    "sha256Hash": "a1b2c3..."
  }
}
```

**cURL (with content):**

```bash
curl "http://localhost:8000/v1.0/collections/d4e5f6a7-.../documents/a1b2c3d4-...?includeContent=true"
```

**Response (200 OK -- with content):**

Note: This response is the raw JSON content, **not** wrapped in the standard response envelope.

```json
{
  "firstName": "John",
  "lastName": "Doe",
  "email": "john@example.com",
  "age": 30,
  "address": {
    "street": "123 Main St",
    "city": "Springfield",
    "state": "IL",
    "zip": "62704"
  },
  "interests": ["hiking", "photography"]
}
```

**Errors:**

- `404 Not Found` -- Collection or document does not exist.

---

#### HEAD /v1.0/collections/{collectionId}/documents/{documentId} -- Check Document Exists

Checks whether a document exists. Returns `200` if found, `404` if not. No response body.

**Path Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `collectionId` | string (UUID) | The unique identifier of the collection. |
| `documentId` | string (UUID) | The unique identifier of the document. |

**cURL:**

```bash
curl -I http://localhost:8000/v1.0/collections/d4e5f6a7-.../documents/a1b2c3d4-...
```

**Response:** `200 OK` (no body) or `404 Not Found` (no body).

---

#### DELETE /v1.0/collections/{collectionId}/documents/{documentId} -- Delete Document

Deletes a document from the collection.

**Path Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `collectionId` | string (UUID) | The unique identifier of the collection. |
| `documentId` | string (UUID) | The unique identifier of the document. |

**cURL:**

```bash
curl -X DELETE http://localhost:8000/v1.0/collections/d4e5f6a7-.../documents/a1b2c3d4-...
```

**Response (200 OK):**

```json
{
  "success": true,
  "statusCode": 200,
  "data": null,
  "processingTimeMs": 8.45,
  "guid": "...",
  "timestampUtc": "2024-01-15T12:00:00.000Z"
}
```

**Errors:**

- `404 Not Found` -- Collection or document does not exist.

---

### Search

#### POST /v1.0/collections/{collectionId}/documents/search -- Search Documents

Searches for documents in a collection. Supports two modes:

1. **Structured filters** -- field-level conditions with optional label/tag filtering.
2. **SQL expressions** -- SQL-like query syntax.

Only one mode should be used per request.

**Path Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `collectionId` | string (UUID) | The unique identifier of the collection. |

##### Structured Filter Search

**cURL:**

```bash
curl -X POST http://localhost:8000/v1.0/collections/d4e5f6a7-.../documents/search \
  -H "Content-Type: application/json" \
  -d '{
    "filters": [
      {
        "field": "address.city",
        "condition": "equals",
        "value": "Springfield"
      },
      {
        "field": "age",
        "condition": "greaterThan",
        "value": "25"
      }
    ],
    "labels": ["active"],
    "tags": {
      "source": "web-form"
    },
    "maxResults": 10,
    "skip": 0,
    "ordering": "createdDescending",
    "includeContent": true
  }'
```

**Request Body (Structured):**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `filters` | Filter[] | No | Array of field filter conditions. |
| `filters[].field` | string | Yes | Dot-notation field path (e.g., `address.city`). |
| `filters[].condition` | string | Yes | Comparison operator (see table below). |
| `filters[].value` | string | Yes | Value to compare against. |
| `labels` | string[] | No | Filter documents by labels (documents must have all specified labels). |
| `tags` | object | No | Filter documents by tag key-value pairs. |
| `maxResults` | integer | No | Maximum number of results to return. |
| `skip` | integer | No | Number of results to skip (for pagination). |
| `ordering` | string | No | Sort order for results (see table below). |
| `includeContent` | boolean | No | If `true`, include document content in results. |

**Filter Conditions:**

| Condition | Description |
|-----------|-------------|
| `equals` | Exact match. |
| `notEquals` | Not equal. |
| `greaterThan` | Greater than (numeric/date comparison). |
| `greaterThanOrEqualTo` | Greater than or equal to. |
| `lessThan` | Less than. |
| `lessThanOrEqualTo` | Less than or equal to. |
| `contains` | String contains substring. |
| `startsWith` | String starts with prefix. |
| `endsWith` | String ends with suffix. |
| `isNull` | Field is null. |
| `isNotNull` | Field is not null. |

**Ordering Options:**

| Value | Description |
|-------|-------------|
| `createdAscending` | Oldest first by creation date. |
| `createdDescending` | Newest first by creation date. |
| `lastUpdateAscending` | Oldest first by last update date. |
| `lastUpdateDescending` | Newest first by last update date. |
| `nameAscending` | Alphabetical by name (A-Z). |
| `nameDescending` | Reverse alphabetical by name (Z-A). |

##### SQL Expression Search

**cURL:**

```bash
curl -X POST http://localhost:8000/v1.0/collections/d4e5f6a7-.../documents/search \
  -H "Content-Type: application/json" \
  -d '{
    "sqlExpression": "SELECT * FROM documents WHERE address.city = '\''Springfield'\'' AND age > 25"
  }'
```

**Request Body (SQL):**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `sqlExpression` | string | Yes | SQL-like query expression. Fields use dot-notation paths. |

##### Search Response

**Response (200 OK):**

```json
{
  "success": true,
  "statusCode": 200,
  "data": {
    "success": true,
    "timestamp": {
      "start": "2024-01-15T12:00:00.000Z",
      "end": "2024-01-15T12:00:00.050Z",
      "totalMs": 50.0
    },
    "maxResults": 10,
    "endOfResults": true,
    "totalRecords": 2,
    "recordsRemaining": 0,
    "documents": [
      {
        "id": "a1b2c3d4-...",
        "collectionId": "d4e5f6a7-...",
        "schemaId": "f0e1d2c3-...",
        "name": "john-doe",
        "labels": ["active"],
        "tags": { "source": "web-form" },
        "createdUtc": "2024-01-15T12:00:00.000Z",
        "lastUpdateUtc": "2024-01-15T12:00:00.000Z",
        "contentLength": 256,
        "sha256Hash": "...",
        "content": {
          "firstName": "John",
          "lastName": "Doe",
          "email": "john@example.com",
          "age": 30,
          "address": {
            "street": "123 Main St",
            "city": "Springfield",
            "state": "IL",
            "zip": "62704"
          }
        }
      }
    ]
  }
}
```

**Errors:**

- `400 Bad Request` -- Invalid filter conditions or malformed SQL expression.
- `404 Not Found` -- Collection does not exist.

---

### Schemas

Schemas are automatically detected from JSON document structure during ingestion. Documents with identical structures share the same schema (deduplicated by hash).

#### GET /v1.0/schemas -- List Schemas

Retrieves all discovered schemas.

**cURL:**

```bash
curl http://localhost:8000/v1.0/schemas
```

**Response (200 OK):**

```json
{
  "success": true,
  "statusCode": 200,
  "data": [
    {
      "id": "f0e1d2c3-b4a5-6789-0123-456789abcdef",
      "hash": "a1b2c3d4e5f6...",
      "createdUtc": "2024-01-15T12:00:00.000Z",
      "lastUpdateUtc": "2024-01-15T12:00:00.000Z"
    }
  ]
}
```

---

#### GET /v1.0/schemas/{schemaId} -- Get Schema

Retrieves a specific schema by its ID.

**Path Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `schemaId` | string (UUID) | The unique identifier of the schema. |

**cURL:**

```bash
curl http://localhost:8000/v1.0/schemas/f0e1d2c3-b4a5-6789-0123-456789abcdef
```

**Response (200 OK):**

```json
{
  "success": true,
  "statusCode": 200,
  "data": {
    "id": "f0e1d2c3-b4a5-6789-0123-456789abcdef",
    "hash": "a1b2c3d4e5f6...",
    "createdUtc": "2024-01-15T12:00:00.000Z",
    "lastUpdateUtc": "2024-01-15T12:00:00.000Z"
  }
}
```

**Errors:**

- `404 Not Found` -- Schema does not exist.

---

#### GET /v1.0/schemas/{schemaId}/elements -- Get Schema Elements

Retrieves the elements (fields) defined in a schema. Each element represents a discovered field path with its detected data type.

**Path Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `schemaId` | string (UUID) | The unique identifier of the schema. |

**cURL:**

```bash
curl http://localhost:8000/v1.0/schemas/f0e1d2c3-b4a5-6789-0123-456789abcdef/elements
```

**Response (200 OK):**

```json
{
  "success": true,
  "statusCode": 200,
  "data": [
    {
      "id": "...",
      "schemaId": "f0e1d2c3-...",
      "position": 0,
      "key": "firstName",
      "dataType": "string",
      "nullable": false,
      "createdUtc": "2024-01-15T12:00:00.000Z",
      "lastUpdateUtc": "2024-01-15T12:00:00.000Z"
    },
    {
      "id": "...",
      "schemaId": "f0e1d2c3-...",
      "position": 1,
      "key": "age",
      "dataType": "integer",
      "nullable": true,
      "createdUtc": "2024-01-15T12:00:00.000Z",
      "lastUpdateUtc": "2024-01-15T12:00:00.000Z"
    },
    {
      "id": "...",
      "schemaId": "f0e1d2c3-...",
      "position": 2,
      "key": "address.city",
      "dataType": "string",
      "nullable": false,
      "createdUtc": "2024-01-15T12:00:00.000Z",
      "lastUpdateUtc": "2024-01-15T12:00:00.000Z"
    }
  ]
}
```

**Errors:**

- `404 Not Found` -- Schema does not exist.

---

### Index Tables

Index tables are the underlying storage for searchable field values. Each unique schema element (field path + data type) maps to its own index table.

#### GET /v1.0/tables -- List Index Tables

Retrieves all index table mappings.

**cURL:**

```bash
curl http://localhost:8000/v1.0/tables
```

**Response (200 OK):**

```json
{
  "success": true,
  "statusCode": 200,
  "data": [
    {
      "id": "...",
      "key": "firstName:string",
      "tableName": "idx_a1b2c3d4"
    },
    {
      "id": "...",
      "key": "address.city:string",
      "tableName": "idx_e5f6a7b8"
    }
  ]
}
```

---

#### GET /v1.0/tables/{tableName}/entries -- Get Index Entries

Retrieves entries from a specific index table with pagination.

**Path Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `tableName` | string | The name of the index table (e.g., `idx_a1b2c3d4`). |

**Query Parameters:**

| Parameter | Type | Default | Max | Description |
|-----------|------|---------|-----|-------------|
| `skip` | integer | 0 | -- | Number of entries to skip. |
| `limit` | integer | 100 | 1000 | Maximum number of entries to return. |

**cURL:**

```bash
curl "http://localhost:8000/v1.0/tables/idx_a1b2c3d4/entries?skip=0&limit=50"
```

**Response (200 OK):**

```json
{
  "success": true,
  "statusCode": 200,
  "data": [
    {
      "id": "...",
      "documentId": "a1b2c3d4-...",
      "schemaId": "f0e1d2c3-...",
      "schemaElementId": "...",
      "position": 0,
      "value": "John"
    },
    {
      "id": "...",
      "documentId": "b2c3d4e5-...",
      "schemaId": "f0e1d2c3-...",
      "schemaElementId": "...",
      "position": 0,
      "value": "Jane"
    }
  ]
}
```

**Errors:**

- `404 Not Found` -- Index table does not exist.

---

## Data Models

### Collection

| Field | Type | Description |
|-------|------|-------------|
| `id` | string (UUID) | Unique identifier. |
| `name` | string | Collection name. |
| `description` | string or null | Optional description. |
| `documentsDirectory` | string or null | Custom document storage directory. |
| `labels` | string[] or null | Categorization labels. |
| `tags` | object or null | Key-value metadata. |
| `createdUtc` | string (ISO 8601) | Creation timestamp. |
| `lastUpdateUtc` | string (ISO 8601) | Last modification timestamp. |
| `schemaEnforcementMode` | string | One of: `none`, `strict`, `warn`. |
| `indexingMode` | string | One of: `all`, `selective`. |

### Document

| Field | Type | Description |
|-------|------|-------------|
| `id` | string (UUID) | Unique identifier. |
| `collectionId` | string (UUID) | Parent collection ID. |
| `schemaId` | string (UUID) | Auto-detected schema ID. |
| `name` | string or null | Optional document name. |
| `labels` | string[] or null | Categorization labels. |
| `tags` | object or null | Key-value metadata. |
| `createdUtc` | string (ISO 8601) | Creation timestamp. |
| `lastUpdateUtc` | string (ISO 8601) | Last modification timestamp. |
| `contentLength` | integer | Size of the stored JSON content in bytes. |
| `sha256Hash` | string | SHA-256 hash of the document content. |
| `content` | object | Only present when `includeContent=true`. The raw JSON document. |

### Schema

| Field | Type | Description |
|-------|------|-------------|
| `id` | string (UUID) | Unique identifier. |
| `hash` | string | Hash of the schema structure (used for deduplication). |
| `createdUtc` | string (ISO 8601) | Creation timestamp. |
| `lastUpdateUtc` | string (ISO 8601) | Last modification timestamp. |

### SchemaElement

| Field | Type | Description |
|-------|------|-------------|
| `id` | string (UUID) | Unique identifier. |
| `schemaId` | string (UUID) | Parent schema ID. |
| `position` | integer | Ordinal position of the field in the schema. |
| `key` | string | Dot-notation field path (e.g., `address.city`). |
| `dataType` | string | Detected data type (e.g., `string`, `integer`, `boolean`, `array`, `object`). |
| `nullable` | boolean | Whether the field can be null. |
| `createdUtc` | string (ISO 8601) | Creation timestamp. |
| `lastUpdateUtc` | string (ISO 8601) | Last modification timestamp. |

### FieldConstraint

| Field | Type | Description |
|-------|------|-------------|
| `id` | string (UUID) | Unique identifier. |
| `collectionId` | string (UUID) | Parent collection ID. |
| `fieldPath` | string | Dot-notation field path this constraint applies to. |
| `dataType` | string | Expected data type for the field. |
| `required` | boolean | Whether the field must be present. |
| `nullable` | boolean | Whether the field can be null. |
| `regexPattern` | string or null | Regular expression the value must match (strings only). |
| `minValue` | number or null | Minimum numeric value. |
| `maxValue` | number or null | Maximum numeric value. |
| `minLength` | integer or null | Minimum string length. |
| `maxLength` | integer or null | Maximum string length. |
| `allowedValues` | array or null | Whitelist of permitted values. |
| `arrayElementType` | string or null | Expected type of array elements (if field is an array). |

### SearchResult

| Field | Type | Description |
|-------|------|-------------|
| `success` | boolean | Whether the search completed successfully. |
| `timestamp` | object | Timing information with `start`, `end`, and `totalMs`. |
| `maxResults` | integer or null | Maximum results that were requested. |
| `continuationToken` | string or null | Token for paginated continuation. |
| `endOfResults` | boolean | Whether all matching results have been returned. |
| `totalRecords` | integer | Total number of matching records. |
| `recordsRemaining` | integer | Number of records not yet returned. |
| `documents` | Document[] | Array of matching documents. |

### IndexTableMapping

| Field | Type | Description |
|-------|------|-------------|
| `id` | string (UUID) | Unique identifier. |
| `key` | string | Field path and type key (e.g., `firstName:string`). |
| `tableName` | string | Name of the underlying index table. |

### IndexTableEntry

| Field | Type | Description |
|-------|------|-------------|
| `id` | string (UUID) | Unique identifier. |
| `documentId` | string (UUID) | ID of the document this entry belongs to. |
| `schemaId` | string (UUID) | Schema ID of the document. |
| `schemaElementId` | string (UUID) | Schema element (field) this entry indexes. |
| `position` | integer | Position within the document (relevant for array elements). |
| `value` | string | The indexed value (stored as string). |

### IndexRebuildResult

| Field | Type | Description |
|-------|------|-------------|
| `collectionId` | string (UUID) | The collection that was rebuilt. |
| `documentsProcessed` | integer | Number of documents processed. |
| `indexesCreated` | integer | Number of new index tables created. |
| `indexesDropped` | integer | Number of index tables dropped (if `dropUnusedIndexes` was true). |
| `valuesInserted` | integer | Total number of index entries inserted. |
| `duration` | string | Human-readable duration (e.g., `00:00:02.345`). |
| `durationMs` | number | Duration in milliseconds. |
| `errors` | string[] | Array of error messages, if any. |
| `success` | boolean | Whether the rebuild completed without errors. |

---

## Key Behaviors

### Automatic Schema Detection

When a document is ingested, Lattice automatically analyzes the JSON structure and creates a schema describing all field paths and their data types. Documents with identical structures share the same schema, deduplicated by hash.

### JSON Path Flattening

Nested JSON objects are flattened to dot-notation paths for indexing and search:

| JSON Structure | Flattened Path |
|---------------|----------------|
| `{ "name": "John" }` | `name` |
| `{ "address": { "city": "Springfield" } }` | `address.city` |
| `{ "items": [{ "product": "Widget" }] }` | `items[0].product` |

### Automatic Indexing

By default (`indexingMode: "all"`), all fields in ingested documents are automatically indexed. This enables search across any field without manual index configuration. Use `selective` mode to index only specific fields for better storage efficiency.

### Object Locking

Lattice uses object locking to prevent concurrent writes to the same document name. If a write is attempted while another write to the same document name is in progress, the server returns `409 Conflict`. The lock is released when the write operation completes.

### Schema Enforcement Modes

| Mode | Behavior |
|------|----------|
| `none` | No schema validation is performed. Any valid JSON is accepted. |
| `strict` | Documents must match the defined field constraints exactly. Documents with extra or missing required fields are rejected with `400`. |
| `warn` | Validation is performed but non-conforming documents are still accepted. Validation warnings are logged server-side. |

### JSON Serialization

All JSON property names in API responses use **camelCase** naming. Enum values are serialized as camelCase strings. Null properties are omitted from responses.
