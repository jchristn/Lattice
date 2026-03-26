# Changelog

## Current release

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
