# Changelog

## Unreleased

**v0.3.0**

### Added
- **Authentication, authorization, RBAC, and multi-tenancy.** When enabled (default), every route except
  health, the OpenAPI spec, and login requires a bearer token.
  - **Two authentication methods**, both via `Authorization: Bearer <value>` (with an `x-token` alias):
    (1) a **session token** from `POST /v1.0/token` (email + password; the tenant is inferred from the
    credentials when omitted, and the response asks the caller to choose when they match multiple tenants),
    and (2) a **credential access key** (`key_...`) presented directly. No `x-api-key`, no secret key,
    no request signing.
  - **Single-tier multi-tenancy**: the tenant is resolved from the principal; there is no tenant id in
    URLs. A system administrator may target another tenant via an explicit `tenantId` in the request body
    (writes) or query (lists). Collections carry an owning `tenantId` (new `tenantid` column across all four
    backends, added by migration on existing databases); the REST and MCP data planes isolate collections
    and, transitively, their documents by tenant, while a system administrator sees all tenants.
  - **RBAC** with deny-over-permit evaluation, resource types and operations (`Write` expands to
    Create/Update/Delete), built-in roles (TenantAdmin, SecurityAdmin, Auditor, CollectionAdmin, Editor,
    Viewer, TenantMember), and per-user/credential assignments. Passwords are hashed with SHA-256.
  - **First-run seeding** of a default tenant, administrator (`admin@lattice` / `password`), and access
    key, printed to the console once.
  - New management endpoints (flat, principal-scoped): `POST/GET/DELETE /v1.0/token`, `GET /v1.0/whoami`,
    and CRUD for `/v1.0/tenants`, `/v1.0/users`, `/v1.0/credentials`, `/v1.0/roles`, `/v1.0/assignments`,
    and `/v1.0/audit`. Data layer implemented across SQLite, MySQL, PostgreSQL, and SQL Server.
