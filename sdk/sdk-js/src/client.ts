/**
 * Lattice SDK Client
 *
 * Main client for interacting with the Lattice REST API.
 */

import {
    Collection,
    Document,
    Schema,
    SchemaElement,
    FieldConstraint,
    IndexedField,
    SearchResult,
    IndexRebuildResult,
    ResponseContext,
    SearchQuery,
    IndexTableMapping,
    SchemaEnforcementMode,
    IndexingMode,
    CreateCollectionOptions,
    IngestDocumentOptions,
    parseCollection,
    parseDocument,
    parseSchema,
    parseSchemaElement,
    parseFieldConstraint,
    parseIndexedField,
    parseSearchResult,
    parseIndexRebuildResult,
    parseIndexTableMapping,
    fieldConstraintToRequest,
    searchQueryToRequest
} from "./models";
import { LatticeError, LatticeConnectionError, LatticeApiError } from "./exceptions";

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
export class LatticeClient {
    private baseUrl: string;
    private timeout: number;

    public collection: CollectionMethods;
    public document: DocumentMethods;
    public search: SearchMethods;
    public schema: SchemaMethods;
    public index: IndexMethods;

    /**
     * Initialize the Lattice client.
     *
     * @param baseUrl - The base URL of the Lattice server (e.g., "http://localhost:8000")
     * @param timeout - Request timeout in milliseconds (default: 30000)
     */
    constructor(baseUrl: string, timeout: number = 30000) {
        this.baseUrl = baseUrl.replace(/\/+$/, "");
        this.timeout = timeout;

        this.collection = new CollectionMethods(this);
        this.document = new DocumentMethods(this);
        this.search = new SearchMethods(this);
        this.schema = new SchemaMethods(this);
        this.index = new IndexMethods(this);
    }

    /**
     * Make an HTTP request to the Lattice API.
     */
    async request<T = any>(options: RequestOptions): Promise<ResponseContext<T>> {
        let url = `${this.baseUrl}${options.path}`;

        if (options.params) {
            const searchParams = new URLSearchParams(options.params);
            url += `?${searchParams.toString()}`;
        }

        const fetchOptions: RequestInit = {
            method: options.method,
            headers: {
                "Content-Type": "application/json"
            }
        };

        if (options.data && (options.method === "POST" || options.method === "PUT")) {
            fetchOptions.body = JSON.stringify(options.data);
        }

        try {
            const controller = new AbortController();
            const timeoutId = setTimeout(() => controller.abort(), this.timeout);
            fetchOptions.signal = controller.signal;

            const response = await fetch(url, fetchOptions);
            clearTimeout(timeoutId);

            // For HEAD requests, we don't have a body
            if (options.method === "HEAD") {
                return {
                    success: response.status === 200,
                    statusCode: response.status,
                    headers: Object.fromEntries(response.headers.entries()),
                    processingTimeMs: 0
                };
            }

            const responseText = await response.text();

            if (responseText) {
                try {
                    const responseData = JSON.parse(responseText);

                    // Check if this is a standard API envelope (has 'success' property)
                    // or raw content (e.g., when includeContent=true for document retrieval)
                    if (responseData.success !== undefined) {
                        // Standard envelope response
                        return {
                            success: responseData.success,
                            statusCode: responseData.statusCode ?? response.status,
                            errorMessage: responseData.errorMessage,
                            data: responseData.data,
                            headers: Object.fromEntries(response.headers.entries()),
                            processingTimeMs: responseData.processingTimeMs ?? 0,
                            guid: responseData.guid,
                            timestampUtc: responseData.timestampUtc ? new Date(responseData.timestampUtc) : undefined
                        };
                    } else {
                        // Raw content response (not wrapped in standard envelope)
                        return {
                            success: response.ok,
                            statusCode: response.status,
                            data: responseData,
                            headers: Object.fromEntries(response.headers.entries()),
                            processingTimeMs: 0
                        };
                    }
                } catch {
                    return {
                        success: response.ok,
                        statusCode: response.status,
                        data: responseText as any,
                        headers: Object.fromEntries(response.headers.entries()),
                        processingTimeMs: 0
                    };
                }
            }

            return {
                success: response.ok,
                statusCode: response.status,
                headers: Object.fromEntries(response.headers.entries()),
                processingTimeMs: 0
            };
        } catch (error: any) {
            if (error.name === "AbortError") {
                throw new LatticeConnectionError(`Request to ${url} timed out`);
            }
            throw new LatticeConnectionError(`Failed to connect to ${url}`, error);
        }
    }

