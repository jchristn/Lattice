# Changelog

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

### Version bumps
- `Lattice.Core`: 0.2.0 -> 0.2.1

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
