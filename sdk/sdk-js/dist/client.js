"use strict";
/**
 * Lattice SDK Client
 *
 * Main client for interacting with the Lattice REST API.
 */
Object.defineProperty(exports, "__esModule", { value: true });
exports.LatticeClient = void 0;
const models_1 = require("./models");
const exceptions_1 = require("./exceptions");
/**
 * Build query params for the optional `maxResults` / `skip` pagination values,
 * merging into an optional base set of params.
 */
function paginationParams(options, base) {
    const params = { ...(base ?? {}) };
    if (options?.maxResults !== undefined)
        params.maxResults = String(options.maxResults);
    if (options?.skip !== undefined)
        params.skip = String(options.skip);
    return Object.keys(params).length > 0 ? params : undefined;
}
/**
 * Client for interacting with the Lattice REST API.
 */
class LatticeClient {
    /**
     * Initialize the Lattice client.
     *
     * @param baseUrl - The base URL of the Lattice server (e.g., "http://localhost:8000")
     * @param timeout - Request timeout in milliseconds (default: 30000)
     */
    constructor(baseUrl, timeout = 30000) {
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
    async request(options) {
        let url = `${this.baseUrl}${options.path}`;
        if (options.params) {
            const searchParams = new URLSearchParams(options.params);
            url += `?${searchParams.toString()}`;
        }
        const fetchOptions = {
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
            }
            finally {
                clearTimeout(timeoutId);
            }
        };
        let response;
        try {
            response = await doFetch();
        }
        catch (error) {
            if (error.name === "AbortError") {
                throw new exceptions_1.LatticeConnectionError(`Request to ${url} timed out`);
            }
            throw new exceptions_1.LatticeConnectionError(`Failed to connect to ${url}`, error);
        }
        const requestId = response.headers.get("X-Lattice-Request-Id") ?? undefined;
        // HEAD responses have no body; read the text for everything else.
        const responseText = options.method === "HEAD" ? "" : await response.text();
        // Parse the body once (may be empty, JSON, or plain text).
        let parsedBody = undefined;
        if (responseText) {
            try {
                parsedBody = JSON.parse(responseText);
            }
            catch {
                parsedBody = responseText;
            }
        }
        if (!response.ok) {
            // Error contract: body is `{ error, detail? }`. Fall back to the
            // status text when the body isn't the expected JSON shape.
            let message;
            let detail = undefined;
            if (parsedBody && typeof parsedBody === "object" && typeof parsedBody.error === "string") {
                message = parsedBody.error;
                detail = parsedBody.detail;
            }
            else if (typeof parsedBody === "string" && parsedBody.length > 0) {
                message = parsedBody;
            }
            else {
                message = response.statusText || `HTTP ${response.status}`;
            }
            throw new exceptions_1.LatticeApiError(message, response.status, detail, requestId);
        }
        // Success: the body IS the payload. Empty body resolves to undefined.
        return parsedBody;
    }
    /**
     * Issue a HEAD request and report whether the resource exists (2xx).
     * Non-2xx responses (e.g. 404) resolve to `false` rather than throwing.
     */
    async head(path) {
        try {
            await this.request({ method: "HEAD", path });
            return true;
        }
        catch (error) {
            if (error instanceof exceptions_1.LatticeApiError) {
                return false;
            }
            throw error;
        }
    }
    /**
     * Check if the Lattice server is healthy.
     */
    async healthCheck() {
        try {
            await this.request({ method: "GET", path: "/v1.0/health" });
            return true;
        }
        catch {
            return false;
        }
    }
}
exports.LatticeClient = LatticeClient;
/**
 * Methods for managing collections.
 */
class CollectionMethods {
    constructor(client) {
        this.client = client;
    }
    /**
     * Create a new collection.
     */
    async create(options) {
        const data = { name: options.name };
        if (options.description)
            data.description = options.description;
        if (options.documentsDirectory)
            data.documentsDirectory = options.documentsDirectory;
        if (options.labels)
            data.labels = options.labels;
        if (options.tags)
            data.tags = options.tags;
        if (options.schemaEnforcementMode !== undefined && options.schemaEnforcementMode !== models_1.SchemaEnforcementMode.None) {
            data.schemaEnforcementMode = options.schemaEnforcementMode;
        }
        if (options.fieldConstraints) {
            data.fieldConstraints = options.fieldConstraints.map(models_1.fieldConstraintToRequest);
        }
        if (options.indexingMode !== undefined && options.indexingMode !== models_1.IndexingMode.All) {
            data.indexingMode = options.indexingMode;
        }
        if (options.indexedFields)
            data.indexedFields = options.indexedFields;
        const result = await this.client.request({
            method: "PUT",
            path: "/v1.0/collections",
            data
        });
        return result ? (0, models_1.parseCollection)(result) : null;
    }
    /**
     * Get all collections.
     */
    async readAll(options = {}) {
        const result = await this.client.request({
            method: "GET",
            path: "/v1.0/collections",
            params: paginationParams(options)
        });
        return (0, models_1.parseEnumerationResult)(result, models_1.parseCollection);
    }
    /**
     * Get a collection by ID.
     */
    async readById(collectionId) {
        const result = await this.client.request({
            method: "GET",
            path: `/v1.0/collections/${collectionId}`
        });
        return result ? (0, models_1.parseCollection)(result) : null;
    }
    /**
     * Check if a collection exists.
     */
    async exists(collectionId) {
        return this.client.head(`/v1.0/collections/${collectionId}`);
    }
    /**
     * Delete a collection.
     */
    async delete(collectionId) {
        try {
            await this.client.request({
                method: "DELETE",
                path: `/v1.0/collections/${collectionId}`
            });
            return true;
        }
        catch (error) {
            if (error instanceof exceptions_1.LatticeApiError) {
                return false;
            }
            throw error;
        }
    }
    /**
     * Get field constraints for a collection.
     */
    async getConstraints(collectionId) {
        const result = await this.client.request({
            method: "GET",
            path: `/v1.0/collections/${collectionId}/constraints`
        });
        if (result && result.fieldConstraints) {
            return result.fieldConstraints.map((c) => (0, models_1.parseFieldConstraint)(c)).filter((c) => c !== null);
        }
        return [];
    }
    /**
     * Update constraints for a collection.
     */
    async updateConstraints(collectionId, schemaEnforcementMode, fieldConstraints) {
        const data = { schemaEnforcementMode };
        if (fieldConstraints) {
            data.fieldConstraints = fieldConstraints.map(models_1.fieldConstraintToRequest);
        }
        try {
            await this.client.request({
                method: "PUT",
                path: `/v1.0/collections/${collectionId}/constraints`,
                data
            });
            return true;
        }
        catch (error) {
            if (error instanceof exceptions_1.LatticeApiError) {
                return false;
            }
            throw error;
        }
    }
    /**
     * Get indexed fields for a collection.
     */
    async getIndexedFields(collectionId) {
        const result = await this.client.request({
            method: "GET",
            path: `/v1.0/collections/${collectionId}/indexing`
        });
        if (result && result.indexedFields) {
            return result.indexedFields.map((f) => (0, models_1.parseIndexedField)(f)).filter((f) => f !== null);
        }
        return [];
    }
    /**
     * Update indexing configuration for a collection.
     */
    async updateIndexing(collectionId, indexingMode, indexedFields, rebuildIndexes = false) {
        const data = {
            indexingMode,
            rebuildIndexes
        };
        if (indexedFields)
            data.indexedFields = indexedFields;
        try {
            await this.client.request({
                method: "PUT",
                path: `/v1.0/collections/${collectionId}/indexing`,
                data
            });
            return true;
        }
        catch (error) {
            if (error instanceof exceptions_1.LatticeApiError) {
                return false;
            }
            throw error;
        }
    }
    /**
     * Rebuild indexes for a collection.
     */
    async rebuildIndexes(collectionId, dropUnusedIndexes = true) {
        const result = await this.client.request({
            method: "POST",
            path: `/v1.0/collections/${collectionId}/indexes/rebuild`,
            data: { dropUnusedIndexes }
        });
        return result ? (0, models_1.parseIndexRebuildResult)(result) : null;
    }
}
/**
 * Methods for managing documents.
 */