- **In-process MCP server** (`POST /v1.0/mcp`): a hand-rolled JSON-RPC 2.0 [Model Context
  Protocol](https://modelcontextprotocol.io) endpoint behind the same auth and RBAC as the REST API, with
  a 22-tool catalog mirroring the data plane plus identity/RBAC read surfaces. Configured under a new
  `Mcp` block in `lattice.json`. Documented in `MCP_API.md`.
- **Dashboard admin console**: the sidebar is reorganized into labeled groups (Data, Structure, Search,
  Manage, Configure), and a new admin-gated Configure section adds Tenants, Users, Credentials, Roles,
  Role Assignments, and Audit Log views (create/list/delete against the new endpoints; the one-time access
  key is revealed on credential creation). Login infers the tenant and prompts to choose only when
  ambiguous; the top bar has a single icon-only sign-out with uniformly sized buttons; and every label,
  input, dropdown, column header, button, and link across the console carries a descriptive hover tooltip.
  The auth/identity/RBAC/audit endpoints now appear in the API Explorer (OpenAPI-documented).
- **Security audit trail**: authentication failures (401) and authorization denials (403) are persisted to
  an append-only audit store with principal, required permission, verdict, path, and response code.
- **Auth telemetry**: five new counters on the `Lattice.Server` meter — `lattice.auth.requests`,
  `lattice.auth.session.events`, `lattice.auth.rbac.mutations`, `lattice.authz.requests`, and
  `lattice.authz.denials`.

### Changed
- `lattice.json` gains `Auth` and `Mcp` configuration blocks (both enabled by default in the server image;
  disabled in the factory image).

## Current release

**v0.2.1** (2026-09-02)

### Added
- **Built-in telemetry (metrics, traces, logs)** across the product, built on `System.Diagnostics.Metrics`
  (`Meter`) and `System.Diagnostics` (`ActivitySource`) and exported via OpenTelemetry:
  - `Lattice.Core`: new `Lattice.Core.Telemetry` namespace (`LatticeTelemetry`, `OperationScope`). Every
    core service method, plus the ingestion, search, and index-rebuild workflows and the pluggable
    database layer, is now instrumented with operation spans and metrics. Emit rides the .NET BCL and is
    a no-op until a host subscribes — no new package dependency.
  - `Lattice.Server`: new `Lattice.Server.Telemetry` namespace (`ServerTelemetry`, `TelemetryBootstrap`).
    100% of HTTP requests are measured (request count, duration, in-flight, request/response body sizes)
    via Watson `PreRouting`/`PostRouting`, and each functional request opens an HTTP server span that
    parents the core-engine spans. The request-history background worker is instrumented too.
  - Telemetry is wired through the [Radiant](https://www.nuget.org/packages/Radiant) host in
    `LatticeServer.Main`, configurable via a new `Telemetry` section in `lattice.json` (OTLP endpoint,
    in-process Prometheus scrape, sampling, Loki export).
- **Observability stack** in `docker/compose.yaml`: OpenTelemetry Collector, Prometheus, Tempo, Loki, and
  Grafana (admin/admin) with provisioned datasources (trace↔log↔metric correlation) and seven
  domain dashboards in a top-level "Lattice" Grafana folder (Overview, HTTP, Services, Ingestion &
  Workflows, Database, Runtime & Process, Logs). Grafana on host port 3001 (the dashboard owns 3000).
- **Dashboard**: new "Observability" page (+ sidebar entry) with cards linking out to Grafana, Prometheus,
  Tempo, Loki, and the OTel Collector, each showing the service name, default credentials, and URL.
- `TELEMETRY.md`: full telemetry reference for developers and devops teams.
- **Configurable CORS** (`Rest.Cors` in `lattice.json`): permissive by default, with a Watson preflight
  route (OPTIONS → 204) and centralized header stamping. Fixes browser access to the API from the
  dashboard's origin.
- **Default collection** (`default`) is created automatically on first server run when no collections exist.
- Helper scripts: `build-all.bat` (builds both images with a tag) and `docker/update.bat`
  (`compose pull` → `down` → `up -d` → `ps -a`).

### Changed
- **BREAKING — REST response envelope removed.** Responses are now the raw payload on success (2xx) and
  `{ "error": "...", "detail"?: ... }` on failure (4xx/5xx); status is conveyed by the HTTP status code.
  Request id and processing time moved to the `X-Lattice-Request-Id` and `X-Lattice-Processing-Time-Ms`
  response headers. All consumers updated to match: dashboard, C#/JavaScript/Python SDKs, the Postman
  collection, `REST_API.md`, and `README.md`.
- **Docker**: dashboard `LATTICE_SERVER_URL` defaults to `http://localhost:8000` (the published host port)
  so the browser can reach the API; `docker/compose.yaml` images pinned to `v0.2.1`.
- **Code quality / code-style compliance**: eliminated sync-over-async (`.GetAwaiter().GetResult()`) —
  `LatticeServer.Main` is now `async`, `RequestHistoryService` is `IAsyncDisposable`, and shutdown is fully
  awaited; server responses, SDK error parsing, and error bodies use named types (`ErrorResponse`,
  `ApiErrorResponse`) instead of `System.Text.Json` DOM types; document content is represented as a raw
  JSON `string` end-to-end (the C# SDK's `Document.Content` is now `string?` rather than `JsonElement?`),
  removing DOM types from the request/response paths (the Core flatten/schema/validation engine still uses
  a JSON DOM to traverse arbitrary document JSON, which is required there); one-class-per-file split of the
  telemetry settings; `System.*`-first using ordering, usings moved inside the namespace in the C# SDK; no
  `var`; `ConfigureAwait(false)` on new awaits.

### Version bumps
- `Lattice.Core`: 0.2.0 -> 0.2.1
- `Lattice.Sdk` (C#): 0.2.0 -> 0.3.0 (breaking: raw responses, `Document.Content` is `string?`)
- `lattice-sdk` (npm): 0.1.3 -> 0.3.0 (breaking: raw responses)
- `lattice-sdk` (pip): 0.1.3 -> 0.3.0 (breaking: raw responses)

## Previous versions

**v0.2.0** (2026-03-26)
- Dependency refresh (Microsoft.Data.SqlClient 7.0.2, Npgsql 10.0.3, MySqlConnector 2.6.2,
  Microsoft.Data.Sqlite 10.0.11, PrettyId 2.0.1); API explorer and database-backed request history;
  Touchstone-based testing infrastructure.

**v0.1.3** (2026-03-26)

### Added
- **Batch document ingestion** (`IngestBatch`) across all layers:
  - `Lattice.Core`: `IDocumentMethods.IngestBatch` with optimized implementation — single collection/constraints/indexed-fields lookup, in-memory schema and mapping caches shared across the batch, per-document coherency (each document fully written with labels, tags, indexes, and file before proceeding to the next)
  - `Lattice.Server`: `PUT /v1.0/collections/{collectionId}/documents/batch` REST endpoint with OpenAPI metadata
  - `BatchDocument` model (`Lattice.Core.Models`) and `BatchIngestRequest`/`BatchIngestDocumentEntry` server request models
  - C# SDK: `IDocumentMethods.IngestBatchAsync` and `BatchIngestDocument` model
  - JavaScript/TypeScript SDK: `DocumentMethods.ingestBatch` and `BatchIngestDocumentEntry` interface
  - Python SDK: `DocumentMethods.ingest_batch` and `BatchIngestDocument` dataclass
- Batch ingestion tests in `Test.Automated` (6 tests), and all SDK test harnesses (C#, JS, Python)
- `REST_API.md`: comprehensive REST API reference with all endpoints, examples, cURL commands, and model definitions
- Postman collection: "Batch Ingest Documents" request

### Changed
- `Test.Throughput` now uses `IngestBatch` for tier ingestion instead of sequential one-by-one calls
- **Search pagination optimization**: no-filter searches now use database-level `COUNT(*)` and `LIMIT/OFFSET` instead of loading all document IDs into memory — eliminates O(n^2) behavior when paginating large collections
- **Removed redundant collection scan** in search: previously enumerated all collection IDs twice per search; now removed entirely for the no-filter path, and uses in-memory `collectionId` check on already-loaded candidates for the filter path
- Extracted `LoadDocumentsIntoResult` helper in `SearchMethods` to eliminate duplicated document loading logic
- Updated `README.md` with accurate API examples and SDK project structure

### Version bumps
- `Lattice.Core`: 0.1.2 -> 0.1.3
- `Lattice.Sdk` (C#): 1.0.0 -> 0.1.3
- `lattice-sdk` (npm): 1.0.0 -> 0.1.3
- `lattice-sdk` (pip): 1.0.0 -> 0.1.3

## Previous versions

**v0.1.2**
- Dashboard UX improvements, setup wizard, factory reset, Docker build improvements

**v0.1.0**
- Initial release
