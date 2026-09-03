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
    async readAll() {
        const result = await this.client.request({
            method: "GET",
            path: "/v1.0/collections"
        });
        if (Array.isArray(result)) {
            return result.map((c) => (0, models_1.parseCollection)(c)).filter((c) => c !== null);
        }
        return [];
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
    async readAllInCollection(collectionId, includeContent = false, includeLabels = true, includeTags = true) {
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
            return result.map((d) => (0, models_1.parseDocument)(d)).filter((d) => d !== null);
        }
        return [];
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
    async readAll() {
        const result = await this.client.request({
            method: "GET",
            path: "/v1.0/schemas"
        });
        if (Array.isArray(result)) {
            return result.map((s) => (0, models_1.parseSchema)(s)).filter((s) => s !== null);
        }
        return [];
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
    async getElements(schemaId) {
        const result = await this.client.request({
            method: "GET",
            path: `/v1.0/schemas/${schemaId}/elements`
        });
        if (Array.isArray(result)) {
            return result.map((e) => (0, models_1.parseSchemaElement)(e)).filter((e) => e !== null);
        }
        return [];
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
    async getMappings() {
        const result = await this.client.request({
            method: "GET",
            path: "/v1.0/tables"
        });
        if (Array.isArray(result)) {
            return result.map((m) => (0, models_1.parseIndexTableMapping)(m)).filter((m) => m !== null);
        }
        return [];
    }
}
//# sourceMappingURL=data:application/json;base64,eyJ2ZXJzaW9uIjozLCJmaWxlIjoiY2xpZW50LmpzIiwic291cmNlUm9vdCI6IiIsInNvdXJjZXMiOlsiLi4vc3JjL2NsaWVudC50cyJdLCJuYW1lcyI6W10sIm1hcHBpbmdzIjoiO0FBQUE7Ozs7R0FJRzs7O0FBRUgscUNBMkJrQjtBQUNsQiw2Q0FBdUU7QUFZdkU7O0dBRUc7QUFDSCxNQUFhLGFBQWE7SUFVdEI7Ozs7O09BS0c7SUFDSCxZQUFZLE9BQWUsRUFBRSxVQUFrQixLQUFLO1FBQ2hELElBQUksQ0FBQyxPQUFPLEdBQUcsT0FBTyxDQUFDLE9BQU8sQ0FBQyxNQUFNLEVBQUUsRUFBRSxDQUFDLENBQUM7UUFDM0MsSUFBSSxDQUFDLE9BQU8sR0FBRyxPQUFPLENBQUM7UUFFdkIsSUFBSSxDQUFDLFVBQVUsR0FBRyxJQUFJLGlCQUFpQixDQUFDLElBQUksQ0FBQyxDQUFDO1FBQzlDLElBQUksQ0FBQyxRQUFRLEdBQUcsSUFBSSxlQUFlLENBQUMsSUFBSSxDQUFDLENBQUM7UUFDMUMsSUFBSSxDQUFDLE1BQU0sR0FBRyxJQUFJLGFBQWEsQ0FBQyxJQUFJLENBQUMsQ0FBQztRQUN0QyxJQUFJLENBQUMsTUFBTSxHQUFHLElBQUksYUFBYSxDQUFDLElBQUksQ0FBQyxDQUFDO1FBQ3RDLElBQUksQ0FBQyxLQUFLLEdBQUcsSUFBSSxZQUFZLENBQUMsSUFBSSxDQUFDLENBQUM7SUFDeEMsQ0FBQztJQUVEOzs7Ozs7OztPQVFHO0lBQ0gsS0FBSyxDQUFDLE9BQU8sQ0FBVSxPQUF1QjtRQUMxQyxJQUFJLEdBQUcsR0FBRyxHQUFHLElBQUksQ0FBQyxPQUFPLEdBQUcsT0FBTyxDQUFDLElBQUksRUFBRSxDQUFDO1FBRTNDLElBQUksT0FBTyxDQUFDLE1BQU0sRUFBRSxDQUFDO1lBQ2pCLE1BQU0sWUFBWSxHQUFHLElBQUksZUFBZSxDQUFDLE9BQU8sQ0FBQyxNQUFNLENBQUMsQ0FBQztZQUN6RCxHQUFHLElBQUksSUFBSSxZQUFZLENBQUMsUUFBUSxFQUFFLEVBQUUsQ0FBQztRQUN6QyxDQUFDO1FBRUQs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S0FBSyxDQUFDLE9BQU8sQ0FBQyxNQUFNLENBQUMsRUFBRSxDQUFDO1lBQ3hCLE9BQU8sTUFBTSxDQUFDLEdBQUcsQ0FBQyxDQUFDLENBQU0sRUFBRSxFQUFFLENBQUMsSUFBQSwyQkFBa0IsRUFBQyxDQUFDLENBQUMsQ0FBQyxDQUFDLE1BQU0sQ0FBQyxDQUFDLENBQUMsRUFBc0IsRUFBRSxDQUFDLENBQUMsS0FBSyxJQUFJLENBQUMsQ0FBQztRQUN2RyxDQUFDO1FBQ0QsT0FBTyxFQUFFLENBQUM7SUFDZCxDQUFDO0NBQ0o7QUFFRDs7R0FFRztBQUNILE1BQU0sWUFBWTtJQUNkLFlBQW9CLE1BQXFCO1FBQXJCLFdBQU0sR0FBTixNQUFNLENBQWU7SUFBRyxDQUFDO0lBRTdDOztPQUVHO0lBQ0gsS0FBSyxDQUFDLFdBQVc7UUFDYixNQUFNLE1BQU0sR0FBRyxNQUFNLElBQUksQ0FBQyxNQUFNLENBQUMsT0FBTyxDQUFDO1lBQ3JDLE1BQU0sRUFBRSxLQUFLO1lBQ2IsSUFBSSxFQUFFLGNBQWM7U0FDdkIsQ0FBQyxDQUFDO1FBRUgsSUFBSSxLQUFLLENBQUMsT0FBTyxDQUFDLE1BQU0sQ0FBQyxFQUFFLENBQUM7WUFDeEIsT0FBTyxNQUFNLENBQUMsR0FBRyxDQUFDLENBQUMsQ0FBTSxFQUFFLEVBQUUsQ0FBQyxJQUFBLCtCQUFzQixFQUFDLENBQUMsQ0FBQyxDQUFDLENBQUMsTUFBTSxDQUFDLENBQUMsQ0FBQyxFQUEwQixFQUFFLENBQUMsQ0FBQyxLQUFLLElBQUksQ0FBQyxDQUFDO1FBQy9HLENBQUM7UUFDRCxPQUFPLEVBQUUsQ0FBQztJQUNkLENBQUM7Q0FDSiIsInNvdXJjZXNDb250ZW50IjpbIi8qKlxuICogTGF0dGljZSBTREsgQ2xpZW50XG4gKlxuICogTWFpbiBjbGllbnQgZm9yIGludGVyYWN0aW5nIHdpdGggdGhlIExhdHRpY2UgUkVTVCBBUEkuXG4gKi9cblxuaW1wb3J0IHtcbiAgICBDb2xsZWN0aW9uLFxuICAgIERvY3VtZW50LFxuICAgIFNjaGVtYSxcbiAgICBTY2hlbWFFbGVtZW50LFxuICAgIEZpZWxkQ29uc3RyYWludCxcbiAgICBJbmRleGVkRmllbGQsXG4gICAgU2VhcmNoUmVzdWx0LFxuICAgIEluZGV4UmVidWlsZFJlc3VsdCxcbiAgICBTZWFyY2hRdWVyeSxcbiAgICBJbmRleFRhYmxlTWFwcGluZyxcbiAgICBTY2hlbWFFbmZvcmNlbWVudE1vZGUsXG4gICAgSW5kZXhpbmdNb2RlLFxuICAgIENyZWF0ZUNvbGxlY3Rpb25PcHRpb25zLFxuICAgIEluZ2VzdERvY3VtZW50T3B0aW9ucyxcbiAgICBCYXRjaEluZ2VzdERvY3VtZW50RW50cnksXG4gICAgcGFyc2VDb2xsZWN0aW9uLFxuICAgIHBhcnNlRG9jdW1lbnQsXG4gICAgcGFyc2VTY2hlbWEsXG4gICAgcGFyc2VTY2hlbWFFbGVtZW50LFxuICAgIHBhcnNlRmllbGRDb25zdHJhaW50LFxuICAgIHBhcnNlSW5kZXhlZEZpZWxkLFxuICAgIHBhcnNlU2VhcmNoUmVzdWx0LFxuICAgIHBhcnNlSW5kZXhSZWJ1aWxkUmVzdWx0LFxuICAgIHBhcnNlSW5kZXhUYWJsZU1hcHBpbmcsXG4gICAgZmllbGRDb25zdHJhaW50VG9SZXF1ZXN0LFxuICAgIHNlYXJjaFF1ZXJ5VG9SZXF1ZXN0XG59IGZyb20gXCIuL21vZGVsc1wiO1xuaW1wb3J0IHsgTGF0dGljZUNvbm5lY3Rpb25FcnJvciwgTGF0dGljZUFwaUVycm9yIH0gZnJvbSBcIi4vZXhjZXB0aW9uc1wiO1xuXG4vKipcbiAqIEhUVFAgcmVxdWVzdCBvcHRpb25zLlxuICovXG5pbnRlcmZhY2UgUmVxdWVzdE9wdGlvbnMge1xuICAgIG1ldGhvZDogc3RyaW5nO1xuICAgIHBhdGg6IHN0cmluZztcbiAgICBkYXRhPzogYW55O1xuICAgIHBhcmFtcz86IFJlY29yZDxzdHJpbmcsIHN0cmluZz47XG59XG5cbi8qKlxuICogQ2xpZW50IGZvciBpbnRlcmFjdGluZyB3aXRoIHRoZSBMYXR0aWNlIFJFU1QgQVBJLlxuICovXG5leHBvcnQgY2xhc3MgTGF0dGljZUNsaWVudCB7XG4gICAgcHJpdmF0ZSBiYXNlVXJsOiBzdHJpbmc7XG4gICAgcHJpdmF0ZSB0aW1lb3V0OiBudW1iZXI7XG5cbiAgICBwdWJsaWMgY29sbGVjdGlvbjogQ29sbGVjdGlvbk1ldGhvZHM7XG4gICAgcHVibGljIGRvY3VtZW50OiBEb2N1bWVudE1ldGhvZHM7XG4gICAgcHVibGljIHNlYXJjaDogU2VhcmNoTWV0aG9kcztcbiAgICBwdWJsaWMgc2NoZW1hOiBTY2hlbWFNZXRob2RzO1xuICAgIHB1YmxpYyBpbmRleDogSW5kZXhNZXRob2RzO1xuXG4gICAgLyoqXG4gICAgICogSW5pdGlhbGl6ZSB0aGUgTGF0dGljZSBjbGllbnQuXG4gICAgICpcbiAgICAgKiBAcGFyYW0gYmFzZVVybCAtIFRoZSBiYXNlIFVSTCBvZiB0aGUgTGF0dGljZSBzZXJ2ZXIgKGUuZy4sIFwiaHR0cDovL2xvY2FsaG9zdDo4MDAwXCIpXG4gICAgICogQHBhcmFtIHRpbWVvdXQgLSBSZXF1ZXN0IHRpbWVvdXQgaW4gbWlsbGlzZWNvbmRzIChkZWZhdWx0OiAzMDAwMClcbiAgICAgKi9cbiAgICBjb25zdHJ1Y3RvcihiYXNlVXJsOiBzdHJpbmcsIHRpbWVvdXQ6IG51bWJlciA9IDMwMDAwKSB7XG4gICAgICAgIHRoaXMuYmFzZVVybCA9IGJhc2VVcmwucmVwbGFjZSgvXFwvKyQvLCBcIlwiKTtcbiAgICAgICAgdGhpcy50aW1lb3V0ID0gdGltZW91dDtcblxuICAgICAgICB0aGlzLmNvbGxlY3Rpb24gPSBuZXcgQ29sbGVjdGlvbk1ldGhvZHModGhpcyk7XG4gICAgICAgIHRoaXMuZG9jdW1lbnQgPSBuZXcgRG9jdW1lbnRNZXRob2RzKHRoaXMpO1xuICAgICAgICB0aGlzLnNlYXJjaCA9IG5ldyBTZWFyY2hNZXRob2RzKHRoaXMpO1xuICAgICAgICB0aGlzLnNjaGVtYSA9IG5ldyBTY2hlbWFNZXRob2RzKHRoaXMpO1xuICAgICAgICB0aGlzLmluZGV4ID0gbmV3IEluZGV4TWV0aG9kcyh0aGlzKTtcbiAgICB9XG5cbiAgICAvKipcbiAgICAgKiBNYWtlIGFuIEhUVFAgcmVxdWVzdCB0byB0aGUgTGF0dGljZSBBUEkuXG4gICAgICpcbiAgICAgKiBPbiBzdWNjZXNzIChIVFRQIDJ4eCkgdGhlIHBhcnNlZCByZXNwb25zZSBib2R5IGlzIHJldHVybmVkIGRpcmVjdGx5IGFzIHRoZVxuICAgICAqIHBheWxvYWQgKGFuIGVtcHR5IGJvZHkgcmVzb2x2ZXMgdG8gYHVuZGVmaW5lZGApLiBPbiBmYWlsdXJlIChub24tMnh4KSBhXG4gICAgICoge0BsaW5rIExhdHRpY2VBcGlFcnJvcn0gaXMgdGhyb3duLCBjYXJyeWluZyB0aGUgc2VydmVyJ3MgYGVycm9yYCBtZXNzYWdlLFxuICAgICAqIHRoZSBIVFRQIHN0YXR1cyBjb2RlLCBhbnkgc3RydWN0dXJlZCBgZGV0YWlsYCwgYW5kIHRoZSByZXF1ZXN0IGlkIGZyb20gdGhlXG4gICAgICogYFgtTGF0dGljZS1SZXF1ZXN0LUlkYCByZXNwb25zZSBoZWFkZXIuXG4gICAgICovXG4gICAgYXN5bmMgcmVxdWVzdDxUID0gYW55PihvcHRpb25zOiBSZXF1ZXN0T3B0aW9ucyk6IFByb21pc2U8VD4ge1xuICAgICAgICBsZXQgdXJsID0gYCR7dGhpcy5iYXNlVXJsfSR7b3B0aW9ucy5wYXRofWA7XG5cbiAgICAgICAgaWYgKG9wdGlvbnMucGFyYW1zKSB7XG4gICAgICAgICAgICBjb25zdCBzZWFyY2hQYXJhbXMgPSBuZXcgVVJMU2VhcmNoUGFyYW1zKG9wdGlvbnMucGFyYW1zKTtcbiAgICAgICAgICAgIHVybCArPSBgPyR7c2VhcmNoUGFyYW1zLnRvU3RyaW5nKCl9YDtcbiAgICAgICAgfVxuXG4gICAgICAgIGNvbnN0IGZldGNoT3B0aW9uczogUmVxdWVzdEluaXQgPSB7XG4gICAgICAgICAgICBtZXRob2Q6IG9wdGlvbnMubWV0aG9kLFxuICAgICAgICAgICAgaGVhZGVyczoge1xuICAgICAgICAgICAgICAgIFwiQ29udGVudC1UeXBlXCI6IFwiYXBwbGljYXRpb24vanNvblwiXG4gICAgICAgICAgICB9XG4gICAgICAgIH07XG5cbiAgICAgICAgaWYgKG9wdGlvbnMuZGF0YSAmJiAob3B0aW9ucy5tZXRob2QgPT09IFwiUE9TVFwiIHx8IG9wdGlvbnMubWV0aG9kID09PSBcIlBVVFwiKSkge1xuICAgICAgICAgICAgZmV0Y2hPcHRpb25zLmJvZHkgPSBKU09OLnN0cmluZ2lmeShvcHRpb25zLmRhdGEpO1xuICAgICAgICB9XG5cbiAgICAgICAgY29uc3QgZG9GZXRjaCA9IGFzeW5jICgpID0+IHtcbiAgICAgICAgICAgIGNvbnN0IGNvbnRyb2xsZXIgPSBuZXcgQWJvcnRDb250cm9sbGVyKCk7XG4gICAgICAgICAgICBjb25zdCB0aW1lb3V0SWQgPSBzZXRUaW1lb3V0KCgpID0+IGNvbnRyb2xsZXIuYWJvcnQoKSwgdGhpcy50aW1lb3V0KTtcbiAgICAgICAgICAgIGZldGNoT3B0aW9ucy5zaWduYWwgPSBjb250cm9sbGVyLnNpZ25hbDtcbiAgICAgICAgICAgIHRyeSB7XG4gICAgICAgICAgICAgICAgcmV0dXJuIGF3YWl0IGZldGNoKHVybCwgZmV0Y2hPcHRpb25zKTtcbiAgICAgICAgICAgIH0gZmluYWxseSB7XG4gICAgICAgICAgICAgICAgY2xlYXJUaW1lb3V0KHRpbWVvdXRJZCk7XG4gICAgICAgICAgICB9XG4gICAgICAgIH07XG5cbiAgICAgICAgbGV0IHJlc3BvbnNlOiBBd2FpdGVkPFJldHVyblR5cGU8dHlwZW9mIGZldGNoPj47XG4gICAgICAgIHRyeSB7XG4gICAgICAgICAgICByZXNwb25zZSA9IGF3YWl0IGRvRmV0Y2goKTtcbiAgICAgICAgfSBjYXRjaCAoZXJyb3I6IGFueSkge1xuICAgICAgICAgICAgaWYgKGVycm9yLm5hbWUgPT09IFwiQWJvcnRFcnJvclwiKSB7XG4gICAgICAgICAgICAgICAgdGhyb3cgbmV3IExhdHRpY2VDb25uZWN0aW9uRXJyb3IoYFJlcXVlc3QgdG8gJHt1cmx9IHRpbWVkIG91dGApO1xuICAgICAgICAgICAgfVxuICAgICAgICAgICAgdGhyb3cgbmV3IExhdHRpY2VDb25uZWN0aW9uRXJyb3IoYEZhaWxlZCB0byBjb25uZWN0IHRvICR7dXJsfWAsIGVycm9yKTtcbiAgICAgICAgfVxuXG4gICAgICAgIGNvbnN0IHJlcXVlc3RJZCA9IHJlc3BvbnNlLmhlYWRlcnMuZ2V0KFwiWC1MYXR0aWNlLVJlcXVlc3QtSWRcIikgPz8gdW5kZWZpbmVkO1xuXG4gICAgICAgIC8vIEhFQUQgcmVzcG9uc2VzIGhhdmUgbm8gYm9keTsgcmVhZCB0aGUgdGV4dCBmb3IgZXZlcnl0aGluZyBlbHNlLlxuICAgICAgICBjb25zdCByZXNwb25zZVRleHQgPSBvcHRpb25zLm1ldGhvZCA9PT0gXCJIRUFEXCIgPyBcIlwiIDogYXdhaXQgcmVzcG9uc2UudGV4dCgpO1xuXG4gICAgICAgIC8vIFBhcnNlIHRoZSBib2R5IG9uY2UgKG1heSBiZSBlbXB0eSwgSlNPTiwgb3IgcGxhaW4gdGV4dCkuXG4gICAgICAgIGxldCBwYXJzZWRCb2R5OiBhbnkgPSB1bmRlZmluZWQ7XG4gICAgICAgIGlmIChyZXNwb25zZVRleHQpIHtcbiAgICAgICAgICAgIHRyeSB7XG4gICAgICAgICAgICAgICAgcGFyc2VkQm9keSA9IEpTT04ucGFyc2UocmVzcG9uc2VUZXh0KTtcbiAgICAgICAgICAgIH0gY2F0Y2gge1xuICAgICAgICAgICAgICAgIHBhcnNlZEJvZHkgPSByZXNwb25zZVRleHQ7XG4gICAgICAgICAgICB9XG4gICAgICAgIH1cblxuICAgICAgICBpZiAoIXJlc3BvbnNlLm9rKSB7XG4gICAgICAgICAgICAvLyBFcnJvciBjb250cmFjdDogYm9keSBpcyBgeyBlcnJvciwgZGV0YWlsPyB9YC4gRmFsbCBiYWNrIHRvIHRoZVxuICAgICAgICAgICAgLy8gc3RhdHVzIHRleHQgd2hlbiB0aGUgYm9keSBpc24ndCB0aGUgZXhwZWN0ZWQgSlNPTiBzaGFwZS5cbiAgICAgICAgICAgIGxldCBtZXNzYWdlOiBzdHJpbmc7XG4gICAgICAgICAgICBsZXQgZGV0YWlsOiBhbnkgPSB1bmRlZmluZWQ7XG5cbiAgICAgICAgICAgIGlmIChwYXJzZWRCb2R5ICYmIHR5cGVvZiBwYXJzZWRCb2R5ID09PSBcIm9iamVjdFwiICYmIHR5cGVvZiBwYXJzZWRCb2R5LmVycm9yID09PSBcInN0cmluZ1wiKSB7XG4gICAgICAgICAgICAgICAgbWVzc2FnZSA9IHBhcnNlZEJvZHkuZXJyb3I7XG4gICAgICAgICAgICAgICAgZGV0YWlsID0gcGFyc2VkQm9keS5kZXRhaWw7XG4gICAgICAgICAgICB9IGVsc2UgaWYgKHR5cGVvZiBwYXJzZWRCb2R5ID09PSBcInN0cmluZ1wiICYmIHBhcnNlZEJvZHkubGVuZ3RoID4gMCkge1xuICAgICAgICAgICAgICAgIG1lc3NhZ2UgPSBwYXJzZWRCb2R5O1xuICAgICAgICAgICAgfSBlbHNlIHtcbiAgICAgICAgICAgICAgICBtZXNzYWdlID0gcmVzcG9uc2Uuc3RhdHVzVGV4dCB8fCBgSFRUUCAke3Jlc3BvbnNlLnN0YXR1c31gO1xuICAgICAgICAgICAgfVxuXG4gICAgICAgICAgICB0aHJvdyBuZXcgTGF0dGljZUFwaUVycm9yKG1lc3NhZ2UsIHJlc3BvbnNlLnN0YXR1cywgZGV0YWlsLCByZXF1ZXN0SWQpO1xuICAgICAgICB9XG5cbiAgICAgICAgLy8gU3VjY2VzczogdGhlIGJvZHkgSVMgdGhlIHBheWxvYWQuIEVtcHR5IGJvZHkgcmVzb2x2ZXMgdG8gdW5kZWZpbmVkLlxuICAgICAgICByZXR1cm4gcGFyc2VkQm9keSBhcyBUO1xuICAgIH1cblxuICAgIC8qKlxuICAgICAqIElzc3VlIGEgSEVBRCByZXF1ZXN0IGFuZCByZXBvcnQgd2hldGhlciB0aGUgcmVzb3VyY2UgZXhpc3RzICgyeHgpLlxuICAgICAqIE5vbi0yeHggcmVzcG9uc2VzIChlLmcuIDQwNCkgcmVzb2x2ZSB0byBgZmFsc2VgIHJhdGhlciB0aGFuIHRocm93aW5nLlxuICAgICAqL1xuICAgIGFzeW5jIGhlYWQocGF0aDogc3RyaW5nKTogUHJvbWlzZTxib29sZWFuPiB7XG4gICAgICAgIHRyeSB7XG4gICAgICAgICAgICBhd2FpdCB0aGlzLnJlcXVlc3QoeyBtZXRob2Q6IFwiSEVBRFwiLCBwYXRoIH0pO1xuICAgICAgICAgICAgcmV0dXJuIHRydWU7XG4gICAgICAgIH0gY2F0Y2ggKGVycm9yKSB7XG4gICAgICAgICAgICBpZiAoZXJyb3IgaW5zdGFuY2VvZiBMYXR0aWNlQXBpRXJyb3IpIHtcbiAgICAgICAgICAgICAgICByZXR1cm4gZmFsc2U7XG4gICAgICAgICAgICB9XG4gICAgICAgICAgICB0aHJvdyBlcnJvcjtcbiAgICAgICAgfVxuICAgIH1cblxuICAgIC8qKlxuICAgICAqIENoZWNrIGlmIHRoZSBMYXR0aWNlIHNlcnZlciBpcyBoZWFsdGh5LlxuICAgICAqL1xuICAgIGFzeW5jIGhlYWx0aENoZWNrKCk6IFByb21pc2U8Ym9vbGVhbj4ge1xuICAgICAgICB0cnkge1xuICAgICAgICAgICAgYXdhaXQgdGhpcy5yZXF1ZXN0KHsgbWV0aG9kOiBcIkdFVFwiLCBwYXRoOiBcIi92MS4wL2hlYWx0aFwiIH0pO1xuICAgICAgICAgICAgcmV0dXJuIHRydWU7XG4gICAgICAgIH0gY2F0Y2gge1xuICAgICAgICAgICAgcmV0dXJuIGZhbHNlO1xuICAgICAgICB9XG4gICAgfVxufVxuXG4vKipcbiAqIE1ldGhvZHMgZm9yIG1hbmFnaW5nIGNvbGxlY3Rpb25zLlxuICovXG5jbGFzcyBDb2xsZWN0aW9uTWV0aG9kcyB7XG4gICAgY29uc3RydWN0b3IocHJpdmF0ZSBjbGllbnQ6IExhdHRpY2VDbGllbnQpIHt9XG5cbiAgICAvKipcbiAgICAgKiBDcmVhdGUgYSBuZXcgY29sbGVjdGlvbi5cbiAgICAgKi9cbiAgICBhc3luYyBjcmVhdGUob3B0aW9uczogQ3JlYXRlQ29sbGVjdGlvbk9wdGlvbnMpOiBQcm9taXNlPENvbGxlY3Rpb24gfCBudWxsPiB7XG4gICAgICAgIGNvbnN0IGRhdGE6IGFueSA9IHsgbmFtZTogb3B0aW9ucy5uYW1lIH07XG5cbiAgICAgICAgaWYgKG9wdGlvbnMuZGVzY3JpcHRpb24pIGRhdGEuZGVzY3JpcHRpb24gPSBvcHRpb25zLmRlc2NyaXB0aW9uO1xuICAgICAgICBpZiAob3B0aW9ucy5kb2N1bWVudHNEaXJlY3RvcnkpIGRhdGEuZG9jdW1lbnRzRGlyZWN0b3J5ID0gb3B0aW9ucy5kb2N1bWVudHNEaXJlY3Rvcnk7XG4gICAgICAgIGlmIChvcHRpb25zLmxhYmVscykgZGF0YS5sYWJlbHMgPSBvcHRpb25zLmxhYmVscztcbiAgICAgICAgaWYgKG9wdGlvbnMudGFncykgZGF0YS50YWdzID0gb3B0aW9ucy50YWdzO1xuICAgICAgICBpZiAob3B0aW9ucy5zY2hlbWFFbmZvcmNlbWVudE1vZGUgIT09IHVuZGVmaW5lZCAmJiBvcHRpb25zLnNjaGVtYUVuZm9yY2VtZW50TW9kZSAhPT0gU2NoZW1hRW5mb3JjZW1lbnRNb2RlLk5vbmUpIHtcbiAgICAgICAgICAgIGRhdGEuc2NoZW1hRW5mb3JjZW1lbnRNb2RlID0gb3B0aW9ucy5zY2hlbWFFbmZvcmNlbWVudE1vZGU7XG4gICAgICAgIH1cbiAgICAgICAgaWYgKG9wdGlvbnMuZmllbGRDb25zdHJhaW50cykge1xuICAgICAgICAgICAgZGF0YS5maWVsZENvbnN0cmFpbnRzID0gb3B0aW9ucy5maWVsZENvbnN0cmFpbnRzLm1hcChmaWVsZENvbnN0cmFpbnRUb1JlcXVlc3QpO1xuICAgICAgICB9XG4gICAgICAgIGlmIChvcHRpb25zLmluZGV4aW5nTW9kZSAhPT0gdW5kZWZpbmVkICYmIG9wdGlvbnMuaW5kZXhpbmdNb2RlICE9PSBJbmRleGluZ01vZGUuQWxsKSB7XG4gICAgICAgICAgICBkYXRhLmluZGV4aW5nTW9kZSA9IG9wdGlvbnMuaW5kZXhpbmdNb2RlO1xuICAgICAgICB9XG4gICAgICAgIGlmIChvcHRpb25zLmluZGV4ZWRGaWVsZHMpIGRhdGEuaW5kZXhlZEZpZWxkcyA9IG9wdGlvbnMuaW5kZXhlZEZpZWxkcztcblxuICAgICAgICBjb25zdCByZXN1bHQgPSBhd2FpdCB0aGlzLmNsaWVudC5yZXF1ZXN0KHtcbiAgICAgICAgICAgIG1ldGhvZDogXCJQVVRcIixcbiAgICAgICAgICAgIHBhdGg6IFwiL3YxLjAvY29sbGVjdGlvbnNcIixcbiAgICAgICAgICAgIGRhdGFcbiAgICAgICAgfSk7XG5cbiAgICAgICAgcmV0dXJuIHJlc3VsdCA/IHBhcnNlQ29sbGVjdGlvbihyZXN1bHQpIDogbnVsbDtcbiAgICB9XG5cbiAgICAvKipcbiAgICAgKiBHZXQgYWxsIGNvbGxlY3Rpb25zLlxuICAgICAqL1xuICAgIGFzeW5jIHJlYWRBbGwoKTogUHJvbWlzZTxDb2xsZWN0aW9uW10+IHtcbiAgICAgICAgY29uc3QgcmVzdWx0ID0gYXdhaXQgdGhpcy5jbGllbnQucmVxdWVzdCh7XG4gICAgICAgICAgICBtZXRob2Q6IFwiR0VUXCIsXG4gICAgICAgICAgICBwYXRoOiBcIi92MS4wL2NvbGxlY3Rpb25zXCJcbiAgICAgICAgfSk7XG5cbiAgICAgICAgaWYgKEFycmF5LmlzQXJyYXkocmVzdWx0KSkge1xuICAgICAgICAgICAgcmV0dXJuIHJlc3VsdC5tYXAoKGM6IGFueSkgPT4gcGFyc2VDb2xsZWN0aW9uKGMpKS5maWx0ZXIoKGMpOiBjIGlzIENvbGxlY3Rpb24gPT4gYyAhPT0gbnVsbCk7XG4gICAgICAgIH1cbiAgICAgICAgcmV0dXJuIFtdO1xuICAgIH1cblxuICAgIC8qKlxuICAgICAqIEdldCBhIGNvbGxlY3Rpb24gYnkgSUQuXG4gICAgICovXG4gICAgYXN5bmMgcmVhZEJ5SWQoY29sbGVjdGlvbklkOiBzdHJpbmcpOiBQcm9taXNlPENvbGxlY3Rpb24gfCBudWxsPiB7XG4gICAgICAgIGNvbnN0IHJlc3VsdCA9IGF3YWl0IHRoaXMuY2xpZW50LnJlcXVlc3Qoe1xuICAgICAgICAgICAgbWV0aG9kOiBcIkdFVFwiLFxuICAgICAgICAgICAgcGF0aDogYC92MS4wL2NvbGxlY3Rpb25zLyR7Y29sbGVjdGlvbklkfWBcbiAgICAgICAgfSk7XG5cbiAgICAgICAgcmV0dXJuIHJlc3VsdCA/IHBhcnNlQ29sbGVjdGlvbihyZXN1bHQpIDogbnVsbDtcbiAgICB9XG5cbiAgICAvKipcbiAgICAgKiBDaGVjayBpZiBhIGNvbGxlY3Rpb24gZXhpc3RzLlxuICAgICAqL1xuICAgIGFzeW5jIGV4aXN0cyhjb2xsZWN0aW9uSWQ6IHN0cmluZyk6IFByb21pc2U8Ym9vbGVhbj4ge1xuICAgICAgICByZXR1cm4gdGhpcy5jbGllbnQuaGVhZChgL3YxLjAvY29sbGVjdGlvbnMvJHtjb2xsZWN0aW9uSWR9YCk7XG4gICAgfVxuXG4gICAgLyoqXG4gICAgICogRGVsZXRlIGEgY29sbGVjdGlvbi5cbiAgICAgKi9cbiAgICBhc3luYyBkZWxldGUoY29sbGVjdGlvbklkOiBzdHJpbmcpOiBQcm9taXNlPGJvb2xlYW4+IHtcbiAgICAgICAgdHJ5IHtcbiAgICAgICAgICAgIGF3YWl0IHRoaXMuY2xpZW50LnJlcXVlc3Qoe1xuICAgICAgICAgICAgICAgIG1ldGhvZDogXCJERUxFVEVcIixcbiAgICAgICAgICAgICAgICBwYXRoOiBgL3YxLjAvY29sbGVjdGlvbnMvJHtjb2xsZWN0aW9uSWR9YFxuICAgICAgICAgICAgfSk7XG4gICAgICAgICAgICByZXR1cm4gdHJ1ZTtcbiAgICAgICAgfSBjYXRjaCAoZXJyb3IpIHtcbiAgICAgICAgICAgIGlmIChlcnJvciBpbnN0YW5jZW9mIExhdHRpY2VBcGlFcnJvcikge1xuICAgICAgICAgICAgICAgIHJldHVybiBmYWxzZTtcbiAgICAgICAgICAgIH1cbiAgICAgICAgICAgIHRocm93IGVycm9yO1xuICAgICAgICB9XG4gICAgfVxuXG4gICAgLyoqXG4gICAgICogR2V0IGZpZWxkIGNvbnN0cmFpbnRzIGZvciBhIGNvbGxlY3Rpb24uXG4gICAgICovXG4gICAgYXN5bmMgZ2V0Q29uc3RyYWludHMoY29sbGVjdGlvbklkOiBzdHJpbmcpOiBQcm9taXNlPEZpZWxkQ29uc3RyYWludFtdPiB7XG4gICAgICAgIGNvbnN0IHJlc3VsdCA9IGF3YWl0IHRoaXMuY2xpZW50LnJlcXVlc3Qoe1xuICAgICAgICAgICAgbWV0aG9kOiBcIkdFVFwiLFxuICAgICAgICAgICAgcGF0aDogYC92MS4wL2NvbGxlY3Rpb25zLyR7Y29sbGVjdGlvbklkfS9jb25zdHJhaW50c2BcbiAgICAgICAgfSk7XG5cbiAgICAgICAgaWYgKHJlc3VsdCAmJiByZXN1bHQuZmllbGRDb25zdHJhaW50cykge1xuICAgICAgICAgICAgcmV0dXJuIHJlc3VsdC5maWVsZENvbnN0cmFpbnRzLm1hcCgoYzogYW55KSA9PiBwYXJzZUZpZWxkQ29uc3RyYWludChjKSkuZmlsdGVyKChjOiBhbnkpID0+IGMgIT09IG51bGwpO1xuICAgICAgICB9XG4gICAgICAgIHJldHVybiBbXTtcbiAgICB9XG5cbiAgICAvKipcbiAgICAgKiBVcGRhdGUgY29uc3RyYWludHMgZm9yIGEgY29sbGVjdGlvbi5cbiAgICAgKi9cbiAgICBhc3luYyB1cGRhdGVDb25zdHJhaW50cyhcbiAgICAgICAgY29sbGVjdGlvbklkOiBzdHJpbmcsXG4gICAgICAgIHNjaGVtYUVuZm9yY2VtZW50TW9kZTogU2NoZW1hRW5mb3JjZW1lbnRNb2RlLFxuICAgICAgICBmaWVsZENvbnN0cmFpbnRzPzogRmllbGRDb25zdHJhaW50W11cbiAgICApOiBQcm9taXNlPGJvb2xlYW4+IHtcbiAgICAgICAgY29uc3QgZGF0YTogYW55ID0geyBzY2hlbWFFbmZvcmNlbWVudE1vZGUgfTtcbiAgICAgICAgaWYgKGZpZWxkQ29uc3RyYWludHMpIHtcbiAgICAgICAgICAgIGRhdGEuZmllbGRDb25zdHJhaW50cyA9IGZpZWxkQ29uc3RyYWludHMubWFwKGZpZWxkQ29uc3RyYWludFRvUmVxdWVzdCk7XG4gICAgICAgIH1cblxuICAgICAgICB0cnkge1xuICAgICAgICAgICAgYXdhaXQgdGhpcy5jbGllbnQucmVxdWVzdCh7XG4gICAgICAgICAgICAgICAgbWV0aG9kOiBcIlBVVFwiLFxuICAgICAgICAgICAgICAgIHBhdGg6IGAvdjEuMC9jb2xsZWN0aW9ucy8ke2NvbGxlY3Rpb25JZH0vY29uc3RyYWludHNgLFxuICAgICAgICAgICAgICAgIGRhdGFcbiAgICAgICAgICAgIH0pO1xuICAgICAgICAgICAgcmV0dXJuIHRydWU7XG4gICAgICAgIH0gY2F0Y2ggKGVycm9yKSB7XG4gICAgICAgICAgICBpZiAoZXJyb3IgaW5zdGFuY2VvZiBMYXR0aWNlQXBpRXJyb3IpIHtcbiAgICAgICAgICAgICAgICByZXR1cm4gZmFsc2U7XG4gICAgICAgICAgICB9XG4gICAgICAgICAgICB0aHJvdyBlcnJvcjtcbiAgICAgICAgfVxuICAgIH1cblxuICAgIC8qKlxuICAgICAqIEdldCBpbmRleGVkIGZpZWxkcyBmb3IgYSBjb2xsZWN0aW9uLlxuICAgICAqL1xuICAgIGFzeW5jIGdldEluZGV4ZWRGaWVsZHMoY29sbGVjdGlvbklkOiBzdHJpbmcpOiBQcm9taXNlPEluZGV4ZWRGaWVsZFtdPiB7XG4gICAgICAgIGNvbnN0IHJlc3VsdCA9IGF3YWl0IHRoaXMuY2xpZW50LnJlcXVlc3Qoe1xuICAgICAgICAgICAgbWV0aG9kOiBcIkdFVFwiLFxuICAgICAgICAgICAgcGF0aDogYC92MS4wL2NvbGxlY3Rpb25zLyR7Y29sbGVjdGlvbklkfS9pbmRleGluZ2BcbiAgICAgICAgfSk7XG5cbiAgICAgICAgaWYgKHJlc3VsdCAmJiByZXN1bHQuaW5kZXhlZEZpZWxkcykge1xuICAgICAgICAgICAgcmV0dXJuIHJlc3VsdC5pbmRleGVkRmllbGRzLm1hcCgoZjogYW55KSA9PiBwYXJzZUluZGV4ZWRGaWVsZChmKSkuZmlsdGVyKChmOiBhbnkpID0+IGYgIT09IG51bGwpO1xuICAgICAgICB9XG4gICAgICAgIHJldHVybiBbXTtcbiAgICB9XG5cbiAgICAvKipcbiAgICAgKiBVcGRhdGUgaW5kZXhpbmcgY29uZmlndXJhdGlvbiBmb3IgYSBjb2xsZWN0aW9uLlxuICAgICAqL1xuICAgIGFzeW5jIHVwZGF0ZUluZGV4aW5nKFxuICAgICAgICBjb2xsZWN0aW9uSWQ6IHN0cmluZyxcbiAgICAgICAgaW5kZXhpbmdNb2RlOiBJbmRleGluZ01vZGUsXG4gICAgICAgIGluZGV4ZWRGaWVsZHM/OiBzdHJpbmdbXSxcbiAgICAgICAgcmVidWlsZEluZGV4ZXM6IGJvb2xlYW4gPSBmYWxzZVxuICAgICk6IFByb21pc2U8Ym9vbGVhbj4ge1xuICAgICAgICBjb25zdCBkYXRhOiBhbnkgPSB7XG4gICAgICAgICAgICBpbmRleGluZ01vZGUsXG4gICAgICAgICAgICByZWJ1aWxkSW5kZXhlc1xuICAgICAgICB9O1xuICAgICAgICBpZiAoaW5kZXhlZEZpZWxkcykgZGF0YS5pbmRleGVkRmllbGRzID0gaW5kZXhlZEZpZWxkcztcblxuICAgICAgICB0cnkge1xuICAgICAgICAgICAgYXdhaXQgdGhpcy5jbGllbnQucmVxdWVzdCh7XG4gICAgICAgICAgICAgICAgbWV0aG9kOiBcIlBVVFwiLFxuICAgICAgICAgICAgICAgIHBhdGg6IGAvdjEuMC9jb2xsZWN0aW9ucy8ke2NvbGxlY3Rpb25JZH0vaW5kZXhpbmdgLFxuICAgICAgICAgICAgICAgIGRhdGFcbiAgICAgICAgICAgIH0pO1xuICAgICAgICAgICAgcmV0dXJuIHRydWU7XG4gICAgICAgIH0gY2F0Y2ggKGVycm9yKSB7XG4gICAgICAgICAgICBpZiAoZXJyb3IgaW5zdGFuY2VvZiBMYXR0aWNlQXBpRXJyb3IpIHtcbiAgICAgICAgICAgICAgICByZXR1cm4gZmFsc2U7XG4gICAgICAgICAgICB9XG4gICAgICAgICAgICB0aHJvdyBlcnJvcjtcbiAgICAgICAgfVxuICAgIH1cblxuICAgIC8qKlxuICAgICAqIFJlYnVpbGQgaW5kZXhlcyBmb3IgYSBjb2xsZWN0aW9uLlxuICAgICAqL1xuICAgIGFzeW5jIHJlYnVpbGRJbmRleGVzKFxuICAgICAgICBjb2xsZWN0aW9uSWQ6IHN0cmluZyxcbiAgICAgICAgZHJvcFVudXNlZEluZGV4ZXM6IGJvb2xlYW4gPSB0cnVlXG4gICAgKTogUHJvbWlzZTxJbmRleFJlYnVpbGRSZXN1bHQgfCBudWxsPiB7XG4gICAgICAgIGNvbnN0IHJlc3VsdCA9IGF3YWl0IHRoaXMuY2xpZW50LnJlcXVlc3Qoe1xuICAgICAgICAgICAgbWV0aG9kOiBcIlBPU1RcIixcbiAgICAgICAgICAgIHBhdGg6IGAvdjEuMC9jb2xsZWN0aW9ucy8ke2NvbGxlY3Rpb25JZH0vaW5kZXhlcy9yZWJ1aWxkYCxcbiAgICAgICAgICAgIGRhdGE6IHsgZHJvcFVudXNlZEluZGV4ZXMgfVxuICAgICAgICB9KTtcblxuICAgICAgICByZXR1cm4gcmVzdWx0ID8gcGFyc2VJbmRleFJlYnVpbGRSZXN1bHQocmVzdWx0KSA6IG51bGw7XG4gICAgfVxufVxuXG4vKipcbiAqIE1ldGhvZHMgZm9yIG1hbmFnaW5nIGRvY3VtZW50cy5cbiAqL1xuY2xhc3MgRG9jdW1lbnRNZXRob2RzIHtcbiAgICBjb25zdHJ1Y3Rvcihwcml2YXRlIGNsaWVudDogTGF0dGljZUNsaWVudCkge31cblxuICAgIC8qKlxuICAgICAqIEluZ2VzdCBhIG5ldyBkb2N1bWVudCBpbnRvIGEgY29sbGVjdGlvbi5cbiAgICAgKi9cbiAgICBhc3luYyBpbmdlc3Qob3B0aW9uczogSW5nZXN0RG9jdW1lbnRPcHRpb25zKTogUHJvbWlzZTxEb2N1bWVudCB8IG51bGw+IHtcbiAgICAgICAgY29uc3QgZGF0YTogYW55ID0geyBjb250ZW50OiBvcHRpb25zLmNvbnRlbnQgfTtcblxuICAgICAgICBpZiAob3B0aW9ucy5uYW1lKSBkYXRhLm5hbWUgPSBvcHRpb25zLm5hbWU7XG4gICAgICAgIGlmIChvcHRpb25zLmxhYmVscykgZGF0YS5sYWJlbHMgPSBvcHRpb25zLmxhYmVscztcbiAgICAgICAgaWYgKG9wdGlvbnMudGFncykgZGF0YS50YWdzID0gb3B0aW9ucy50YWdzO1xuXG4gICAgICAgIGNvbnN0IHJlc3VsdCA9IGF3YWl0IHRoaXMuY2xpZW50LnJlcXVlc3Qoe1xuICAgICAgICAgICAgbWV0aG9kOiBcIlBVVFwiLFxuICAgICAgICAgICAgcGF0aDogYC92MS4wL2NvbGxlY3Rpb25zLyR7b3B0aW9ucy5jb2xsZWN0aW9uSWR9L2RvY3VtZW50c2AsXG4gICAgICAgICAgICBkYXRhXG4gICAgICAgIH0pO1xuXG4gICAgICAgIHJldHVybiByZXN1bHQgPyBwYXJzZURvY3VtZW50KHJlc3VsdCkgOiBudWxsO1xuICAgIH1cblxuICAgIC8qKlxuICAgICAqIEluZ2VzdCBtdWx0aXBsZSBkb2N1bWVudHMgaW50byBhIGNvbGxlY3Rpb24gaW4gYSBzaW5nbGUgYmF0Y2ggb3BlcmF0aW9uLlxuICAgICAqL1xuICAgIGFzeW5jIGluZ2VzdEJhdGNoKFxuICAgICAgICBjb2xsZWN0aW9uSWQ6IHN0cmluZyxcbiAgICAgICAgZG9jdW1lbnRzOiBCYXRjaEluZ2VzdERvY3VtZW50RW50cnlbXVxuICAgICk6IFByb21pc2U8RG9jdW1lbnRbXSB8IG51bGw+IHtcbiAgICAgICAgY29uc3QgcmVzdWx0ID0gYXdhaXQgdGhpcy5jbGllbnQucmVxdWVzdCh7XG4gICAgICAgICAgICBtZXRob2Q6IFwiUFVUXCIsXG4gICAgICAgICAgICBwYXRoOiBgL3YxLjAvY29sbGVjdGlvbnMvJHtjb2xsZWN0aW9uSWR9L2RvY3VtZW50cy9iYXRjaGAsXG4gICAgICAgICAgICBkYXRhOiB7XG4gICAgICAgICAgICAgICAgZG9jdW1lbnRzOiBkb2N1bWVudHMubWFwKGRvYyA9PiB7XG4gICAgICAgICAgICAgICAgICAgIGNvbnN0IGVudHJ5OiBhbnkgPSB7IGNvbnRlbnQ6IGRvYy5jb250ZW50IH07XG4gICAgICAgICAgICAgICAgICAgIGlmIChkb2MubmFtZSkgZW50cnkubmFtZSA9IGRvYy5uYW1lO1xuICAgICAgICAgICAgICAgICAgICBpZiAoZG9jLmxhYmVscykgZW50cnkubGFiZWxzID0gZG9jLmxhYmVscztcbiAgICAgICAgICAgICAgICAgICAgaWYgKGRvYy50YWdzKSBlbnRyeS50YWdzID0gZG9jLnRhZ3M7XG4gICAgICAgICAgICAgICAgICAgIHJldHVybiBlbnRyeTtcbiAgICAgICAgICAgICAgICB9KVxuICAgICAgICAgICAgfVxuICAgICAgICB9KTtcblxuICAgICAgICBpZiAoQXJyYXkuaXNBcnJheShyZXN1bHQpKSB7XG4gICAgICAgICAgICByZXR1cm4gcmVzdWx0Lm1hcCgoZDogYW55KSA9PiBwYXJzZURvY3VtZW50KGQpKS5maWx0ZXIoKGQpOiBkIGlzIERvY3VtZW50ID0+IGQgIT09IG51bGwpO1xuICAgICAgICB9XG4gICAgICAgIHJldHVybiBudWxsO1xuICAgIH1cblxuICAgIC8qKlxuICAgICAqIEdldCBhbGwgZG9jdW1lbnRzIGluIGEgY29sbGVjdGlvbi5cbiAgICAgKi9cbiAgICBhc3luYyByZWFkQWxsSW5Db2xsZWN0aW9uKFxuICAgICAgICBjb2xsZWN0aW9uSWQ6IHN0cmluZyxcbiAgICAgICAgaW5jbHVkZUNvbnRlbnQ6IGJvb2xlYW4gPSBmYWxzZSxcbiAgICAgICAgaW5jbHVkZUxhYmVsczogYm9vbGVhbiA9IHRydWUsXG4gICAgICAgIGluY2x1ZGVUYWdzOiBib29sZWFuID0gdHJ1ZVxuICAgICk6IFByb21pc2U8RG9jdW1lbnRbXT4ge1xuICAgICAgICBjb25zdCByZXN1bHQgPSBhd2FpdCB0aGlzLmNsaWVudC5yZXF1ZXN0KHtcbiAgICAgICAgICAgIG1ldGhvZDogXCJHRVRcIixcbiAgICAgICAgICAgIHBhdGg6IGAvdjEuMC9jb2xsZWN0aW9ucy8ke2NvbGxlY3Rpb25JZH0vZG9jdW1lbnRzYCxcbiAgICAgICAgICAgIHBhcmFtczoge1xuICAgICAgICAgICAgICAgIGluY2x1ZGVDb250ZW50OiBTdHJpbmcoaW5jbHVkZUNvbnRlbnQpLFxuICAgICAgICAgICAgICAgIGluY2x1ZGVMYWJlbHM6IFN0cmluZyhpbmNsdWRlTGFiZWxzKSxcbiAgICAgICAgICAgICAgICBpbmNsdWRlVGFnczogU3RyaW5nKGluY2x1ZGVUYWdzKVxuICAgICAgICAgICAgfVxuICAgICAgICB9KTtcblxuICAgICAgICBpZiAoQXJyYXkuaXNBcnJheShyZXN1bHQpKSB7XG4gICAgICAgICAgICByZXR1cm4gcmVzdWx0Lm1hcCgoZDogYW55KSA9PiBwYXJzZURvY3VtZW50KGQpKS5maWx0ZXIoKGQpOiBkIGlzIERvY3VtZW50ID0+IGQgIT09IG51bGwpO1xuICAgICAgICB9XG4gICAgICAgIHJldHVybiBbXTtcbiAgICB9XG5cbiAgICAvKipcbiAgICAgKiBHZXQgYSBkb2N1bWVudCBieSBJRC5cbiAgICAgKi9cbiAgICBhc3luYyByZWFkQnlJZChcbiAgICAgICAgY29sbGVjdGlvbklkOiBzdHJpbmcsXG4gICAgICAgIGRvY3VtZW50SWQ6IHN0cmluZyxcbiAgICAgICAgaW5jbHVkZUNvbnRlbnQ6IGJvb2xlYW4gPSBmYWxzZSxcbiAgICAgICAgaW5jbHVkZUxhYmVsczogYm9vbGVhbiA9IHRydWUsXG4gICAgICAgIGluY2x1ZGVUYWdzOiBib29sZWFuID0gdHJ1ZVxuICAgICk6IFByb21pc2U8RG9jdW1lbnQgfCBudWxsPiB7XG4gICAgICAgIGlmIChpbmNsdWRlQ29udGVudCkge1xuICAgICAgICAgICAgLy8gV2hlbiBpbmNsdWRlQ29udGVudD10cnVlLCB0aGUgc2VydmVyIHJldHVybnMgT05MWSB0aGUgcmF3IGRvY3VtZW50IGJvZHksXG4gICAgICAgICAgICAvLyBub3QgdGhlIGRvY3VtZW50IG1ldGFkYXRhLiBXZSBtYWtlIHR3byByZXF1ZXN0czpcbiAgICAgICAgICAgIC8vIDEuIEdldCBkb2N1bWVudCBtZXRhZGF0YSAod2l0aG91dCBjb250ZW50KVxuICAgICAgICAgICAgLy8gMi4gR2V0IHJhdyBjb250ZW50IHNlcGFyYXRlbHlcbiAgICAgICAgICAgIC8vIFRoZW4gY29tYmluZSB0aGVtLlxuXG4gICAgICAgICAgICAvLyBGaXJzdCwgZ2V0IGRvY3VtZW50IG1ldGFkYXRhXG4gICAgICAgICAgICBjb25zdCBtZXRhZGF0YSA9IGF3YWl0IHRoaXMuY2xpZW50LnJlcXVlc3Qoe1xuICAgICAgICAgICAgICAgIG1ldGhvZDogXCJHRVRcIixcbiAgICAgICAgICAgICAgICBwYXRoOiBgL3YxLjAvY29sbGVjdGlvbnMvJHtjb2xsZWN0aW9uSWR9L2RvY3VtZW50cy8ke2RvY3VtZW50SWR9YCxcbiAgICAgICAgICAgICAgICBwYXJhbXM6IHtcbiAgICAgICAgICAgICAgICAgICAgaW5jbHVkZUNvbnRlbnQ6IFwiZmFsc2VcIixcbiAgICAgICAgICAgICAgICAgICAgaW5jbHVkZUxhYmVsczogU3RyaW5nKGluY2x1ZGVMYWJlbHMpLFxuICAgICAgICAgICAgICAgICAgICBpbmNsdWRlVGFnczogU3RyaW5nKGluY2x1ZGVUYWdzKVxuICAgICAgICAgICAgICAgIH1cbiAgICAgICAgICAgIH0pO1xuXG4gICAgICAgICAgICBpZiAoIW1ldGFkYXRhKSB7XG4gICAgICAgICAgICAgICAgcmV0dXJuIG51bGw7XG4gICAgICAgICAgICB9XG5cbiAgICAgICAgICAgIGNvbnN0IGRvYyA9IHBhcnNlRG9jdW1lbnQobWV0YWRhdGEpO1xuICAgICAgICAgICAgaWYgKCFkb2MpIHtcbiAgICAgICAgICAgICAgICByZXR1cm4gbnVsbDtcbiAgICAgICAgICAgIH1cblxuICAgICAgICAgICAgLy8gTm93IGdldCB0aGUgcmF3IGNvbnRlbnRcbiAgICAgICAgICAgIGNvbnN0IGNvbnRlbnQgPSBhd2FpdCB0aGlzLmNsaWVudC5yZXF1ZXN0KHtcbiAgICAgICAgICAgICAgICBtZXRob2Q6IFwiR0VUXCIsXG4gICAgICAgICAgICAgICAgcGF0aDogYC92MS4wL2NvbGxlY3Rpb25zLyR7Y29sbGVjdGlvbklkfS9kb2N1bWVudHMvJHtkb2N1bWVudElkfWAsXG4gICAgICAgICAgICAgICAgcGFyYW1zOiB7XG4gICAgICAgICAgICAgICAgICAgIGluY2x1ZGVDb250ZW50OiBcInRydWVcIixcbiAgICAgICAgICAgICAgICAgICAgaW5jbHVkZUxhYmVsczogXCJmYWxzZVwiLFxuICAgICAgICAgICAgICAgICAgICBpbmNsdWRlVGFnczogXCJmYWxzZVwiXG4gICAgICAgICAgICAgICAgfVxuICAgICAgICAgICAgfSk7XG5cbiAgICAgICAgICAgIGlmIChjb250ZW50ICE9PSB1bmRlZmluZWQgJiYgY29udGVudCAhPT0gbnVsbCkge1xuICAgICAgICAgICAgICAgIGRvYy5jb250ZW50ID0gY29udGVudDtcbiAgICAgICAgICAgIH1cblxuICAgICAgICAgICAgcmV0dXJuIGRvYztcbiAgICAgICAgfVxuXG4gICAgICAgIC8vIE5vcm1hbCBmbG93IHdoZW4gaW5jbHVkZUNvbnRlbnQ9ZmFsc2VcbiAgICAgICAgY29uc3QgcmVzdWx0ID0gYXdhaXQgdGhpcy5jbGllbnQucmVxdWVzdCh7XG4gICAgICAgICAgICBtZXRob2Q6IFwiR0VUXCIsXG4gICAgICAgICAgICBwYXRoOiBgL3YxLjAvY29sbGVjdGlvbnMvJHtjb2xsZWN0aW9uSWR9L2RvY3VtZW50cy8ke2RvY3VtZW50SWR9YCxcbiAgICAgICAgICAgIHBhcmFtczoge1xuICAgICAgICAgICAgICAgIGluY2x1ZGVDb250ZW50OiBcImZhbHNlXCIsXG4gICAgICAgICAgICAgICAgaW5jbHVkZUxhYmVsczogU3RyaW5nKGluY2x1ZGVMYWJlbHMpLFxuICAgICAgICAgICAgICAgIGluY2x1ZGVUYWdzOiBTdHJpbmcoaW5jbHVkZVRhZ3MpXG4gICAgICAgICAgICB9XG4gICAgICAgIH0pO1xuXG4gICAgICAgIHJldHVybiByZXN1bHQgPyBwYXJzZURvY3VtZW50KHJlc3VsdCkgOiBudWxsO1xuICAgIH1cblxuICAgIC8qKlxuICAgICAqIENoZWNrIGlmIGEgZG9jdW1lbnQgZXhpc3RzLlxuICAgICAqL1xuICAgIGFzeW5jIGV4aXN0cyhjb2xsZWN0aW9uSWQ6IHN0cmluZywgZG9jdW1lbnRJZDogc3RyaW5nKTogUHJvbWlzZTxib29sZWFuPiB7XG4gICAgICAgIHJldHVybiB0aGlzLmNsaWVudC5oZWFkKGAvdjEuMC9jb2xsZWN0aW9ucy8ke2NvbGxlY3Rpb25JZH0vZG9jdW1lbnRzLyR7ZG9jdW1lbnRJZH1gKTtcbiAgICB9XG5cbiAgICAvKipcbiAgICAgKiBEZWxldGUgYSBkb2N1bWVudC5cbiAgICAgKi9cbiAgICBhc3luYyBkZWxldGUoY29sbGVjdGlvbklkOiBzdHJpbmcsIGRvY3VtZW50SWQ6IHN0cmluZyk6IFByb21pc2U8Ym9vbGVhbj4ge1xuICAgICAgICB0cnkge1xuICAgICAgICAgICAgYXdhaXQgdGhpcy5jbGllbnQucmVxdWVzdCh7XG4gICAgICAgICAgICAgICAgbWV0aG9kOiBcIkRFTEVURVwiLFxuICAgICAgICAgICAgICAgIHBhdGg6IGAvdjEuMC9jb2xsZWN0aW9ucy8ke2NvbGxlY3Rpb25JZH0vZG9jdW1lbnRzLyR7ZG9jdW1lbnRJZH1gXG4gICAgICAgICAgICB9KTtcbiAgICAgICAgICAgIHJldHVybiB0cnVlO1xuICAgICAgICB9IGNhdGNoIChlcnJvcikge1xuICAgICAgICAgICAgaWYgKGVycm9yIGluc3RhbmNlb2YgTGF0dGljZUFwaUVycm9yKSB7XG4gICAgICAgICAgICAgICAgcmV0dXJuIGZhbHNlO1xuICAgICAgICAgICAgfVxuICAgICAgICAgICAgdGhyb3cgZXJyb3I7XG4gICAgICAgIH1cbiAgICB9XG59XG5cbi8qKlxuICogTWV0aG9kcyBmb3Igc2VhcmNoaW5nIGRvY3VtZW50cy5cbiAqL1xuY2xhc3MgU2VhcmNoTWV0aG9kcyB7XG4gICAgY29uc3RydWN0b3IocHJpdmF0ZSBjbGllbnQ6IExhdHRpY2VDbGllbnQpIHt9XG5cbiAgICAvKipcbiAgICAgKiBTZWFyY2ggZm9yIGRvY3VtZW50cy5cbiAgICAgKi9cbiAgICBhc3luYyBzZWFyY2gocXVlcnk6IFNlYXJjaFF1ZXJ5KTogUHJvbWlzZTxTZWFyY2hSZXN1bHQgfCBudWxsPiB7XG4gICAgICAgIGNvbnN0IHJlc3VsdCA9IGF3YWl0IHRoaXMuY2xpZW50LnJlcXVlc3Qoe1xuICAgICAgICAgICAgbWV0aG9kOiBcIlBPU1RcIixcbiAgICAgICAgICAgIHBhdGg6IGAvdjEuMC9jb2xsZWN0aW9ucy8ke3F1ZXJ5LmNvbGxlY3Rpb25JZH0vZG9jdW1lbnRzL3NlYXJjaGAsXG4gICAgICAgICAgICBkYXRhOiBzZWFyY2hRdWVyeVRvUmVxdWVzdChxdWVyeSlcbiAgICAgICAgfSk7XG5cbiAgICAgICAgcmV0dXJuIHJlc3VsdCA/IHBhcnNlU2VhcmNoUmVzdWx0KHJlc3VsdCkgOiBudWxsO1xuICAgIH1cblxuICAgIC8qKlxuICAgICAqIFNlYXJjaCBkb2N1bWVudHMgdXNpbmcgYSBTUUwtbGlrZSBleHByZXNzaW9uLlxuICAgICAqL1xuICAgIGFzeW5jIHNlYXJjaEJ5U3FsKGNvbGxlY3Rpb25JZDogc3RyaW5nLCBzcWxFeHByZXNzaW9uOiBzdHJpbmcpOiBQcm9taXNlPFNlYXJjaFJlc3VsdCB8IG51bGw+IHtcbiAgICAgICAgY29uc3QgcmVzdWx0ID0gYXdhaXQgdGhpcy5jbGllbnQucmVxdWVzdCh7XG4gICAgICAgICAgICBtZXRob2Q6IFwiUE9TVFwiLFxuICAgICAgICAgICAgcGF0aDogYC92MS4wL2NvbGxlY3Rpb25zLyR7Y29sbGVjdGlvbklkfS9kb2N1bWVudHMvc2VhcmNoYCxcbiAgICAgICAgICAgIGRhdGE6IHsgc3FsRXhwcmVzc2lvbiB9XG4gICAgICAgIH0pO1xuXG4gICAgICAgIHJldHVybiByZXN1bHQgPyBwYXJzZVNlYXJjaFJlc3VsdChyZXN1bHQpIDogbnVsbDtcbiAgICB9XG5cbiAgICAvKipcbiAgICAgKiBFbnVtZXJhdGUgZG9jdW1lbnRzIGluIGEgY29sbGVjdGlvbi5cbiAgICAgKi9cbiAgICBhc3luYyBlbnVtZXJhdGUocXVlcnk6IFNlYXJjaFF1ZXJ5KTogUHJvbWlzZTxTZWFyY2hSZXN1bHQgfCBudWxsPiB7XG4gICAgICAgIHJldHVybiB0aGlzLnNlYXJjaChxdWVyeSk7XG4gICAgfVxufVxuXG4vKipcbiAqIE1ldGhvZHMgZm9yIG1hbmFnaW5nIHNjaGVtYXMuXG4gKi9cbmNsYXNzIFNjaGVtYU1ldGhvZHMge1xuICAgIGNvbnN0cnVjdG9yKHByaXZhdGUgY2xpZW50OiBMYXR0aWNlQ2xpZW50KSB7fVxuXG4gICAgLyoqXG4gICAgICogR2V0IGFsbCBzY2hlbWFzLlxuICAgICAqL1xuICAgIGFzeW5jIHJlYWRBbGwoKTogUHJvbWlzZTxTY2hlbWFbXT4ge1xuICAgICAgICBjb25zdCByZXN1bHQgPSBhd2FpdCB0aGlzLmNsaWVudC5yZXF1ZXN0KHtcbiAgICAgICAgICAgIG1ldGhvZDogXCJHRVRcIixcbiAgICAgICAgICAgIHBhdGg6IFwiL3YxLjAvc2NoZW1hc1wiXG4gICAgICAgIH0pO1xuXG4gICAgICAgIGlmIChBcnJheS5pc0FycmF5KHJlc3VsdCkpIHtcbiAgICAgICAgICAgIHJldHVybiByZXN1bHQubWFwKChzOiBhbnkpID0+IHBhcnNlU2NoZW1hKHMpKS5maWx0ZXIoKHMpOiBzIGlzIFNjaGVtYSA9PiBzICE9PSBudWxsKTtcbiAgICAgICAgfVxuICAgICAgICByZXR1cm4gW107XG4gICAgfVxuXG4gICAgLyoqXG4gICAgICogR2V0IGEgc2NoZW1hIGJ5IElELlxuICAgICAqL1xuICAgIGFzeW5jIHJlYWRCeUlkKHNjaGVtYUlkOiBzdHJpbmcpOiBQcm9taXNlPFNjaGVtYSB8IG51bGw+IHtcbiAgICAgICAgY29uc3QgcmVzdWx0ID0gYXdhaXQgdGhpcy5jbGllbnQucmVxdWVzdCh7XG4gICAgICAgICAgICBtZXRob2Q6IFwiR0VUXCIsXG4gICAgICAgICAgICBwYXRoOiBgL3YxLjAvc2NoZW1hcy8ke3NjaGVtYUlkfWBcbiAgICAgICAgfSk7XG5cbiAgICAgICAgcmV0dXJuIHJlc3VsdCA/IHBhcnNlU2NoZW1hKHJlc3VsdCkgOiBudWxsO1xuICAgIH1cblxuICAgIC8qKlxuICAgICAqIEdldCBlbGVtZW50cyBmb3IgYSBzY2hlbWEuXG4gICAgICovXG4gICAgYXN5bmMgZ2V0RWxlbWVudHMoc2NoZW1hSWQ6IHN0cmluZyk6IFByb21pc2U8U2NoZW1hRWxlbWVudFtdPiB7XG4gICAgICAgIGNvbnN0IHJlc3VsdCA9IGF3YWl0IHRoaXMuY2xpZW50LnJlcXVlc3Qoe1xuICAgICAgICAgICAgbWV0aG9kOiBcIkdFVFwiLFxuICAgICAgICAgICAgcGF0aDogYC92MS4wL3NjaGVtYXMvJHtzY2hlbWFJZH0vZWxlbWVudHNgXG4gICAgICAgIH0pO1xuXG4gICAgICAgIGlmIChBcnJheS5pc0FycmF5KHJlc3VsdCkpIHtcbiAgICAgICAgICAgIHJldHVybiByZXN1bHQubWFwKChlOiBhbnkpID0+IHBhcnNlU2NoZW1hRWxlbWVudChlKSkuZmlsdGVyKChlKTogZSBpcyBTY2hlbWFFbGVtZW50ID0+IGUgIT09IG51bGwpO1xuICAgICAgICB9XG4gICAgICAgIHJldHVybiBbXTtcbiAgICB9XG59XG5cbi8qKlxuICogTWV0aG9kcyBmb3IgbWFuYWdpbmcgaW5kZXhlcy5cbiAqL1xuY2xhc3MgSW5kZXhNZXRob2RzIHtcbiAgICBjb25zdHJ1Y3Rvcihwcml2YXRlIGNsaWVudDogTGF0dGljZUNsaWVudCkge31cblxuICAgIC8qKlxuICAgICAqIEdldCBhbGwgaW5kZXggdGFibGUgbWFwcGluZ3MuXG4gICAgICovXG4gICAgYXN5bmMgZ2V0TWFwcGluZ3MoKTogUHJvbWlzZTxJbmRleFRhYmxlTWFwcGluZ1tdPiB7XG4gICAgICAgIGNvbnN0IHJlc3VsdCA9IGF3YWl0IHRoaXMuY2xpZW50LnJlcXVlc3Qoe1xuICAgICAgICAgICAgbWV0aG9kOiBcIkdFVFwiLFxuICAgICAgICAgICAgcGF0aDogXCIvdjEuMC90YWJsZXNcIlxuICAgICAgICB9KTtcblxuICAgICAgICBpZiAoQXJyYXkuaXNBcnJheShyZXN1bHQpKSB7XG4gICAgICAgICAgICByZXR1cm4gcmVzdWx0Lm1hcCgobTogYW55KSA9PiBwYXJzZUluZGV4VGFibGVNYXBwaW5nKG0pKS5maWx0ZXIoKG0pOiBtIGlzIEluZGV4VGFibGVNYXBwaW5nID0+IG0gIT09IG51bGwpO1xuICAgICAgICB9XG4gICAgICAgIHJldHVybiBbXTtcbiAgICB9XG59XG5cbiJdfQ==