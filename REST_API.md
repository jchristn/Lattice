# Lattice REST API Reference

Comprehensive reference for the Lattice Server REST API. Lattice is a JSON document store with automatic schema detection, full-text indexing, and flexible search capabilities.

---

## Table of Contents

- [Base URL](#base-url)
- [Authentication & Authorization](#authentication--authorization)
  - [Authentication Methods](#authentication-methods)
  - [Public (Unauthenticated) Routes](#public-unauthenticated-routes)
  - [Multi-Tenancy](#multi-tenancy)
  - [Authorization (RBAC)](#authorization-rbac)
  - [First-Run Bootstrap](#first-run-bootstrap)
  - [Login Flow (Example)](#login-flow-example)
- [Response Format](#response-format)
- [Enumeration & Pagination](#enumeration--pagination)
- [Error Handling](#error-handling)
- [CORS](#cors)
- [Endpoints](#endpoints)
  - [Health](#health)
    - [GET / -- Root Health Check](#get----root-health-check)
    - [GET /v1.0/health -- Versioned Health Check](#get-v10health--versioned-health-check)
  - [Authentication & Session](#authentication--session)
    - [POST /v1.0/token -- Login](#post-v10token--login)
    - [GET /v1.0/token -- Current Principal](#get-v10token--current-principal)
    - [DELETE /v1.0/token -- Logout](#delete-v10token--logout)
  - [Tenants](#tenants)
    - [GET /v1.0/tenants -- List Tenants](#get-v10tenants--list-tenants)
    - [PUT /v1.0/tenants -- Create Tenant](#put-v10tenants--create-tenant)
    - [GET /v1.0/tenants/{tenantId} -- Get Tenant](#get-v10tenantstenantid--get-tenant)
    - [PUT /v1.0/tenants/{tenantId} -- Update Tenant](#put-v10tenantstenantid--update-tenant)
    - [DELETE /v1.0/tenants/{tenantId} -- Delete Tenant](#delete-v10tenantstenantid--delete-tenant)
  - [Users](#users)
    - [GET /v1.0/users -- List Users](#get-v10users--list-users)
    - [PUT /v1.0/users -- Create User](#put-v10users--create-user)
    - [GET /v1.0/users/{userId} -- Get User](#get-v10usersuserid--get-user)
    - [PUT /v1.0/users/{userId} -- Update User](#put-v10usersuserid--update-user)
    - [DELETE /v1.0/users/{userId} -- Delete User](#delete-v10usersuserid--delete-user)
  - [Credentials](#credentials)
    - [GET /v1.0/credentials -- List Credentials](#get-v10credentials--list-credentials)
    - [PUT /v1.0/credentials -- Create Credential](#put-v10credentials--create-credential)
    - [GET /v1.0/credentials/{credentialId} -- Get Credential](#get-v10credentialscredentialid--get-credential)
    - [PUT /v1.0/credentials/{credentialId} -- Update Credential](#put-v10credentialscredentialid--update-credential)
    - [DELETE /v1.0/credentials/{credentialId} -- Delete Credential](#delete-v10credentialscredentialid--delete-credential)
  - [Roles](#roles)
    - [GET /v1.0/roles -- List Roles](#get-v10roles--list-roles)
    - [PUT /v1.0/roles -- Create Role](#put-v10roles--create-role)
    - [GET /v1.0/roles/{roleId} -- Get Role](#get-v10rolesroleid--get-role)
    - [PUT /v1.0/roles/{roleId} -- Update Role](#put-v10rolesroleid--update-role)
    - [DELETE /v1.0/roles/{roleId} -- Delete Role](#delete-v10rolesroleid--delete-role)
  - [Assignments](#assignments)
    - [GET /v1.0/assignments -- List Assignments](#get-v10assignments--list-assignments)
    - [PUT /v1.0/assignments -- Create Assignment](#put-v10assignments--create-assignment)
    - [DELETE /v1.0/assignments/{assignmentId} -- Delete Assignment](#delete-v10assignmentsassignmentid--delete-assignment)
  - [Audit](#audit)
    - [GET /v1.0/audit -- List Audit Entries](#get-v10audit--list-audit-entries)
    - [GET /v1.0/audit/{auditId} -- Get Audit Entry](#get-v10auditauditid--get-audit-entry)
    - [DELETE /v1.0/audit/{auditId} -- Delete Audit Entry](#delete-v10auditauditid--delete-audit-entry)
  - [Collections](#collections)
    - [PUT /v1.0/collections -- Create Collection](#put-v10collections--create-collection)
    - [GET /v1.0/collections -- List Collections](#get-v10collections--list-collections)
    - [GET /v1.0/collections/{collectionId} -- Get Collection](#get-v10collectionscollectionid--get-collection)
    - [HEAD /v1.0/collections/{collectionId} -- Check Collection Exists](#head-v10collectionscollectionid--check-collection-exists)
    - [PUT /v1.0/collections/{collectionId} -- Update Collection](#put-v10collectionscollectionid--update-collection)
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

## Authentication & Authorization

As of **v0.3.0**, Lattice enforces authentication and role-based authorization. Authentication is **enabled by default**. When it is enabled, every route requires a valid credential except for the small set of [public routes](#public-unauthenticated-routes) listed below.

Requests carry their credential in the standard `Authorization` header:

```
Authorization: Bearer <value>
```

The header `x-token: <value>` is accepted as an alias for `Authorization: Bearer <value>`.

An unauthenticated request to a protected route is rejected with **HTTP 401**:

```json
{ "error": "Authentication required" }
```

An authenticated request that lacks the required permission is rejected with **HTTP 403**:

```json
{ "error": "Authorization denied: requires Collection:Create" }
```

The `<ResourceType>:<Operation>` in the 403 message names the permission that was required (see [Authorization (RBAC)](#authorization-rbac)).

### Authentication Methods

There are exactly **two** ways to authenticate, both presented as the `Authorization: Bearer <value>` header:

1. **Session token.** Obtain a session token by posting an email and password (and, optionally, a tenant id) to [`POST /v1.0/token`](#post-v10token--login). The tenant is inferred from the credentials when omitted; if they match more than one tenant the response asks you to choose one. The `token` returned in the response is used as the bearer value. Session tokens expire (default **60 minutes**); after expiry, log in again. A session may be ended early with [`DELETE /v1.0/token`](#delete-v10token--logout).
2. **Access key.** A credential's access key (format `access_...`) is presented **directly** as the bearer value. Access keys are long-lived and intended for machine-to-machine use. Create one with [`PUT /v1.0/credentials`](#put-v10credentials--create-credential); the raw access key is returned at creation and, because it is persisted, can also be retrieved later by reading the credential.

> There is **no** `x-api-key` header, no separate secret key, and no request signing. A bearer value is either a session token or an access key; the server resolves which one it is.

To inspect the identity a bearer value resolves to, call [`GET /v1.0/token`](#get-v10token--current-principal) (alias `GET /v1.0/whoami`).

### Public (Unauthenticated) Routes

When authentication is enabled, the following routes are reachable **without** a bearer value. Every other route requires authentication.

| Route | Purpose |
|-------|---------|
| `GET /` | Root health check. |
| `GET /v1.0/health` | Versioned health check. |
| `POST /v1.0/token` | Login (exchange credentials for a session token). |
| OpenAPI / Swagger specification | API description document. |

### Multi-Tenancy

Lattice is **single-tier multi-tenant**: every user, credential, and record belongs to exactly one tenant, and data and authorization decisions never cross tenants.

The active tenant is **resolved from the principal** — from the session token or from the credential that authenticated the request. **There is no tenant id in any URL.**

A **system administrator** (a principal whose `isAdmin` is `true`) may act on a tenant other than its own:

- For **writes** (e.g. `PUT /v1.0/users`), by including an explicit `tenantId` in the request body.
- For **list endpoints** (e.g. `GET /v1.0/users`), by passing a `tenantId` **query parameter**.

Non-admin principals are always scoped to their own tenant; a `tenantId` they supply is ignored.

### Authorization (RBAC)

Authorization is **role-based** and evaluated **deny-over-permit**: an explicit deny always wins over any permit. A **system administrator** and a **tenant administrator** (within their own tenant) bypass RBAC evaluation entirely.

A permission is a pair of **resource type** and **operation**, written `<ResourceType>:<Operation>`.

**Resource types:**

`All`, `Tenant`, `User`, `Credential`, `Session`, `Role`, `Permission`, `Assignment`, `Audit`, `Collection`, `Document`, `Schema`, `Index`, `RequestHistory`.

**Operations:**

| Operation | Meaning |
|-----------|---------|
| `All` | Every operation. |
| `Create` | Create a resource. |
| `Read` | Read or list a resource. |
| `Write` | Expands to `Create` + `Update` + `Delete`. |
| `Update` | Modify an existing resource. |
| `Delete` | Remove a resource. |
| `Execute` | Run an operation (e.g. search, rebuild). |
| `Admin` | Administrative control over the resource type. |

**Built-in roles:** `TenantAdmin`, `SecurityAdmin`, `Auditor`, `CollectionAdmin`, `Editor`, `Viewer`, `TenantMember`. Built-in roles are seeded at first boot, are global (not owned by a tenant), and are visible to every tenant. Roles are listed with [`GET /v1.0/roles`](#get-v10roles--list-roles) and bound to users with [`PUT /v1.0/assignments`](#put-v10assignments--create-assignment).

### First-Run Bootstrap

On its **first start**, the server seeds:

- A default **tenant**.
- A default **administrator** user — email `admin@lattice`, password `password`.
- A default **access key** for that administrator.

These bootstrap values (including the raw access key) are printed to the server console **once**, at first start. Change the default administrator password after the first login. Passwords are hashed with **SHA-256**; the server never stores or returns a plaintext password.

### Login Flow (Example)

**1. Exchange credentials for a session token.** `POST /v1.0/token` is public:

```bash
curl -X POST http://localhost:8000/v1.0/token \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@lattice",
    "password": "password",
    "tenantId": "ten_abcdef0123456789"
  }'
```

```json
{
  "token": "sess_9f8e7d6c5b4a...",
  "expiresUtc": "2026-09-03T13:00:00.000Z",
  "tenantId": "ten_abcdef0123456789",
  "userId": "usr_0123456789abcdef",
  "email": "admin@lattice",
  "isAdmin": true,
  "isTenantAdmin": false
}
```

**2. Use the session token** as the bearer on a protected route:

```bash
curl http://localhost:8000/v1.0/collections \
  -H "Authorization: Bearer sess_9f8e7d6c5b4a..."
```

**3. Or use an access key** directly as the bearer (no login step required):

```bash
curl http://localhost:8000/v1.0/collections \
  -H "Authorization: Bearer access_1a2b3c4d5e6f..."
```

The `x-token` header may be used in place of `Authorization: Bearer` with either value:

```bash
curl http://localhost:8000/v1.0/collections \
  -H "x-token: access_1a2b3c4d5e6f..."
```

> **MCP:** The MCP JSON-RPC endpoint at `POST /v1.0/mcp` uses the same bearer authentication described here and is documented separately in [`MCP_API.md`](MCP_API.md).

---

## Response Format

Lattice returns **raw payloads**. There is no response envelope -- the body of a successful response *is* the resource you requested, and the HTTP status code conveys success or failure.

**Success (HTTP 2xx):** The response body is the payload directly.

- List/enumeration endpoints return an **EnumerationResult** object with the items under `objects` (e.g. `GET /v1.0/collections` → `{ ..., "objects": [ { ... }, ... ] }`). See [Enumeration & Pagination](#enumeration--pagination).
- Single-resource endpoints return a JSON object (e.g. `GET /v1.0/collections/{id}` → `{ ... }`).
- Operations with no payload (e.g. some `DELETE`s) return an **empty body** (zero length) with a 2xx status.

```json
{
  "success": true,
  "totalRecords": 1,
  "objects": [
    { "id": "d4e5f6a7-b8c9-0123-4567-89abcdef0123", "name": "customers" }
  ]
}
```

**Error (HTTP 4xx/5xx):** The response body is an error object. The HTTP status code carries the status.

```json
{
  "error": "Name is required for collection creation"
}
```

When structured information is available, a `detail` field is included:

```json
{
  "error": "Schema validation failed",
  "detail": {
    "errors": [
      "Field 'email' is required but missing",
      "Field 'age' must be of type integer"
    ]
  }
}
```

### Response Headers

Request correlation and timing metadata are returned in response headers (previously fields on the envelope):

| Header | Description |
|--------|-------------|
| `X-Lattice-Request-Id` | Unique identifier for this request/response (UUID). Previously the envelope `guid`. |
| `X-Lattice-Processing-Time-Ms` | Server-side processing time in milliseconds. Previously the envelope `processingTimeMs`. |

All JSON property names use **camelCase**. Both success and error bodies use `Content-Type: application/json` (an empty success body still uses this content type).

---

## Enumeration & Pagination

All GET endpoints that list a set of resources return an **EnumerationResult** object rather than a bare JSON array. The items live in the `objects` array, and the surrounding fields carry pagination metadata.

**EnumerationResult shape (camelCase JSON):**

```json
{
  "success": true,
  "timestamp": {
    "start": "2024-01-15T12:00:00.000Z",
    "end": "2024-01-15T12:00:00.001Z",
    "totalMs": 0.67
  },
  "maxResults": 100,
  "skip": 0,
  "iterationsRequired": 1,
  "endOfResults": true,
  "totalRecords": 1,
  "recordsRemaining": 0,
  "objects": [ /* the items -- Collection, Document, Schema, etc. */ ]
}
```

**Fields:**

| Field | Type | Description |
|-------|------|-------------|
| `success` | boolean | Whether the enumeration completed successfully. |
| `timestamp` | object | Timing information with `start`, `end`, and `totalMs`. |
| `maxResults` | integer | The page size that was applied. |
| `skip` | integer | The number of records that were skipped. |
| `iterationsRequired` | integer | Number of internal iterations required to build the page. |
| `endOfResults` | boolean | `true` when no further records remain after this page. |
| `totalRecords` | integer | Total number of records available (across all pages). |
| `recordsRemaining` | integer | Number of records still available after this page. |
| `objects` | T[] | The array of returned items. |

**Query parameters (all optional):**

| Parameter | Type | Default | Max | Description |
|-----------|------|---------|-----|-------------|
| `maxResults` | integer | 100 | 1000 | Page size (minimum 1). |
| `skip` | integer | 0 | -- | Number of records to skip before returning results. |

To page through results, increment `skip` by `maxResults` until `endOfResults` is `true` (or `recordsRemaining` reaches `0`).

The following six GET endpoints return an EnumerationResult:

| Endpoint | `objects` item type |
|----------|---------------------|
| `GET /v1.0/collections` | Collection |
| `GET /v1.0/collections/{collectionId}/documents` | Document |
| `GET /v1.0/schemas` | Schema |
| `GET /v1.0/schemas/{schemaId}/elements` | SchemaElement |
| `GET /v1.0/tables` | IndexTableMapping |
| `GET /v1.0/tables/{tableName}/entries` | IndexTableEntry |

> Single-object GETs (e.g. `GET /v1.0/collections/{id}`), configuration objects (constraints, indexing), the `POST` search endpoint (which returns a `SearchResult`), and the request-history endpoints are **not** EnumerationResult and are documented with their own shapes.

---

## Error Handling

When an error occurs, the HTTP status code indicates the failure and the response body contains an error object:

```json
{
  "error": "Name is required for collection creation"
}
```

Errors that carry structured information add a `detail` field. Schema validation errors, for example:

```json
{
  "error": "Schema validation failed",
  "detail": {
    "errors": [
      "Field 'email' is required but missing",
      "Field 'age' must be of type integer"
    ]
  }
}
```

A document lock conflict (`409`) reports the lock holder in `detail`:

```json
{
  "error": "Document is locked",
  "detail": {
    "collectionId": "d4e5f6a7-b8c9-0123-4567-89abcdef0123",
    "documentName": "john-doe",
    "lockedByHostname": "server-02",
    "lockCreatedUtc": "2024-01-15T12:00:00.000Z"
  }
}
```

Clients should treat the HTTP status code as the source of truth: `2xx` means success, anything else is an error. On an error, parse `{ error, detail? }`; if the body is not JSON, fall back to the HTTP status text.

### HTTP Status Codes

| Code | Meaning | When Used |
|------|---------|-----------|
| 200 | OK | Successful read, update, or delete operations. |
| 201 | Created | Successful creation of a collection or document. |
| 400 | Bad Request | Missing required fields, invalid JSON, validation errors. |
| 401 | Unauthorized | Authentication required but no valid bearer value was presented. Body: `{ "error": "Authentication required" }`. See [Authentication & Authorization](#authentication--authorization). |
| 403 | Forbidden | Authenticated but the principal lacks the required permission. Body: `{ "error": "Authorization denied: requires <ResourceType>:<Operation>" }`. |
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
  "status": "Healthy",
  "version": "1.0.0",
  "timestamp": "2024-01-15T12:00:00.000Z"
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

### Authentication & Session

Routes for logging in, inspecting the current principal, and logging out. See [Authentication & Authorization](#authentication--authorization) for the overall model.

#### POST /v1.0/token -- Login

Exchanges an email and password for a session token. **This route is public** (no bearer required). The returned `token` is used as the bearer value on subsequent requests; it expires after the configured session lifetime (default 60 minutes).

`tenantId` is **optional**. When omitted, the tenant is inferred from the credentials. If the email and password match a user in exactly one tenant, a token is issued. If they match users in **more than one** tenant, the response instead has `tenantSelectionRequired: true` and lists the candidate `tenants` (no token) — repeat the request with a chosen `tenantId`. A wrong password matches no tenants (it does not reveal tenant membership).

**cURL:**

```bash
# Tenant inferred from the credentials (omit tenantId):
curl -X POST http://localhost:8000/v1.0/token \
  -H "Content-Type: application/json" \
  -d '{ "email": "admin@lattice", "password": "password" }'
```

**Request Body:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `email` | string | **Yes** | User email (unique within a tenant). |
| `password` | string | **Yes** | User password. |
| `tenantId` | string | No | Tenant to authenticate against. Omit to infer it from the credentials. |

**Response (200 OK) -- token issued:**

```json
{
  "token": "sess_9f8e7d6c5b4a...",
  "expiresUtc": "2026-09-03T13:00:00.000Z",
  "tenantId": "ten_abcdef0123456789",
  "userId": "usr_0123456789abcdef",
  "email": "admin@lattice",
  "isAdmin": true,
  "isTenantAdmin": false
}
```

**Response (200 OK) -- tenant selection required** (credentials match multiple tenants and none was supplied):

```json
{
  "tenantSelectionRequired": true,
  "tenants": [
    { "tenantId": "ten_abcdef0123456789", "tenantName": "Acme" },
    { "tenantId": "ten_1122334455667788", "tenantName": "Globex" }
  ]
}
```

Repeat the request including the chosen `tenantId` to obtain a token.

**Errors:**

- `400 Bad Request` -- `email` or `password` missing. Body: `{ "error": "email and password are required" }`.
- `401 Unauthorized` -- Credentials are invalid or the user/tenant is inactive. Body: `{ "error": "Invalid credentials" }`.

---

#### GET /v1.0/token -- Current Principal

Returns the resolved principal for the bearer value on the request. Requires any authenticated principal. `GET /v1.0/whoami` and `GET /v1.0/token/details` are aliases that return the same payload.

**cURL:**

```bash
curl http://localhost:8000/v1.0/token \
  -H "Authorization: Bearer sess_9f8e7d6c5b4a..."
```

**Response (200 OK):**

```json
{
  "isAuthenticated": true,
  "principalType": "User",
  "tenantId": "ten_abcdef0123456789",
  "userId": "usr_0123456789abcdef",
  "credentialId": null,
  "email": "admin@lattice",
  "isAdmin": true,
  "isTenantAdmin": false
}
```

`principalType` is `User` when authenticated with a session token, or `Credential` when authenticated with an access key (in which case `credentialId` is populated and `userId` refers to the credential's owning user).

**Errors:**

- `401 Unauthorized` -- No valid bearer value was presented. Body: `{ "error": "Authentication required" }`.

---

#### DELETE /v1.0/token -- Logout

Revokes the current session (logout). Applies to session-token principals; the underlying session is marked revoked and can no longer authenticate. Requires any authenticated principal.

**cURL:**

```bash
curl -X DELETE http://localhost:8000/v1.0/token \
  -H "Authorization: Bearer sess_9f8e7d6c5b4a..."
```

**Response (200 OK):** Empty body (zero length).

---

### Tenants

Tenant management. These routes require `Tenant` permissions and in practice are used by a system administrator. See [Multi-Tenancy](#multi-tenancy).

#### GET /v1.0/tenants -- List Tenants

Lists tenants. Returns an [EnumerationResult](#enumeration--pagination) with `Tenant` items in `objects`.

**Query Parameters:**

| Parameter | Type | Default | Max | Description |
|-----------|------|---------|-----|-------------|
| `maxResults` | integer | 100 | 1000 | Page size (minimum 1). |
| `skip` | integer | 0 | -- | Number of records to skip. |

**cURL:**

```bash
curl "http://localhost:8000/v1.0/tenants?maxResults=100&skip=0" \
  -H "Authorization: Bearer sess_9f8e7d6c5b4a..."
```

**Response (200 OK):**

```json
{
  "success": true,
  "timestamp": { "start": "2026-09-03T12:00:00.000Z", "end": "2026-09-03T12:00:00.001Z", "totalMs": 0.42 },
  "maxResults": 100,
  "skip": 0,
  "iterationsRequired": 1,
  "endOfResults": true,
  "totalRecords": 1,
  "recordsRemaining": 0,
  "objects": [
    {
      "id": "ten_abcdef0123456789",
      "name": "Default",
      "active": true,
      "isProtected": true,
      "createdUtc": "2026-09-01T00:00:00.000Z",
      "lastUpdateUtc": "2026-09-01T00:00:00.000Z"
    }
  ]
}
```

---

#### PUT /v1.0/tenants -- Create Tenant

Creates a new tenant.

**cURL:**

```bash
curl -X PUT http://localhost:8000/v1.0/tenants \
  -H "Authorization: Bearer sess_9f8e7d6c5b4a..." \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Acme, Inc.",
    "region": "us-east"
  }'
```

**Request Body:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `name` | string | **Yes** | Human-readable tenant name. |
| `region` | string | No | Optional region label. |

**Response (201 Created):**

```json
{
  "id": "ten_0f1e2d3c4b5a6978",
  "name": "Acme, Inc.",
  "region": "us-east",
  "active": true,
  "isProtected": false,
  "createdUtc": "2026-09-03T12:00:00.000Z",
  "lastUpdateUtc": "2026-09-03T12:00:00.000Z"
}
```

**Errors:**

- `400 Bad Request` -- `name` is missing. Body: `{ "error": "name is required" }`.

---

#### GET /v1.0/tenants/{tenantId} -- Get Tenant

Retrieves a tenant by id.

**Path Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `tenantId` | string | The unique identifier of the tenant. |

**cURL:**

```bash
curl http://localhost:8000/v1.0/tenants/ten_abcdef0123456789 \
  -H "Authorization: Bearer sess_9f8e7d6c5b4a..."
```

**Response (200 OK):** A `Tenant` object (see [Tenant](#tenant)).

**Errors:**

- `404 Not Found` -- Tenant does not exist. Body: `{ "error": "Tenant not found" }`.

---

#### PUT /v1.0/tenants/{tenantId} -- Update Tenant

Updates a tenant. **Only the fields supplied** in the body are changed; omitted fields are left untouched.

**Path Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `tenantId` | string | The unique identifier of the tenant. |

**cURL:**

```bash
curl -X PUT http://localhost:8000/v1.0/tenants/ten_0f1e2d3c4b5a6978 \
  -H "Authorization: Bearer sess_9f8e7d6c5b4a..." \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Acme Holdings",
    "active": true
  }'
```

**Request Body:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `name` | string | No | New tenant name. Omit to leave unchanged. |
| `active` | boolean | No | New active flag. Omit to leave unchanged. |

**Response (200 OK):** The updated `Tenant` object (see [Tenant](#tenant)).

**Errors:**

- `404 Not Found` -- Tenant does not exist or is not visible. Body: `{ "error": "Tenant not found" }`.

---

#### DELETE /v1.0/tenants/{tenantId} -- Delete Tenant

Deletes a tenant. Protected tenants (e.g. the seeded default tenant) cannot be deleted.

**Path Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `tenantId` | string | The unique identifier of the tenant. |

**cURL:**

```bash
curl -X DELETE http://localhost:8000/v1.0/tenants/ten_0f1e2d3c4b5a6978 \
  -H "Authorization: Bearer sess_9f8e7d6c5b4a..."
```

**Response (200 OK):** Empty body (zero length).

**Errors:**

- `404 Not Found` -- Tenant does not exist. Body: `{ "error": "Tenant not found" }`.
- `409 Conflict` -- Tenant is protected. Body: `{ "error": "Tenant is protected" }`.

---

### Users

User management within a tenant. Passwords are supplied at creation and stored only as a SHA-256 hash; the hash is **never** returned.

#### GET /v1.0/users -- List Users

Lists users in the caller's tenant. Returns an [EnumerationResult](#enumeration--pagination) with `User` items in `objects`. A system administrator may list another tenant's users with the `tenantId` query parameter.

**Query Parameters:**

| Parameter | Type | Default | Max | Description |
|-----------|------|---------|-----|-------------|
| `tenantId` | string | -- | -- | System administrators only: list users in this tenant instead of the caller's. |
| `maxResults` | integer | 100 | 1000 | Page size (minimum 1). |
| `skip` | integer | 0 | -- | Number of records to skip. |

**cURL:**

```bash
curl "http://localhost:8000/v1.0/users?maxResults=100&skip=0" \
  -H "Authorization: Bearer sess_9f8e7d6c5b4a..."
```

**Response (200 OK):**

```json
{
  "success": true,
  "timestamp": { "start": "2026-09-03T12:00:00.000Z", "end": "2026-09-03T12:00:00.001Z", "totalMs": 0.51 },
  "maxResults": 100,
  "skip": 0,
  "iterationsRequired": 1,
  "endOfResults": true,
  "totalRecords": 1,
  "recordsRemaining": 0,
  "objects": [
    {
      "id": "usr_0123456789abcdef",
      "tenantId": "ten_abcdef0123456789",
      "firstName": "Default",
      "lastName": "Administrator",
      "email": "admin@lattice",
      "isAdmin": true,
      "isTenantAdmin": false,
      "active": true,
      "isProtected": true,
      "createdUtc": "2026-09-01T00:00:00.000Z",
      "lastUpdateUtc": "2026-09-01T00:00:00.000Z"
    }
  ]
}
```

---

#### PUT /v1.0/users -- Create User

Creates a user. The `password` is hashed (SHA-256) server-side and never returned. `isAdmin` is honored only when the caller is itself a system administrator; otherwise it is forced to `false`.

**cURL:**

```bash
curl -X PUT http://localhost:8000/v1.0/users \
  -H "Authorization: Bearer sess_9f8e7d6c5b4a..." \
  -H "Content-Type: application/json" \
  -d '{
    "email": "jane@acme.example",
    "password": "s3cr3t!",
    "firstName": "Jane",
    "lastName": "Smith",
    "isTenantAdmin": false
  }'
```

**Request Body:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `email` | string | **Yes** | User email (unique within the tenant). |
| `password` | string | **Yes** | Plaintext password; stored only as a SHA-256 hash. |
| `firstName` | string | No | First name. |
| `lastName` | string | No | Last name. |
| `isAdmin` | boolean | No | System administrator. Honored only when the caller is a system administrator. Default `false`. |
| `isTenantAdmin` | boolean | No | Tenant administrator. Default `false`. |
| `tenantId` | string | No | Target tenant. Honored only for system administrators acting cross-tenant. |

**Response (201 Created):**

```json
{
  "id": "usr_fedcba9876543210",
  "tenantId": "ten_abcdef0123456789",
  "firstName": "Jane",
  "lastName": "Smith",
  "email": "jane@acme.example",
  "isAdmin": false,
  "isTenantAdmin": false,
  "active": true,
  "isProtected": false,
  "createdUtc": "2026-09-03T12:00:00.000Z",
  "lastUpdateUtc": "2026-09-03T12:00:00.000Z"
}
```

**Errors:**

- `400 Bad Request` -- `email` or `password` is missing. Body: `{ "error": "email and password are required" }`.

---

#### GET /v1.0/users/{userId} -- Get User

Retrieves a user by id. Non-admins may only read users in their own tenant.

**Path Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `userId` | string | The unique identifier of the user. |

**cURL:**

```bash
curl http://localhost:8000/v1.0/users/usr_fedcba9876543210 \
  -H "Authorization: Bearer sess_9f8e7d6c5b4a..."
```

**Response (200 OK):** A `User` object with `passwordSha256` omitted (see [User](#user)).

**Errors:**

- `404 Not Found` -- User does not exist or is not visible to the caller's tenant. Body: `{ "error": "User not found" }`.

---

#### PUT /v1.0/users/{userId} -- Update User

Updates a user. **Only the fields supplied** in the body are changed. A new `password` is hashed (SHA-256) server-side and is never returned. `isAdmin` is honored only when the caller is itself a system administrator; otherwise it is ignored.

**Path Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `userId` | string | The unique identifier of the user. |

**cURL:**

```bash
curl -X PUT http://localhost:8000/v1.0/users/usr_fedcba9876543210 \
  -H "Authorization: Bearer sess_9f8e7d6c5b4a..." \
  -H "Content-Type: application/json" \
  -d '{
    "firstName": "Jane",
    "lastName": "Doe",
    "isTenantAdmin": true
  }'
```

**Request Body:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `firstName` | string | No | New first name. Omit to leave unchanged. |
| `lastName` | string | No | New last name. Omit to leave unchanged. |
| `password` | string | No | New password; stored only as a SHA-256 hash and never returned. Omit to leave unchanged. |
| `isTenantAdmin` | boolean | No | New tenant-administrator flag. Omit to leave unchanged. |
| `isAdmin` | boolean | No | New system-administrator flag. Honored only when the caller is a system administrator; otherwise ignored. Omit to leave unchanged. |
| `active` | boolean | No | New active flag. Omit to leave unchanged. |

**Response (200 OK):** The updated `User` object with `passwordSha256` omitted (see [User](#user)).

**Errors:**

- `404 Not Found` -- User does not exist or is not visible. Body: `{ "error": "User not found" }`.

---

#### DELETE /v1.0/users/{userId} -- Delete User

Deletes a user. Protected users (e.g. the seeded default administrator) cannot be deleted.

**Path Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `userId` | string | The unique identifier of the user. |

**cURL:**

```bash
curl -X DELETE http://localhost:8000/v1.0/users/usr_fedcba9876543210 \
  -H "Authorization: Bearer sess_9f8e7d6c5b4a..."
```

**Response (200 OK):** Empty body (zero length).

**Errors:**

- `404 Not Found` -- User does not exist or is not visible. Body: `{ "error": "User not found" }`.
- `409 Conflict` -- User is protected. Body: `{ "error": "User is protected" }`.

---

### Credentials

Machine credentials (access keys) owned by a user within a tenant. The raw access key is returned at creation **and** is persisted, so it can be retrieved again on subsequent credential reads.

> **Security note:** The raw access key is stored in **plaintext** (alongside its SHA-256 hash) so that it can be returned on credential reads. Only the `accessKeySha256` hash is ever withheld from responses. Treat access keys as secrets and restrict `Credential` read permissions accordingly.

#### GET /v1.0/credentials -- List Credentials

Lists credentials in the caller's tenant. Returns an [EnumerationResult](#enumeration--pagination) with `Credential` items in `objects`. A system administrator may list another tenant's credentials with the `tenantId` query parameter. The raw `accessKey` **is** included on each item (it is persisted); only the stored `accessKeySha256` hash is withheld.

**Query Parameters:**

| Parameter | Type | Default | Max | Description |
|-----------|------|---------|-----|-------------|
| `tenantId` | string | -- | -- | System administrators only: list credentials in this tenant instead of the caller's. |
| `maxResults` | integer | 100 | 1000 | Page size (minimum 1). |
| `skip` | integer | 0 | -- | Number of records to skip. |

**cURL:**

```bash
curl "http://localhost:8000/v1.0/credentials?maxResults=100&skip=0" \
  -H "Authorization: Bearer sess_9f8e7d6c5b4a..."
```

**Response (200 OK):**

```json
{
  "success": true,
  "timestamp": { "start": "2026-09-03T12:00:00.000Z", "end": "2026-09-03T12:00:00.001Z", "totalMs": 0.48 },
  "maxResults": 100,
  "skip": 0,
  "iterationsRequired": 1,
  "endOfResults": true,
  "totalRecords": 1,
  "recordsRemaining": 0,
  "objects": [
    {
      "id": "crd_1122334455667788",
      "tenantId": "ten_abcdef0123456789",
      "userId": "usr_0123456789abcdef",
      "name": "default",
      "accessKey": "access_1a2b3c4d5e6f7a8b9cd12",
      "accessKeyLast4": "cd12",
      "active": true,
      "isProtected": true,
      "createdUtc": "2026-09-01T00:00:00.000Z",
      "lastUpdateUtc": "2026-09-01T00:00:00.000Z"
    }
  ]
}
```

---

#### PUT /v1.0/credentials -- Create Credential

Creates a credential and generates its access key. The raw `accessKey` (format `access_...`) is included in the response and is also persisted, so it can be retrieved again on subsequent credential reads (see the security note in [Credentials](#credentials)). When `userId` is omitted, the credential is owned by the calling user.

**cURL:**

```bash
curl -X PUT http://localhost:8000/v1.0/credentials \
  -H "Authorization: Bearer sess_9f8e7d6c5b4a..." \
  -H "Content-Type: application/json" \
  -d '{
    "name": "ci-pipeline"
  }'
```

**Request Body:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `name` | string | No | Human-readable credential name. |
| `userId` | string | No | Owning user id. Defaults to the calling user when omitted. |
| `tenantId` | string | No | Target tenant. Honored only for system administrators acting cross-tenant. |

**Response (201 Created):**

```json
{
  "id": "crd_99aabbccddeeff00",
  "tenantId": "ten_abcdef0123456789",
  "userId": "usr_0123456789abcdef",
  "name": "ci-pipeline",
  "accessKey": "access_1a2b3c4d5e6f7a8b9c0d",
  "accessKeyLast4": "0c0d",
  "active": true,
  "isProtected": false,
  "createdUtc": "2026-09-03T12:00:00.000Z",
  "lastUpdateUtc": "2026-09-03T12:00:00.000Z"
}
```

> The `accessKey` field is persisted and is also returned on subsequent reads of the credential (`GET` list and `GET` by id). Only the stored `accessKeySha256` hash is never returned.

**Errors:**

- `400 Bad Request` -- No `userId` supplied and none could be inferred from the caller. Body: `{ "error": "userId is required" }`.

---

#### GET /v1.0/credentials/{credentialId} -- Get Credential

Retrieves a credential by id. The raw `accessKey` **is** returned (it is persisted server-side); only the stored `accessKeySha256` hash is withheld. Non-admins may only read credentials in their own tenant.

**Path Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `credentialId` | string | The unique identifier of the credential. |

**cURL:**

```bash
curl http://localhost:8000/v1.0/credentials/crd_99aabbccddeeff00 \
  -H "Authorization: Bearer sess_9f8e7d6c5b4a..."
```

**Response (200 OK):** A `Credential` object including the raw `accessKey`; the stored `accessKeySha256` hash is omitted (see [Credential](#credential)).

**Errors:**

- `404 Not Found` -- Credential does not exist or is not visible. Body: `{ "error": "Credential not found" }`.

---

#### PUT /v1.0/credentials/{credentialId} -- Update Credential

Updates a credential. **Only the fields supplied** in the body are changed. The access key itself cannot be rotated here; the stored `accessKeySha256` hash is never returned.

**Path Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `credentialId` | string | The unique identifier of the credential. |

**cURL:**

```bash
curl -X PUT http://localhost:8000/v1.0/credentials/crd_99aabbccddeeff00 \
  -H "Authorization: Bearer sess_9f8e7d6c5b4a..." \
  -H "Content-Type: application/json" \
  -d '{
    "name": "ci-pipeline-renamed",
    "active": false
  }'
```

**Request Body:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `name` | string | No | New credential name. Omit to leave unchanged. |
| `active` | boolean | No | New active flag. Omit to leave unchanged. |

**Response (200 OK):** The updated `Credential` object including the raw `accessKey`; the stored `accessKeySha256` hash is omitted (see [Credential](#credential)).

**Errors:**

- `404 Not Found` -- Credential does not exist or is not visible. Body: `{ "error": "Credential not found" }`.

---

#### DELETE /v1.0/credentials/{credentialId} -- Delete Credential

Deletes (revokes) a credential. Protected credentials (e.g. the seeded default credential) cannot be deleted.

**Path Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `credentialId` | string | The unique identifier of the credential. |

**cURL:**

```bash
curl -X DELETE http://localhost:8000/v1.0/credentials/crd_99aabbccddeeff00 \
  -H "Authorization: Bearer sess_9f8e7d6c5b4a..."
```

**Response (200 OK):** Empty body (zero length).

**Errors:**

- `404 Not Found` -- Credential does not exist or is not visible. Body: `{ "error": "Credential not found" }`.
- `409 Conflict` -- Credential is protected. Body: `{ "error": "Credential is protected" }`.

---

### Roles

Roles group permissions and are bound to users through [assignments](#assignments). Built-in roles are seeded at first boot and visible to every tenant. See [Authorization (RBAC)](#authorization-rbac).

#### GET /v1.0/roles -- List Roles

Lists the roles visible to the caller's tenant (built-in roles plus any custom roles owned by the tenant). Returns an [EnumerationResult](#enumeration--pagination) with `UserRole` items in `objects`.

**Query Parameters:**

| Parameter | Type | Default | Max | Description |
|-----------|------|---------|-----|-------------|
| `maxResults` | integer | 100 | 1000 | Page size (minimum 1). |
| `skip` | integer | 0 | -- | Number of records to skip. |

**cURL:**

```bash
curl http://localhost:8000/v1.0/roles \
  -H "Authorization: Bearer sess_9f8e7d6c5b4a..."
```

**Response (200 OK):**

```json
{
  "success": true,
  "timestamp": { "start": "2026-09-03T12:00:00.000Z", "end": "2026-09-03T12:00:00.001Z", "totalMs": 0.39 },
  "maxResults": 100,
  "skip": 0,
  "iterationsRequired": 1,
  "endOfResults": true,
  "totalRecords": 7,
  "recordsRemaining": 0,
  "objects": [
    {
      "id": "rol_00112233",
      "name": "TenantAdmin",
      "isBuiltIn": true,
      "active": true,
      "isProtected": true,
      "createdUtc": "2026-09-01T00:00:00.000Z",
      "lastUpdateUtc": "2026-09-01T00:00:00.000Z"
    },
    {
      "id": "rol_44556677",
      "name": "Viewer",
      "isBuiltIn": true,
      "active": true,
      "isProtected": true,
      "createdUtc": "2026-09-01T00:00:00.000Z",
      "lastUpdateUtc": "2026-09-01T00:00:00.000Z"
    }
  ]
}
```

> A built-in role has a null `tenantId` (omitted from the response). A tenant-scoped custom role carries the owning `tenantId`.

---

#### PUT /v1.0/roles -- Create Role

Creates a **custom role** owned by the caller's tenant, together with the grants (permissions) it confers. Built-in roles are global and read-only; a custom role's name must be unique within the tenant.

Each entry in `permissions` is a **grant** — a `permissionType` of `permit` or `deny`, a list of `resourceTypes`, and a list of `operationTypes`. Authorization is evaluated **deny-over-permit** (see [Authorization (RBAC)](#authorization-rbac)). A grant is skipped unless it names at least one resource type and one operation type.

**cURL:**

```bash
curl -X PUT http://localhost:8000/v1.0/roles \
  -H "Authorization: Bearer sess_9f8e7d6c5b4a..." \
  -H "Content-Type: application/json" \
  -d '{
    "name": "collection-reader",
    "permissions": [
      {
        "permissionType": "permit",
        "resourceTypes": ["collection", "document"],
        "operationTypes": ["read"]
      }
    ]
  }'
```

**Request Body:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `name` | string | **Yes** | Role name (unique within the tenant). |
| `permissions` | RolePermissionSpec[] | No | The grants the role confers. See [RolePermissionSpec](#rolepermissionspec). |

**RolePermissionSpec fields:**

| Field | Type | Description |
|-------|------|-------------|
| `permissionType` | string | `permit` or `deny`. A deny wins over any permit during evaluation. |
| `resourceTypes` | string[] | Resource types the grant covers: `all`, `tenant`, `user`, `credential`, `session`, `role`, `permission`, `assignment`, `audit`, `collection`, `document`, `schema`, `index`, `requestHistory`. |
| `operationTypes` | string[] | Operations the grant covers: `all`, `create`, `read`, `write` (expands to `create` + `update` + `delete`), `update`, `delete`, `execute`, `admin`. |

**Response (201 Created):** The created role with its `permissions` (see [RoleDetailResponse](#roledetailresponse)).

```json
{
  "id": "rol_8899aabbccddeeff",
  "tenantId": "ten_abcdef0123456789",
  "name": "collection-reader",
  "isBuiltIn": false,
  "active": true,
  "isProtected": false,
  "createdUtc": "2026-09-03T12:00:00.000Z",
  "lastUpdateUtc": "2026-09-03T12:00:00.000Z",
  "permissions": [
    {
      "permissionType": "permit",
      "resourceTypes": ["collection", "document"],
      "operationTypes": ["read"]
    }
  ]
}
```

**Errors:**

- `400 Bad Request` -- `name` is missing. Body: `{ "error": "name is required" }`.
- `409 Conflict` -- A role with that name already exists. Body: `{ "error": "A role with that name already exists" }`.

---

#### GET /v1.0/roles/{roleId} -- Get Role

Retrieves a role by id together with its grants. Built-in (global) roles are visible to every tenant; a custom role is visible only to its owning tenant (a system administrator may read any role).

**Path Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `roleId` | string | The unique identifier of the role. |

**cURL:**

```bash
curl http://localhost:8000/v1.0/roles/rol_8899aabbccddeeff \
  -H "Authorization: Bearer sess_9f8e7d6c5b4a..."
```

**Response (200 OK):** The role with its `permissions` (see [RoleDetailResponse](#roledetailresponse)).

**Errors:**

- `404 Not Found` -- Role does not exist or is not visible. Body: `{ "error": "Role not found" }`.

---

#### PUT /v1.0/roles/{roleId} -- Update Role

Updates a custom role. A supplied `name` renames the role; a supplied `permissions` array **replaces** the role's grants in full (the previous grants are cleared first). Omitting `permissions` leaves the existing grants untouched. Built-in (global) roles are read-only and cannot be modified.

**Path Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `roleId` | string | The unique identifier of the role. |

**cURL:**

```bash
curl -X PUT http://localhost:8000/v1.0/roles/rol_8899aabbccddeeff \
  -H "Authorization: Bearer sess_9f8e7d6c5b4a..." \
  -H "Content-Type: application/json" \
  -d '{
    "name": "collection-editor",
    "permissions": [
      {
        "permissionType": "permit",
        "resourceTypes": ["collection", "document"],
        "operationTypes": ["read", "write"]
      }
    ]
  }'
```

**Request Body:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `name` | string | No | New role name. Omit to leave unchanged. |
| `permissions` | RolePermissionSpec[] | No | Replacement set of grants. When supplied, it **fully replaces** the role's existing grants. Omit to leave the grants unchanged. See [RolePermissionSpec](#rolepermissionspec). |

**Response (200 OK):** The updated role with its `permissions` (see [RoleDetailResponse](#roledetailresponse)).

**Errors:**

- `404 Not Found` -- Role does not exist or is not visible. Body: `{ "error": "Role not found" }`.
- `409 Conflict` -- The role is a built-in (global) role and cannot be modified. Body: `{ "error": "Built-in roles cannot be modified" }`.

---

#### DELETE /v1.0/roles/{roleId} -- Delete Role

Deletes a custom role along with all of its grants. Built-in (global) roles cannot be deleted.

**Path Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `roleId` | string | The unique identifier of the role. |

**cURL:**

```bash
curl -X DELETE http://localhost:8000/v1.0/roles/rol_8899aabbccddeeff \
  -H "Authorization: Bearer sess_9f8e7d6c5b4a..."
```

**Response (200 OK):** Empty body (zero length).

**Errors:**

- `404 Not Found` -- Role does not exist or is not visible. Body: `{ "error": "Role not found" }`.
- `409 Conflict` -- The role is a built-in (global) role and cannot be deleted. Body: `{ "error": "Built-in roles cannot be deleted" }`.

---

### Assignments

Assignments bind a role to a user, optionally scoped to a specific resource. See [Authorization (RBAC)](#authorization-rbac).

#### GET /v1.0/assignments -- List Assignments

Lists role assignments in the caller's tenant. Returns an [EnumerationResult](#enumeration--pagination) with `UserRoleAssignment` items in `objects`. A system administrator may list another tenant's assignments with the `tenantId` query parameter.

**Query Parameters:**

| Parameter | Type | Default | Max | Description |
|-----------|------|---------|-----|-------------|
| `tenantId` | string | -- | -- | System administrators only: list assignments in this tenant instead of the caller's. |
| `maxResults` | integer | 100 | 1000 | Page size (minimum 1). |
| `skip` | integer | 0 | -- | Number of records to skip. |

**cURL:**

```bash
curl "http://localhost:8000/v1.0/assignments?maxResults=100&skip=0" \
  -H "Authorization: Bearer sess_9f8e7d6c5b4a..."
```

**Response (200 OK):**

```json
{
  "success": true,
  "timestamp": { "start": "2026-09-03T12:00:00.000Z", "end": "2026-09-03T12:00:00.001Z", "totalMs": 0.44 },
  "maxResults": 100,
  "skip": 0,
  "iterationsRequired": 1,
  "endOfResults": true,
  "totalRecords": 1,
  "recordsRemaining": 0,
  "objects": [
    {
      "id": "ura_a1b2c3d4",
      "tenantId": "ten_abcdef0123456789",
      "userId": "usr_fedcba9876543210",
      "roleId": null,
      "roleName": "Editor",
      "resourceScope": "tenant",
      "resourceId": null,
      "inheritsToChildren": true,
      "active": true,
      "createdUtc": "2026-09-03T12:00:00.000Z",
      "lastUpdateUtc": "2026-09-03T12:00:00.000Z"
    }
  ]
}
```

---

#### PUT /v1.0/assignments -- Create Assignment

Assigns a role to a user. The request must include `userId` and **one of** `roleId` or `roleName`. A `roleName` resolves to a built-in role definition when no stored role record matches.

**cURL:**

```bash
curl -X PUT http://localhost:8000/v1.0/assignments \
  -H "Authorization: Bearer sess_9f8e7d6c5b4a..." \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "usr_fedcba9876543210",
    "roleName": "Editor"
  }'
```

**Request Body:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `userId` | string | **Yes** | User the assignment applies to. |
| `roleId` | string | Cond. | Assigned role by id. Provide this **or** `roleName`. |
| `roleName` | string | Cond. | Assigned role by name (e.g. `Editor`). Provide this **or** `roleId`. |
| `resourceScope` | string | No | `tenant` (default) or `resource`. |
| `resourceId` | string | No | Target resource id when `resourceScope` is `resource`. |
| `tenantId` | string | No | Target tenant. Honored only for system administrators acting cross-tenant. |

**Response (201 Created):** The created `UserRoleAssignment` (see [UserRoleAssignment](#userroleassignment)).

**Errors:**

- `400 Bad Request` -- `userId` is missing, or neither `roleId` nor `roleName` was supplied. Body: `{ "error": "userId and a roleId or roleName are required" }`.

---

#### DELETE /v1.0/assignments/{assignmentId} -- Delete Assignment

Removes a role assignment.

**Path Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `assignmentId` | string | The unique identifier of the assignment. |

**cURL:**

```bash
curl -X DELETE http://localhost:8000/v1.0/assignments/ura_a1b2c3d4 \
  -H "Authorization: Bearer sess_9f8e7d6c5b4a..."
```

**Response (200 OK):** Empty body (zero length).

**Errors:**

- `404 Not Found` -- Assignment does not exist or is not visible. Body: `{ "error": "Assignment not found" }`.

---

### Audit

The security audit log is an append-only record of authentication and authorization events. In particular, authentication failures are recorded as `AuthFailure` (with response code `401`) and authorization denials as `AuthzDenied` (with response code `403`); each entry captures the principal, the required permission, the verdict, the request path, and the response code.

#### GET /v1.0/audit -- List Audit Entries

Lists audit entries for the caller's tenant. Returns an [EnumerationResult](#enumeration--pagination) with `AuditEntry` items in `objects`. A system administrator may target another tenant with the `tenantId` query parameter.

**Query Parameters:**

| Parameter | Type | Default | Max | Description |
|-----------|------|---------|-----|-------------|
| `eventType` | string | -- | -- | Filter to a single event type (e.g. `AuthFailure`, `AuthzDenied`). |
| `tenantId` | string | -- | -- | System administrators only: audit entries for this tenant instead of the caller's. |
| `maxResults` | integer | 100 | 1000 | Page size (minimum 1). |
| `skip` | integer | 0 | -- | Number of records to skip. |

**cURL:**

```bash
curl "http://localhost:8000/v1.0/audit?eventType=AuthzDenied&maxResults=50&skip=0" \
  -H "Authorization: Bearer sess_9f8e7d6c5b4a..."
```

**Response (200 OK):**

```json
{
  "success": true,
  "timestamp": { "start": "2026-09-03T12:00:00.000Z", "end": "2026-09-03T12:00:00.002Z", "totalMs": 1.10 },
  "maxResults": 50,
  "skip": 0,
  "iterationsRequired": 1,
  "endOfResults": true,
  "totalRecords": 1,
  "recordsRemaining": 0,
  "objects": [
    {
      "id": "aud_5566778899",
      "tenantId": "ten_abcdef0123456789",
      "eventType": "AuthzDenied",
      "requestId": "0c5a6f2e-1b3d-4a7c-9e8f-2b1c0d3e4f5a",
      "principalType": "User",
      "principalId": "usr_fedcba9876543210",
      "userId": "usr_fedcba9876543210",
      "resourceType": "Collection",
      "requestType": "Collection",
      "method": "PUT",
      "path": "/v1.0/collections",
      "sourceIp": "127.0.0.1",
      "authzResult": "DeniedImplicit",
      "denialReason": "no permit for Collection:Create",
      "requiredPermission": "Collection:Create",
      "responseCode": 403,
      "createdUtc": "2026-09-03T12:00:00.000Z"
    }
  ]
}
```

---

#### GET /v1.0/audit/{auditId} -- Get Audit Entry

Retrieves a single audit entry by id. Non-admins may only read entries in their own tenant.

**Path Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `auditId` | string | The unique identifier of the audit entry. |

**cURL:**

```bash
curl http://localhost:8000/v1.0/audit/aud_5566778899 \
  -H "Authorization: Bearer sess_9f8e7d6c5b4a..."
```

**Response (200 OK):** An `AuditEntry` object (see [AuditEntry](#auditentry)).

**Errors:**

- `404 Not Found` -- Audit entry does not exist or is not visible. Body: `{ "error": "Audit entry not found" }`.

---

#### DELETE /v1.0/audit/{auditId} -- Delete Audit Entry

Deletes an audit entry.

**Path Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `auditId` | string | The unique identifier of the audit entry. |

**cURL:**

```bash
curl -X DELETE http://localhost:8000/v1.0/audit/aud_5566778899 \
  -H "Authorization: Bearer sess_9f8e7d6c5b4a..."
```

**Response (200 OK):** Empty body (zero length).

**Errors:**

- `404 Not Found` -- Audit entry does not exist or is not visible. Body: `{ "error": "Audit entry not found" }`.

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
}
```

**Errors:**

- `400 Bad Request` -- `name` is missing or empty. Body: `{ "error": "Name is required for collection creation" }`.

---

#### GET /v1.0/collections -- List Collections

Retrieves all collections. Returns an [EnumerationResult](#enumeration--pagination) with `Collection` items in `objects`.

**Query Parameters:**

| Parameter | Type | Default | Max | Description |
|-----------|------|---------|-----|-------------|
| `maxResults` | integer | 100 | 1000 | Page size (minimum 1). |
| `skip` | integer | 0 | -- | Number of records to skip. |

**cURL:**

```bash
curl "http://localhost:8000/v1.0/collections?maxResults=100&skip=0"
```

**Response (200 OK):**

```json
{
  "success": true,
  "timestamp": { "start": "2024-01-15T12:00:00.000Z", "end": "2024-01-15T12:00:00.001Z", "totalMs": 0.67 },
  "maxResults": 100,
  "skip": 0,
  "iterationsRequired": 1,
  "endOfResults": true,
  "totalRecords": 1,
  "recordsRemaining": 0,
  "objects": [
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
  ]
}
```

> On first server start (when no collections exist), Lattice automatically creates a collection named `default` (indexing mode `all`, schema enforcement `none`), so this list is never empty on a fresh install.

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
```

**Errors:**

- `404 Not Found` -- Collection with the given ID does not exist. Body: `{ "error": "Collection not found" }`.

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

#### PUT /v1.0/collections/{collectionId} -- Update Collection

Updates a collection's descriptive fields. **Only the fields supplied** in the body are changed; omitted fields are left untouched. (Schema constraints and indexing configuration are managed through the dedicated [constraints](#put-v10collectionscollectionidconstraints--update-constraints) and [indexing](#put-v10collectionscollectionidindexing--update-indexing-config) endpoints.)

**Path Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `collectionId` | string (UUID) | The unique identifier of the collection. |

**cURL:**

```bash
curl -X PUT http://localhost:8000/v1.0/collections/d4e5f6a7-b8c9-0123-4567-89abcdef0123 \
  -H "Authorization: Bearer sess_9f8e7d6c5b4a..." \
  -H "Content-Type: application/json" \
  -d '{
    "name": "customers-v2",
    "description": "Updated customer records"
  }'
```

**Request Body:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `name` | string | No | New collection name. Omit to leave unchanged. |
| `description` | string | No | New description. Omit to leave unchanged. |

**Response (200 OK):** The updated `Collection` object (see [Collection](#collection)).

**Errors:**

- `400 Bad Request` -- No request body was supplied. Body: `{ "error": "A collection body is required" }`.
- `404 Not Found` -- Collection does not exist. Body: `{ "error": "Collection not found" }`.

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

**Response (200 OK):** Empty body (zero length).

**Errors:**

- `404 Not Found` -- Collection does not exist. Body: `{ "error": "Collection not found" }`.

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
  "indexingMode": "selective",
  "indexedFields": ["name", "email", "address.city"]
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
```

**Errors:**

- `404 Not Found` -- Collection does not exist.

---

### Documents

#### GET /v1.0/collections/{collectionId}/documents -- List Documents

Retrieves metadata for all documents in a collection. Returns an [EnumerationResult](#enumeration--pagination) with `Document` items in `objects`.

**Path Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `collectionId` | string (UUID) | The unique identifier of the collection. |

**Query Parameters:**

| Parameter | Type | Default | Max | Description |
|-----------|------|---------|-----|-------------|
| `maxResults` | integer | 100 | 1000 | Page size (minimum 1). |
| `skip` | integer | 0 | -- | Number of records to skip. |

**cURL:**

```bash
curl "http://localhost:8000/v1.0/collections/d4e5f6a7-b8c9-0123-4567-89abcdef0123/documents?maxResults=100&skip=0"
```

**Response (200 OK):**

```json
{
  "success": true,
  "timestamp": { "start": "2024-01-15T12:00:00.000Z", "end": "2024-01-15T12:00:00.001Z", "totalMs": 0.67 },
  "maxResults": 100,
  "skip": 0,
  "iterationsRequired": 1,
  "endOfResults": true,
  "totalRecords": 1,
  "recordsRemaining": 0,
  "objects": [
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
```

**Errors:**

- `400 Bad Request` -- `content` is missing or not a valid JSON object; schema validation failure. On a validation failure the body includes `detail`, e.g. `{ "error": "Schema validation failed", "detail": { "errors": [ ... ] } }`.
- `404 Not Found` -- Collection does not exist. Body: `{ "error": "Collection not found" }`.
- `409 Conflict` -- Object lock held on the same document name (concurrent write). Body: `{ "error": "Document is locked", "detail": { "collectionId": "...", "documentName": "...", "lockedByHostname": "...", "lockCreatedUtc": "..." } }`.

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
[
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
```

**Errors:**

- `400 Bad Request` -- `documents` array is missing or empty; a document is missing `content`. Body: `{ "error": "<message>" }`.
- `404 Not Found` -- Collection does not exist. Body: `{ "error": "Collection not found" }`.

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
| `includeContent` | boolean | `false` | If `true`, returns the raw JSON document content instead of the document metadata object. |

**cURL (metadata only):**

```bash
curl http://localhost:8000/v1.0/collections/d4e5f6a7-.../documents/a1b2c3d4-...
```

**Response (200 OK -- metadata):**

```json
{
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
```

**cURL (with content):**

```bash
curl "http://localhost:8000/v1.0/collections/d4e5f6a7-.../documents/a1b2c3d4-...?includeContent=true"
```

**Response (200 OK -- with content):**

With `includeContent=true`, the payload is the document's raw JSON content itself (rather than the metadata object):

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

- `404 Not Found` -- Collection or document does not exist. Body: `{ "error": "Document not found" }`.

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

**Response (200 OK):** Empty body (zero length).

**Errors:**

- `404 Not Found` -- Collection or document does not exist. Body: `{ "error": "Document not found" }`.

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
```

> The `success` field here is part of the `SearchResult` payload (search-level status), not a response envelope. It is unrelated to the removed envelope.

**Errors:**

- `400 Bad Request` -- Invalid filter conditions or malformed SQL expression. Body: `{ "error": "<message>" }`.
- `404 Not Found` -- Collection does not exist. Body: `{ "error": "Collection not found" }`.

---

### Schemas

Schemas are automatically detected from JSON document structure during ingestion. Documents with identical structures share the same schema (deduplicated by hash).

#### GET /v1.0/schemas -- List Schemas

Retrieves all discovered schemas. Returns an [EnumerationResult](#enumeration--pagination) with `Schema` items in `objects`.

**Query Parameters:**

| Parameter | Type | Default | Max | Description |
|-----------|------|---------|-----|-------------|
| `maxResults` | integer | 100 | 1000 | Page size (minimum 1). |
| `skip` | integer | 0 | -- | Number of records to skip. |

**cURL:**

```bash
curl "http://localhost:8000/v1.0/schemas?maxResults=100&skip=0"
```

**Response (200 OK):**

```json
{
  "success": true,
  "timestamp": { "start": "2024-01-15T12:00:00.000Z", "end": "2024-01-15T12:00:00.001Z", "totalMs": 0.67 },
  "maxResults": 100,
  "skip": 0,
  "iterationsRequired": 1,
  "endOfResults": true,
  "totalRecords": 1,
  "recordsRemaining": 0,
  "objects": [
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
  "id": "f0e1d2c3-b4a5-6789-0123-456789abcdef",
  "hash": "a1b2c3d4e5f6...",
  "createdUtc": "2024-01-15T12:00:00.000Z",
  "lastUpdateUtc": "2024-01-15T12:00:00.000Z"
}
```

**Errors:**

- `404 Not Found` -- Schema does not exist. Body: `{ "error": "Schema not found" }`.

---

#### GET /v1.0/schemas/{schemaId}/elements -- Get Schema Elements

Retrieves the elements (fields) defined in a schema. Each element represents a discovered field path with its detected data type. Returns an [EnumerationResult](#enumeration--pagination) with `SchemaElement` items in `objects`.

**Path Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `schemaId` | string (UUID) | The unique identifier of the schema. |

**Query Parameters:**

| Parameter | Type | Default | Max | Description |
|-----------|------|---------|-----|-------------|
| `maxResults` | integer | 100 | 1000 | Page size (minimum 1). |
| `skip` | integer | 0 | -- | Number of records to skip. |

**cURL:**

```bash
curl "http://localhost:8000/v1.0/schemas/f0e1d2c3-b4a5-6789-0123-456789abcdef/elements?maxResults=100&skip=0"
```

**Response (200 OK):**

```json
{
  "success": true,
  "timestamp": { "start": "2024-01-15T12:00:00.000Z", "end": "2024-01-15T12:00:00.001Z", "totalMs": 0.67 },
  "maxResults": 100,
  "skip": 0,
  "iterationsRequired": 1,
  "endOfResults": true,
  "totalRecords": 3,
  "recordsRemaining": 0,
  "objects": [
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

- `404 Not Found` -- Schema does not exist. Body: `{ "error": "Schema not found" }`.

---

### Index Tables

Index tables are the underlying storage for searchable field values. Each unique schema element (field path + data type) maps to its own index table.

#### GET /v1.0/tables -- List Index Tables

Retrieves all index table mappings. Returns an [EnumerationResult](#enumeration--pagination) with `IndexTableMapping` items in `objects`.

**Query Parameters:**

| Parameter | Type | Default | Max | Description |
|-----------|------|---------|-----|-------------|
| `maxResults` | integer | 100 | 1000 | Page size (minimum 1). |
| `skip` | integer | 0 | -- | Number of records to skip. |

**cURL:**

```bash
curl "http://localhost:8000/v1.0/tables?maxResults=100&skip=0"
```

**Response (200 OK):**

```json
{
  "success": true,
  "timestamp": { "start": "2024-01-15T12:00:00.000Z", "end": "2024-01-15T12:00:00.001Z", "totalMs": 0.67 },
  "maxResults": 100,
  "skip": 0,
  "iterationsRequired": 1,
  "endOfResults": true,
  "totalRecords": 2,
  "recordsRemaining": 0,
  "objects": [
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

Retrieves entries from a specific index table with pagination. Returns an [EnumerationResult](#enumeration--pagination) with `IndexTableEntry` items in `objects`.

> **Changed:** this endpoint previously returned `{ tableName, fieldKey, entries, totalCount, skip, limit }`. It now returns a plain EnumerationResult: the entries are in `objects`, the count is in `totalRecords`, and `tableName`/`fieldKey` are no longer included (the caller already knows the selected table and can obtain the field key from `GET /v1.0/tables`).

**Path Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `tableName` | string | The name of the index table (e.g., `idx_a1b2c3d4`). |

**Query Parameters:**

| Parameter | Type | Default | Max | Description |
|-----------|------|---------|-----|-------------|
| `maxResults` | integer | 100 | 1000 | Page size (minimum 1). |
| `skip` | integer | 0 | -- | Number of entries to skip. |
| `limit` | integer | 100 | 1000 | **Legacy** alias for `maxResults`, still accepted for backward compatibility. |

**cURL:**

```bash
curl "http://localhost:8000/v1.0/tables/idx_a1b2c3d4/entries?skip=0&maxResults=50"
```

**Response (200 OK):**

```json
{
  "success": true,
  "timestamp": { "start": "2024-01-15T12:00:00.000Z", "end": "2024-01-15T12:00:00.001Z", "totalMs": 0.67 },
  "maxResults": 50,
  "skip": 0,
  "iterationsRequired": 1,
  "endOfResults": true,
  "totalRecords": 2,
  "recordsRemaining": 0,
  "objects": [
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

- `404 Not Found` -- Index table does not exist. Body: `{ "error": "Index table not found" }`.

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

### EnumerationResult&lt;T&gt;

Returned by all list/enumeration GET endpoints. See [Enumeration & Pagination](#enumeration--pagination).

| Field | Type | Description |
|-------|------|-------------|
| `success` | boolean | Whether the enumeration completed successfully. |
| `timestamp` | object | Timing information with `start`, `end`, and `totalMs`. |
| `maxResults` | integer | The page size that was applied. |
| `skip` | integer | The number of records that were skipped. |
| `iterationsRequired` | integer | Number of internal iterations required to build the page. |
| `endOfResults` | boolean | `true` when no further records remain after this page. |
| `totalRecords` | integer | Total number of records available across all pages. |
| `recordsRemaining` | integer | Number of records still available after this page. |
| `objects` | T[] | The array of returned items. |

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

### Tenant

| Field | Type | Description |
|-------|------|-------------|
| `id` | string | Unique identifier (`ten_...`). |
| `name` | string | Human-readable tenant name. |
| `region` | string or null | Optional region label. |
| `active` | boolean | Whether the tenant is active. Inactive tenants cannot authenticate. |
| `isProtected` | boolean | Whether the tenant is protected from deletion (e.g. the seeded default tenant). |
| `createdUtc` | string (ISO 8601) | Creation timestamp. |
| `lastUpdateUtc` | string (ISO 8601) | Last modification timestamp. |

### User

| Field | Type | Description |
|-------|------|-------------|
| `id` | string | Unique identifier (`usr_...`). |
| `tenantId` | string | Identifier of the owning tenant. |
| `firstName` | string or null | First name. |
| `lastName` | string or null | Last name. |
| `email` | string | Email address. Unique within the tenant. |
| `isAdmin` | boolean | Whether the user is a system administrator (full cross-tenant access). |
| `isTenantAdmin` | boolean | Whether the user is a tenant administrator (bypasses RBAC within its own tenant). |
| `active` | boolean | Whether the user is active. Inactive users cannot authenticate. |
| `isProtected` | boolean | Whether the user is protected from deletion (e.g. the seeded default administrator). |
| `createdUtc` | string (ISO 8601) | Creation timestamp. |
| `lastUpdateUtc` | string (ISO 8601) | Last modification timestamp. |

> The password hash (`passwordSha256`) is a stored field and is **never** included in any response.

### Credential

| Field | Type | Description |
|-------|------|-------------|
| `id` | string | Unique identifier (`crd_...`). |
| `tenantId` | string | Identifier of the owning tenant. |
| `userId` | string | Identifier of the owning user. |
| `name` | string or null | Human-readable credential name. |
| `accessKey` | string | The raw access key (`access_...`). Persisted server-side and returned on credential reads (create, list, and get). Stored in plaintext to allow retrieval; treat it as a secret. |
| `accessKeySha256` | string | SHA-256 hash of the access key, stored server-side. **Never returned** in API responses. |
| `accessKeyLast4` | string | Last four characters of the access key, retained for display. |
| `expiresUtc` | string (ISO 8601) or null | Optional expiration; null means the credential does not expire. |
| `lastUsedUtc` | string (ISO 8601) or null | When the credential was last used to authenticate, or null. |
| `active` | boolean | Whether the credential is active. Inactive credentials cannot authenticate. |
| `isProtected` | boolean | Whether the credential is protected from deletion (e.g. the seeded default credential). |
| `createdUtc` | string (ISO 8601) | Creation timestamp. |
| `lastUpdateUtc` | string (ISO 8601) | Last modification timestamp. |

### UserRole

| Field | Type | Description |
|-------|------|-------------|
| `id` | string | Unique identifier (`rol_...`). |
| `tenantId` | string or null | Owning tenant, or null for a global built-in role. |
| `name` | string | Role name (e.g. `TenantAdmin`, `Editor`, `Viewer`). |
| `isBuiltIn` | boolean | Whether this is a built-in role seeded by the platform. |
| `active` | boolean | Whether the role is active. |
| `isProtected` | boolean | Whether the role is protected from modification and deletion. |
| `createdUtc` | string (ISO 8601) | Creation timestamp. |
| `lastUpdateUtc` | string (ISO 8601) | Last modification timestamp. |

### RolePermissionSpec

A single grant within a role: whether it permits or denies a set of operations on a set of resource types. A role's `permissions` is a list of these.

| Field | Type | Description |
|-------|------|-------------|
| `permissionType` | string | `permit` or `deny`. A deny wins over any permit during evaluation. |
| `resourceTypes` | string[] | Resource types the grant covers: `all`, `tenant`, `user`, `credential`, `session`, `role`, `permission`, `assignment`, `audit`, `collection`, `document`, `schema`, `index`, `requestHistory`. |
| `operationTypes` | string[] | Operations the grant covers: `all`, `create`, `read`, `write` (expands to `create` + `update` + `delete`), `update`, `delete`, `execute`, `admin`. |

### RoleDetailResponse

A role together with the grants it confers. Returned by the role create, get, and update endpoints. Carries the same descriptive fields as [UserRole](#userrole) plus a `permissions` array.

| Field | Type | Description |
|-------|------|-------------|
| `id` | string | Unique identifier (`rol_...`). |
| `tenantId` | string or null | Owning tenant, or null for a global built-in role. |
| `name` | string | Role name. |
| `isBuiltIn` | boolean | Whether this is a global built-in role (read-only). |
| `active` | boolean | Whether the role is active. |
| `isProtected` | boolean | Whether the role is protected from modification and deletion. |
| `createdUtc` | string (ISO 8601) | Creation timestamp. |
| `lastUpdateUtc` | string (ISO 8601) | Last modification timestamp. |
| `permissions` | RolePermissionSpec[] | The grants the role confers. See [RolePermissionSpec](#rolepermissionspec). |

### UserRoleAssignment

| Field | Type | Description |
|-------|------|-------------|
| `id` | string | Unique identifier (`ura_...`). |
| `tenantId` | string | Identifier of the owning tenant. |
| `userId` | string | User the assignment applies to. |
| `roleId` | string or null | Assigned role by id, or null when referenced by name. |
| `roleName` | string or null | Assigned role by name, used when referenced by name or as a fallback. |
| `resourceScope` | string | `tenant` (tenant-wide) or `resource` (a specific resource). |
| `resourceId` | string or null | Target resource id for a `resource`-scoped assignment, or null. |
| `inheritsToChildren` | boolean | For a `tenant`-scoped assignment, whether it also satisfies checks on child resources. |
| `active` | boolean | Whether the assignment is active. |
| `createdUtc` | string (ISO 8601) | Creation timestamp. |
| `lastUpdateUtc` | string (ISO 8601) | Last modification timestamp. |

### AuditEntry

| Field | Type | Description |
|-------|------|-------------|
| `id` | string | Unique identifier (`aud_...`). |
| `tenantId` | string or null | Tenant the event pertains to, or null for system-level events. |
| `eventType` | string | Event kind (e.g. `AuthSuccess`, `AuthFailure`, `AuthzDenied`, `SessionRevoked`). |
| `requestId` | string or null | Correlating request identifier. |
| `correlationId` | string or null | Correlation identifier spanning related requests, or null. |
| `traceId` | string or null | Trace identifier for telemetry correlation, or null. |
| `principalType` | string or null | Principal kind (`User` or `Credential`), or null when unauthenticated. |
| `principalId` | string or null | Identifier of the principal, or null. |
| `userId` | string or null | Identifier of the user involved, or null. |
| `credentialId` | string or null | Identifier of the credential involved, or null. |
| `resourceType` | string or null | Resource type the request targeted, or null. |
| `resourceId` | string or null | Identifier of the resource the request targeted, or null. |
| `requestType` | string or null | The classified request type. |
| `method` | string or null | HTTP method of the request. |
| `path` | string or null | Request path. |
| `sourceIp` | string or null | Source IP address. |
| `authResult` | string or null | Authentication result (e.g. `Success`, `NotFound`, `Inactive`, `Invalid`), or null. |
| `authzResult` | string or null | Authorization result (e.g. `Permitted`, `DeniedExplicit`, `DeniedImplicit`), or null. |
| `denialReason` | string or null | Reason authorization was denied, or null. |
| `bypassReason` | string or null | Reason an administrative bypass was applied, or null. |
| `requiredPermission` | string or null | The permission the request required (`<ResourceType>:<Operation>`), or null. |
| `responseCode` | integer | The HTTP response code returned. |
| `createdUtc` | string (ISO 8601) | When the event occurred (UTC). |

### AuthTokenResponse

Returned by [`POST /v1.0/token`](#post-v10token--login).

| Field | Type | Description |
|-------|------|-------------|
| `token` | string | The session token to present as a bearer value. |
| `expiresUtc` | string (ISO 8601) | When the token expires (UTC). |
| `tenantId` | string | Tenant identifier. |
| `userId` | string | User identifier. |
| `email` | string | User email. |
| `isAdmin` | boolean | Whether the user is a system administrator. |
| `isTenantAdmin` | boolean | Whether the user is a tenant administrator. |

### WhoAmIResponse

Returned by [`GET /v1.0/token`](#get-v10token--current-principal) and its aliases.

| Field | Type | Description |
|-------|------|-------------|
| `isAuthenticated` | boolean | Whether the request is authenticated. |
| `principalType` | string or null | The kind of principal (`User` or `Credential`). |
| `tenantId` | string or null | Tenant identifier. |
| `userId` | string or null | User identifier. |
| `credentialId` | string or null | Credential identifier, when the principal is a credential. |
| `email` | string or null | User email. |
| `isAdmin` | boolean | Whether the principal is a system administrator. |
| `isTenantAdmin` | boolean | Whether the principal is a tenant administrator. |

---

## Key Behaviors

### Default Collection

On the first server start, when no collections exist yet, Lattice automatically creates a collection named `default` with indexing mode `all` and schema enforcement mode `none`. This means `GET /v1.0/collections` returns at least this one collection on a fresh install.

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