class DocumentMethods {
    constructor(client) {
        this.client = client;
    }
    /**
     * Ingest a new document into a collection.
     */
    async ingest(options) {
        const data = { content: options.content };
        if (options.name)
            data.name = options.name;
        if (options.labels)
            data.labels = options.labels;
        if (options.tags)
            data.tags = options.tags;
        const result = await this.client.request({
            method: "PUT",
            path: `/v1.0/collections/${options.collectionId}/documents`,
            data
        });
        return result ? (0, models_1.parseDocument)(result) : null;
    }
    /**
     * Ingest multiple documents into a collection in a single batch operation.
     */
    async ingestBatch(collectionId, documents) {
        const result = await this.client.request({
            method: "PUT",
            path: `/v1.0/collections/${collectionId}/documents/batch`,
            data: {
                documents: documents.map(doc => {
                    const entry = { content: doc.content };
                    if (doc.name)
                        entry.name = doc.name;
                    if (doc.labels)
                        entry.labels = doc.labels;
                    if (doc.tags)
                        entry.tags = doc.tags;
                    return entry;
                })
            }
        });
        if (Array.isArray(result)) {
            return result.map((d) => (0, models_1.parseDocument)(d)).filter((d) => d !== null);
        }
        return null;
    }
    /**
     * Get all documents in a collection.
     */
    async readAllInCollection(collectionId, includeContent = false, includeLabels = true, includeTags = true, options = {}) {
        const result = await this.client.request({
            method: "GET",
            path: `/v1.0/collections/${collectionId}/documents`,
            params: paginationParams(options, {
                includeContent: String(includeContent),
                includeLabels: String(includeLabels),
                includeTags: String(includeTags)
            })
        });
        return (0, models_1.parseEnumerationResult)(result, models_1.parseDocument);
    }
    /**
     * Get a document by ID.
     */
    async readById(collectionId, documentId, includeContent = false, includeLabels = true, includeTags = true) {
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
            const doc = (0, models_1.parseDocument)(metadata);
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
        return result ? (0, models_1.parseDocument)(result) : null;
    }
    /**
     * Check if a document exists.
     */
    async exists(collectionId, documentId) {
        return this.client.head(`/v1.0/collections/${collectionId}/documents/${documentId}`);
    }
    /**
     * Delete a document.
     */
    async delete(collectionId, documentId) {
        try {
            await this.client.request({
                method: "DELETE",
                path: `/v1.0/collections/${collectionId}/documents/${documentId}`
            });
            return true;
        }
        catch (error) {
            if (error instanceof exceptions_1.LatticeApiError) {
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
    constructor(client) {
        this.client = client;
    }
    /**
     * Search for documents.
     */
    async search(query) {
        const result = await this.client.request({
            method: "POST",
            path: `/v1.0/collections/${query.collectionId}/documents/search`,
            data: (0, models_1.searchQueryToRequest)(query)
        });
        return result ? (0, models_1.parseSearchResult)(result) : null;
    }
    /**
     * Search documents using a SQL-like expression.
     */
    async searchBySql(collectionId, sqlExpression) {
        const result = await this.client.request({
            method: "POST",
            path: `/v1.0/collections/${collectionId}/documents/search`,
            data: { sqlExpression }
        });
        return result ? (0, models_1.parseSearchResult)(result) : null;
    }
    /**
     * Enumerate documents in a collection.
     */
    async enumerate(query) {
        return this.search(query);
    }
}
/**
 * Methods for managing schemas.
 */
class SchemaMethods {
    constructor(client) {
        this.client = client;
    }
    /**
     * Get all schemas.
     */
    async readAll(options = {}) {
        const result = await this.client.request({
            method: "GET",
            path: "/v1.0/schemas",
            params: paginationParams(options)
        });
        return (0, models_1.parseEnumerationResult)(result, models_1.parseSchema);
    }
    /**
     * Get a schema by ID.
     */
    async readById(schemaId) {
        const result = await this.client.request({
            method: "GET",
            path: `/v1.0/schemas/${schemaId}`
        });
        return result ? (0, models_1.parseSchema)(result) : null;
    }
    /**
     * Get elements for a schema.
     */
    async getElements(schemaId, options = {}) {
        const result = await this.client.request({
            method: "GET",
            path: `/v1.0/schemas/${schemaId}/elements`,
            params: paginationParams(options)
        });
        return (0, models_1.parseEnumerationResult)(result, models_1.parseSchemaElement);
    }
}
/**
 * Methods for managing indexes.
 */
class IndexMethods {
    constructor(client) {
        this.client = client;
    }
    /**
     * Get all index table mappings.
     */
    async getMappings(options = {}) {
        const result = await this.client.request({
            method: "GET",
            path: "/v1.0/tables",
            params: paginationParams(options)
        });
        return (0, models_1.parseEnumerationResult)(result, models_1.parseIndexTableMapping);
    }
    /**
     * Get the entries for an index table. The entries are returned in the
     * `objects` array of the {@link EnumerationResult}; the total number of
     * entries is available on `totalRecords`.
     */
    async getEntries(tableName, options = {}) {
        const result = await this.client.request({
            method: "GET",
            path: `/v1.0/tables/${encodeURIComponent(tableName)}/entries`,
            params: paginationParams(options)
        });
        return (0, models_1.parseEnumerationResult)(result, models_1.parseIndexTableEntry);
    }
}
//# sourceMappingURL=data:application/json;base64,eyJ2ZXJzaW9uIjozLCJmaWxlIjoiY2xpZW50LmpzIiwic291cmNlUm9vdCI6IiIsInNvdXJjZXMiOlsiLi4vc3JjL2NsaWVudC50cyJdLCJuYW1lcyI6W10sIm1hcHBpbmdzIjoiO0FBQUE7Ozs7R0FJRzs7O0FBRUgscUNBZ0NrQjtBQUNsQiw2Q0FBdUU7QUFZdkU7OztHQUdHO0FBQ0gsU0FBUyxnQkFBZ0IsQ0FDckIsT0FBMkIsRUFDM0IsSUFBNkI7SUFFN0IsTUFBTSxNQUFNLEdBQTJCLEVBQUUsR0FBRyxDQUFDLElBQUksSUFBSSxFQUFFLENBQUMsRUFBRSxDQUFDO0lBQzNELElBQUksT0FBTyxFQUFFLFVBQVUsS0FBSyxTQUFTO1FBQUUsTUFBTSxDQUFDLFVBQVUsR0FBRyxNQUFNLENBQUMsT0FBTyxDQUFDLFVBQVUsQ0FBQyxDQUFDO0lBQ3RGLElBQUksT0FBTyxFQUFFLElBQUksS0FBSyxTQUFTO1FBQUUsTUFBTSxDQUFDLElBQUksR0FBRyxNQUFNLENBQUMsT0FBTyxDQUFDLElBQUksQ0FBQyxDQUFDO0lBQ3BFLE9BQU8sTUFBTSxDQUFDLElBQUksQ0FBQyxNQUFNLENBQUMsQ0FBQyxNQUFNLEdBQUcsQ0FBQyxDQUFDLENBQUMsQ0FBQyxNQUFNLENBQUMsQ0FBQyxDQUFDLFNBQVMsQ0FBQztBQUMvRCxDQUFDO0FBRUQ7O0dBRUc7QUFDSCxNQUFhLGFBQWE7SUFVdEI7Ozs7O09BS0c7SUFDSCxZQUFZLE9BQWUsRUFBRSxVQUFrQixLQUFLO1FBQ2hELElBQUksQ0FBQyxPQUFPLEdBQUcsT0FBTyxDQUFDLE9BQU8sQ0FBQyxNQUFNLEVBQUUsRUFBRSxDQUFDLENBQUM7UUFDM0MsSUFBSSxDQUFDLE9BQU8sR0FBRyxPQUFPLENBQUM7UUFFdkIsSUFBSSxDQUFDLFVBQVUsR0FBRyxJQUFJLGlCQUFpQixDQUFDLElBQUksQ0FBQyxDQUFDO1FBQzlDLElBQUksQ0FBQyxRQUFRLEdBQUcsSUFBSSxlQUFlLENBQUMsSUFBSSxDQUFDLENBQUM7UUFDMUMsSUFBSSxDQUFDLE1BQU0sR0FBRyxJQUFJLGFB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QUFLO1lBQ2IsSUFBSSxFQUFFLGlCQUFpQixRQUFRLFdBQVc7WUFDMUMsTUFBTSxFQUFFLGdCQUFnQixDQUFDLE9BQU8sQ0FBQztTQUNwQyxDQUFDLENBQUM7UUFFSCxPQUFPLElBQUEsK0JBQXNCLEVBQUMsTUFBTSxFQUFFLDJCQUFrQixDQUFDLENBQUM7SUFDOUQsQ0FBQztDQUNKO0FBRUQ7O0dBRUc7QUFDSCxNQUFNLFlBQVk7SUFDZCxZQUFvQixNQUFxQjtRQUFyQixXQUFNLEdBQU4sTUFBTSxDQUFlO0lBQUcsQ0FBQztJQUU3Qzs7T0FFRztJQUNILEtBQUssQ0FBQyxXQUFXLENBQUMsVUFBNkIsRUFBRTtRQUM3QyxNQUFNLE1BQU0sR0FBRyxNQUFNLElBQUksQ0FBQyxNQUFNLENBQUMsT0FBTyxDQUFDO1lBQ3JDLE1BQU0sRUFBRSxLQUFLO1lBQ2IsSUFBSSxFQUFFLGNBQWM7WUFDcEIsTUFBTSxFQUFFLGdCQUFnQixDQUFDLE9BQU8sQ0FBQztTQUNwQyxDQUFDLENBQUM7UUFFSCxPQUFPLElBQUEsK0JBQXNCLEVBQUMsTUFBTSxFQUFFLCtCQUFzQixDQUFDLENBQUM7SUFDbEUsQ0FBQztJQUVEOzs7O09BSUc7SUFDSCxLQUFLLENBQUMsVUFBVSxDQUNaLFNBQWlCLEVBQ2pCLFVBQTZCLEVBQUU7UUFFL0IsTUFBTSxNQUFNLEdBQUcsTUFBTSxJQUFJLENBQUMsTUFBTSxDQUFDLE9BQU8sQ0FBQztZQUNyQyxNQUFNLEVBQUUsS0FBSztZQUNiLElBQUksRUFBRSxnQkFBZ0Isa0JBQWtCLENBQUMsU0FBUyxDQUFDLFVBQVU7WUFDN0QsTUFBTSxFQUFFLGdCQUFnQixDQUFDLE9BQU8sQ0FBQztTQUNwQyxDQUFDLENBQUM7UUFFSCxPQUFPLElBQUEsK0JBQXNCLEVBQUMsTUFBTSxFQUFFLDZCQUFvQixDQUFDLENBQUM7SUFDaEUsQ0FBQztDQUNKIiwic291cmNlc0NvbnRlbnQiOlsiLyoqXG4gKiBMYXR0aWNlIFNESyBDbGllbnRcbiAqXG4gKiBNYWluIGNsaWVudCBmb3IgaW50ZXJhY3Rpbmcgd2l0aCB0aGUgTGF0dGljZSBSRVNUIEFQSS5cbiAqL1xuXG5pbXBvcnQge1xuICAgIENvbGxlY3Rpb24sXG4gICAgRG9jdW1lbnQsXG4gICAgU2NoZW1hLFxuICAgIFNjaGVtYUVsZW1lbnQsXG4gICAgRmllbGRDb25zdHJhaW50LFxuICAgIEluZGV4ZWRGaWVsZCxcbiAgICBTZWFyY2hSZXN1bHQsXG4gICAgSW5kZXhSZWJ1aWxkUmVzdWx0LFxuICAgIFNlYXJjaFF1ZXJ5LFxuICAgIEluZGV4VGFibGVNYXBwaW5nLFxuICAgIEluZGV4VGFibGVFbnRyeSxcbiAgICBFbnVtZXJhdGlvblJlc3VsdCxcbiAgICBQYWdpbmF0aW9uT3B0aW9ucyxcbiAgICBTY2hlbWFFbmZvcmNlbWVudE1vZGUsXG4gICAgSW5kZXhpbmdNb2RlLFxuICAgIENyZWF0ZUNvbGxlY3Rpb25PcHRpb25zLFxuICAgIEluZ2VzdERvY3VtZW50T3B0aW9ucyxcbiAgICBCYXRjaEluZ2VzdERvY3VtZW50RW50cnksXG4gICAgcGFyc2VDb2xsZWN0aW9uLFxuICAgIHBhcnNlRG9jdW1lbnQsXG4gICAgcGFyc2VTY2hlbWEsXG4gICAgcGFyc2VTY2hlbWFFbGVtZW50LFxuICAgIHBhcnNlRmllbGRDb25zdHJhaW50LFxuICAgIHBhcnNlSW5kZXhlZEZpZWxkLFxuICAgIHBhcnNlU2VhcmNoUmVzdWx0LFxuICAgIHBhcnNlSW5kZXhSZWJ1aWxkUmVzdWx0LFxuICAgIHBhcnNlSW5kZXhUYWJsZU1hcHBpbmcsXG4gICAgcGFyc2VJbmRleFRhYmxlRW50cnksXG4gICAgcGFyc2VFbnVtZXJhdGlvblJlc3VsdCxcbiAgICBmaWVsZENvbnN0cmFpbnRUb1JlcXVlc3QsXG4gICAgc2VhcmNoUXVlcnlUb1JlcXVlc3Rcbn0gZnJvbSBcIi4vbW9kZWxzXCI7XG5pbXBvcnQgeyBMYXR0aWNlQ29ubmVjdGlvbkVycm9yLCBMYXR0aWNlQXBpRXJyb3IgfSBmcm9tIFwiLi9leGNlcHRpb25zXCI7XG5cbi8qKlxuICogSFRUUCByZXF1ZXN0IG9wdGlvbnMuXG4gKi9cbmludGVyZmFjZSBSZXF1ZXN0T3B0aW9ucyB7XG4gICAgbWV0aG9kOiBzdHJpbmc7XG4gICAgcGF0aDogc3RyaW5nO1xuICAgIGRhdGE/OiBhbnk7XG4gICAgcGFyYW1zPzogUmVjb3JkPHN0cmluZywgc3RyaW5nPjtcbn1cblxuLyoqXG4gKiBCdWlsZCBxdWVyeSBwYXJhbXMgZm9yIHRoZSBvcHRpb25hbCBgbWF4UmVzdWx0c2AgLyBgc2tpcGAgcGFnaW5hdGlvbiB2YWx1ZXMsXG4gKiBtZXJnaW5nIGludG8gYW4gb3B0aW9uYWwgYmFzZSBzZXQgb2YgcGFyYW1zLlxuICovXG5mdW5jdGlvbiBwYWdpbmF0aW9uUGFyYW1zKFxuICAgIG9wdGlvbnM/OiBQYWdpbmF0aW9uT3B0aW9ucyxcbiAgICBiYXNlPzogUmVjb3JkPHN0cmluZywgc3RyaW5nPlxuKTogUmVjb3JkPHN0cmluZywgc3RyaW5nPiB8IHVuZGVmaW5lZCB7XG4gICAgY29uc3QgcGFyYW1zOiBSZWNvcmQ8c3RyaW5nLCBzdHJpbmc+ID0geyAuLi4oYmFzZSA/PyB7fSkgfTtcbiAgICBpZiAob3B0aW9ucz8ubWF4UmVzdWx0cyAhPT0gdW5kZWZpbmVkKSBwYXJhbXMubWF4UmVzdWx0cyA9IFN0cmluZyhvcHRpb25zLm1heFJlc3VsdHMpO1xuICAgIGlmIChvcHRpb25zPy5za2lwICE9PSB1bmRlZmluZWQpIHBhcmFtcy5za2lwID0gU3RyaW5nKG9wdGlvbnMuc2tpcCk7XG4gICAgcmV0dXJuIE9iamVjdC5rZXlzKHBhcmFtcykubGVuZ3RoID4gMCA/IHBhcmFtcyA6IHVuZGVmaW5lZDtcbn1cblxuLyoqXG4gKiBDbGllbnQgZm9yIGludGVyYWN0aW5nIHdpdGggdGhlIExhdHRpY2UgUkVTVCBBUEkuXG4gKi9cbmV4cG9ydCBjbGFzcyBMYXR0aWNlQ2xpZW50IHtcbiAgICBwcml2YXRlIGJhc2VVcmw6IHN0cmluZztcbiAgICBwcml2YXRlIHRpbWVvdXQ6IG51bWJlcjtcblxuICAgIHB1YmxpYyBjb2xsZWN0aW9uOiBDb2xsZWN0aW9uTWV0aG9kcztcbiAgICBwdWJsaWMgZG9jdW1lbnQ6IERvY3VtZW50TWV0aG9kcztcbiAgICBwdWJsaWMgc2VhcmNoOiBTZWFyY2hNZXRob2RzO1xuICAgIHB1YmxpYyBzY2hlbWE6IFNjaGVtYU1ldGhvZHM7XG4gICAgcHVibGljIGluZGV4OiBJbmRleE1ldGhvZHM7XG5cbiAgICAvKipcbiAgICAgKiBJbml0aWFsaXplIHRoZSBMYXR0aWNlIGNsaWVudC5cbiAgICAgKlxuICAgICAqIEBwYXJhbSBiYXNlVXJsIC0gVGhlIGJhc2UgVVJMIG9mIHRoZSBMYXR0aWNlIHNlcnZlciAoZS5nLiwgXCJodHRwOi8vbG9jYWxob3N0OjgwMDBcIilcbiAgICAgKiBAcGFyYW0gdGltZW91dCAtIFJlcXVlc3QgdGltZW91dCBpbiBtaWxsaXNlY29uZHMgKGRlZmF1bHQ6IDMwMDAwKVxuICAgICAqL1xuICAgIGNvbnN0cnVjdG9yKGJhc2VVcmw6IHN0cmluZywgdGltZW91dDogbnVtYmVyID0gMzAwMDApIHtcbiAgICAgICAgdGhpcy5iYXNlVXJsID0gYmFzZVVybC5yZXBsYWNlKC9cXC8rJC8sIFwiXCIpO1xuICAgICAgICB0aGlzLnRpbWVvdXQgPSB0aW1lb3V0O1xuXG4gICAgICAgIHRoaXMuY29sbGVjdGlvbiA9IG5ldyBDb2xsZWN0aW9uTWV0aG9kcyh0aGlzKTtcbiAgICAgICAgdGhpcy5kb2N1bWVudCA9IG5ldyBEb2N1bWVudE1ldGhvZHModGhpcyk7XG4gICAgICAgIHRoaXMuc2VhcmNoID0gbmV3IFNlYXJjaE1ldGhvZHModGhpcyk7XG4gICAgICAgIHRoaXMuc2NoZW1hID0gbmV3IFNjaGVtYU1ldGhvZHModGhpcyk7XG4gICAgICAgIHRoaXMuaW5kZXggPSBuZXcgSW5kZXhNZXRob2RzKHRoaXMpO1xuICAgIH1cblxuICAgIC8qKlxuICAgICAqIE1ha2UgYW4gSFRUUCByZXF1ZXN0IHRvIHRoZSBMYXR0aWNlIEFQSS5cbiAgICAgKlxuICAgICAqIE9uIHN1Y2Nlc3MgKEhUVFAgMnh4KSB0aGUgcGFyc2VkIHJlc3BvbnNlIGJvZHkgaXMgcmV0dXJuZWQgZGlyZWN0bHkgYXMgdGhlXG4gICAgICogcGF5bG9hZCAoYW4gZW1wdHkgYm9keSByZXNvbHZlcyB0byBgdW5kZWZpbmVkYCkuIE9uIGZhaWx1cmUgKG5vbi0yeHgpIGFcbiAgICAgKiB7QGxpbmsgTGF0dGljZUFwaUVycm9yfSBpcyB0aHJvd24sIGNhcnJ5aW5nIHRoZSBzZXJ2ZXIncyBgZXJyb3JgIG1lc3NhZ2UsXG4gICAgICogdGhlIEhUVFAgc3RhdHVzIGNvZGUsIGFueSBzdHJ1Y3R1cmVkIGBkZXRhaWxgLCBhbmQgdGhlIHJlcXVlc3QgaWQgZnJvbSB0aGVcbiAgICAgKiBgWC1MYXR0aWNlLVJlcXVlc3QtSWRgIHJlc3BvbnNlIGhlYWRlci5cbiAgICAgKi9cbiAgICBhc3luYyByZXF1ZXN0PFQgPSBhbnk+KG9wdGlvbnM6IFJlcXVlc3RPcHRpb25zKTogUHJvbWlzZTxUPiB7XG4gICAgICAgIGxldCB1cmwgPSBgJHt0aGlzLmJhc2VVcmx9JHtvcHRpb25zLnBhdGh9YDtcblxuICAgICAgICBpZiAob3B0aW9ucy5wYXJhbXMpIHtcbiAgICAgICAgICAgIGNvbnN0IHNlYXJjaFBhcmFtcyA9IG5ldyBVUkxTZWFyY2hQYXJhbXMob3B0aW9ucy5wYXJhbXMpO1xuICAgICAgICAgICAgdXJsICs9IGA/JHtzZWFyY2hQYXJhbXMudG9TdHJpbmcoKX1gO1xuICAgICAgICB9XG5cbiAgICAgICAgY29uc3QgZmV0Y2hPcHRpb25zOiBSZXF1ZXN0SW5pdCA9IHtcbiAgICAgICAgICAgIG1ldGhvZDogb3B0aW9ucy5tZXRob2QsXG4gICAgICAgICAgICBoZWFkZXJzOiB7XG4gICAgICAgICAgICAgICAgXCJDb250ZW50LVR5cGVcIjogXCJhcHBsaWNhdGlvbi9qc29uXCJcbiAgICAgICAgICAgIH1cbiAgICAgICAgfTtcblxuICAgICAgICBpZiAob3B0aW9ucy5kYXRhICYmIChvcHRpb25zLm1ldGhvZCA9PT0gXCJQT1NUXCIgfHwgb3B0aW9ucy5tZXRob2QgPT09IFwiUFVUXCIpKSB7XG4gICAgICAgICAgICBmZXRjaE9wdGlvbnMuYm9keSA9IEpTT04uc3RyaW5naWZ5KG9wdGlvbnMuZGF0YSk7XG4gICAgICAgIH1cblxuICAgICAgICBjb25zdCBkb0ZldGNoID0gYXN5bmMgKCkgPT4ge1xuICAgICAgICAgICAgY29uc3QgY29udHJvbGxlciA9IG5ldyBBYm9ydENvbnRyb2xsZXIoKTtcbiAgICAgICAgICAgIGNvbnN0IHRpbWVvdXRJZCA9IHNldFRpbWVvdXQoKCkgPT4gY29udHJvbGxlci5hYm9ydCgpLCB0aGlzLnRpbWVvdXQpO1xuICAgICAgICAgICAgZmV0Y2hPcHRpb25zLnNpZ25hbCA9IGNvbnRyb2xsZXIuc2lnbmFsO1xuICAgICAgICAgICAgdHJ5IHtcbiAgICAgICAgICAgICAgICByZXR1cm4gYXdhaXQgZmV0Y2godXJsLCBmZXRjaE9wdGlvbnMpO1xuICAgICAgICAgICAgfSBmaW5hbGx5IHtcbiAgICAgICAgICAgICAgICBjbGVhclRpbWVvdXQodGltZW91dElkKTtcbiAgICAgICAgICAgIH1cbiAgICAgICAgfTtcblxuICAgICAgICBsZXQgcmVzcG9uc2U6IEF3YWl0ZWQ8UmV0dXJuVHlwZTx0eXBlb2YgZmV0Y2g+PjtcbiAgICAgICAgdHJ5IHtcbiAgICAgICAgICAgIHJlc3BvbnNlID0gYXdhaXQgZG9GZXRjaCgpO1xuICAgICAgICB9IGNhdGNoIChlcnJvcjogYW55KSB7XG4gICAgICAgICAgICBpZiAoZXJyb3IubmFtZSA9PT0gXCJBYm9ydEVycm9yXCIpIHtcbiAgICAgICAgICAgICAgICB0aHJvdyBuZXcgTGF0dGljZUNvbm5lY3Rpb25FcnJvcihgUmVxdWVzdCB0byAke3VybH0gdGltZWQgb3V0YCk7XG4gICAgICAgICAgICB9XG4gICAgICAgICAgICB0aHJvdyBuZXcgTGF0dGljZUNvbm5lY3Rpb25FcnJvcihgRmFpbGVkIHRvIGNvbm5lY3QgdG8gJHt1cmx9YCwgZXJyb3IpO1xuICAgICAgICB9XG5cbiAgICAgICAgY29uc3QgcmVxdWVzdElkID0gcmVzcG9uc2UuaGVhZGVycy5nZXQoXCJYLUxhdHRpY2UtUmVxdWVzdC1JZFwiKSA/PyB1bmRlZmluZWQ7XG5cbiAgICAgICAgLy8gSEVBRCByZXNwb25zZXMgaGF2ZSBubyBib2R5OyByZWFkIHRoZSB0ZXh0IGZvciBldmVyeXRoaW5nIGVsc2UuXG4gICAgICAgIGNvbnN0IHJlc3BvbnNlVGV4dCA9IG9wdGlvbnMubWV0aG9kID09PSBcIkhFQURcIiA/IFwiXCIgOiBhd2FpdCByZXNwb25zZS50ZXh0KCk7XG5cbiAgICAgICAgLy8gUGFyc2UgdGhlIGJvZHkgb25jZSAobWF5IGJlIGVtcHR5LCBKU09OLCBvciBwbGFpbiB0ZXh0KS5cbiAgICAgICAgbGV0IHBhcnNlZEJvZHk6IGFueSA9IHVuZGVmaW5lZDtcbiAgICAgICAgaWYgKHJlc3BvbnNlVGV4dCkge1xuICAgICAgICAgICAgdHJ5IHtcbiAgICAgICAgICAgICAgICBwYXJzZWRCb2R5ID0gSlNPTi5wYXJzZShyZXNwb25zZVRleHQpO1xuICAgICAgICAgICAgfSBjYXRjaCB7XG4gICAgICAgICAgICAgICAgcGFyc2VkQm9keSA9IHJlc3BvbnNlVGV4dDtcbiAgICAgICAgICAgIH1cbiAgICAgICAgfVxuXG4gICAgICAgIGlmICghcmVzcG9uc2Uub2spIHtcbiAgICAgICAgICAgIC8vIEVycm9yIGNvbnRyYWN0OiBib2R5IGlzIGB7IGVycm9yLCBkZXRhaWw/IH1gLiBGYWxsIGJhY2sgdG8gdGhlXG4gICAgICAgICAgICAvLyBzdGF0dXMgdGV4dCB3aGVuIHRoZSBib2R5IGlzbid0IHRoZSBleHBlY3RlZCBKU09OIHNoYXBlLlxuICAgICAgICAgICAgbGV0IG1lc3NhZ2U6IHN0cmluZztcbiAgICAgICAgICAgIGxldCBkZXRhaWw6IGFueSA9IHVuZGVmaW5lZDtcblxuICAgICAgICAgICAgaWYgKHBhcnNlZEJvZHkgJiYgdHlwZW9mIHBhcnNlZEJvZHkgPT09IFwib2JqZWN0XCIgJiYgdHlwZW9mIHBhcnNlZEJvZHkuZXJyb3IgPT09IFwic3RyaW5nXCIpIHtcbiAgICAgICAgICAgICAgICBtZXNzYWdlID0gcGFyc2VkQm9keS5lcnJvcjtcbiAgICAgICAgICAgICAgICBkZXRhaWwgPSBwYXJzZWRCb2R5LmRldGFpbDtcbiAgICAgICAgICAgIH0gZWxzZSBpZiAodHlwZW9mIHBhcnNlZEJvZHkgPT09IFwic3RyaW5nXCIgJiYgcGFyc2VkQm9keS5sZW5ndGggPiAwKSB7XG4gICAgICAgICAgICAgICAgbWVzc2FnZSA9IHBhcnNlZEJvZHk7XG4gICAgICAgICAgICB9IGVsc2Uge1xuICAgICAgICAgICAgICAgIG1lc3NhZ2UgPSByZXNwb25zZS5zdGF0dXNUZXh0IHx8IGBIVFRQICR7cmVzcG9uc2Uuc3RhdHVzfWA7XG4gICAgICAgICAgICB9XG5cbiAgICAgICAgICAgIHRocm93IG5ldyBMYXR0aWNlQXBpRXJyb3IobWVzc2FnZSwgcmVzcG9uc2Uuc3RhdHVzLCBkZXRhaWwsIHJlcXVlc3RJZCk7XG4gICAgICAgIH1cblxuICAgICAgICAvLyBTdWNjZXNzOiB0aGUgYm9keSBJUyB0aGUgcGF5bG9hZC4gRW1wdHkgYm9keSByZXNvbHZlcyB0byB1bmRlZmluZWQuXG4gICAgICAgIHJldHVybiBwYXJzZWRCb2R5IGFzIFQ7XG4gICAgfVxuXG4gICAgLyoqXG4gICAgICogSXNzdWUgYSBIRUFEIHJlcXVlc3QgYW5kIHJlcG9ydCB3aGV0aGVyIHRoZSByZXNvdXJjZSBleGlzdHMgKDJ4eCkuXG4gICAgICogTm9uLTJ4eCByZXNwb25zZXMgKGUuZy4gNDA0KSByZXNvbHZlIHRvIGBmYWxzZWAgcmF0aGVyIHRoYW4gdGhyb3dpbmcuXG4gICAgICovXG4gICAgYXN5bmMgaGVhZChwYXRoOiBzdHJpbmcpOiBQcm9taXNlPGJvb2xlYW4+IHtcbiAgICAgICAgdHJ5IHtcbiAgICAgICAgICAgIGF3YWl0IHRoaXMucmVxdWVzdCh7IG1ldGhvZDogXCJIRUFEXCIsIHBhdGggfSk7XG4gICAgICAgICAgICByZXR1cm4gdHJ1ZTtcbiAgICAgICAgfSBjYXRjaCAoZXJyb3IpIHtcbiAgICAgICAgICAgIGlmIChlcnJvciBpbnN0YW5jZW9mIExhdHRpY2VBcGlFcnJvcikge1xuICAgICAgICAgICAgICAgIHJldHVybiBmYWxzZTtcbiAgICAgICAgICAgIH1cbiAgICAgICAgICAgIHRocm93IGVycm9yO1xuICAgICAgICB9XG4gICAgfVxuXG4gICAgLyoqXG4gICAgICogQ2hlY2sgaWYgdGhlIExhdHRpY2Ugc2VydmVyIGlzIGhlYWx0aHkuXG4gICAgICovXG4gICAgYXN5bmMgaGVhbHRoQ2hlY2soKTogUHJvbWlzZTxib29sZWFuPiB7XG4gICAgICAgIHRyeSB7XG4gICAgICAgICAgICBhd2FpdCB0aGlzLnJlcXVlc3QoeyBtZXRob2Q6IFwiR0VUXCIsIHBhdGg6IFwiL3YxLjAvaGVhbHRoXCIgfSk7XG4gICAgICAgICAgICByZXR1cm4gdHJ1ZTtcbiAgICAgICAgfSBjYXRjaCB7XG4gICAgICAgICAgICByZXR1cm4gZmFsc2U7XG4gICAgICAgIH1cbiAgICB9XG59XG5cbi8qKlxuICogTWV0aG9kcyBmb3IgbWFuYWdpbmcgY29sbGVjdGlvbnMuXG4gKi9cbmNsYXNzIENvbGxlY3Rpb25NZXRob2RzIHtcbiAgICBjb25zdHJ1Y3Rvcihwcml2YXRlIGNsaWVudDogTGF0dGljZUNsaWVudCkge31cblxuICAgIC8qKlxuICAgICAqIENyZWF0ZSBhIG5ldyBjb2xsZWN0aW9uLlxuICAgICAqL1xuICAgIGFzeW5jIGNyZWF0ZShvcHRpb25zOiBDcmVhdGVDb2xsZWN0aW9uT3B0aW9ucyk6IFByb21pc2U8Q29sbGVjdGlvbiB8IG51bGw+IHtcbiAgICAgICAgY29uc3QgZGF0YTogYW55ID0geyBuYW1lOiBvcHRpb25zLm5hbWUgfTtcblxuICAgICAgICBpZiAob3B0aW9ucy5kZXNjcmlwdGlvbikgZGF0YS5kZXNjcmlwdGlvbiA9IG9wdGlvbnMuZGVzY3JpcHRpb247XG4gICAgICAgIGlmIChvcHRpb25zLmRvY3VtZW50c0RpcmVjdG9yeSkgZGF0YS5kb2N1bWVudHNEaXJlY3RvcnkgPSBvcHRpb25zLmRvY3VtZW50c0RpcmVjdG9yeTtcbiAgICAgICAgaWYgKG9wdGlvbnMubGFiZWxzKSBkYXRhLmxhYmVscyA9IG9wdGlvbnMubGFiZWxzO1xuICAgICAgICBpZiAob3B0aW9ucy50YWdzKSBkYXRhLnRhZ3MgPSBvcHRpb25zLnRhZ3M7XG4gICAgICAgIGlmIChvcHRpb25zLnNjaGVtYUVuZm9yY2VtZW50TW9kZSAhPT0gdW5kZWZpbmVkICYmIG9wdGlvbnMuc2NoZW1hRW5mb3JjZW1lbnRNb2RlICE9PSBTY2hlbWFFbmZvcmNlbWVudE1vZGUuTm9uZSkge1xuICAgICAgICAgICAgZGF0YS5zY2hlbWFFbmZvcmNlbWVudE1vZGUgPSBvcHRpb25zLnNjaGVtYUVuZm9yY2VtZW50TW9kZTtcbiAgICAgICAgfVxuICAgICAgICBpZiAob3B0aW9ucy5maWVsZENvbnN0cmFpbnRzKSB7XG4gICAgICAgICAgICBkYXRhLmZpZWxkQ29uc3RyYWludHMgPSBvcHRpb25zLmZpZWxkQ29uc3RyYWludHMubWFwKGZpZWxkQ29uc3RyYWludFRvUmVxdWVzdCk7XG4gICAgICAgIH1cbiAgICAgICAgaWYgKG9wdGlvbnMuaW5kZXhpbmdNb2RlICE9PSB1bmRlZmluZWQgJiYgb3B0aW9ucy5pbmRleGluZ01vZGUgIT09IEluZGV4aW5nTW9kZS5BbGwpIHtcbiAgICAgICAgICAgIGRhdGEuaW5kZXhpbmdNb2RlID0gb3B0aW9ucy5pbmRleGluZ01vZGU7XG4gICAgICAgIH1cbiAgICAgICAgaWYgKG9wdGlvbnMuaW5kZXhlZEZpZWxkcykgZGF0YS5pbmRleGVkRmllbGRzID0gb3B0aW9ucy5pbmRleGVkRmllbGRzO1xuXG4gICAgICAgIGNvbnN0IHJlc3VsdCA9IGF3YWl0IHRoaXMuY2xpZW50LnJlcXVlc3Qoe1xuICAgICAgICAgICAgbWV0aG9kOiBcIlBVVFwiLFxuICAgICAgICAgICAgcGF0aDogXCIvdjEuMC9jb2xsZWN0aW9uc1wiLFxuICAgICAgICAgICAgZGF0YVxuICAgICAgICB9KTtcblxuICAgICAgICByZXR1cm4gcmVzdWx0ID8gcGFyc2VDb2xsZWN0aW9uKHJlc3VsdCkgOiBudWxsO1xuICAgIH1cblxuICAgIC8qKlxuICAgICAqIEdldCBhbGwgY29sbGVjdGlvbnMuXG4gICAgICovXG4gICAgYXN5bmMgcmVhZEFsbChvcHRpb25zOiBQYWdpbmF0aW9uT3B0aW9ucyA9IHt9KTogUHJvbWlzZTxFbnVtZXJhdGlvblJlc3VsdDxDb2xsZWN0aW9uPj4ge1xuICAgICAgICBjb25zdCByZXN1bHQgPSBhd2FpdCB0aGlzLmNsaWVudC5yZXF1ZXN0KHtcbiAgICAgICAgICAgIG1ldGhvZDogXCJHRVRcIixcbiAgICAgICAgICAgIHBhdGg6IFwiL3YxLjAvY29sbGVjdGlvbnNcIixcbiAgICAgICAgICAgIHBhcmFtczogcGFnaW5hdGlvblBhcmFtcyhvcHRpb25zKVxuICAgICAgICB9KTtcblxuICAgICAgICByZXR1cm4gcGFyc2VFbnVtZXJhdGlvblJlc3VsdChyZXN1bHQsIHBhcnNlQ29sbGVjdGlvbik7XG4gICAgfVxuXG4gICAgLyoqXG4gICAgICogR2V0IGEgY29sbGVjdGlvbiBieSBJRC5cbiAgICAgKi9cbiAgICBhc3luYyByZWFkQnlJZChjb2xsZWN0aW9uSWQ6IHN0cmluZyk6IFByb21pc2U8Q29sbGVjdGlvbiB8IG51bGw+IHtcbiAgICAgICAgY29uc3QgcmVzdWx0ID0gYXdhaXQgdGhpcy5jbGllbnQucmVxdWVzdCh7XG4gICAgICAgICAgICBtZXRob2Q6IFwiR0VUXCIsXG4gICAgICAgICAgICBwYXRoOiBgL3YxLjAvY29sbGVjdGlvbnMvJHtjb2xsZWN0aW9uSWR9YFxuICAgICAgICB9KTtcblxuICAgICAgICByZXR1cm4gcmVzdWx0ID8gcGFyc2VDb2xsZWN0aW9uKHJlc3VsdCkgOiBudWxsO1xuICAgIH1cblxuICAgIC8qKlxuICAgICAqIENoZWNrIGlmIGEgY29sbGVjdGlvbiBleGlzdHMuXG4gICAgICovXG4gICAgYXN5bmMgZXhpc3RzKGNvbGxlY3Rpb25JZDogc3RyaW5nKTogUHJvbWlzZTxib29sZWFuPiB7XG4gICAgICAgIHJldHVybiB0aGlzLmNsaWVudC5oZWFkKGAvdjEuMC9jb2xsZWN0aW9ucy8ke2NvbGxlY3Rpb25JZH1gKTtcbiAgICB9XG5cbiAgICAvKipcbiAgICAgKiBEZWxldGUgYSBjb2xsZWN0aW9uLlxuICAgICAqL1xuICAgIGFzeW5jIGRlbGV0ZShjb2xsZWN0aW9uSWQ6IHN0cmluZyk6IFByb21pc2U8Ym9vbGVhbj4ge1xuICAgICAgICB0cnkge1xuICAgICAgICAgICAgYXdhaXQgdGhpcy5jbGllbnQucmVxdWVzdCh7XG4gICAgICAgICAgICAgICAgbWV0aG9kOiBcIkRFTEVURVwiLFxuICAgICAgICAgICAgICAgIHBhdGg6IGAvdjEuMC9jb2xsZWN0aW9ucy8ke2NvbGxlY3Rpb25JZH1gXG4gICAgICAgICAgICB9KTtcbiAgICAgICAgICAgIHJldHVybiB0cnVlO1xuICAgICAgICB9IGNhdGNoIChlcnJvcikge1xuICAgICAgICAgICAgaWYgKGVycm9yIGluc3RhbmNlb2YgTGF0dGljZUFwaUVycm9yKSB7XG4gICAgICAgICAgICAgICAgcmV0dXJuIGZhbHNlO1xuICAgICAgICAgICAgfVxuICAgICAgICAgICAgdGhyb3cgZXJyb3I7XG4gICAgICAgIH1cbiAgICB9XG5cbiAgICAvKipcbiAgICAgKiBHZXQgZmllbGQgY29uc3RyYWludHMgZm9yIGEgY29sbGVjdGlvbi5cbiAgICAgKi9cbiAgICBhc3luYyBnZXRDb25zdHJhaW50cyhjb2xsZWN0aW9uSWQ6IHN0cmluZyk6IFByb21pc2U8RmllbGRDb25zdHJhaW50W10+IHtcbiAgICAgICAgY29uc3QgcmVzdWx0ID0gYXdhaXQgdGhpcy5jbGllbnQucmVxdWVzdCh7XG4gICAgICAgICAgICBtZXRob2Q6IFwiR0VUXCIsXG4gICAgICAgICAgICBwYXRoOiBgL3YxLjAvY29sbGVjdGlvbnMvJHtjb2xsZWN0aW9uSWR9L2NvbnN0cmFpbnRzYFxuICAgICAgICB9KTtcblxuICAgICAgICBpZiAocmVzdWx0ICYmIHJlc3VsdC5maWVsZENvbnN0cmFpbnRzKSB7XG4gICAgICAgICAgICByZXR1cm4gcmVzdWx0LmZpZWxkQ29uc3RyYWludHMubWFwKChjOiBhbnkpID0+IHBhcnNlRmllbGRDb25zdHJhaW50KGMpKS5maWx0ZXIoKGM6IGFueSkgPT4gYyAhPT0gbnVsbCk7XG4gICAgICAgIH1cbiAgICAgICAgcmV0dXJuIFtdO1xuICAgIH1cblxuICAgIC8qKlxuICAgICAqIFVwZGF0ZSBjb25zdHJhaW50cyBmb3IgYSBjb2xsZWN0aW9uLlxuICAgICAqL1xuICAgIGFzeW5jIHVwZGF0ZUNvbnN0cmFpbnRzKFxuICAgICAgICBjb2xsZWN0aW9uSWQ6IHN0cmluZyxcbiAgICAgICAgc2NoZW1hRW5mb3JjZW1lbnRNb2RlOiBTY2hlbWFFbmZvcmNlbWVudE1vZGUsXG4gICAgICAgIGZpZWxkQ29uc3RyYWludHM/OiBGaWVsZENvbnN0cmFpbnRbXVxuICAgICk6IFByb21pc2U8Ym9vbGVhbj4ge1xuICAgICAgICBjb25zdCBkYXRhOiBhbnkgPSB7IHNjaGVtYUVuZm9yY2VtZW50TW9kZSB9O1xuICAgICAgICBpZiAoZmllbGRDb25zdHJhaW50cykge1xuICAgICAgICAgICAgZGF0YS5maWVsZENvbnN0cmFpbnRzID0gZmllbGRDb25zdHJhaW50cy5tYXAoZmllbGRDb25zdHJhaW50VG9SZXF1ZXN0KTtcbiAgICAgICAgfVxuXG4gICAgICAgIHRyeSB7XG4gICAgICAgICAgICBhd2FpdCB0aGlzLmNsaWVudC5yZXF1ZXN0KHtcbiAgICAgICAgICAgICAgICBtZXRob2Q6IFwiUFVUXCIsXG4gICAgICAgICAgICAgICAgcGF0aDogYC92MS4wL2NvbGxlY3Rpb25zLyR7Y29sbGVjdGlvbklkfS9jb25zdHJhaW50c2AsXG4gICAgICAgICAgICAgICAgZGF0YVxuICAgICAgICAgICAgfSk7XG4gICAgICAgICAgICByZXR1cm4gdHJ1ZTtcbiAgICAgICAgfSBjYXRjaCAoZXJyb3IpIHtcbiAgICAgICAgICAgIGlmIChlcnJvciBpbnN0YW5jZW9mIExhdHRpY2VBcGlFcnJvcikge1xuICAgICAgICAgICAgICAgIHJldHVybiBmYWxzZTtcbiAgICAgICAgICAgIH1cbiAgICAgICAgICAgIHRocm93IGVycm9yO1xuICAgICAgICB9XG4gICAgfVxuXG4gICAgLyoqXG4gICAgICogR2V0IGluZGV4ZWQgZmllbGRzIGZvciBhIGNvbGxlY3Rpb24uXG4gICAgICovXG4gICAgYXN5bmMgZ2V0SW5kZXhlZEZpZWxkcyhjb2xsZWN0aW9uSWQ6IHN0cmluZyk6IFByb21pc2U8SW5kZXhlZEZpZWxkW10+IHtcbiAgICAgICAgY29uc3QgcmVzdWx0ID0gYXdhaXQgdGhpcy5jbGllbnQucmVxdWVzdCh7XG4gICAgICAgICAgICBtZXRob2Q6IFwiR0VUXCIsXG4gICAgICAgICAgICBwYXRoOiBgL3YxLjAvY29sbGVjdGlvbnMvJHtjb2xsZWN0aW9uSWR9L2luZGV4aW5nYFxuICAgICAgICB9KTtcblxuICAgICAgICBpZiAocmVzdWx0ICYmIHJlc3VsdC5pbmRleGVkRmllbGRzKSB7XG4gICAgICAgICAgICByZXR1cm4gcmVzdWx0LmluZGV4ZWRGaWVsZHMubWFwKChmOiBhbnkpID0+IHBhcnNlSW5kZXhlZEZpZWxkKGYpKS5maWx0ZXIoKGY6IGFueSkgPT4gZiAhPT0gbnVsbCk7XG4gICAgICAgIH1cbiAgICAgICAgcmV0dXJuIFtdO1xuICAgIH1cblxuICAgIC8qKlxuICAgICAqIFVwZGF0ZSBpbmRleGluZyBjb25maWd1cmF0aW9uIGZvciBhIGNvbGxlY3Rpb24uXG4gICAgICovXG4gICAgYXN5bmMgdXBkYXRlSW5kZXhpbmcoXG4gICAgICAgIGNvbGxlY3Rpb25JZDogc3RyaW5nLFxuICAgICAgICBpbmRleGluZ01vZGU6IEluZGV4aW5nTW9kZSxcbiAgICAgICAgaW5kZXhlZEZpZWxkcz86IHN0cmluZ1tdLFxuICAgICAgICByZWJ1aWxkSW5kZXhlczogYm9vbGVhbiA9IGZhbHNlXG4gICAgKTogUHJvbWlzZTxib29sZWFuPiB7XG4gICAgICAgIGNvbnN0IGRhdGE6IGFueSA9IHtcbiAgICAgICAgICAgIGluZGV4aW5nTW9kZSxcbiAgICAgICAgICAgIHJlYnVpbGRJbmRleGVzXG4gICAgICAgIH07XG4gICAgICAgIGlmIChpbmRleGVkRmllbGRzKSBkYXRhLmluZGV4ZWRGaWVsZHMgPSBpbmRleGVkRmllbGRzO1xuXG4gICAgICAgIHRyeSB7XG4gICAgICAgICAgICBhd2FpdCB0aGlzLmNsaWVudC5yZXF1ZXN0KHtcbiAgICAgICAgICAgICAgICBtZXRob2Q6IFwiUFVUXCIsXG4gICAgICAgICAgICAgICAgcGF0aDogYC92MS4wL2NvbGxlY3Rpb25zLyR7Y29sbGVjdGlvbklkfS9pbmRleGluZ2AsXG4gICAgICAgICAgICAgICAgZGF0YVxuICAgICAgICAgICAgfSk7XG4gICAgICAgICAgICByZXR1cm4gdHJ1ZTtcbiAgICAgICAgfSBjYXRjaCAoZXJyb3IpIHtcbiAgICAgICAgICAgIGlmIChlcnJvciBpbnN0YW5jZW9mIExhdHRpY2VBcGlFcnJvcikge1xuICAgICAgICAgICAgICAgIHJldHVybiBmYWxzZTtcbiAgICAgICAgICAgIH1cbiAgICAgICAgICAgIHRocm93IGVycm9yO1xuICAgICAgICB9XG4gICAgfVxuXG4gICAgLyoqXG4gICAgICogUmVidWlsZCBpbmRleGVzIGZvciBhIGNvbGxlY3Rpb24uXG4gICAgICovXG4gICAgYXN5bmMgcmVidWlsZEluZGV4ZXMoXG4gICAgICAgIGNvbGxlY3Rpb25JZDogc3RyaW5nLFxuICAgICAgICBkcm9wVW51c2VkSW5kZXhlczogYm9vbGVhbiA9IHRydWVcbiAgICApOiBQcm9taXNlPEluZGV4UmVidWlsZFJlc3VsdCB8IG51bGw+IHtcbiAgICAgICAgY29uc3QgcmVzdWx0ID0gYXdhaXQgdGhpcy5jbGllbnQucmVxdWVzdCh7XG4gICAgICAgICAgICBtZXRob2Q6IFwiUE9TVFwiLFxuICAgICAgICAgICAgcGF0aDogYC92MS4wL2NvbGxlY3Rpb25zLyR7Y29sbGVjdGlvbklkfS9pbmRleGVzL3JlYnVpbGRgLFxuICAgICAgICAgICAgZGF0YTogeyBkcm9wVW51c2VkSW5kZXhlcyB9XG4gICAgICAgIH0pO1xuXG4gICAgICAgIHJldHVybiByZXN1bHQgPyBwYXJzZUluZGV4UmVidWlsZFJlc3VsdChyZXN1bHQpIDogbnVsbDtcbiAgICB9XG59XG5cbi8qKlxuICogTWV0aG9kcyBmb3IgbWFuYWdpbmcgZG9jdW1lbnRzLlxuICovXG5jbGFzcyBEb2N1bWVudE1ldGhvZHMge1xuICAgIGNvbnN0cnVjdG9yKHByaXZhdGUgY2xpZW50OiBMYXR0aWNlQ2xpZW50KSB7fVxuXG4gICAgLyoqXG4gICAgICogSW5nZXN0IGEgbmV3IGRvY3VtZW50IGludG8gYSBjb2xsZWN0aW9uLlxuICAgICAqL1xuICAgIGFzeW5jIGluZ2VzdChvcHRpb25zOiBJbmdlc3REb2N1bWVudE9wdGlvbnMpOiBQcm9taXNlPERvY3VtZW50IHwgbnVsbD4ge1xuICAgICAgICBjb25zdCBkYXRhOiBhbnkgPSB7IGNvbnRlbnQ6IG9wdGlvbnMuY29udGVudCB9O1xuXG4gICAgICAgIGlmIChvcHRpb25zLm5hbWUpIGRhdGEubmFtZSA9IG9wdGlvbnMubmFtZTtcbiAgICAgICAgaWYgKG9wdGlvbnMubGFiZWxzKSBkYXRhLmxhYmVscyA9IG9wdGlvbnMubGFiZWxzO1xuICAgICAgICBpZiAob3B0aW9ucy50YWdzKSBkYXRhLnRhZ3MgPSBvcHRpb25zLnRhZ3M7XG5cbiAgICAgICAgY29uc3QgcmVzdWx0ID0gYXdhaXQgdGhpcy5jbGllbnQucmVxdWVzdCh7XG4gICAgICAgICAgICBtZXRob2Q6IFwiUFVUXCIsXG4gICAgICAgICAgICBwYXRoOiBgL3YxLjAvY29sbGVjdGlvbnMvJHtvcHRpb25zLmNvbGxlY3Rpb25JZH0vZG9jdW1lbnRzYCxcbiAgICAgICAgICAgIGRhdGFcbiAgICAgICAgfSk7XG5cbiAgICAgICAgcmV0dXJuIHJlc3VsdCA/IHBhcnNlRG9jdW1lbnQocmVzdWx0KSA6IG51bGw7XG4gICAgfVxuXG4gICAgLyoqXG4gICAgICogSW5nZXN0IG11bHRpcGxlIGRvY3VtZW50cyBpbnRvIGEgY29sbGVjdGlvbiBpbiBhIHNpbmdsZSBiYXRjaCBvcGVyYXRpb24uXG4gICAgICovXG4gICAgYXN5bmMgaW5nZXN0QmF0Y2goXG4gICAgICAgIGNvbGxlY3Rpb25JZDogc3RyaW5nLFxuICAgICAgICBkb2N1bWVudHM6IEJhdGNoSW5nZXN0RG9jdW1lbnRFbnRyeVtdXG4gICAgKTogUHJvbWlzZTxEb2N1bWVudFtdIHwgbnVsbD4ge1xuICAgICAgICBjb25zdCByZXN1bHQgPSBhd2FpdCB0aGlzLmNsaWVudC5yZXF1ZXN0KHtcbiAgICAgICAgICAgIG1ldGhvZDogXCJQVVRcIixcbiAgICAgICAgICAgIHBhdGg6IGAvdjEuMC9jb2xsZWN0aW9ucy8ke2NvbGxlY3Rpb25JZH0vZG9jdW1lbnRzL2JhdGNoYCxcbiAgICAgICAgICAgIGRhdGE6IHtcbiAgICAgICAgICAgICAgICBkb2N1bWVudHM6IGRvY3VtZW50cy5tYXAoZG9jID0+IHtcbiAgICAgICAgICAgICAgICAgICAgY29uc3QgZW50cnk6IGFueSA9IHsgY29udGVudDogZG9jLmNvbnRlbnQgfTtcbiAgICAgICAgICAgICAgICAgICAgaWYgKGRvYy5uYW1lKSBlbnRyeS5uYW1lID0gZG9jLm5hbWU7XG4gICAgICAgICAgICAgICAgICAgIGlmIChkb2MubGFiZWxzKSBlbnRyeS5sYWJlbHMgPSBkb2MubGFiZWxzO1xuICAgICAgICAgICAgICAgICAgICBpZiAoZG9jLnRhZ3MpIGVudHJ5LnRhZ3MgPSBkb2MudGFncztcbiAgICAgICAgICAgICAgICAgICAgcmV0dXJuIGVudHJ5O1xuICAgICAgICAgICAgICAgIH0pXG4gICAgICAgICAgICB9XG4gICAgICAgIH0pO1xuXG4gICAgICAgIGlmIChBcnJheS5pc0FycmF5KHJlc3VsdCkpIHtcbiAgICAgICAgICAgIHJldHVybiByZXN1bHQubWFwKChkOiBhbnkpID0+IHBhcnNlRG9jdW1lbnQoZCkpLmZpbHRlcigoZCk6IGQgaXMgRG9jdW1lbnQgPT4gZCAhPT0gbnVsbCk7XG4gICAgICAgIH1cbiAgICAgICAgcmV0dXJuIG51bGw7XG4gICAgfVxuXG4gICAgLyoqXG4gICAgICogR2V0IGFsbCBkb2N1bWVudHMgaW4gYSBjb2xsZWN0aW9uLlxuICAgICAqL1xuICAgIGFzeW5jIHJlYWRBbGxJbkNvbGxlY3Rpb24oXG4gICAgICAgIGNvbGxlY3Rpb25JZDogc3RyaW5nLFxuICAgICAgICBpbmNsdWRlQ29udGVudDogYm9vbGVhbiA9IGZhbHNlLFxuICAgICAgICBpbmNsdWRlTGFiZWxzOiBib29sZWFuID0gdHJ1ZSxcbiAgICAgICAgaW5jbHVkZVRhZ3M6IGJvb2xlYW4gPSB0cnVlLFxuICAgICAgICBvcHRpb25zOiBQYWdpbmF0aW9uT3B0aW9ucyA9IHt9XG4gICAgKTogUHJvbWlzZTxFbnVtZXJhdGlvblJlc3VsdDxEb2N1bWVudD4+IHtcbiAgICAgICAgY29uc3QgcmVzdWx0ID0gYXdhaXQgdGhpcy5jbGllbnQucmVxdWVzdCh7XG4gICAgICAgICAgICBtZXRob2Q6IFwiR0VUXCIsXG4gICAgICAgICAgICBwYXRoOiBgL3YxLjAvY29sbGVjdGlvbnMvJHtjb2xsZWN0aW9uSWR9L2RvY3VtZW50c2AsXG4gICAgICAgICAgICBwYXJhbXM6IHBhZ2luYXRpb25QYXJhbXMob3B0aW9ucywge1xuICAgICAgICAgICAgICAgIGluY2x1ZGVDb250ZW50OiBTdHJpbmcoaW5jbHVkZUNvbnRlbnQpLFxuICAgICAgICAgICAgICAgIGluY2x1ZGVMYWJlbHM6IFN0cmluZyhpbmNsdWRlTGFiZWxzKSxcbiAgICAgICAgICAgICAgICBpbmNsdWRlVGFnczogU3RyaW5nKGluY2x1ZGVUYWdzKVxuICAgICAgICAgICAgfSlcbiAgICAgICAgfSk7XG5cbiAgICAgICAgcmV0dXJuIHBhcnNlRW51bWVyYXRpb25SZXN1bHQocmVzdWx0LCBwYXJzZURvY3VtZW50KTtcbiAgICB9XG5cbiAgICAvKipcbiAgICAgKiBHZXQgYSBkb2N1bWVudCBieSBJRC5cbiAgICAgKi9cbiAgICBhc3luYyByZWFkQnlJZChcbiAgICAgICAgY29sbGVjdGlvbklkOiBzdHJpbmcsXG4gICAgICAgIGRvY3VtZW50SWQ6IHN0cmluZyxcbiAgICAgICAgaW5jbHVkZUNvbnRlbnQ6IGJvb2xlYW4gPSBmYWxzZSxcbiAgICAgICAgaW5jbHVkZUxhYmVsczogYm9vbGVhbiA9IHRydWUsXG4gICAgICAgIGluY2x1ZGVUYWdzOiBib29sZWFuID0gdHJ1ZVxuICAgICk6IFByb21pc2U8RG9jdW1lbnQgfCBudWxsPiB7XG4gICAgICAgIGlmIChpbmNsdWRlQ29udGVudCkge1xuICAgICAgICAgICAgLy8gV2hlbiBpbmNsdWRlQ29udGVudD10cnVlLCB0aGUgc2VydmVyIHJldHVybnMgT05MWSB0aGUgcmF3IGRvY3VtZW50IGJvZHksXG4gICAgICAgICAgICAvLyBub3QgdGhlIGRvY3VtZW50IG1ldGFkYXRhLiBXZSBtYWtlIHR3byByZXF1ZXN0czpcbiAgICAgICAgICAgIC8vIDEuIEdldCBkb2N1bWVudCBtZXRhZGF0YSAod2l0aG91dCBjb250ZW50KVxuICAgICAgICAgICAgLy8gMi4gR2V0IHJhdyBjb250ZW50IHNlcGFyYXRlbHlcbiAgICAgICAgICAgIC8vIFRoZW4gY29tYmluZSB0aGVtLlxuXG4gICAgICAgICAgICAvLyBGaXJzdCwgZ2V0IGRvY3VtZW50IG1ldGFkYXRhXG4gICAgICAgICAgICBjb25zdCBtZXRhZGF0YSA9IGF3YWl0IHRoaXMuY2xpZW50LnJlcXVlc3Qoe1xuICAgICAgICAgICAgICAgIG1ldGhvZDogXCJHRVRcIixcbiAgICAgICAgICAgICAgICBwYXRoOiBgL3YxLjAvY29sbGVjdGlvbnMvJHtjb2xsZWN0aW9uSWR9L2RvY3VtZW50cy8ke2RvY3VtZW50SWR9YCxcbiAgICAgICAgICAgICAgICBwYXJhbXM6IHtcbiAgICAgICAgICAgICAgICAgICAgaW5jbHVkZUNvbnRlbnQ6IFwiZmFsc2VcIixcbiAgICAgICAgICAgICAgICAgICAgaW5jbHVkZUxhYmVsczogU3RyaW5nKGluY2x1ZGVMYWJlbHMpLFxuICAgICAgICAgICAgICAgICAgICBpbmNsdWRlVGFnczogU3RyaW5nKGluY2x1ZGVUYWdzKVxuICAgICAgICAgICAgICAgIH1cbiAgICAgICAgICAgIH0pO1xuXG4gICAgICAgICAgICBpZiAoIW1ldGFkYXRhKSB7XG4gICAgICAgICAgICAgICAgcmV0dXJuIG51bGw7XG4gICAgICAgICAgICB9XG5cbiAgICAgICAgICAgIGNvbnN0IGRvYyA9IHBhcnNlRG9jdW1lbnQobWV0YWRhdGEpO1xuICAgICAgICAgICAgaWYgKCFkb2MpIHtcbiAgICAgICAgICAgICAgICByZXR1cm4gbnVsbDtcbiAgICAgICAgICAgIH1cblxuICAgICAgICAgICAgLy8gTm93IGdldCB0aGUgcmF3IGNvbnRlbnRcbiAgICAgICAgICAgIGNvbnN0IGNvbnRlbnQgPSBhd2FpdCB0aGlzLmNsaWVudC5yZXF1ZXN0KHtcbiAgICAgICAgICAgICAgICBtZXRob2Q6IFwiR0VUXCIsXG4gICAgICAgICAgICAgICAgcGF0aDogYC92MS4wL2NvbGxlY3Rpb25zLyR7Y29sbGVjdGlvbklkfS9kb2N1bWVudHMvJHtkb2N1bWVudElkfWAsXG4gICAgICAgICAgICAgICAgcGFyYW1zOiB7XG4gICAgICAgICAgICAgICAgICAgIGluY2x1ZGVDb250ZW50OiBcInRydWVcIixcbiAgICAgICAgICAgICAgICAgICAgaW5jbHVkZUxhYmVsczogXCJmYWxzZVwiLFxuICAgICAgICAgICAgICAgICAgICBpbmNsdWRlVGFnczogXCJmYWxzZVwiXG4gICAgICAgICAgICAgICAgfVxuICAgICAgICAgICAgfSk7XG5cbiAgICAgICAgICAgIGlmIChjb250ZW50ICE9PSB1bmRlZmluZWQgJiYgY29udGVudCAhPT0gbnVsbCkge1xuICAgICAgICAgICAgICAgIGRvYy5jb250ZW50ID0gY29udGVudDtcbiAgICAgICAgICAgIH1cblxuICAgICAgICAgICAgcmV0dXJuIGRvYztcbiAgICAgICAgfVxuXG4gICAgICAgIC8vIE5vcm1hbCBmbG93IHdoZW4gaW5jbHVkZUNvbnRlbnQ9ZmFsc2VcbiAgICAgICAgY29uc3QgcmVzdWx0ID0gYXdhaXQgdGhpcy5jbGllbnQucmVxdWVzdCh7XG4gICAgICAgICAgICBtZXRob2Q6IFwiR0VUXCIsXG4gICAgICAgICAgICBwYXRoOiBgL3YxLjAvY29sbGVjdGlvbnMvJHtjb2xsZWN0aW9uSWR9L2RvY3VtZW50cy8ke2RvY3VtZW50SWR9YCxcbiAgICAgICAgICAgIHBhcmFtczoge1xuICAgICAgICAgICAgICAgIGluY2x1ZGVDb250ZW50OiBcImZhbHNlXCIsXG4gICAgICAgICAgICAgICAgaW5jbHVkZUxhYmVsczogU3RyaW5nKGluY2x1ZGVMYWJlbHMpLFxuICAgICAgICAgICAgICAgIGluY2x1ZGVUYWdzOiBTdHJpbmcoaW5jbHVkZVRhZ3MpXG4gICAgICAgICAgICB9XG4gICAgICAgIH0pO1xuXG4gICAgICAgIHJldHVybiByZXN1bHQgPyBwYXJzZURvY3VtZW50KHJlc3VsdCkgOiBudWxsO1xuICAgIH1cblxuICAgIC8qKlxuICAgICAqIENoZWNrIGlmIGEgZG9jdW1lbnQgZXhpc3RzLlxuICAgICAqL1xuICAgIGFzeW5jIGV4aXN0cyhjb2xsZWN0aW9uSWQ6IHN0cmluZywgZG9jdW1lbnRJZDogc3RyaW5nKTogUHJvbWlzZTxib29sZWFuPiB7XG4gICAgICAgIHJldHVybiB0aGlzLmNsaWVudC5oZWFkKGAvdjEuMC9jb2xsZWN0aW9ucy8ke2NvbGxlY3Rpb25JZH0vZG9jdW1lbnRzLyR7ZG9jdW1lbnRJZH1gKTtcbiAgICB9XG5cbiAgICAvKipcbiAgICAgKiBEZWxldGUgYSBkb2N1bWVudC5cbiAgICAgKi9cbiAgICBhc3luYyBkZWxldGUoY29sbGVjdGlvbklkOiBzdHJpbmcsIGRvY3VtZW50SWQ6IHN0cmluZyk6IFByb21pc2U8Ym9vbGVhbj4ge1xuICAgICAgICB0cnkge1xuICAgICAgICAgICAgYXdhaXQgdGhpcy5jbGllbnQucmVxdWVzdCh7XG4gICAgICAgICAgICAgICAgbWV0aG9kOiBcIkRFTEVURVwiLFxuICAgICAgICAgICAgICAgIHBhdGg6IGAvdjEuMC9jb2xsZWN0aW9ucy8ke2NvbGxlY3Rpb25JZH0vZG9jdW1lbnRzLyR7ZG9jdW1lbnRJZH1gXG4gICAgICAgICAgICB9KTtcbiAgICAgICAgICAgIHJldHVybiB0cnVlO1xuICAgICAgICB9IGNhdGNoIChlcnJvcikge1xuICAgICAgICAgICAgaWYgKGVycm9yIGluc3RhbmNlb2YgTGF0dGljZUFwaUVycm9yKSB7XG4gICAgICAgICAgICAgICAgcmV0dXJuIGZhbHNlO1xuICAgICAgICAgICAgfVxuICAgICAgICAgICAgdGhyb3cgZXJyb3I7XG4gICAgICAgIH1cbiAgICB9XG59XG5cbi8qKlxuICogTWV0aG9kcyBmb3Igc2VhcmNoaW5nIGRvY3VtZW50cy5cbiAqL1xuY2xhc3MgU2VhcmNoTWV0aG9kcyB7XG4gICAgY29uc3RydWN0b3IocHJpdmF0ZSBjbGllbnQ6IExhdHRpY2VDbGllbnQpIHt9XG5cbiAgICAvKipcbiAgICAgKiBTZWFyY2ggZm9yIGRvY3VtZW50cy5cbiAgICAgKi9cbiAgICBhc3luYyBzZWFyY2gocXVlcnk6IFNlYXJjaFF1ZXJ5KTogUHJvbWlzZTxTZWFyY2hSZXN1bHQgfCBudWxsPiB7XG4gICAgICAgIGNvbnN0IHJlc3VsdCA9IGF3YWl0IHRoaXMuY2xpZW50LnJlcXVlc3Qoe1xuICAgICAgICAgICAgbWV0aG9kOiBcIlBPU1RcIixcbiAgICAgICAgICAgIHBhdGg6IGAvdjEuMC9jb2xsZWN0aW9ucy8ke3F1ZXJ5LmNvbGxlY3Rpb25JZH0vZG9jdW1lbnRzL3NlYXJjaGAsXG4gICAgICAgICAgICBkYXRhOiBzZWFyY2hRdWVyeVRvUmVxdWVzdChxdWVyeSlcbiAgICAgICAgfSk7XG5cbiAgICAgICAgcmV0dXJuIHJlc3VsdCA/IHBhcnNlU2VhcmNoUmVzdWx0KHJlc3VsdCkgOiBudWxsO1xuICAgIH1cblxuICAgIC8qKlxuICAgICAqIFNlYXJjaCBkb2N1bWVudHMgdXNpbmcgYSBTUUwtbGlrZSBleHByZXNzaW9uLlxuICAgICAqL1xuICAgIGFzeW5jIHNlYXJjaEJ5U3FsKGNvbGxlY3Rpb25JZDogc3RyaW5nLCBzcWxFeHByZXNzaW9uOiBzdHJpbmcpOiBQcm9taXNlPFNlYXJjaFJlc3VsdCB8IG51bGw+IHtcbiAgICAgICAgY29uc3QgcmVzdWx0ID0gYXdhaXQgdGhpcy5jbGllbnQucmVxdWVzdCh7XG4gICAgICAgICAgICBtZXRob2Q6IFwiUE9TVFwiLFxuICAgICAgICAgICAgcGF0aDogYC92MS4wL2NvbGxlY3Rpb25zLyR7Y29sbGVjdGlvbklkfS9kb2N1bWVudHMvc2VhcmNoYCxcbiAgICAgICAgICAgIGRhdGE6IHsgc3FsRXhwcmVzc2lvbiB9XG4gICAgICAgIH0pO1xuXG4gICAgICAgIHJldHVybiByZXN1bHQgPyBwYXJzZVNlYXJjaFJlc3VsdChyZXN1bHQpIDogbnVsbDtcbiAgICB9XG5cbiAgICAvKipcbiAgICAgKiBFbnVtZXJhdGUgZG9jdW1lbnRzIGluIGEgY29sbGVjdGlvbi5cbiAgICAgKi9cbiAgICBhc3luYyBlbnVtZXJhdGUocXVlcnk6IFNlYXJjaFF1ZXJ5KTogUHJvbWlzZTxTZWFyY2hSZXN1bHQgfCBudWxsPiB7XG4gICAgICAgIHJldHVybiB0aGlzLnNlYXJjaChxdWVyeSk7XG4gICAgfVxufVxuXG4vKipcbiAqIE1ldGhvZHMgZm9yIG1hbmFnaW5nIHNjaGVtYXMuXG4gKi9cbmNsYXNzIFNjaGVtYU1ldGhvZHMge1xuICAgIGNvbnN0cnVjdG9yKHByaXZhdGUgY2xpZW50OiBMYXR0aWNlQ2xpZW50KSB7fVxuXG4gICAgLyoqXG4gICAgICogR2V0IGFsbCBzY2hlbWFzLlxuICAgICAqL1xuICAgIGFzeW5jIHJlYWRBbGwob3B0aW9uczogUGFnaW5hdGlvbk9wdGlvbnMgPSB7fSk6IFByb21pc2U8RW51bWVyYXRpb25SZXN1bHQ8U2NoZW1hPj4ge1xuICAgICAgICBjb25zdCByZXN1bHQgPSBhd2FpdCB0aGlzLmNsaWVudC5yZXF1ZXN0KHtcbiAgICAgICAgICAgIG1ldGhvZDogXCJHRVRcIixcbiAgICAgICAgICAgIHBhdGg6IFwiL3YxLjAvc2NoZW1hc1wiLFxuICAgICAgICAgICAgcGFyYW1zOiBwYWdpbmF0aW9uUGFyYW1zKG9wdGlvbnMpXG4gICAgICAgIH0pO1xuXG4gICAgICAgIHJldHVybiBwYXJzZUVudW1lcmF0aW9uUmVzdWx0KHJlc3VsdCwgcGFyc2VTY2hlbWEpO1xuICAgIH1cblxuICAgIC8qKlxuICAgICAqIEdldCBhIHNjaGVtYSBieSBJRC5cbiAgICAgKi9cbiAgICBhc3luYyByZWFkQnlJZChzY2hlbWFJZDogc3RyaW5nKTogUHJvbWlzZTxTY2hlbWEgfCBudWxsPiB7XG4gICAgICAgIGNvbnN0IHJlc3VsdCA9IGF3YWl0IHRoaXMuY2xpZW50LnJlcXVlc3Qoe1xuICAgICAgICAgICAgbWV0aG9kOiBcIkdFVFwiLFxuICAgICAgICAgICAgcGF0aDogYC92MS4wL3NjaGVtYXMvJHtzY2hlbWFJZH1gXG4gICAgICAgIH0pO1xuXG4gICAgICAgIHJldHVybiByZXN1bHQgPyBwYXJzZVNjaGVtYShyZXN1bHQpIDogbnVsbDtcbiAgICB9XG5cbiAgICAvKipcbiAgICAgKiBHZXQgZWxlbWVudHMgZm9yIGEgc2NoZW1hLlxuICAgICAqL1xuICAgIGFzeW5jIGdldEVsZW1lbnRzKFxuICAgICAgICBzY2hlbWFJZDogc3RyaW5nLFxuICAgICAgICBvcHRpb25zOiBQYWdpbmF0aW9uT3B0aW9ucyA9IHt9XG4gICAgKTogUHJvbWlzZTxFbnVtZXJhdGlvblJlc3VsdDxTY2hlbWFFbGVtZW50Pj4ge1xuICAgICAgICBjb25zdCByZXN1bHQgPSBhd2FpdCB0aGlzLmNsaWVudC5yZXF1ZXN0KHtcbiAgICAgICAgICAgIG1ldGhvZDogXCJHRVRcIixcbiAgICAgICAgICAgIHBhdGg6IGAvdjEuMC9zY2hlbWFzLyR7c2NoZW1hSWR9L2VsZW1lbnRzYCxcbiAgICAgICAgICAgIHBhcmFtczogcGFnaW5hdGlvblBhcmFtcyhvcHRpb25zKVxuICAgICAgICB9KTtcblxuICAgICAgICByZXR1cm4gcGFyc2VFbnVtZXJhdGlvblJlc3VsdChyZXN1bHQsIHBhcnNlU2NoZW1hRWxlbWVudCk7XG4gICAgfVxufVxuXG4vKipcbiAqIE1ldGhvZHMgZm9yIG1hbmFnaW5nIGluZGV4ZXMuXG4gKi9cbmNsYXNzIEluZGV4TWV0aG9kcyB7XG4gICAgY29uc3RydWN0b3IocHJpdmF0ZSBjbGllbnQ6IExhdHRpY2VDbGllbnQpIHt9XG5cbiAgICAvKipcbiAgICAgKiBHZXQgYWxsIGluZGV4IHRhYmxlIG1hcHBpbmdzLlxuICAgICAqL1xuICAgIGFzeW5jIGdldE1hcHBpbmdzKG9wdGlvbnM6IFBhZ2luYXRpb25PcHRpb25zID0ge30pOiBQcm9taXNlPEVudW1lcmF0aW9uUmVzdWx0PEluZGV4VGFibGVNYXBwaW5nPj4ge1xuICAgICAgICBjb25zdCByZXN1bHQgPSBhd2FpdCB0aGlzLmNsaWVudC5yZXF1ZXN0KHtcbiAgICAgICAgICAgIG1ldGhvZDogXCJHRVRcIixcbiAgICAgICAgICAgIHBhdGg6IFwiL3YxLjAvdGFibGVzXCIsXG4gICAgICAgICAgICBwYXJhbXM6IHBhZ2luYXRpb25QYXJhbXMob3B0aW9ucylcbiAgICAgICAgfSk7XG5cbiAgICAgICAgcmV0dXJuIHBhcnNlRW51bWVyYXRpb25SZXN1bHQocmVzdWx0LCBwYXJzZUluZGV4VGFibGVNYXBwaW5nKTtcbiAgICB9XG5cbiAgICAvKipcbiAgICAgKiBHZXQgdGhlIGVudHJpZXMgZm9yIGFuIGluZGV4IHRhYmxlLiBUaGUgZW50cmllcyBhcmUgcmV0dXJuZWQgaW4gdGhlXG4gICAgICogYG9iamVjdHNgIGFycmF5IG9mIHRoZSB7QGxpbmsgRW51bWVyYXRpb25SZXN1bHR9OyB0aGUgdG90YWwgbnVtYmVyIG9mXG4gICAgICogZW50cmllcyBpcyBhdmFpbGFibGUgb24gYHRvdGFsUmVjb3Jkc2AuXG4gICAgICovXG4gICAgYXN5bmMgZ2V0RW50cmllcyhcbiAgICAgICAgdGFibGVOYW1lOiBzdHJpbmcsXG4gICAgICAgIG9wdGlvbnM6IFBhZ2luYXRpb25PcHRpb25zID0ge31cbiAgICApOiBQcm9taXNlPEVudW1lcmF0aW9uUmVzdWx0PEluZGV4VGFibGVFbnRyeT4+IHtcbiAgICAgICAgY29uc3QgcmVzdWx0ID0gYXdhaXQgdGhpcy5jbGllbnQucmVxdWVzdCh7XG4gICAgICAgICAgICBtZXRob2Q6IFwiR0VUXCIsXG4gICAgICAgICAgICBwYXRoOiBgL3YxLjAvdGFibGVzLyR7ZW5jb2RlVVJJQ29tcG9uZW50KHRhYmxlTmFtZSl9L2VudHJpZXNgLFxuICAgICAgICAgICAgcGFyYW1zOiBwYWdpbmF0aW9uUGFyYW1zKG9wdGlvbnMpXG4gICAgICAgIH0pO1xuXG4gICAgICAgIHJldHVybiBwYXJzZUVudW1lcmF0aW9uUmVzdWx0KHJlc3VsdCwgcGFyc2VJbmRleFRhYmxlRW50cnkpO1xuICAgIH1cbn1cblxuIl19