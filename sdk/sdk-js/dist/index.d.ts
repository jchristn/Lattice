/**
 * Lattice SDK for JavaScript/TypeScript
 *
 * A comprehensive REST SDK for consuming a Lattice server.
 */
export { LatticeClient } from "./client";
export { Collection, Document, Schema, SchemaElement, FieldConstraint, IndexedField, SearchFilter, SearchQuery, SearchResult, IndexRebuildResult, ResponseContext, IndexTableMapping, CreateCollectionOptions, IngestDocumentOptions, SchemaEnforcementMode, IndexingMode, SearchCondition, EnumerationOrder, DataType } from "./models";
export { LatticeError, LatticeConnectionError, LatticeApiError, LatticeValidationError } from "./exceptions";
