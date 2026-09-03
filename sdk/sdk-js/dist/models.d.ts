/**
 * Lattice SDK Models
 *
 * Data models for the Lattice REST API.
 */
/**
 * Schema enforcement mode for collections.
 */
export declare enum SchemaEnforcementMode {
    None = "none",
    Strict = "strict",
    Flexible = "flexible",
    Partial = "partial"
}
/**
 * Indexing mode for collections.
 */
export declare enum IndexingMode {
    All = "all",
    Selective = "selective",
    None = "none"
}
/**
 * Search condition operators.
 */
export declare enum SearchCondition {
    Equals = "Equals",
    NotEquals = "NotEquals",
    GreaterThan = "GreaterThan",
    GreaterThanOrEqualTo = "GreaterThanOrEqualTo",
    LessThan = "LessThan",
    LessThanOrEqualTo = "LessThanOrEqualTo",
    IsNull = "IsNull",
    IsNotNull = "IsNotNull",
    Contains = "Contains",
    StartsWith = "StartsWith",
    EndsWith = "EndsWith",
    Like = "Like"
}
/**
 * Enumeration ordering options.
 */
export declare enum EnumerationOrder {
    CreatedAscending = "CreatedAscending",
    CreatedDescending = "CreatedDescending",
    LastUpdateAscending = "LastUpdateAscending",
    LastUpdateDescending = "LastUpdateDescending",
    NameAscending = "NameAscending",
    NameDescending = "NameDescending"
}
/**
 * Data types for field constraints and schema elements.
 */
export declare enum DataType {
    String = "string",
    Integer = "integer",
    Number = "number",
    Boolean = "boolean",
    Array = "array",
    Object = "object",
    Null = "null"
}
/**
 * Represents a Lattice collection.
 */
export interface Collection {
    id: string;
    name: string;
    description?: string;
    documentsDirectory?: string;
    labels: string[];
    tags: Record<string, string>;
    createdUtc?: Date;
    lastUpdateUtc?: Date;
    schemaEnforcementMode: SchemaEnforcementMode;
    indexingMode: IndexingMode;
}
/**
 * Represents a Lattice document.
 */
export interface Document {
    id: string;
    collectionId: string;
    schemaId: string;
    name?: string;
    labels: string[];
    tags: Record<string, string>;
    createdUtc?: Date;
    lastUpdateUtc?: Date;
    content?: any;
    contentLength: number;
    sha256Hash?: string;
}
/**
 * Represents a Lattice schema.
 */
export interface Schema {
    id: string;
    name?: string;
    hash?: string;
    createdUtc?: Date;
    lastUpdateUtc?: Date;
}
/**
 * Represents an element within a schema.
 */
export interface SchemaElement {
    id: string;
    schemaId: string;
    position: number;
    key: string;
    dataType: string;
    nullable: boolean;
    createdUtc?: Date;
    lastUpdateUtc?: Date;
}
/**
 * Represents a field constraint for schema validation.
 */
export interface FieldConstraint {
    id?: string;
    collectionId?: string;
    fieldPath: string;
    dataType?: string;
    required?: boolean;
    nullable?: boolean;
    regexPattern?: string;
    minValue?: number;
    maxValue?: number;
    minLength?: number;
    maxLength?: number;
    allowedValues?: string[];
    arrayElementType?: string;
    createdUtc?: Date;
    lastUpdateUtc?: Date;
}
/**
 * Represents an indexed field in a collection.
 */
export interface IndexedField {
    id: string;
    collectionId: string;
    fieldPath: string;
    createdUtc?: Date;
    lastUpdateUtc?: Date;
}
/**
 * Represents a search filter.
 */
export interface SearchFilter {
    field: string;
    condition: SearchCondition;
    value?: string;
}
/**
 * Represents a search query.
 */
export interface SearchQuery {
    collectionId: string;
    filters?: SearchFilter[];
    labels?: string[];
    tags?: Record<string, string>;
    maxResults?: number;
    skip?: number;
    ordering?: EnumerationOrder;
    includeContent?: boolean;
}
/**
 * Represents search results.
 */
export interface SearchResult {
    success: boolean;
    timestamp?: Date;
    maxResults?: number;
    continuationToken?: string;
    endOfResults: boolean;
    totalRecords: number;
    recordsRemaining: number;
    documents: Document[];
}
/**
 * Represents the result of an index rebuild operation.
 */
