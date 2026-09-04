# Lattice MCP API

Lattice exposes a [Model Context Protocol](https://modelcontextprotocol.io) (MCP) endpoint so that agents
and LLM tools can browse and manipulate the document store through a well-typed tool catalog. The endpoint
is served **in-process by the Lattice server** over JSON-RPC 2.0 at:

```
POST /v1.0/mcp
```

It is behind the **same authentication and authorization** as the REST API: every tool call uses the same
bearer credentials and is evaluated against the same RBAC model, so a principal can never do more through
MCP than it can through REST.

The endpoint is enabled by default and configured under the `Mcp` block in `lattice.json`:

```json
"Mcp": {
  "Enable": true,
  "Path": "/v1.0/mcp",
  "ServerName": "lattice"
}
```

## Transport

- **Protocol**: JSON-RPC 2.0 over HTTP. Every response is returned as **HTTP 200** with a JSON-RPC envelope
  (`result` on success, `error` on failure) — application-level failures are JSON-RPC errors, not HTTP
  status codes.
- **Content type**: `application/json`. A `Content-Length` header is required (chunked request bodies are
  not read).
- **Request id**: the JSON-RPC `id` (string, number, or null) is echoed back verbatim with its original
  type preserved.

## Authentication

Present credentials with the `Authorization: Bearer <value>` header (the `x-token` header alias is also
accepted), exactly as with the REST API. The bearer value is either:

1. A **session token** obtained from `POST /v1.0/token` (email + password + tenant), or
2. A **credential access key** (`key_...`) presented directly.

`initialize`, `ping`, and `notifications/initialized` are accepted without credentials (they carry no data),
but `tools/list` and `tools/call` require authentication. An unauthenticated `tools/call` returns:

```json
{ "jsonrpc": "2.0", "id": 1, "error": { "code": -32000, "message": "Authentication required." } }
```

Each `tools/call` is additionally authorized against the caller's grants for the resource and operation the
tool maps to (see [Tool authorization](#tool-authorization)). A tool the caller may not invoke returns
`error.code -32000` with message `"Not permitted."`.

The tenant a tool operates on is resolved from the caller. A system administrator may target another tenant
by passing `tenantId` in a management tool's arguments; non-admins are always scoped to their own tenant.

## Methods

| Method | Auth | Description |
|---|---|---|
| `initialize` | no | Handshake. Returns protocol version, server info, and capabilities. |
| `ping` | no | Returns an empty result. |
| `notifications/initialized` | no | Client-ready notification. Returns HTTP 202, no body. |
| `tools/list` | yes | Returns the tool catalog with JSON-Schema input contracts. |
| `tools/call` | yes | Invokes a tool by name with arguments. |

### initialize

Request:

```json
{ "jsonrpc": "2.0", "id": 1, "method": "initialize", "params": { "protocolVersion": "2024-11-05" } }
```

Response:

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "protocolVersion": "2024-11-05",
    "serverInfo": { "name": "lattice", "version": "0.3.0" },
    "capabilities": { "tools": {} }
  }
}
```

### tools/call result shape

A successful `tools/call` wraps the tool's output as MCP text content:

```json
{
  "jsonrpc": "2.0",
  "id": 4,
  "result": {
    "content": [ { "type": "text", "text": "<the tool result serialized as JSON>" } ],
    "isError": false
  }
}
```

The `text` field is the tool's result serialized as a JSON string; parse it to obtain the structured value.
List tools return the same `EnumerationResult` paging shape as the REST API (`totalRecords`, `skip`,
`maxResults`, `recordsRemaining`, `endOfResults`, `objects[]`).

## Tools

Twenty-two tools mirror the REST data plane plus the identity/RBAC management read surfaces.

### Meta

| Tool | Arguments | Description |
|---|---|---|
| `lattice_capabilities` | — | Describe the platform, the tool list, and the paging protocol. |
| `lattice_whoami` | — | Return the resolved principal (tenant, user, admin status). |

### Collections

| Tool | Arguments | Description |
|---|---|---|
| `lattice_list_collections` | `maxResults?`, `skip?` | Enumerate collections (paged). |
| `lattice_get_collection` | `collectionId` | Fetch a single collection. |
| `lattice_create_collection` | `name`, `description?`, `labels?`, `tags?` | Create a collection. |
| `lattice_delete_collection` | `collectionId` | Delete a collection and its documents. |

### Documents

| Tool | Arguments | Description |
|---|---|---|
| `lattice_list_documents` | `collectionId`, `maxResults?`, `skip?` | Enumerate documents in a collection. |
| `lattice_get_document` | `collectionId`, `documentId`, `includeContent?` | Fetch a document. |
| `lattice_create_document` | `collectionId`, `content`, `name?`, `labels?`, `tags?` | Ingest a JSON document (`content` is the body as a JSON object). |
| `lattice_delete_document` | `documentId` | Delete a document. |
| `lattice_search_documents` | `collectionId`, `sqlExpression?`, `maxResults?`, `skip?`, `includeContent?` | Search a collection with a SQL-like expression. |

### Schemas & index tables

| Tool | Arguments | Description |
|---|---|---|
| `lattice_list_schemas` | `maxResults?`, `skip?` | Enumerate discovered schemas. |
| `lattice_get_schema` | `schemaId` | Fetch a schema. |
| `lattice_get_schema_elements` | `schemaId` | Fetch a schema's elements (fields). |
| `lattice_list_tables` | `maxResults?`, `skip?` | Enumerate index-table mappings. |
| `lattice_get_table_entries` | `tableName`, `skip?`, `limit?` | Fetch entries from an index table. |

### Identity, RBAC & audit

| Tool | Arguments | Description |
|---|---|---|
| `lattice_list_tenants` | `maxResults?`, `skip?` | Enumerate tenants (system administrator). |
| `lattice_list_users` | `tenantId?`, `maxResults?`, `skip?` | Enumerate users in the caller's tenant. |
| `lattice_list_credentials` | `tenantId?`, `maxResults?`, `skip?` | Enumerate access-key credentials. |
| `lattice_create_credential` | `name?`, `userId?`, `tenantId?` | Create an access key (returned once). |
| `lattice_list_roles` | `maxResults?`, `skip?` | Enumerate roles visible to the tenant. |
| `lattice_list_audit` | `tenantId?`, `eventType?`, `maxResults?`, `skip?` | Enumerate audit entries. |

## Tool authorization

Each tool maps to a resource type and operation, evaluated against the caller's grants (same model as REST;
system and tenant admins bypass evaluation):

| Tool(s) | Resource | Operation |
|---|---|---|
| `lattice_capabilities`, `lattice_whoami` | — | any authenticated principal |
| `lattice_list_collections`, `lattice_get_collection` | Collection | Read |
| `lattice_create_collection` | Collection | Create |
| `lattice_delete_collection` | Collection | Delete |
| `lattice_list_documents`, `lattice_get_document`, `lattice_search_documents` | Document | Read |
| `lattice_create_document` | Document | Create |
| `lattice_delete_document` | Document | Delete |
| `lattice_list_schemas`, `lattice_get_schema`, `lattice_get_schema_elements` | Schema | Read |
| `lattice_list_tables`, `lattice_get_table_entries` | Index | Read |
| `lattice_list_tenants` | Tenant | Read |
| `lattice_list_users` | User | Read |
| `lattice_list_credentials` | Credential | Read |
| `lattice_create_credential` | Credential | Create |
| `lattice_list_roles` | Role | Read |
| `lattice_list_audit` | Audit | Read |

## Error codes

| Code | Meaning |
|---|---|
| `-32700` | Parse error (malformed JSON). |
| `-32600` | Invalid request (missing method). |
| `-32601` | Method not found / unknown tool. |
| `-32602` | Invalid params (missing tool name or required argument). |
| `-32000` | Authentication required, not permitted, or a resource was not found. |

## End-to-end example

Obtain credentials (session token via login, or use a credential access key), then call tools:

```bash
# 1) Log in for a session token (or skip this and use an key_... key directly).
TOKEN=$(curl -s http://localhost:8000/v1.0/token \
  -H 'Content-Type: application/json' \
  -d '{"email":"admin@lattice","password":"password","tenantId":"ten_..."}' \
  | jq -r .token)

# 2) List tools.
curl -s http://localhost:8000/v1.0/mcp \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d '{"jsonrpc":"2.0","id":1,"method":"tools/list"}'

# 3) Enumerate collections.
curl -s http://localhost:8000/v1.0/mcp \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d '{"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"lattice_list_collections","arguments":{"maxResults":10}}}'

# 4) Ingest a document.
curl -s http://localhost:8000/v1.0/mcp \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d '{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"lattice_create_document","arguments":{"collectionId":"col_...","content":{"first":"Joel","last":"Christner"},"name":"person"}}}'
```

See `REST_API.md` for the authentication endpoints (`POST /v1.0/token`, credential management) and the full
REST surface.
