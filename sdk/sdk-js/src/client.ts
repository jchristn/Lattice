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
    SearchQuery,
    IndexTableMapping,
    SchemaEnforcementMode,
    IndexingMode,
    CreateCollectionOptions,
    IngestDocumentOptions,
    BatchIngestDocumentEntry,
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
import { LatticeConnectionError, LatticeApiError } from "./exceptions";

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
     *
     * On success (HTTP 2xx) the parsed response body is returned directly as the
     * payload (an empty body resolves to `undefined`). On failure (non-2xx) a
     * {@link LatticeApiError} is thrown, carrying the server's `error` message,
     * the HTTP status code, any structured `detail`, and the request id from the
     * `X-Lattice-Request-Id` response header.
     */
    async request<T = any>(options: RequestOptions): Promise<T> {
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

        const doFetch = async () => {
            const controller = new AbortController();
            const timeoutId = setTimeout(() => controller.abort(), this.timeout);
            fetchOptions.signal = controller.signal;
            try {
                return await fetch(url, fetchOptions);
            } finally {
                clearTimeout(timeoutId);
            }
        };

        let response: Awaited<ReturnType<typeof fetch>>;
        try {
            response = await doFetch();
        } catch (error: any) {
            if (error.name === "AbortError") {
                throw new LatticeConnectionError(`Request to ${url} timed out`);
            }
            throw new LatticeConnectionError(`Failed to connect to ${url}`, error);
        }

        const requestId = response.headers.get("X-Lattice-Request-Id") ?? undefined;

        // HEAD responses have no body; read the text for everything else.
        const responseText = options.method === "HEAD" ? "" : await response.text();

        // Parse the body once (may be empty, JSON, or plain text).
        let parsedBody: any = undefined;
        if (responseText) {
            try {
                parsedBody = JSON.parse(responseText);
            } catch {
                parsedBody = responseText;
            }
        }

        if (!response.ok) {
            // Error contract: body is `{ error, detail? }`. Fall back to the
            // status text when the body isn't the expected JSON shape.
            let message: string;
            let detail: any = undefined;

            if (parsedBody && typeof parsedBody === "object" && typeof parsedBody.error === "string") {
                message = parsedBody.error;
                detail = parsedBody.detail;
            } else if (typeof parsedBody === "string" && parsedBody.length > 0) {
                message = parsedBody;
            } else {
                message = response.statusText || `HTTP ${response.status}`;
            }

            throw new LatticeApiError(message, response.status, detail, requestId);
        }

        // Success: the body IS the payload. Empty body resolves to undefined.
        return parsedBody as T;
    }

    /**
     * Issue a HEAD request and report whether the resource exists (2xx).
     * Non-2xx responses (e.g. 404) resolve to `false` rather than throwing.
     */
    async head(path: string): Promise<boolean> {
        try {
            await this.request({ method: "HEAD", path });
            return true;
        } catch (error) {
            if (error instanceof LatticeApiError) {
                return false;
            }
            throw error;
        }
    }

    /**
     * Check if the Lattice server is healthy.
     */
    async healthCheck(): Promise<boolean> {
        try {
            await this.request({ method: "GET", path: "/v1.0/health" });
            return true;
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

        const result = await this.client.request({
            method: "PUT",
            path: "/v1.0/collections",
            data
        });

        return result ? parseCollection(result) : null;
    }

    /**
     * Get all collections.
     */
    async readAll(): Promise<Collection[]> {
        const result = await this.client.request({
            method: "GET",
            path: "/v1.0/collections"
        });

        if (Array.isArray(result)) {
            return result.map((c: any) => parseCollection(c)).filter((c): c is Collection => c !== null);
        }
        return [];
    }

    /**
     * Get a collection by ID.
     */
    async readById(collectionId: string): Promise<Collection | null> {
        const result = await this.client.request({
            method: "GET",
            path: `/v1.0/collections/${collectionId}`
        });

        return result ? parseCollection(result) : null;
    }

    /**
     * Check if a collection exists.
     */
    async exists(collectionId: string): Promise<boolean> {
        return this.client.head(`/v1.0/collections/${collectionId}`);
    }

    /**
     * Delete a collection.
     */
    async delete(collectionId: string): Promise<boolean> {
        try {
            await this.client.request({
                method: "DELETE",
                path: `/v1.0/collections/${collectionId}`
            });
            return true;
        } catch (error) {
            if (error instanceof LatticeApiError) {
                return false;
            }
            throw error;
        }
    }

    /**
     * Get field constraints for a collection.
     */
    async getConstraints(collectionId: string): Promise<FieldConstraint[]> {
        const result = await this.client.request({
            method: "GET",
            path: `/v1.0/collections/${collectionId}/constraints`
        });

        if (result && result.fieldConstraints) {
            return result.fieldConstraints.map((c: any) => parseFieldConstraint(c)).filter((c: any) => c !== null);
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

        try {
            await this.client.request({
                method: "PUT",
                path: `/v1.0/collections/${collectionId}/constraints`,
                data
            });
            return true;
        } catch (error) {
            if (error instanceof LatticeApiError) {
                return false;
            }
            throw error;
        }
    }

    /**
     * Get indexed fields for a collection.
     */
    async getIndexedFields(collectionId: string): Promise<IndexedField[]> {
        const result = await this.client.request({
            method: "GET",
            path: `/v1.0/collections/${collectionId}/indexing`
        });

        if (result && result.indexedFields) {
            return result.indexedFields.map((f: any) => parseIndexedField(f)).filter((f: any) => f !== null);
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

        try {
            await this.client.request({
                method: "PUT",
                path: `/v1.0/collections/${collectionId}/indexing`,
                data
            });
            return true;
        } catch (error) {
            if (error instanceof LatticeApiError) {
                return false;
            }
            throw error;
        }
    }

    /**
     * Rebuild indexes for a collection.
     */
    async rebuildIndexes(
        collectionId: string,
        dropUnusedIndexes: boolean = true
    ): Promise<IndexRebuildResult | null> {
        const result = await this.client.request({
            method: "POST",
            path: `/v1.0/collections/${collectionId}/indexes/rebuild`,
            data: { dropUnusedIndexes }
        });

        return result ? parseIndexRebuildResult(result) : null;
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

        const result = await this.client.request({
            method: "PUT",
            path: `/v1.0/collections/${options.collectionId}/documents`,
            data
        });

        return result ? parseDocument(result) : null;
    }

    /**
     * Ingest multiple documents into a collection in a single batch operation.
     */
    async ingestBatch(
        collectionId: string,
        documents: BatchIngestDocumentEntry[]
    ): Promise<Document[] | null> {
        const result = await this.client.request({
            method: "PUT",
            path: `/v1.0/collections/${collectionId}/documents/batch`,
            data: {
                documents: documents.map(doc => {
                    const entry: any = { content: doc.content };
                    if (doc.name) entry.name = doc.name;
                    if (doc.labels) entry.labels = doc.labels;
                    if (doc.tags) entry.tags = doc.tags;
                    return entry;
                })
            }
        });

        if (Array.isArray(result)) {
            return result.map((d: any) => parseDocument(d)).filter((d): d is Document => d !== null);
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
        const result = await this.client.request({
            method: "GET",
            path: `/v1.0/collections/${collectionId}/documents`,
            params: {
                includeContent: String(includeContent),
                includeLabels: String(includeLabels),
                includeTags: String(includeTags)
            }
        });

        if (Array.isArray(result)) {
            return result.map((d: any) => parseDocument(d)).filter((d): d is Document => d !== null);
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
            // not the document metadata. We make two requests:
            // 1. Get document metadata (without content)
            // 2. Get raw content separately
            // Then combine them.

            // First, get document metadata
            const metadata = await this.client.request({
                method: "GET",
                path: `/v1.0/collections/${collectionId}/documents/${documentId}`,
                params: {
                    includeContent: "false",
                    includeLabels: String(includeLabels),
                    includeTags: String(includeTags)
                }
            });

            if (!metadata) {
                return null;
            }

            const doc = parseDocument(metadata);
            if (!doc) {
                return null;
            }

            // Now get the raw content
            const content = await this.client.request({
                method: "GET",
                path: `/v1.0/collections/${collectionId}/documents/${documentId}`,
                params: {
                    includeContent: "true",
                    includeLabels: "false",
                    includeTags: "false"
                }
            });

            if (content !== undefined && content !== null) {
                doc.content = content;
            }

            return doc;
        }

        // Normal flow when includeContent=false
        const result = await this.client.request({
            method: "GET",
            path: `/v1.0/collections/${collectionId}/documents/${documentId}`,
            params: {
                includeContent: "false",
                includeLabels: String(includeLabels),
                includeTags: String(includeTags)
            }
        });

        return result ? parseDocument(result) : null;
    }

    /**
     * Check if a document exists.
     */
    async exists(collectionId: string, documentId: string): Promise<boolean> {
        return this.client.head(`/v1.0/collections/${collectionId}/documents/${documentId}`);
    }

    /**
     * Delete a document.
     */
    async delete(collectionId: string, documentId: string): Promise<boolean> {
        try {
            await this.client.request({
                method: "DELETE",
                path: `/v1.0/collections/${collectionId}/documents/${documentId}`
            });
            return true;
        } catch (error) {
            if (error instanceof LatticeApiError) {
                return false;
            }
            throw error;
        }
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
        const result = await this.client.request({
            method: "POST",
            path: `/v1.0/collections/${query.collectionId}/documents/search`,
            data: searchQueryToRequest(query)
        });

        return result ? parseSearchResult(result) : null;
    }

    /**
     * Search documents using a SQL-like expression.
     */
    async searchBySql(collectionId: string, sqlExpression: string): Promise<SearchResult | null> {
        const result = await this.client.request({
            method: "POST",
            path: `/v1.0/collections/${collectionId}/documents/search`,
            data: { sqlExpression }
        });

        return result ? parseSearchResult(result) : null;
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
        const result = await this.client.request({
            method: "GET",
            path: "/v1.0/schemas"
        });

        if (Array.isArray(result)) {
            return result.map((s: any) => parseSchema(s)).filter((s): s is Schema => s !== null);
        }
        return [];
    }

    /**
     * Get a schema by ID.
     */
    async readById(schemaId: string): Promise<Schema | null> {
        const result = await this.client.request({
            method: "GET",
            path: `/v1.0/schemas/${schemaId}`
        });

        return result ? parseSchema(result) : null;
    }

    /**
     * Get elements for a schema.
     */
    async getElements(schemaId: string): Promise<SchemaElement[]> {
        const result = await this.client.request({
            method: "GET",
            path: `/v1.0/schemas/${schemaId}/elements`
        });

        if (Array.isArray(result)) {
            return result.map((e: any) => parseSchemaElement(e)).filter((e): e is SchemaElement => e !== null);
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
        const result = await this.client.request({
            method: "GET",
            path: "/v1.0/tables"
        });

        if (Array.isArray(result)) {
            return result.map((m: any) => parseIndexTableMapping(m)).filter((m): m is IndexTableMapping => m !== null);
        }
        return [];
    }
}