    /**
     * Check if the Lattice server is healthy.
     */
    async healthCheck(): Promise<boolean> {
        try {
            const response = await this.request({ method: "GET", path: "/v1.0/health" });
            return response.success;
        } catch {
            return false;
        }
    }
}

/**
 * Methods for managing collections.
 */
class CollectionMethods {
    constructor(private client: LatticeClient) {}

    /**
     * Create a new collection.
     */
    async create(options: CreateCollectionOptions): Promise<Collection | null> {
        const data: any = { name: options.name };

        if (options.description) data.description = options.description;
        if (options.documentsDirectory) data.documentsDirectory = options.documentsDirectory;
        if (options.labels) data.labels = options.labels;
        if (options.tags) data.tags = options.tags;
        if (options.schemaEnforcementMode !== undefined && options.schemaEnforcementMode !== SchemaEnforcementMode.None) {
            data.schemaEnforcementMode = options.schemaEnforcementMode;
        }
        if (options.fieldConstraints) {
            data.fieldConstraints = options.fieldConstraints.map(fieldConstraintToRequest);
        }
        if (options.indexingMode !== undefined && options.indexingMode !== IndexingMode.All) {
            data.indexingMode = options.indexingMode;
        }
        if (options.indexedFields) data.indexedFields = options.indexedFields;

        const response = await this.client.request({
            method: "PUT",
            path: "/v1.0/collections",
            data
        });

        if (response.success && response.data) {
            return parseCollection(response.data);
        }
        return null;
    }

    /**
     * Get all collections.
     */
    async readAll(): Promise<Collection[]> {
        const response = await this.client.request({
            method: "GET",
            path: "/v1.0/collections"
        });

        if (response.success && response.data) {
            return response.data.map((c: any) => parseCollection(c)).filter((c: any) => c !== null);
        }
        return [];
    }

    /**
     * Get a collection by ID.
     */
    async readById(collectionId: string): Promise<Collection | null> {
        const response = await this.client.request({
            method: "GET",
            path: `/v1.0/collections/${collectionId}`
        });

        if (response.success && response.data) {
            return parseCollection(response.data);
        }
        return null;
    }

    /**
     * Check if a collection exists.
     */
    async exists(collectionId: string): Promise<boolean> {
        const response = await this.client.request({
            method: "HEAD",
            path: `/v1.0/collections/${collectionId}`
        });
        return response.success;
    }

    /**
     * Delete a collection.
     */
    async delete(collectionId: string): Promise<boolean> {
        const response = await this.client.request({
            method: "DELETE",
            path: `/v1.0/collections/${collectionId}`
        });
        return response.success;
    }

    /**
     * Get field constraints for a collection.
     */
    async getConstraints(collectionId: string): Promise<FieldConstraint[]> {
        const response = await this.client.request({
            method: "GET",
            path: `/v1.0/collections/${collectionId}/constraints`
        });

        if (response.success && response.data && response.data.fieldConstraints) {
            return response.data.fieldConstraints.map((c: any) => parseFieldConstraint(c)).filter((c: any) => c !== null);
        }
        return [];
    }

    /**
     * Update constraints for a collection.
     */
    async updateConstraints(
        collectionId: string,
        schemaEnforcementMode: SchemaEnforcementMode,
        fieldConstraints?: FieldConstraint[]
    ): Promise<boolean> {
        const data: any = { schemaEnforcementMode };
        if (fieldConstraints) {
            data.fieldConstraints = fieldConstraints.map(fieldConstraintToRequest);
        }

        const response = await this.client.request({
            method: "PUT",
            path: `/v1.0/collections/${collectionId}/constraints`,
            data
        });
        return response.success;
    }

