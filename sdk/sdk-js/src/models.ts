/**
 * Lattice SDK Models
 *
 * Data models for the Lattice REST API.
 */

/**
 * Schema enforcement mode for collections.
 */
export enum SchemaEnforcementMode {
    None = 0,
    Strict = 1,
    Flexible = 2,
    Partial = 3
}

/**
 * Indexing mode for collections.
 */
export enum IndexingMode {
    All = 0,
    Selective = 1,
    None = 2
}

/**
 * Search condition operators.
 */
export enum SearchCondition {
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
export enum EnumerationOrder {
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
export enum DataType {
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
export function parseDate(value: any): Date | undefined {
    if (!value) return undefined;
    if (value instanceof Date) return value;
    if (typeof value === "string") {
        const date = new Date(value);
        return isNaN(date.getTime()) ? undefined : date;
    }
    return undefined;
}

/**
 * Parse a Collection from API response data.
 */
export function parseCollection(data: any): Collection | null {
    if (!data) return null;
    return {
        id: data.id || "",
        name: data.name || "",
        description: data.description,
        documentsDirectory: data.documentsDirectory,
        labels: data.labels || [],
        tags: data.tags || {},
        createdUtc: parseDate(data.createdUtc),
        lastUpdateUtc: parseDate(data.lastUpdateUtc),
        schemaEnforcementMode: data.schemaEnforcementMode ?? SchemaEnforcementMode.None,
        indexingMode: data.indexingMode ?? IndexingMode.All
    };
}

/**
 * Parse a Document from API response data.
 */
export function parseDocument(data: any): Document | null {
    if (!data) return null;
    return {
        id: data.id || "",
        collectionId: data.collectionId || "",
        schemaId: data.schemaId || "",
        name: data.name,
        labels: data.labels || [],
        tags: data.tags || {},
        createdUtc: parseDate(data.createdUtc),
        lastUpdateUtc: parseDate(data.lastUpdateUtc),
        content: data.content,
        contentLength: data.contentLength || 0,
        sha256Hash: data.sha256Hash
    };
}

/**
 * Parse a Schema from API response data.
 */
export function parseSchema(data: any): Schema | null {
    if (!data) return null;
    return {
        id: data.id || "",
        name: data.name,
        hash: data.hash,
        createdUtc: parseDate(data.createdUtc),
        lastUpdateUtc: parseDate(data.lastUpdateUtc)
    };
}

/**
 * Parse a SchemaElement from API response data.
 */
export function parseSchemaElement(data: any): SchemaElement | null {
    if (!data) return null;
    return {
        id: data.id || "",
        schemaId: data.schemaId || "",
        position: data.position || 0,
        key: data.key || "",
        dataType: data.dataType || "",
        nullable: data.nullable || false,
        createdUtc: parseDate(data.createdUtc),
        lastUpdateUtc: parseDate(data.lastUpdateUtc)
    };
}

/**
 * Parse a FieldConstraint from API response data.
 */
export function parseFieldConstraint(data: any): FieldConstraint | null {
    if (!data) return null;
    return {
        id: data.id,
        collectionId: data.collectionId,
        fieldPath: data.fieldPath || "",
        dataType: data.dataType,
        required: data.required,
        nullable: data.nullable,
        regexPattern: data.regexPattern,
        minValue: data.minValue,
        maxValue: data.maxValue,
        minLength: data.minLength,
        maxLength: data.maxLength,
        allowedValues: data.allowedValues,
        arrayElementType: data.arrayElementType,
        createdUtc: parseDate(data.createdUtc),
        lastUpdateUtc: parseDate(data.lastUpdateUtc)
    };
}

/**
 * Parse an IndexedField from API response data.
 */
export function parseIndexedField(data: any): IndexedField | null {
    if (!data) return null;
    return {
        id: data.id || "",
        collectionId: data.collectionId || "",
        fieldPath: data.fieldPath || "",
        createdUtc: parseDate(data.createdUtc),
        lastUpdateUtc: parseDate(data.lastUpdateUtc)
    };
}

/**
 * Parse a SearchResult from API response data.
 */
export function parseSearchResult(data: any): SearchResult | null {
    if (!data) return null;

    let timestamp: Date | undefined;
    if (data.timestamp) {
        if (typeof data.timestamp === "object" && data.timestamp.utc) {
            timestamp = parseDate(data.timestamp.utc);
        } else {
            timestamp = parseDate(data.timestamp);
        }
    }

    return {
        success: data.success || false,
        timestamp,
        maxResults: data.maxResults,
        continuationToken: data.continuationToken,
        endOfResults: data.endOfResults || false,
        totalRecords: data.totalRecords || 0,
        recordsRemaining: data.recordsRemaining || 0,
        documents: (data.documents || []).map((d: any) => parseDocument(d)).filter((d: any) => d !== null)
    };
}

/**
 * Parse an IndexRebuildResult from API response data.
 */
export function parseIndexRebuildResult(data: any): IndexRebuildResult | null {
    if (!data) return null;
    return {
        collectionId: data.collectionId || "",
        documentsProcessed: data.documentsProcessed || 0,
        indexesCreated: data.indexesCreated || [],
        indexesDropped: data.indexesDropped || [],
        valuesInserted: data.valuesInserted || 0,
        duration: data.duration,
        durationMs: data.durationMs || 0,
        errors: data.errors || [],
        success: data.success || false
    };
}

/**
 * Parse an IndexTableMapping from API response data.
 */
export function parseIndexTableMapping(data: any): IndexTableMapping | null {
    if (!data) return null;
    return {
        key: data.key || "",
        tableName: data.tableName || ""
    };
}

/**
 * Convert a FieldConstraint to API request format.
 */
export function fieldConstraintToRequest(constraint: FieldConstraint): any {
    const result: any = {
        fieldPath: constraint.fieldPath
    };
    if (constraint.dataType) result.dataType = constraint.dataType;
    if (constraint.required) result.required = constraint.required;
    if (constraint.nullable !== undefined) result.nullable = constraint.nullable;
    if (constraint.regexPattern) result.regexPattern = constraint.regexPattern;
    if (constraint.minValue !== undefined) result.minValue = constraint.minValue;
    if (constraint.maxValue !== undefined) result.maxValue = constraint.maxValue;
    if (constraint.minLength !== undefined) result.minLength = constraint.minLength;
    if (constraint.maxLength !== undefined) result.maxLength = constraint.maxLength;
    if (constraint.allowedValues) result.allowedValues = constraint.allowedValues;
    if (constraint.arrayElementType) result.arrayElementType = constraint.arrayElementType;
    return result;
}

/**
 * Convert a SearchFilter to API request format.
 */
export function searchFilterToRequest(filter: SearchFilter): any {
    const result: any = {
        field: filter.field,
        condition: filter.condition
    };
    if (filter.value !== undefined) result.value = filter.value;
    return result;
}

/**
 * Convert a SearchQuery to API request format.
 */
export function searchQueryToRequest(query: SearchQuery): any {
    const result: any = {};
    if (query.filters) result.filters = query.filters.map(searchFilterToRequest);
    if (query.labels) result.labels = query.labels;
    if (query.tags) result.tags = query.tags;
    if (query.maxResults !== undefined) result.maxResults = query.maxResults;
    if (query.skip !== undefined) result.skip = query.skip;
    if (query.ordering) result.ordering = query.ordering;
    if (query.includeContent) result.includeContent = query.includeContent;
    return result;
}
