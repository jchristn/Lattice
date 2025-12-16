/**
 * Lattice SDK Client
 *
 * Main client for interacting with the Lattice REST API.
 */
import { Collection, Document, Schema, SchemaElement, FieldConstraint, IndexedField, SearchResult, IndexRebuildResult, ResponseContext, SearchQuery, IndexTableMapping, SchemaEnforcementMode, IndexingMode, CreateCollectionOptions, IngestDocumentOptions } from "./models";
/**
 * HTTP request options.
 */
interface RequestOptions {
    method: string;
    path: string;
    data?: any;
    params?: Record<string, string>;
}
/**
 * Client for interacting with the Lattice REST API.
 */
export declare class LatticeClient {
    private baseUrl;
    private timeout;
    collection: CollectionMethods;
    document: DocumentMethods;
    search: SearchMethods;
    schema: SchemaMethods;
    index: IndexMethods;
    /**
     * Initialize the Lattice client.
     *
     * @param baseUrl - The base URL of the Lattice server (e.g., "http://localhost:8000")
     * @param timeout - Request timeout in milliseconds (default: 30000)
     */
    constructor(baseUrl: string, timeout?: number);
    /**
     * Make an HTTP request to the Lattice API.
     */
    request<T = any>(options: RequestOptions): Promise<ResponseContext<T>>;
    /**
     * Check if the Lattice server is healthy.
     */
    healthCheck(): Promise<boolean>;
}
/**
 * Methods for managing collections.
 */
declare class CollectionMethods {
    private client;
    constructor(client: LatticeClient);
    /**
     * Create a new collection.
     */
    create(options: CreateCollectionOptions): Promise<Collection | null>;
    /**
     * Get all collections.
     */
    readAll(): Promise<Collection[]>;
    /**
     * Get a collection by ID.
     */
    readById(collectionId: string): Promise<Collection | null>;
    /**
     * Check if a collection exists.
     */
    exists(collectionId: string): Promise<boolean>;
    /**
     * Delete a collection.
     */
    delete(collectionId: string): Promise<boolean>;
    /**
     * Get field constraints for a collection.
     */
    getConstraints(collectionId: string): Promise<FieldConstraint[]>;
    /**
     * Update constraints for a collection.
     */
    updateConstraints(collectionId: string, schemaEnforcementMode: SchemaEnforcementMode, fieldConstraints?: FieldConstraint[]): Promise<boolean>;
    /**
     * Get indexed fields for a collection.
     */
    getIndexedFields(collectionId: string): Promise<IndexedField[]>;
    /**
     * Update indexing configuration for a collection.
     */
    updateIndexing(collectionId: string, indexingMode: IndexingMode, indexedFields?: string[], rebuildIndexes?: boolean): Promise<boolean>;
    /**
     * Rebuild indexes for a collection.
     */
    rebuildIndexes(collectionId: string, dropUnusedIndexes?: boolean): Promise<IndexRebuildResult | null>;
}
/**
 * Methods for managing documents.
 */
declare class DocumentMethods {
    private client;
    constructor(client: LatticeClient);
    /**
     * Ingest a new document into a collection.
     */
    ingest(options: IngestDocumentOptions): Promise<Document | null>;
    /**
     * Get all documents in a collection.
     */
    readAllInCollection(collectionId: string, includeContent?: boolean, includeLabels?: boolean, includeTags?: boolean): Promise<Document[]>;
    /**
     * Get a document by ID.
     */
    readById(collectionId: string, documentId: string, includeContent?: boolean, includeLabels?: boolean, includeTags?: boolean): Promise<Document | null>;
    /**
     * Check if a document exists.
     */
    exists(collectionId: string, documentId: string): Promise<boolean>;
    /**
     * Delete a document.
     */
    delete(collectionId: string, documentId: string): Promise<boolean>;
}
/**
 * Methods for searching documents.
 */
declare class SearchMethods {
    private client;
    constructor(client: LatticeClient);
    /**
     * Search for documents.
     */
    search(query: SearchQuery): Promise<SearchResult | null>;
    /**
     * Search documents using a SQL-like expression.
     */
    searchBySql(collectionId: string, sqlExpression: string): Promise<SearchResult | null>;
    /**
     * Enumerate documents in a collection.
     */
    enumerate(query: SearchQuery): Promise<SearchResult | null>;
}
/**
 * Methods for managing schemas.
 */
declare class SchemaMethods {
    private client;
    constructor(client: LatticeClient);
    /**
     * Get all schemas.
     */
    readAll(): Promise<Schema[]>;
    /**
     * Get a schema by ID.
     */
    readById(schemaId: string): Promise<Schema | null>;
    /**
     * Get elements for a schema.
     */
    getElements(schemaId: string): Promise<SchemaElement[]>;
}
/**
 * Methods for managing indexes.
 */
declare class IndexMethods {
    private client;
    constructor(client: LatticeClient);
    /**
     * Get all index table mappings.
     */
    getMappings(): Promise<IndexTableMapping[]>;
}
export {};