export interface IndexRebuildResult {
    collectionId: string;
    documentsProcessed: number;
    indexesCreated: string[];
    indexesDropped: string[];
    valuesInserted: number;
    duration?: string;
    durationMs: number;
    errors: string[];
    success: boolean;
}
/**
 * Represents an index table mapping.
 */
export interface IndexTableMapping {
    key: string;
    tableName: string;
}
/**
 * Represents an entry in an index table.
 */
export interface IndexTableEntry {
    id: string;
    documentId: string;
    position?: number | null;
    value?: string | null;
    createdUtc?: Date;
}
/**
 * Timing metadata included on an EnumerationResult.
 */
export interface EnumerationTimestamp {
    start?: Date;
    end?: Date;
    totalMs?: number;
}
/**
 * Paginated result envelope returned by the GET list endpoints.
 *
 * The items live in {@link objects}; the remaining fields describe the
 * pagination window and totals.
 */
export interface EnumerationResult<T> {
    success: boolean;
    timestamp?: EnumerationTimestamp;
    maxResults: number;
    skip: number;
    iterationsRequired: number;
    continuationToken?: string | null;
    endOfResults: boolean;
    totalRecords: number;
    recordsRemaining: number;
    objects: T[];
}
/**
 * Optional pagination parameters accepted by the list endpoints.
 */
export interface PaginationOptions {
    maxResults?: number;
    skip?: number;
}
/**
 * Options for creating a collection.
 */
export interface CreateCollectionOptions {
    name: string;
    description?: string;
    documentsDirectory?: string;
    labels?: string[];
    tags?: Record<string, string>;
    schemaEnforcementMode?: SchemaEnforcementMode;
    fieldConstraints?: FieldConstraint[];
    indexingMode?: IndexingMode;
    indexedFields?: string[];
}
/**
 * Options for ingesting a document.
 */
export interface IngestDocumentOptions {
    collectionId: string;
    content: any;
    name?: string;
    labels?: string[];
    tags?: Record<string, string>;
}
/**
 * Options for batch ingesting documents.
 */
export interface BatchIngestDocumentEntry {
    content: any;
    name?: string;
    labels?: string[];
    tags?: Record<string, string>;
}
/**
 * Parse a date from API response.
 */
export declare function parseDate(value: any): Date | undefined;
/**
 * Parse a Collection from API response data.
 */
export declare function parseCollection(data: any): Collection | null;
/**
 * Parse a Document from API response data.
 */
export declare function parseDocument(data: any): Document | null;
/**
 * Parse a Schema from API response data.
 */
export declare function parseSchema(data: any): Schema | null;
/**
 * Parse a SchemaElement from API response data.
 */
export declare function parseSchemaElement(data: any): SchemaElement | null;
/**
 * Parse a FieldConstraint from API response data.
 */
export declare function parseFieldConstraint(data: any): FieldConstraint | null;
/**
 * Parse an IndexedField from API response data.
 */
export declare function parseIndexedField(data: any): IndexedField | null;
/**
 * Parse a SearchResult from API response data.
 */
export declare function parseSearchResult(data: any): SearchResult | null;
/**
 * Parse an IndexRebuildResult from API response data.
 */
export declare function parseIndexRebuildResult(data: any): IndexRebuildResult | null;
/**
 * Parse an IndexTableMapping from API response data.
 */
export declare function parseIndexTableMapping(data: any): IndexTableMapping | null;
/**
 * Parse an IndexTableEntry from API response data.
 */
export declare function parseIndexTableEntry(data: any): IndexTableEntry | null;
/**
 * Parse an EnumerationResult envelope from API response data, mapping each
 * item in `objects` through the supplied element parser.
 */
export declare function parseEnumerationResult<T>(data: any, parseItem: (item: any) => T | null): EnumerationResult<T>;
/**
 * Convert a FieldConstraint to API request format.
 */
export declare function fieldConstraintToRequest(constraint: FieldConstraint): any;
/**
 * Convert a SearchFilter to API request format.
 */
export declare function searchFilterToRequest(filter: SearchFilter): any;
/**
 * Convert a SearchQuery to API request format.
 */
export declare function searchQueryToRequest(query: SearchQuery): any;
