/**
 * Lattice SDK Models
 *
 * Data models for the Lattice REST API.
 */
/**
 * Schema enforcement mode for collections.
 */
export declare enum SchemaEnforcementMode {
    None = 0,
    Strict = 1,
    Flexible = 2,
    Partial = 3
}
/**
 * Indexing mode for collections.
 */
export declare enum IndexingMode {
    All = 0,
    Selective = 1,
    None = 2
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
 * Represents the standard API response wrapper.
 */
export interface ResponseContext<T = any> {
    success: boolean;
    statusCode: number;
    errorMessage?: string;
    data?: T;
    headers: Record<string, string>;
    processingTimeMs: number;
    guid?: string;
    timestampUtc?: Date;
}
/**
 * Represents an index table mapping.
 */
export interface IndexTableMapping {
    key: string;
    tableName: string;
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