    /**
     * Get indexed fields for a collection.
     */
    async getIndexedFields(collectionId: string): Promise<IndexedField[]> {
        const response = await this.client.request({
            method: "GET",
            path: `/v1.0/collections/${collectionId}/indexing`
        });

        if (response.success && response.data && response.data.indexedFields) {
            return response.data.indexedFields.map((f: any) => parseIndexedField(f)).filter((f: any) => f !== null);
        }
        return [];
    }

    /**
     * Update indexing configuration for a collection.
     */
    async updateIndexing(
        collectionId: string,
        indexingMode: IndexingMode,
        indexedFields?: string[],
        rebuildIndexes: boolean = false
    ): Promise<boolean> {
        const data: any = {
            indexingMode,
            rebuildIndexes
        };
        if (indexedFields) data.indexedFields = indexedFields;

        const response = await this.client.request({
            method: "PUT",
            path: `/v1.0/collections/${collectionId}/indexing`,
            data
        });
        return response.success;
    }

    /**
     * Rebuild indexes for a collection.
     */
    async rebuildIndexes(
        collectionId: string,
        dropUnusedIndexes: boolean = true
    ): Promise<IndexRebuildResult | null> {
        const response = await this.client.request({
            method: "POST",
            path: `/v1.0/collections/${collectionId}/indexes/rebuild`,
            data: { dropUnusedIndexes }
        });

        if (response.success && response.data) {
            return parseIndexRebuildResult(response.data);
        }
        return null;
    }
}

/**
 * Methods for managing documents.
 */
class DocumentMethods {
    constructor(private client: LatticeClient) {}

    /**
     * Ingest a new document into a collection.
     */
    async ingest(options: IngestDocumentOptions): Promise<Document | null> {
        const data: any = { content: options.content };

        if (options.name) data.name = options.name;
        if (options.labels) data.labels = options.labels;
        if (options.tags) data.tags = options.tags;

        const response = await this.client.request({
            method: "PUT",
            path: `/v1.0/collections/${options.collectionId}/documents`,
            data
        });

        if (response.success && response.data) {
            return parseDocument(response.data);
        }
        return null;
    }

    /**
     * Get all documents in a collection.
     */
    async readAllInCollection(
        collectionId: string,
        includeContent: boolean = false,
        includeLabels: boolean = true,
        includeTags: boolean = true
    ): Promise<Document[]> {
        const response = await this.client.request({
            method: "GET",
            path: `/v1.0/collections/${collectionId}/documents`,
            params: {
                includeContent: String(includeContent),
                includeLabels: String(includeLabels),
                includeTags: String(includeTags)
            }
        });

        if (response.success && response.data) {
            return response.data.map((d: any) => parseDocument(d)).filter((d: any) => d !== null);
        }
        return [];
    }

    /**
     * Get a document by ID.
     */
    async readById(
        collectionId: string,
        documentId: string,
        includeContent: boolean = false,
        includeLabels: boolean = true,
        includeTags: boolean = true
    ): Promise<Document | null> {
        if (includeContent) {
            // When includeContent=true, the server returns ONLY the raw document body,
            // not wrapped in the standard API envelope. We need to make two requests:
            // 1. Get document metadata (without content)
            // 2. Get raw content separately
            // Then combine them.

            // First, get document metadata
            const metadataResponse = await this.client.request({
                method: "GET",
                path: `/v1.0/collections/${collectionId}/documents/${documentId}`,
                params: {
                    includeContent: "false",
                    includeLabels: String(includeLabels),
                    includeTags: String(includeTags)
                }
            });

            if (!metadataResponse.success || !metadataResponse.data) {
                return null;
            }

            const doc = parseDocument(metadataResponse.data);
            if (!doc) {
                return null;
            }

            // Now get the raw content
            const contentResponse = await this.client.request({
                method: "GET",
                path: `/v1.0/collections/${collectionId}/documents/${documentId}`,
                params: {
                    includeContent: "true",
                    includeLabels: "false",
                    includeTags: "false"
                }
            });

            if (contentResponse.success && contentResponse.data !== undefined) {
                doc.content = contentResponse.data;
            }

            return doc;
        }

        // Normal flow when includeContent=false
        const response = await this.client.request({
            method: "GET",
            path: `/v1.0/collections/${collectionId}/documents/${documentId}`,
            params: {
                includeContent: "false",
                includeLabels: String(includeLabels),
                includeTags: String(includeTags)
            }
        });

        if (response.success && response.data) {
            return parseDocument(response.data);
        }
        return null;
    }

    /**
     * Check if a document exists.
     */
    async exists(collectionId: string, documentId: string): Promise<boolean> {
        const response = await this.client.request({
            method: "HEAD",
            path: `/v1.0/collections/${collectionId}/documents/${documentId}`
        });
        return response.success;
    }

    /**
     * Delete a document.
     */
    async delete(collectionId: string, documentId: string): Promise<boolean> {
        const response = await this.client.request({
            method: "DELETE",
            path: `/v1.0/collections/${collectionId}/documents/${documentId}`
        });
        return response.success;
    }
}

/**
 * Methods for searching documents.
 */
class SearchMethods {
    constructor(private client: LatticeClient) {}

    /**
     * Search for documents.
     */
    async search(query: SearchQuery): Promise<SearchResult | null> {
        const response = await this.client.request({
            method: "POST",
            path: `/v1.0/collections/${query.collectionId}/documents/search`,
            data: searchQueryToRequest(query)
        });

        if (response.success && response.data) {
            return parseSearchResult(response.data);
        }
        return null;
    }

    /**
     * Search documents using a SQL-like expression.
     */
    async searchBySql(collectionId: string, sqlExpression: string): Promise<SearchResult | null> {
        const response = await this.client.request({
            method: "POST",
            path: `/v1.0/collections/${collectionId}/documents/search`,
            data: { sqlExpression }
        });

        if (response.success && response.data) {
            return parseSearchResult(response.data);
        }
        return null;
    }

    /**
     * Enumerate documents in a collection.
     */
    async enumerate(query: SearchQuery): Promise<SearchResult | null> {
        return this.search(query);
    }
}

/**
 * Methods for managing schemas.
 */
class SchemaMethods {
    constructor(private client: LatticeClient) {}

    /**
     * Get all schemas.
     */
    async readAll(): Promise<Schema[]> {
        const response = await this.client.request({
            method: "GET",
            path: "/v1.0/schemas"
        });

        if (response.success && response.data) {
            return response.data.map((s: any) => parseSchema(s)).filter((s: any) => s !== null);
        }
        return [];
    }

    /**
     * Get a schema by ID.
     */
    async readById(schemaId: string): Promise<Schema | null> {
        const response = await this.client.request({
            method: "GET",
            path: `/v1.0/schemas/${schemaId}`
        });

        if (response.success && response.data) {
            return parseSchema(response.data);
        }
        return null;
    }

    /**
     * Get elements for a schema.
     */
    async getElements(schemaId: string): Promise<SchemaElement[]> {
        const response = await this.client.request({
            method: "GET",
            path: `/v1.0/schemas/${schemaId}/elements`
        });

        if (response.success && response.data) {
            return response.data.map((e: any) => parseSchemaElement(e)).filter((e: any) => e !== null);
        }
        return [];
    }
}

/**
 * Methods for managing indexes.
 */
class IndexMethods {
    constructor(private client: LatticeClient) {}

    /**
     * Get all index table mappings.
     */
    async getMappings(): Promise<IndexTableMapping[]> {
        const response = await this.client.request({
            method: "GET",
            path: "/v1.0/tables"
        });

        if (response.success && response.data) {
            return response.data.map((m: any) => parseIndexTableMapping(m)).filter((m: any) => m !== null);
        }
        return [];
    }
}

