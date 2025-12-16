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
                    }
                    else {
                        // Raw content response (not wrapped in standard envelope)
                        return {
                            success: response.ok,
                            statusCode: response.status,
                            data: responseData,
                            headers: Object.fromEntries(response.headers.entries()),
                            processingTimeMs: 0
                        };
                    }
                }
                catch {
                    return {
                        success: response.ok,
                        statusCode: response.status,
                        data: responseText,
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
        }
        catch (error) {
            if (error.name === "AbortError") {
                throw new exceptions_1.LatticeConnectionError(`Request to ${url} timed out`);
            }
            throw new exceptions_1.LatticeConnectionError(`Failed to connect to ${url}`, error);
        }
    }
    /**
     * Check if the Lattice server is healthy.
     */
    async healthCheck() {
        try {
            const response = await this.request({ method: "GET", path: "/v1.0/health" });
            return response.success;
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
        const response = await this.client.request({
            method: "PUT",
            path: "/v1.0/collections",
            data
        });
        if (response.success && response.data) {
            return (0, models_1.parseCollection)(response.data);
        }
        return null;
    }
    /**
     * Get all collections.
     */
    async readAll() {
        const response = await this.client.request({
            method: "GET",
            path: "/v1.0/collections"
        });
        if (response.success && response.data) {
            return response.data.map((c) => (0, models_1.parseCollection)(c)).filter((c) => c !== null);
        }
        return [];
    }
    /**
     * Get a collection by ID.
     */
    async readById(collectionId) {
        const response = await this.client.request({
            method: "GET",
            path: `/v1.0/collections/${collectionId}`
        });
        if (response.success && response.data) {
            return (0, models_1.parseCollection)(response.data);
        }
        return null;
    }
    /**
     * Check if a collection exists.
     */
    async exists(collectionId) {
        const response = await this.client.request({
            method: "HEAD",
            path: `/v1.0/collections/${collectionId}`
        });
        return response.success;
    }
    /**
     * Delete a collection.
     */
    async delete(collectionId) {
        const response = await this.client.request({
            method: "DELETE",
            path: `/v1.0/collections/${collectionId}`
        });
        return response.success;
    }
    /**
     * Get field constraints for a collection.
     */
    async getConstraints(collectionId) {
        const response = await this.client.request({
            method: "GET",
            path: `/v1.0/collections/${collectionId}/constraints`
        });
        if (response.success && response.data && response.data.fieldConstraints) {
            return response.data.fieldConstraints.map((c) => (0, models_1.parseFieldConstraint)(c)).filter((c) => c !== null);
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
    async getIndexedFields(collectionId) {
        const response = await this.client.request({
            method: "GET",
            path: `/v1.0/collections/${collectionId}/indexing`
        });
        if (response.success && response.data && response.data.indexedFields) {
            return response.data.indexedFields.map((f) => (0, models_1.parseIndexedField)(f)).filter((f) => f !== null);
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
    async rebuildIndexes(collectionId, dropUnusedIndexes = true) {
        const response = await this.client.request({
            method: "POST",
            path: `/v1.0/collections/${collectionId}/indexes/rebuild`,
            data: { dropUnusedIndexes }
        });
        if (response.success && response.data) {
            return (0, models_1.parseIndexRebuildResult)(response.data);
        }
        return null;
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
        const response = await this.client.request({
            method: "PUT",
            path: `/v1.0/collections/${options.collectionId}/documents`,
            data
        });
        if (response.success && response.data) {
            return (0, models_1.parseDocument)(response.data);
        }
        return null;
    }
    /**
     * Get all documents in a collection.
     */
    async readAllInCollection(collectionId, includeContent = false, includeLabels = true, includeTags = true) {
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
            return response.data.map((d) => (0, models_1.parseDocument)(d)).filter((d) => d !== null);
        }
        return [];
    }
    /**
     * Get a document by ID.
     */
    async readById(collectionId, documentId, includeContent = false, includeLabels = true, includeTags = true) {
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
            const doc = (0, models_1.parseDocument)(metadataResponse.data);
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
            return (0, models_1.parseDocument)(response.data);
        }
        return null;
    }
    /**
     * Check if a document exists.
     */
    async exists(collectionId, documentId) {
        const response = await this.client.request({
            method: "HEAD",
            path: `/v1.0/collections/${collectionId}/documents/${documentId}`
        });
        return response.success;
    }
    /**
     * Delete a document.
     */
    async delete(collectionId, documentId) {
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
    constructor(client) {
        this.client = client;
    }
    /**
     * Search for documents.
     */
    async search(query) {
        const response = await this.client.request({
            method: "POST",
            path: `/v1.0/collections/${query.collectionId}/documents/search`,
            data: (0, models_1.searchQueryToRequest)(query)
        });
        if (response.success && response.data) {
            return (0, models_1.parseSearchResult)(response.data);
        }
        return null;
    }
    /**
     * Search documents using a SQL-like expression.
     */
    async searchBySql(collectionId, sqlExpression) {
        const response = await this.client.request({
            method: "POST",
            path: `/v1.0/collections/${collectionId}/documents/search`,
            data: { sqlExpression }
        });
        if (response.success && response.data) {
            return (0, models_1.parseSearchResult)(response.data);
        }
        return null;
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
        const response = await this.client.request({
            method: "GET",
            path: "/v1.0/schemas"
        });
        if (response.success && response.data) {
            return response.data.map((s) => (0, models_1.parseSchema)(s)).filter((s) => s !== null);
        }
        return [];
    }
    /**
     * Get a schema by ID.
     */
    async readById(schemaId) {
        const response = await this.client.request({
            method: "GET",
            path: `/v1.0/schemas/${schemaId}`
        });
        if (response.success && response.data) {
            return (0, models_1.parseSchema)(response.data);
        }
        return null;
    }
    /**
     * Get elements for a schema.
     */
    async getElements(schemaId) {
        const response = await this.client.request({
            method: "GET",
            path: `/v1.0/schemas/${schemaId}/elements`
        });
        if (response.success && response.data) {
            return response.data.map((e) => (0, models_1.parseSchemaElement)(e)).filter((e) => e !== null);
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
        const response = await this.client.request({
            method: "GET",
            path: "/v1.0/tables"
        });
        if (response.success && response.data) {
            return response.data.map((m) => (0, models_1.parseIndexTableMapping)(m)).filter((m) => m !== null);
        }
        return [];
    }
}
//# sourceMappingURL=data:application/json;base64,eyJ2ZXJzaW9uIjozLCJmaWxlIjoiY2xpZW50LmpzIiwic291cmNlUm9vdCI6IiIsInNvdXJjZXMiOlsiLi4vc3JjL2NsaWVudC50cyJdLCJuYW1lcyI6W10sIm1hcHBpbmdzIjoiO0FBQUE7Ozs7R0FJRzs7O0FBRUgscUNBMkJrQjtBQUNsQiw2Q0FBcUY7QUFZckY7O0dBRUc7QUFDSCxNQUFhLGFBQWE7SUFVdEI7Ozs7O09BS0c7SUFDSCxZQUFZLE9BQWUsRUFBRSxVQUFrQixLQUFLO1FBQ2hELElBQUksQ0FBQyxPQUFPLEdBQUcsT0FBTyxDQUFDLE9BQU8sQ0FBQyxNQUFNLEVBQUUsRUFBRSxDQUFDLENBQUM7UUFDM0MsSUFBSSxDQUFDLE9BQU8sR0FBRyxPQUFPLENBQUM7UUFFdkIsSUFBSSxDQUFDLFVBQVUsR0FBRyxJQUFJLGlCQUFpQixDQUFDLElBQUksQ0FBQyxDQUFDO1FBQzlDLElBQUksQ0FBQyxRQUFRLEdBQUcsSUFBSSxlQUFlLENBQUMsSUFBSSxDQUFDLENBQUM7UUFDMUMsSUFBSSxDQUFDLE1BQU0sR0FBRyxJQUFJLGFBQWEsQ0FBQyxJQUFJLENBQUMsQ0FBQztRQUN0QyxJQUFJLENBQUMsTUFBTSxHQUFHLElBQUksYUFBYSxDQUFDLElBQUksQ0FBQyxDQUFDO1FBQ3RDLElBQUksQ0FBQyxLQUFLLEdBQUcsSUFBSSxZQUFZLENBQUMsSUFBSSxDQUFDLENBQUM7SUFDeEMsQ0FBQztJQUVEOztPQUVHO0lBQ0gsS0FBSyxDQUFDLE9BQU8sQ0FBVSxPQUF1QjtRQUMxQyxJQUFJLEdBQUcsR0FBRyxHQUFHLElBQUksQ0FBQyxPQUFPLEdBQUcsT0FBTyxDQUFDLElBQUksRUFBRSxDQUFDO1FBRTNDLElBQUksT0FBTyxDQUFDLE1BQU0sRUFBRSxDQUFDO1lBQ2pCLE1BQU0sWUFBWSxHQUFHLElBQUksZUFBZSxDQUFDLE9BQU8sQ0FBQyxNQUFNLENBQUMsQ0FBQztZQUN6RCxHQUFHLElBQUksSUFBSSxZQUFZLENBQUMsUUFBUSxFQUFFLEVBQUUsQ0FBQztRQUN6QyxDQUFDO1FBRUQsTUFBTSxZ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QUFHLENBQUM7SUFFN0M7O09BRUc7SUFDSCxLQUFLLENBQUMsV0FBVztRQUNiLE1BQU0sUUFBUSxHQUFHLE1BQU0sSUFBSSxDQUFDLE1BQU0sQ0FBQyxPQUFPLENBQUM7WUFDdkMsTUFBTSxFQUFFLEtBQUs7WUFDYixJQUFJLEVBQUUsY0FBYztTQUN2QixDQUFDLENBQUM7UUFFSCxJQUFJLFFBQVEsQ0FBQyxPQUFPLElBQUksUUFBUSxDQUFDLElBQUksRUFBRSxDQUFDO1lBQ3BDLE9BQU8sUUFBUSxDQUFDLElBQUksQ0FBQyxHQUFHLENBQUMsQ0FBQyxDQUFNLEVBQUUsRUFBRSxDQUFDLElBQUEsK0JBQXNCLEVBQUMsQ0FBQyxDQUFDLENBQUMsQ0FBQyxNQUFNLENBQUMsQ0FBQyxDQUFNLEVBQUUsRUFBRSxDQUFDLENBQUMsS0FBSyxJQUFJLENBQUMsQ0FBQztRQUNuRyxDQUFDO1FBQ0QsT0FBTyxFQUFFLENBQUM7SUFDZCxDQUFDO0NBQ0oiLCJzb3VyY2VzQ29udGVudCI6WyIvKipcbiAqIExhdHRpY2UgU0RLIENsaWVudFxuICpcbiAqIE1haW4gY2xpZW50IGZvciBpbnRlcmFjdGluZyB3aXRoIHRoZSBMYXR0aWNlIFJFU1QgQVBJLlxuICovXG5cbmltcG9ydCB7XG4gICAgQ29sbGVjdGlvbixcbiAgICBEb2N1bWVudCxcbiAgICBTY2hlbWEsXG4gICAgU2NoZW1hRWxlbWVudCxcbiAgICBGaWVsZENvbnN0cmFpbnQsXG4gICAgSW5kZXhlZEZpZWxkLFxuICAgIFNlYXJjaFJlc3VsdCxcbiAgICBJbmRleFJlYnVpbGRSZXN1bHQsXG4gICAgUmVzcG9uc2VDb250ZXh0LFxuICAgIFNlYXJjaFF1ZXJ5LFxuICAgIEluZGV4VGFibGVNYXBwaW5nLFxuICAgIFNjaGVtYUVuZm9yY2VtZW50TW9kZSxcbiAgICBJbmRleGluZ01vZGUsXG4gICAgQ3JlYXRlQ29sbGVjdGlvbk9wdGlvbnMsXG4gICAgSW5nZXN0RG9jdW1lbnRPcHRpb25zLFxuICAgIHBhcnNlQ29sbGVjdGlvbixcbiAgICBwYXJzZURvY3VtZW50LFxuICAgIHBhcnNlU2NoZW1hLFxuICAgIHBhcnNlU2NoZW1hRWxlbWVudCxcbiAgICBwYXJzZUZpZWxkQ29uc3RyYWludCxcbiAgICBwYXJzZUluZGV4ZWRGaWVsZCxcbiAgICBwYXJzZVNlYXJjaFJlc3VsdCxcbiAgICBwYXJzZUluZGV4UmVidWlsZFJlc3VsdCxcbiAgICBwYXJzZUluZGV4VGFibGVNYXBwaW5nLFxuICAgIGZpZWxkQ29uc3RyYWludFRvUmVxdWVzdCxcbiAgICBzZWFyY2hRdWVyeVRvUmVxdWVzdFxufSBmcm9tIFwiLi9tb2RlbHNcIjtcbmltcG9ydCB7IExhdHRpY2VFcnJvciwgTGF0dGljZUNvbm5lY3Rpb25FcnJvciwgTGF0dGljZUFwaUVycm9yIH0gZnJvbSBcIi4vZXhjZXB0aW9uc1wiO1xuXG4vKipcbiAqIEhUVFAgcmVxdWVzdCBvcHRpb25zLlxuICovXG5pbnRlcmZhY2UgUmVxdWVzdE9wdGlvbnMge1xuICAgIG1ldGhvZDogc3RyaW5nO1xuICAgIHBhdGg6IHN0cmluZztcbiAgICBkYXRhPzogYW55O1xuICAgIHBhcmFtcz86IFJlY29yZDxzdHJpbmcsIHN0cmluZz47XG59XG5cbi8qKlxuICogQ2xpZW50IGZvciBpbnRlcmFjdGluZyB3aXRoIHRoZSBMYXR0aWNlIFJFU1QgQVBJLlxuICovXG5leHBvcnQgY2xhc3MgTGF0dGljZUNsaWVudCB7XG4gICAgcHJpdmF0ZSBiYXNlVXJsOiBzdHJpbmc7XG4gICAgcHJpdmF0ZSB0aW1lb3V0OiBudW1iZXI7XG5cbiAgICBwdWJsaWMgY29sbGVjdGlvbjogQ29sbGVjdGlvbk1ldGhvZHM7XG4gICAgcHVibGljIGRvY3VtZW50OiBEb2N1bWVudE1ldGhvZHM7XG4gICAgcHVibGljIHNlYXJjaDogU2VhcmNoTWV0aG9kcztcbiAgICBwdWJsaWMgc2NoZW1hOiBTY2hlbWFNZXRob2RzO1xuICAgIHB1YmxpYyBpbmRleDogSW5kZXhNZXRob2RzO1xuXG4gICAgLyoqXG4gICAgICogSW5pdGlhbGl6ZSB0aGUgTGF0dGljZSBjbGllbnQuXG4gICAgICpcbiAgICAgKiBAcGFyYW0gYmFzZVVybCAtIFRoZSBiYXNlIFVSTCBvZiB0aGUgTGF0dGljZSBzZXJ2ZXIgKGUuZy4sIFwiaHR0cDovL2xvY2FsaG9zdDo4MDAwXCIpXG4gICAgICogQHBhcmFtIHRpbWVvdXQgLSBSZXF1ZXN0IHRpbWVvdXQgaW4gbWlsbGlzZWNvbmRzIChkZWZhdWx0OiAzMDAwMClcbiAgICAgKi9cbiAgICBjb25zdHJ1Y3RvcihiYXNlVXJsOiBzdHJpbmcsIHRpbWVvdXQ6IG51bWJlciA9IDMwMDAwKSB7XG4gICAgICAgIHRoaXMuYmFzZVVybCA9IGJhc2VVcmwucmVwbGFjZSgvXFwvKyQvLCBcIlwiKTtcbiAgICAgICAgdGhpcy50aW1lb3V0ID0gdGltZW91dDtcblxuICAgICAgICB0aGlzLmNvbGxlY3Rpb24gPSBuZXcgQ29sbGVjdGlvbk1ldGhvZHModGhpcyk7XG4gICAgICAgIHRoaXMuZG9jdW1lbnQgPSBuZXcgRG9jdW1lbnRNZXRob2RzKHRoaXMpO1xuICAgICAgICB0aGlzLnNlYXJjaCA9IG5ldyBTZWFyY2hNZXRob2RzKHRoaXMpO1xuICAgICAgICB0aGlzLnNjaGVtYSA9IG5ldyBTY2hlbWFNZXRob2RzKHRoaXMpO1xuICAgICAgICB0aGlzLmluZGV4ID0gbmV3IEluZGV4TWV0aG9kcyh0aGlzKTtcbiAgICB9XG5cbiAgICAvKipcbiAgICAgKiBNYWtlIGFuIEhUVFAgcmVxdWVzdCB0byB0aGUgTGF0dGljZSBBUEkuXG4gICAgICovXG4gICAgYXN5bmMgcmVxdWVzdDxUID0gYW55PihvcHRpb25zOiBSZXF1ZXN0T3B0aW9ucyk6IFByb21pc2U8UmVzcG9uc2VDb250ZXh0PFQ+PiB7XG4gICAgICAgIGxldCB1cmwgPSBgJHt0aGlzLmJhc2VVcmx9JHtvcHRpb25zLnBhdGh9YDtcblxuICAgICAgICBpZiAob3B0aW9ucy5wYXJhbXMpIHtcbiAgICAgICAgICAgIGNvbnN0IHNlYXJjaFBhcmFtcyA9IG5ldyBVUkxTZWFyY2hQYXJhbXMob3B0aW9ucy5wYXJhbXMpO1xuICAgICAgICAgICAgdXJsICs9IGA/JHtzZWFyY2hQYXJhbXMudG9TdHJpbmcoKX1gO1xuICAgICAgICB9XG5cbiAgICAgICAgY29uc3QgZmV0Y2hPcHRpb25zOiBSZXF1ZXN0SW5pdCA9IHtcbiAgICAgICAgICAgIG1ldGhvZDogb3B0aW9ucy5tZXRob2QsXG4gICAgICAgICAgICBoZWFkZXJzOiB7XG4gICAgICAgICAgICAgICAgXCJDb250ZW50LVR5cGVcIjogXCJhcHBsaWNhdGlvbi9qc29uXCJcbiAgICAgICAgICAgIH1cbiAgICAgICAgfTtcblxuICAgICAgICBpZiAob3B0aW9ucy5kYXRhICYmIChvcHRpb25zLm1ldGhvZCA9PT0gXCJQT1NUXCIgfHwgb3B0aW9ucy5tZXRob2QgPT09IFwiUFVUXCIpKSB7XG4gICAgICAgICAgICBmZXRjaE9wdGlvbnMuYm9keSA9IEpTT04uc3RyaW5naWZ5KG9wdGlvbnMuZGF0YSk7XG4gICAgICAgIH1cblxuICAgICAgICB0cnkge1xuICAgICAgICAgICAgY29uc3QgY29udHJvbGxlciA9IG5ldyBBYm9ydENvbnRyb2xsZXIoKTtcbiAgICAgICAgICAgIGNvbnN0IHRpbWVvdXRJZCA9IHNldFRpbWVvdXQoKCkgPT4gY29udHJvbGxlci5hYm9ydCgpLCB0aGlzLnRpbWVvdXQpO1xuICAgICAgICAgICAgZmV0Y2hPcHRpb25zLnNpZ25hbCA9IGNvbnRyb2xsZXIuc2lnbmFsO1xuXG4gICAgICAgICAgICBjb25zdCByZXNwb25zZSA9IGF3YWl0IGZldGNoKHVybCwgZmV0Y2hPcHRpb25zKTtcbiAgICAgICAgICAgIGNsZWFyVGltZW91dCh0aW1lb3V0SWQpO1xuXG4gICAgICAgICAgICAvLyBGb3IgSEVBRCByZXF1ZXN0cywgd2UgZG9uJ3QgaGF2ZSBhIGJvZHlcbiAgICAgICAgICAgIGlmIChvcHRpb25zLm1ldGhvZCA9PT0gXCJIRUFEXCIpIHtcbiAgICAgICAgICAgICAgICByZXR1cm4ge1xuICAgICAgICAgICAgICAgICAgICBzdWNjZXNzOiByZXNwb25zZS5zdGF0dXMgPT09IDIwMCxcbiAgICAgICAgICAgICAgICAgICAgc3RhdHVzQ29kZTogcmVzcG9uc2Uuc3RhdHVzLFxuICAgICAgICAgICAgICAgICAgICBoZWFkZXJzOiBPYmplY3QuZnJvbUVudHJpZXMocmVzcG9uc2UuaGVhZGVycy5lbnRyaWVzKCkpLFxuICAgICAgICAgICAgICAgICAgICBwcm9jZXNzaW5nVGltZU1zOiAwXG4gICAgICAgICAgICAgICAgfTtcbiAgICAgICAgICAgIH1cblxuICAgICAgICAgICAgY29uc3QgcmVzcG9uc2VUZXh0ID0gYXdhaXQgcmVzcG9uc2UudGV4dCgpO1xuXG4gICAgICAgICAgICBpZiAocmVzcG9uc2VUZXh0KSB7XG4gICAgICAgICAgICAgICAgdHJ5IHtcbiAgICAgICAgICAgICAgICAgICAgY29uc3QgcmVzcG9uc2VEYXRhID0gSlNPTi5wYXJzZShyZXNwb25zZVRleHQpO1xuXG4gICAgICAgICAgICAgICAgICAgIC8vIENoZWNrIGlmIHRoaXMgaXMgYSBzdGFuZGFyZCBBUEkgZW52ZWxvcGUgKGhhcyAnc3VjY2VzcycgcHJvcGVydHkpXG4gICAgICAgICAgICAgICAgICAgIC8vIG9yIHJhdyBjb250ZW50IChlLmcuLCB3aGVuIGluY2x1ZGVDb250ZW50PXRydWUgZm9yIGRvY3VtZW50IHJldHJpZXZhbClcbiAgICAgICAgICAgICAgICAgICAgaWYgKHJlc3BvbnNlRGF0YS5zdWNjZXNzICE9PSB1bmRlZmluZWQpIHtcbiAgICAgICAgICAgICAgICAgICAgICAgIC8vIFN0YW5kYXJkIGVudmVsb3BlIHJlc3BvbnNlXG4gICAgICAgICAgICAgICAgICAgICAgICByZXR1cm4ge1xuICAgICAgICAgICAgICAgICAgICAgICAgICAgIHN1Y2Nlc3M6IHJlc3BvbnNlRGF0YS5zdWNjZXNzLFxuICAgICAgICAgICAgICAgICAgICAgICAgICAgIHN0YXR1c0NvZGU6IHJlc3BvbnNlRGF0YS5zdGF0dXNDb2RlID8/IHJlc3BvbnNlLnN0YXR1cyxcbiAgICAgICAgICAgICAgICAgICAgICAgICAgICBlcnJvck1lc3NhZ2U6IHJlc3BvbnNlRGF0YS5lcnJvck1lc3NhZ2UsXG4gICAgICAgICAgICAgICAgICAgICAgICAgICAgZGF0YTogcmVzcG9uc2VEYXRhLmRhdGEsXG4gICAgICAgICAgICAgICAgICAgICAgICAgICAgaGVhZGVyczogT2JqZWN0LmZyb21FbnRyaWVzKHJlc3BvbnNlLmhlYWRlcnMuZW50cmllcygpKSxcbiAgICAgICAgICAgICAgICAgICAgICAgICAgICBwcm9jZXNzaW5nVGltZU1zOiByZXNwb25zZURhdGEucHJvY2Vzc2luZ1RpbWVNcyA/PyAwLFxuICAgICAgICAgICAgICAgICAgICAgICAgICAgIGd1aWQ6IHJlc3BvbnNlRGF0YS5ndWlkLFxuICAgICAgICAgICAgICAgICAgICAgICAgICAgIHRpbWVzdGFtcFV0YzogcmVzcG9uc2VEYXRhLnRpbWVzdGFtcFV0YyA/IG5ldyBEYXRlKHJlc3BvbnNlRGF0YS50aW1lc3RhbXBVdGMpIDogdW5kZWZpbmVkXG4gICAgICAgICAgICAgICAgICAgICAgICB9O1xuICAgICAgICAgICAgICAgICAgICB9IGVsc2Uge1xuICAgICAgICAgICAgICAgICAgICAgICAgLy8gUmF3IGNvbnRlbnQgcmVzcG9uc2UgKG5vdCB3cmFwcGVkIGluIHN0YW5kYXJkIGVudmVsb3BlKVxuICAgICAgICAgICAgICAgICAgICAgICAgcmV0dXJuIHtcbiAgICAgICAgICAgICAgICAgICAgICAgICAgICBzdWNjZXNzOiByZXNwb25zZS5vayxcbiAgICAgICAgICAgICAgICAgICAgICAgICAgICBzdGF0dXNDb2RlOiByZXNwb25zZS5zdGF0dXMsXG4gICAgICAgICAgICAgICAgICAgICAgICAgICAgZGF0YTogcmVzcG9uc2VEYXRhLFxuICAgICAgICAgICAgICAgICAgICAgICAgICAgIGhlYWRlcnM6IE9iamVjdC5mcm9tRW50cmllcyhyZXNwb25zZS5oZWFkZXJzLmVudHJpZXMoKSksXG4gICAgICAgICAgICAgICAgICAgICAgICAgICAgcHJvY2Vzc2luZ1RpbWVNczogMFxuICAgICAgICAgICAgICAgICAgICAgICAgfTtcbiAgICAgICAgICAgICAgICAgICAgfVxuICAgICAgICAgICAgICAgIH0gY2F0Y2gge1xuICAgICAgICAgICAgICAgICAgICByZXR1cm4ge1xuICAgICAgICAgICAgICAgICAgICAgICAgc3VjY2VzczogcmVzcG9uc2Uub2ssXG4gICAgICAgICAgICAgICAgICAgICAgICBzdGF0dXNDb2RlOiByZXNwb25zZS5zdGF0dXMsXG4gICAgICAgICAgICAgICAgICAgICAgICBkYXRhOiByZXNwb25zZVRleHQgYXMgYW55LFxuICAgICAgICAgICAgICAgICAgICAgICAgaGVhZGVyczogT2JqZWN0LmZyb21FbnRyaWVzKHJlc3BvbnNlLmhlYWRlcnMuZW50cmllcygpKSxcbiAgICAgICAgICAgICAgICAgICAgICAgIHByb2Nlc3NpbmdUaW1lTXM6IDBcbiAgICAgICAgICAgICAgICAgICAgfTtcbiAgICAgICAgICAgICAgICB9XG4gICAgICAgICAgICB9XG5cbiAgICAgICAgICAgIHJldHVybiB7XG4gICAgICAgICAgICAgICAgc3VjY2VzczogcmVzcG9uc2Uub2ssXG4gICAgICAgICAgICAgICAgc3RhdHVzQ29kZTogcmVzcG9uc2Uuc3RhdHVzLFxuICAgICAgICAgICAgICAgIGhlYWRlcnM6IE9iamVjdC5mcm9tRW50cmllcyhyZXNwb25zZS5oZWFkZXJzLmVudHJpZXMoKSksXG4gICAgICAgICAgICAgICAgcHJvY2Vzc2luZ1RpbWVNczogMFxuICAgICAgICAgICAgfTtcbiAgICAgICAgfSBjYXRjaCAoZXJyb3I6IGFueSkge1xuICAgICAgICAgICAgaWYgKGVycm9yLm5hbWUgPT09IFwiQWJvcnRFcnJvclwiKSB7XG4gICAgICAgICAgICAgICAgdGhyb3cgbmV3IExhdHRpY2VDb25uZWN0aW9uRXJyb3IoYFJlcXVlc3QgdG8gJHt1cmx9IHRpbWVkIG91dGApO1xuICAgICAgICAgICAgfVxuICAgICAgICAgICAgdGhyb3cgbmV3IExhdHRpY2VDb25uZWN0aW9uRXJyb3IoYEZhaWxlZCB0byBjb25uZWN0IHRvICR7dXJsfWAsIGVycm9yKTtcbiAgICAgICAgfVxuICAgIH1cblxuICAgIC8qKlxuICAgICAqIENoZWNrIGlmIHRoZSBMYXR0aWNlIHNlcnZlciBpcyBoZWFsdGh5LlxuICAgICAqL1xuICAgIGFzeW5jIGhlYWx0aENoZWNrKCk6IFByb21pc2U8Ym9vbGVhbj4ge1xuICAgICAgICB0cnkge1xuICAgICAgICAgICAgY29uc3QgcmVzcG9uc2UgPSBhd2FpdCB0aGlzLnJlcXVlc3QoeyBtZXRob2Q6IFwiR0VUXCIsIHBhdGg6IFwiL3YxLjAvaGVhbHRoXCIgfSk7XG4gICAgICAgICAgICByZXR1cm4gcmVzcG9uc2Uuc3VjY2VzcztcbiAgICAgICAgfSBjYXRjaCB7XG4gICAgICAgICAgICByZXR1cm4gZmFsc2U7XG4gICAgICAgIH1cbiAgICB9XG59XG5cbi8qKlxuICogTWV0aG9kcyBmb3IgbWFuYWdpbmcgY29sbGVjdGlvbnMuXG4gKi9cbmNsYXNzIENvbGxlY3Rpb25NZXRob2RzIHtcbiAgICBjb25zdHJ1Y3Rvcihwcml2YXRlIGNsaWVudDogTGF0dGljZUNsaWVudCkge31cblxuICAgIC8qKlxuICAgICAqIENyZWF0ZSBhIG5ldyBjb2xsZWN0aW9uLlxuICAgICAqL1xuICAgIGFzeW5jIGNyZWF0ZShvcHRpb25zOiBDcmVhdGVDb2xsZWN0aW9uT3B0aW9ucyk6IFByb21pc2U8Q29sbGVjdGlvbiB8IG51bGw+IHtcbiAgICAgICAgY29uc3QgZGF0YTogYW55ID0geyBuYW1lOiBvcHRpb25zLm5hbWUgfTtcblxuICAgICAgICBpZiAob3B0aW9ucy5kZXNjcmlwdGlvbikgZGF0YS5kZXNjcmlwdGlvbiA9IG9wdGlvbnMuZGVzY3JpcHRpb247XG4gICAgICAgIGlmIChvcHRpb25zLmRvY3VtZW50c0RpcmVjdG9yeSkgZGF0YS5kb2N1bWVudHNEaXJlY3RvcnkgPSBvcHRpb25zLmRvY3VtZW50c0RpcmVjdG9yeTtcbiAgICAgICAgaWYgKG9wdGlvbnMubGFiZWxzKSBkYXRhLmxhYmVscyA9IG9wdGlvbnMubGFiZWxzO1xuICAgICAgICBpZiAob3B0aW9ucy50YWdzKSBkYXRhLnRhZ3MgPSBvcHRpb25zLnRhZ3M7XG4gICAgICAgIGlmIChvcHRpb25zLnNjaGVtYUVuZm9yY2VtZW50TW9kZSAhPT0gdW5kZWZpbmVkICYmIG9wdGlvbnMuc2NoZW1hRW5mb3JjZW1lbnRNb2RlICE9PSBTY2hlbWFFbmZvcmNlbWVudE1vZGUuTm9uZSkge1xuICAgICAgICAgICAgZGF0YS5zY2hlbWFFbmZvcmNlbWVudE1vZGUgPSBvcHRpb25zLnNjaGVtYUVuZm9yY2VtZW50TW9kZTtcbiAgICAgICAgfVxuICAgICAgICBpZiAob3B0aW9ucy5maWVsZENvbnN0cmFpbnRzKSB7XG4gICAgICAgICAgICBkYXRhLmZpZWxkQ29uc3RyYWludHMgPSBvcHRpb25zLmZpZWxkQ29uc3RyYWludHMubWFwKGZpZWxkQ29uc3RyYWludFRvUmVxdWVzdCk7XG4gICAgICAgIH1cbiAgICAgICAgaWYgKG9wdGlvbnMuaW5kZXhpbmdNb2RlICE9PSB1bmRlZmluZWQgJiYgb3B0aW9ucy5pbmRleGluZ01vZGUgIT09IEluZGV4aW5nTW9kZS5BbGwpIHtcbiAgICAgICAgICAgIGRhdGEuaW5kZXhpbmdNb2RlID0gb3B0aW9ucy5pbmRleGluZ01vZGU7XG4gICAgICAgIH1cbiAgICAgICAgaWYgKG9wdGlvbnMuaW5kZXhlZEZpZWxkcykgZGF0YS5pbmRleGVkRmllbGRzID0gb3B0aW9ucy5pbmRleGVkRmllbGRzO1xuXG4gICAgICAgIGNvbnN0IHJlc3BvbnNlID0gYXdhaXQgdGhpcy5jbGllbnQucmVxdWVzdCh7XG4gICAgICAgICAgICBtZXRob2Q6IFwiUFVUXCIsXG4gICAgICAgICAgICBwYXRoOiBcIi92MS4wL2NvbGxlY3Rpb25zXCIsXG4gICAgICAgICAgICBkYXRhXG4gICAgICAgIH0pO1xuXG4gICAgICAgIGlmIChyZXNwb25zZS5zdWNjZXNzICYmIHJlc3BvbnNlLmRhdGEpIHtcbiAgICAgICAgICAgIHJldHVybiBwYXJzZUNvbGxlY3Rpb24ocmVzcG9uc2UuZGF0YSk7XG4gICAgICAgIH1cbiAgICAgICAgcmV0dXJuIG51bGw7XG4gICAgfVxuXG4gICAgLyoqXG4gICAgICogR2V0IGFsbCBjb2xsZWN0aW9ucy5cbiAgICAgKi9cbiAgICBhc3luYyByZWFkQWxsKCk6IFByb21pc2U8Q29sbGVjdGlvbltdPiB7XG4gICAgICAgIGNvbnN0IHJlc3BvbnNlID0gYXdhaXQgdGhpcy5jbGllbnQucmVxdWVzdCh7XG4gICAgICAgICAgICBtZXRob2Q6IFwiR0VUXCIsXG4gICAgICAgICAgICBwYXRoOiBcIi92MS4wL2NvbGxlY3Rpb25zXCJcbiAgICAgICAgfSk7XG5cbiAgICAgICAgaWYgKHJlc3BvbnNlLnN1Y2Nlc3MgJiYgcmVzcG9uc2UuZGF0YSkge1xuICAgICAgICAgICAgcmV0dXJuIHJlc3BvbnNlLmRhdGEubWFwKChjOiBhbnkpID0+IHBhcnNlQ29sbGVjdGlvbihjKSkuZmlsdGVyKChjOiBhbnkpID0+IGMgIT09IG51bGwpO1xuICAgICAgICB9XG4gICAgICAgIHJldHVybiBbXTtcbiAgICB9XG5cbiAgICAvKipcbiAgICAgKiBHZXQgYSBjb2xsZWN0aW9uIGJ5IElELlxuICAgICAqL1xuICAgIGFzeW5jIHJlYWRCeUlkKGNvbGxlY3Rpb25JZDogc3RyaW5nKTogUHJvbWlzZTxDb2xsZWN0aW9uIHwgbnVsbD4ge1xuICAgICAgICBjb25zdCByZXNwb25zZSA9IGF3YWl0IHRoaXMuY2xpZW50LnJlcXVlc3Qoe1xuICAgICAgICAgICAgbWV0aG9kOiBcIkdFVFwiLFxuICAgICAgICAgICAgcGF0aDogYC92MS4wL2NvbGxlY3Rpb25zLyR7Y29sbGVjdGlvbklkfWBcbiAgICAgICAgfSk7XG5cbiAgICAgICAgaWYgKHJlc3BvbnNlLnN1Y2Nlc3MgJiYgcmVzcG9uc2UuZGF0YSkge1xuICAgICAgICAgICAgcmV0dXJuIHBhcnNlQ29sbGVjdGlvbihyZXNwb25zZS5kYXRhKTtcbiAgICAgICAgfVxuICAgICAgICByZXR1cm4gbnVsbDtcbiAgICB9XG5cbiAgICAvKipcbiAgICAgKiBDaGVjayBpZiBhIGNvbGxlY3Rpb24gZXhpc3RzLlxuICAgICAqL1xuICAgIGFzeW5jIGV4aXN0cyhjb2xsZWN0aW9uSWQ6IHN0cmluZyk6IFByb21pc2U8Ym9vbGVhbj4ge1xuICAgICAgICBjb25zdCByZXNwb25zZSA9IGF3YWl0IHRoaXMuY2xpZW50LnJlcXVlc3Qoe1xuICAgICAgICAgICAgbWV0aG9kOiBcIkhFQURcIixcbiAgICAgICAgICAgIHBhdGg6IGAvdjEuMC9jb2xsZWN0aW9ucy8ke2NvbGxlY3Rpb25JZH1gXG4gICAgICAgIH0pO1xuICAgICAgICByZXR1cm4gcmVzcG9uc2Uuc3VjY2VzcztcbiAgICB9XG5cbiAgICAvKipcbiAgICAgKiBEZWxldGUgYSBjb2xsZWN0aW9uLlxuICAgICAqL1xuICAgIGFzeW5jIGRlbGV0ZShjb2xsZWN0aW9uSWQ6IHN0cmluZyk6IFByb21pc2U8Ym9vbGVhbj4ge1xuICAgICAgICBjb25zdCByZXNwb25zZSA9IGF3YWl0IHRoaXMuY2xpZW50LnJlcXVlc3Qoe1xuICAgICAgICAgICAgbWV0aG9kOiBcIkRFTEVURVwiLFxuICAgICAgICAgICAgcGF0aDogYC92MS4wL2NvbGxlY3Rpb25zLyR7Y29sbGVjdGlvbklkfWBcbiAgICAgICAgfSk7XG4gICAgICAgIHJldHVybiByZXNwb25zZS5zdWNjZXNzO1xuICAgIH1cblxuICAgIC8qKlxuICAgICAqIEdldCBmaWVsZCBjb25zdHJhaW50cyBmb3IgYSBjb2xsZWN0aW9uLlxuICAgICAqL1xuICAgIGFzeW5jIGdldENvbnN0cmFpbnRzKGNvbGxlY3Rpb25JZDogc3RyaW5nKTogUHJvbWlzZTxGaWVsZENvbnN0cmFpbnRbXT4ge1xuICAgICAgICBjb25zdCByZXNwb25zZSA9IGF3YWl0IHRoaXMuY2xpZW50LnJlcXVlc3Qoe1xuICAgICAgICAgICAgbWV0aG9kOiBcIkdFVFwiLFxuICAgICAgICAgICAgcGF0aDogYC92MS4wL2NvbGxlY3Rpb25zLyR7Y29sbGVjdGlvbklkfS9jb25zdHJhaW50c2BcbiAgICAgICAgfSk7XG5cbiAgICAgICAgaWYgKHJlc3BvbnNlLnN1Y2Nlc3MgJiYgcmVzcG9uc2UuZGF0YSAmJiByZXNwb25zZS5kYXRhLmZpZWxkQ29uc3RyYWludHMpIHtcbiAgICAgICAgICAgIHJldHVybiByZXNwb25zZS5kYXRhLmZpZWxkQ29uc3RyYWludHMubWFwKChjOiBhbnkpID0+IHBhcnNlRmllbGRDb25zdHJhaW50KGMpKS5maWx0ZXIoKGM6IGFueSkgPT4gYyAhPT0gbnVsbCk7XG4gICAgICAgIH1cbiAgICAgICAgcmV0dXJuIFtdO1xuICAgIH1cblxuICAgIC8qKlxuICAgICAqIFVwZGF0ZSBjb25zdHJhaW50cyBmb3IgYSBjb2xsZWN0aW9uLlxuICAgICAqL1xuICAgIGFzeW5jIHVwZGF0ZUNvbnN0cmFpbnRzKFxuICAgICAgICBjb2xsZWN0aW9uSWQ6IHN0cmluZyxcbiAgICAgICAgc2NoZW1hRW5mb3JjZW1lbnRNb2RlOiBTY2hlbWFFbmZvcmNlbWVudE1vZGUsXG4gICAgICAgIGZpZWxkQ29uc3RyYWludHM/OiBGaWVsZENvbnN0cmFpbnRbXVxuICAgICk6IFByb21pc2U8Ym9vbGVhbj4ge1xuICAgICAgICBjb25zdCBkYXRhOiBhbnkgPSB7IHNjaGVtYUVuZm9yY2VtZW50TW9kZSB9O1xuICAgICAgICBpZiAoZmllbGRDb25zdHJhaW50cykge1xuICAgICAgICAgICAgZGF0YS5maWVsZENvbnN0cmFpbnRzID0gZmllbGRDb25zdHJhaW50cy5tYXAoZmllbGRDb25zdHJhaW50VG9SZXF1ZXN0KTtcbiAgICAgICAgfVxuXG4gICAgICAgIGNvbnN0IHJlc3BvbnNlID0gYXdhaXQgdGhpcy5jbGllbnQucmVxdWVzdCh7XG4gICAgICAgICAgICBtZXRob2Q6IFwiUFVUXCIsXG4gICAgICAgICAgICBwYXRoOiBgL3YxLjAvY29sbGVjdGlvbnMvJHtjb2xsZWN0aW9uSWR9L2NvbnN0cmFpbnRzYCxcbiAgICAgICAgICAgIGRhdGFcbiAgICAgICAgfSk7XG4gICAgICAgIHJldHVybiByZXNwb25zZS5zdWNjZXNzO1xuICAgIH1cblxuICAgIC8qKlxuICAgICAqIEdldCBpbmRleGVkIGZpZWxkcyBmb3IgYSBjb2xsZWN0aW9uLlxuICAgICAqL1xuICAgIGFzeW5jIGdldEluZGV4ZWRGaWVsZHMoY29sbGVjdGlvbklkOiBzdHJpbmcpOiBQcm9taXNlPEluZGV4ZWRGaWVsZFtdPiB7XG4gICAgICAgIGNvbnN0IHJlc3BvbnNlID0gYXdhaXQgdGhpcy5jbGllbnQucmVxdWVzdCh7XG4gICAgICAgICAgICBtZXRob2Q6IFwiR0VUXCIsXG4gICAgICAgICAgICBwYXRoOiBgL3YxLjAvY29sbGVjdGlvbnMvJHtjb2xsZWN0aW9uSWR9L2luZGV4aW5nYFxuICAgICAgICB9KTtcblxuICAgICAgICBpZiAocmVzcG9uc2Uuc3VjY2VzcyAmJiByZXNwb25zZS5kYXRhICYmIHJlc3BvbnNlLmRhdGEuaW5kZXhlZEZpZWxkcykge1xuICAgICAgICAgICAgcmV0dXJuIHJlc3BvbnNlLmRhdGEuaW5kZXhlZEZpZWxkcy5tYXAoKGY6IGFueSkgPT4gcGFyc2VJbmRleGVkRmllbGQoZikpLmZpbHRlcigoZjogYW55KSA9PiBmICE9PSBudWxsKTtcbiAgICAgICAgfVxuICAgICAgICByZXR1cm4gW107XG4gICAgfVxuXG4gICAgLyoqXG4gICAgICogVXBkYXRlIGluZGV4aW5nIGNvbmZpZ3VyYXRpb24gZm9yIGEgY29sbGVjdGlvbi5cbiAgICAgKi9cbiAgICBhc3luYyB1cGRhdGVJbmRleGluZyhcbiAgICAgICAgY29sbGVjdGlvbklkOiBzdHJpbmcsXG4gICAgICAgIGluZGV4aW5nTW9kZTogSW5kZXhpbmdNb2RlLFxuICAgICAgICBpbmRleGVkRmllbGRzPzogc3RyaW5nW10sXG4gICAgICAgIHJlYnVpbGRJbmRleGVzOiBib29sZWFuID0gZmFsc2VcbiAgICApOiBQcm9taXNlPGJvb2xlYW4+IHtcbiAgICAgICAgY29uc3QgZGF0YTogYW55ID0ge1xuICAgICAgICAgICAgaW5kZXhpbmdNb2RlLFxuICAgICAgICAgICAgcmVidWlsZEluZGV4ZXNcbiAgICAgICAgfTtcbiAgICAgICAgaWYgKGluZGV4ZWRGaWVsZHMpIGRhdGEuaW5kZXhlZEZpZWxkcyA9IGluZGV4ZWRGaWVsZHM7XG5cbiAgICAgICAgY29uc3QgcmVzcG9uc2UgPSBhd2FpdCB0aGlzLmNsaWVudC5yZXF1ZXN0KHtcbiAgICAgICAgICAgIG1ldGhvZDogXCJQVVRcIixcbiAgICAgICAgICAgIHBhdGg6IGAvdjEuMC9jb2xsZWN0aW9ucy8ke2NvbGxlY3Rpb25JZH0vaW5kZXhpbmdgLFxuICAgICAgICAgICAgZGF0YVxuICAgICAgICB9KTtcbiAgICAgICAgcmV0dXJuIHJlc3BvbnNlLnN1Y2Nlc3M7XG4gICAgfVxuXG4gICAgLyoqXG4gICAgICogUmVidWlsZCBpbmRleGVzIGZvciBhIGNvbGxlY3Rpb24uXG4gICAgICovXG4gICAgYXN5bmMgcmVidWlsZEluZGV4ZXMoXG4gICAgICAgIGNvbGxlY3Rpb25JZDogc3RyaW5nLFxuICAgICAgICBkcm9wVW51c2VkSW5kZXhlczogYm9vbGVhbiA9IHRydWVcbiAgICApOiBQcm9taXNlPEluZGV4UmVidWlsZFJlc3VsdCB8IG51bGw+IHtcbiAgICAgICAgY29uc3QgcmVzcG9uc2UgPSBhd2FpdCB0aGlzLmNsaWVudC5yZXF1ZXN0KHtcbiAgICAgICAgICAgIG1ldGhvZDogXCJQT1NUXCIsXG4gICAgICAgICAgICBwYXRoOiBgL3YxLjAvY29sbGVjdGlvbnMvJHtjb2xsZWN0aW9uSWR9L2luZGV4ZXMvcmVidWlsZGAsXG4gICAgICAgICAgICBkYXRhOiB7IGRyb3BVbnVzZWRJbmRleGVzIH1cbiAgICAgICAgfSk7XG5cbiAgICAgICAgaWYgKHJlc3BvbnNlLnN1Y2Nlc3MgJiYgcmVzcG9uc2UuZGF0YSkge1xuICAgICAgICAgICAgcmV0dXJuIHBhcnNlSW5kZXhSZWJ1aWxkUmVzdWx0KHJlc3BvbnNlLmRhdGEpO1xuICAgICAgICB9XG4gICAgICAgIHJldHVybiBudWxsO1xuICAgIH1cbn1cblxuLyoqXG4gKiBNZXRob2RzIGZvciBtYW5hZ2luZyBkb2N1bWVudHMuXG4gKi9cbmNsYXNzIERvY3VtZW50TWV0aG9kcyB7XG4gICAgY29uc3RydWN0b3IocHJpdmF0ZSBjbGllbnQ6IExhdHRpY2VDbGllbnQpIHt9XG5cbiAgICAvKipcbiAgICAgKiBJbmdlc3QgYSBuZXcgZG9jdW1lbnQgaW50byBhIGNvbGxlY3Rpb24uXG4gICAgICovXG4gICAgYXN5bmMgaW5nZXN0KG9wdGlvbnM6IEluZ2VzdERvY3VtZW50T3B0aW9ucyk6IFByb21pc2U8RG9jdW1lbnQgfCBudWxsPiB7XG4gICAgICAgIGNvbnN0IGRhdGE6IGFueSA9IHsgY29udGVudDogb3B0aW9ucy5jb250ZW50IH07XG5cbiAgICAgICAgaWYgKG9wdGlvbnMubmFtZSkgZGF0YS5uYW1lID0gb3B0aW9ucy5uYW1lO1xuICAgICAgICBpZiAob3B0aW9ucy5sYWJlbHMpIGRhdGEubGFiZWxzID0gb3B0aW9ucy5sYWJlbHM7XG4gICAgICAgIGlmIChvcHRpb25zLnRhZ3MpIGRhdGEudGFncyA9IG9wdGlvbnMudGFncztcblxuICAgICAgICBjb25zdCByZXNwb25zZSA9IGF3YWl0IHRoaXMuY2xpZW50LnJlcXVlc3Qoe1xuICAgICAgICAgICAgbWV0aG9kOiBcIlBVVFwiLFxuICAgICAgICAgICAgcGF0aDogYC92MS4wL2NvbGxlY3Rpb25zLyR7b3B0aW9ucy5jb2xsZWN0aW9uSWR9L2RvY3VtZW50c2AsXG4gICAgICAgICAgICBkYXRhXG4gICAgICAgIH0pO1xuXG4gICAgICAgIGlmIChyZXNwb25zZS5zdWNjZXNzICYmIHJlc3BvbnNlLmRhdGEpIHtcbiAgICAgICAgICAgIHJldHVybiBwYXJzZURvY3VtZW50KHJlc3BvbnNlLmRhdGEpO1xuICAgICAgICB9XG4gICAgICAgIHJldHVybiBudWxsO1xuICAgIH1cblxuICAgIC8qKlxuICAgICAqIEdldCBhbGwgZG9jdW1lbnRzIGluIGEgY29sbGVjdGlvbi5cbiAgICAgKi9cbiAgICBhc3luYyByZWFkQWxsSW5Db2xsZWN0aW9uKFxuICAgICAgICBjb2xsZWN0aW9uSWQ6IHN0cmluZyxcbiAgICAgICAgaW5jbHVkZUNvbnRlbnQ6IGJvb2xlYW4gPSBmYWxzZSxcbiAgICAgICAgaW5jbHVkZUxhYmVsczogYm9vbGVhbiA9IHRydWUsXG4gICAgICAgIGluY2x1ZGVUYWdzOiBib29sZWFuID0gdHJ1ZVxuICAgICk6IFByb21pc2U8RG9jdW1lbnRbXT4ge1xuICAgICAgICBjb25zdCByZXNwb25zZSA9IGF3YWl0IHRoaXMuY2xpZW50LnJlcXVlc3Qoe1xuICAgICAgICAgICAgbWV0aG9kOiBcIkdFVFwiLFxuICAgICAgICAgICAgcGF0aDogYC92MS4wL2NvbGxlY3Rpb25zLyR7Y29sbGVjdGlvbklkfS9kb2N1bWVudHNgLFxuICAgICAgICAgICAgcGFyYW1zOiB7XG4gICAgICAgICAgICAgICAgaW5jbHVkZUNvbnRlbnQ6IFN0cmluZyhpbmNsdWRlQ29udGVudCksXG4gICAgICAgICAgICAgICAgaW5jbHVkZUxhYmVsczogU3RyaW5nKGluY2x1ZGVMYWJlbHMpLFxuICAgICAgICAgICAgICAgIGluY2x1ZGVUYWdzOiBTdHJpbmcoaW5jbHVkZVRhZ3MpXG4gICAgICAgICAgICB9XG4gICAgICAgIH0pO1xuXG4gICAgICAgIGlmIChyZXNwb25zZS5zdWNjZXNzICYmIHJlc3BvbnNlLmRhdGEpIHtcbiAgICAgICAgICAgIHJldHVybiByZXNwb25zZS5kYXRhLm1hcCgoZDogYW55KSA9PiBwYXJzZURvY3VtZW50KGQpKS5maWx0ZXIoKGQ6IGFueSkgPT4gZCAhPT0gbnVsbCk7XG4gICAgICAgIH1cbiAgICAgICAgcmV0dXJuIFtdO1xuICAgIH1cblxuICAgIC8qKlxuICAgICAqIEdldCBhIGRvY3VtZW50IGJ5IElELlxuICAgICAqL1xuICAgIGFzeW5jIHJlYWRCeUlkKFxuICAgICAgICBjb2xsZWN0aW9uSWQ6IHN0cmluZyxcbiAgICAgICAgZG9jdW1lbnRJZDogc3RyaW5nLFxuICAgICAgICBpbmNsdWRlQ29udGVudDogYm9vbGVhbiA9IGZhbHNlLFxuICAgICAgICBpbmNsdWRlTGFiZWxzOiBib29sZWFuID0gdHJ1ZSxcbiAgICAgICAgaW5jbHVkZVRhZ3M6IGJvb2xlYW4gPSB0cnVlXG4gICAgKTogUHJvbWlzZTxEb2N1bWVudCB8IG51bGw+IHtcbiAgICAgICAgaWYgKGluY2x1ZGVDb250ZW50KSB7XG4gICAgICAgICAgICAvLyBXaGVuIGluY2x1ZGVDb250ZW50PXRydWUsIHRoZSBzZXJ2ZXIgcmV0dXJucyBPTkxZIHRoZSByYXcgZG9jdW1lbnQgYm9keSxcbiAgICAgICAgICAgIC8vIG5vdCB3cmFwcGVkIGluIHRoZSBzdGFuZGFyZCBBUEkgZW52ZWxvcGUuIFdlIG5lZWQgdG8gbWFrZSB0d28gcmVxdWVzdHM6XG4gICAgICAgICAgICAvLyAxLiBHZXQgZG9jdW1lbnQgbWV0YWRhdGEgKHdpdGhvdXQgY29udGVudClcbiAgICAgICAgICAgIC8vIDIuIEdldCByYXcgY29udGVudCBzZXBhcmF0ZWx5XG4gICAgICAgICAgICAvLyBUaGVuIGNvbWJpbmUgdGhlbS5cblxuICAgICAgICAgICAgLy8gRmlyc3QsIGdldCBkb2N1bWVudCBtZXRhZGF0YVxuICAgICAgICAgICAgY29uc3QgbWV0YWRhdGFSZXNwb25zZSA9IGF3YWl0IHRoaXMuY2xpZW50LnJlcXVlc3Qoe1xuICAgICAgICAgICAgICAgIG1ldGhvZDogXCJHRVRcIixcbiAgICAgICAgICAgICAgICBwYXRoOiBgL3YxLjAvY29sbGVjdGlvbnMvJHtjb2xsZWN0aW9uSWR9L2RvY3VtZW50cy8ke2RvY3VtZW50SWR9YCxcbiAgICAgICAgICAgICAgICBwYXJhbXM6IHtcbiAgICAgICAgICAgICAgICAgICAgaW5jbHVkZUNvbnRlbnQ6IFwiZmFsc2VcIixcbiAgICAgICAgICAgICAgICAgICAgaW5jbHVkZUxhYmVsczogU3RyaW5nKGluY2x1ZGVMYWJlbHMpLFxuICAgICAgICAgICAgICAgICAgICBpbmNsdWRlVGFnczogU3RyaW5nKGluY2x1ZGVUYWdzKVxuICAgICAgICAgICAgICAgIH1cbiAgICAgICAgICAgIH0pO1xuXG4gICAgICAgICAgICBpZiAoIW1ldGFkYXRhUmVzcG9uc2Uuc3VjY2VzcyB8fCAhbWV0YWRhdGFSZXNwb25zZS5kYXRhKSB7XG4gICAgICAgICAgICAgICAgcmV0dXJuIG51bGw7XG4gICAgICAgICAgICB9XG5cbiAgICAgICAgICAgIGNvbnN0IGRvYyA9IHBhcnNlRG9jdW1lbnQobWV0YWRhdGFSZXNwb25zZS5kYXRhKTtcbiAgICAgICAgICAgIGlmICghZG9jKSB7XG4gICAgICAgICAgICAgICAgcmV0dXJuIG51bGw7XG4gICAgICAgICAgICB9XG5cbiAgICAgICAgICAgIC8vIE5vdyBnZXQgdGhlIHJhdyBjb250ZW50XG4gICAgICAgICAgICBjb25zdCBjb250ZW50UmVzcG9uc2UgPSBhd2FpdCB0aGlzLmNsaWVudC5yZXF1ZXN0KHtcbiAgICAgICAgICAgICAgICBtZXRob2Q6IFwiR0VUXCIsXG4gICAgICAgICAgICAgICAgcGF0aDogYC92MS4wL2NvbGxlY3Rpb25zLyR7Y29sbGVjdGlvbklkfS9kb2N1bWVudHMvJHtkb2N1bWVudElkfWAsXG4gICAgICAgICAgICAgICAgcGFyYW1zOiB7XG4gICAgICAgICAgICAgICAgICAgIGluY2x1ZGVDb250ZW50OiBcInRydWVcIixcbiAgICAgICAgICAgICAgICAgICAgaW5jbHVkZUxhYmVsczogXCJmYWxzZVwiLFxuICAgICAgICAgICAgICAgICAgICBpbmNsdWRlVGFnczogXCJmYWxzZVwiXG4gICAgICAgICAgICAgICAgfVxuICAgICAgICAgICAgfSk7XG5cbiAgICAgICAgICAgIGlmIChjb250ZW50UmVzcG9uc2Uuc3VjY2VzcyAmJiBjb250ZW50UmVzcG9uc2UuZGF0YSAhPT0gdW5kZWZpbmVkKSB7XG4gICAgICAgICAgICAgICAgZG9jLmNvbnRlbnQgPSBjb250ZW50UmVzcG9uc2UuZGF0YTtcbiAgICAgICAgICAgIH1cblxuICAgICAgICAgICAgcmV0dXJuIGRvYztcbiAgICAgICAgfVxuXG4gICAgICAgIC8vIE5vcm1hbCBmbG93IHdoZW4gaW5jbHVkZUNvbnRlbnQ9ZmFsc2VcbiAgICAgICAgY29uc3QgcmVzcG9uc2UgPSBhd2FpdCB0aGlzLmNsaWVudC5yZXF1ZXN0KHtcbiAgICAgICAgICAgIG1ldGhvZDogXCJHRVRcIixcbiAgICAgICAgICAgIHBhdGg6IGAvdjEuMC9jb2xsZWN0aW9ucy8ke2NvbGxlY3Rpb25JZH0vZG9jdW1lbnRzLyR7ZG9jdW1lbnRJZH1gLFxuICAgICAgICAgICAgcGFyYW1zOiB7XG4gICAgICAgICAgICAgICAgaW5jbHVkZUNvbnRlbnQ6IFwiZmFsc2VcIixcbiAgICAgICAgICAgICAgICBpbmNsdWRlTGFiZWxzOiBTdHJpbmcoaW5jbHVkZUxhYmVscyksXG4gICAgICAgICAgICAgICAgaW5jbHVkZVRhZ3M6IFN0cmluZyhpbmNsdWRlVGFncylcbiAgICAgICAgICAgIH1cbiAgICAgICAgfSk7XG5cbiAgICAgICAgaWYgKHJlc3BvbnNlLnN1Y2Nlc3MgJiYgcmVzcG9uc2UuZGF0YSkge1xuICAgICAgICAgICAgcmV0dXJuIHBhcnNlRG9jdW1lbnQocmVzcG9uc2UuZGF0YSk7XG4gICAgICAgIH1cbiAgICAgICAgcmV0dXJuIG51bGw7XG4gICAgfVxuXG4gICAgLyoqXG4gICAgICogQ2hlY2sgaWYgYSBkb2N1bWVudCBleGlzdHMuXG4gICAgICovXG4gICAgYXN5bmMgZXhpc3RzKGNvbGxlY3Rpb25JZDogc3RyaW5nLCBkb2N1bWVudElkOiBzdHJpbmcpOiBQcm9taXNlPGJvb2xlYW4+IHtcbiAgICAgICAgY29uc3QgcmVzcG9uc2UgPSBhd2FpdCB0aGlzLmNsaWVudC5yZXF1ZXN0KHtcbiAgICAgICAgICAgIG1ldGhvZDogXCJIRUFEXCIsXG4gICAgICAgICAgICBwYXRoOiBgL3YxLjAvY29sbGVjdGlvbnMvJHtjb2xsZWN0aW9uSWR9L2RvY3VtZW50cy8ke2RvY3VtZW50SWR9YFxuICAgICAgICB9KTtcbiAgICAgICAgcmV0dXJuIHJlc3BvbnNlLnN1Y2Nlc3M7XG4gICAgfVxuXG4gICAgLyoqXG4gICAgICogRGVsZXRlIGEgZG9jdW1lbnQuXG4gICAgICovXG4gICAgYXN5bmMgZGVsZXRlKGNvbGxlY3Rpb25JZDogc3RyaW5nLCBkb2N1bWVudElkOiBzdHJpbmcpOiBQcm9taXNlPGJvb2xlYW4+IHtcbiAgICAgICAgY29uc3QgcmVzcG9uc2UgPSBhd2FpdCB0aGlzLmNsaWVudC5yZXF1ZXN0KHtcbiAgICAgICAgICAgIG1ldGhvZDogXCJERUxFVEVcIixcbiAgICAgICAgICAgIHBhdGg6IGAvdjEuMC9jb2xsZWN0aW9ucy8ke2NvbGxlY3Rpb25JZH0vZG9jdW1lbnRzLyR7ZG9jdW1lbnRJZH1gXG4gICAgICAgIH0pO1xuICAgICAgICByZXR1cm4gcmVzcG9uc2Uuc3VjY2VzcztcbiAgICB9XG59XG5cbi8qKlxuICogTWV0aG9kcyBmb3Igc2VhcmNoaW5nIGRvY3VtZW50cy5cbiAqL1xuY2xhc3MgU2VhcmNoTWV0aG9kcyB7XG4gICAgY29uc3RydWN0b3IocHJpdmF0ZSBjbGllbnQ6IExhdHRpY2VDbGllbnQpIHt9XG5cbiAgICAvKipcbiAgICAgKiBTZWFyY2ggZm9yIGRvY3VtZW50cy5cbiAgICAgKi9cbiAgICBhc3luYyBzZWFyY2gocXVlcnk6IFNlYXJjaFF1ZXJ5KTogUHJvbWlzZTxTZWFyY2hSZXN1bHQgfCBudWxsPiB7XG4gICAgICAgIGNvbnN0IHJlc3BvbnNlID0gYXdhaXQgdGhpcy5jbGllbnQucmVxdWVzdCh7XG4gICAgICAgICAgICBtZXRob2Q6IFwiUE9TVFwiLFxuICAgICAgICAgICAgcGF0aDogYC92MS4wL2NvbGxlY3Rpb25zLyR7cXVlcnkuY29sbGVjdGlvbklkfS9kb2N1bWVudHMvc2VhcmNoYCxcbiAgICAgICAgICAgIGRhdGE6IHNlYXJjaFF1ZXJ5VG9SZXF1ZXN0KHF1ZXJ5KVxuICAgICAgICB9KTtcblxuICAgICAgICBpZiAocmVzcG9uc2Uuc3VjY2VzcyAmJiByZXNwb25zZS5kYXRhKSB7XG4gICAgICAgICAgICByZXR1cm4gcGFyc2VTZWFyY2hSZXN1bHQocmVzcG9uc2UuZGF0YSk7XG4gICAgICAgIH1cbiAgICAgICAgcmV0dXJuIG51bGw7XG4gICAgfVxuXG4gICAgLyoqXG4gICAgICogU2VhcmNoIGRvY3VtZW50cyB1c2luZyBhIFNRTC1saWtlIGV4cHJlc3Npb24uXG4gICAgICovXG4gICAgYXN5bmMgc2VhcmNoQnlTcWwoY29sbGVjdGlvbklkOiBzdHJpbmcsIHNxbEV4cHJlc3Npb246IHN0cmluZyk6IFByb21pc2U8U2VhcmNoUmVzdWx0IHwgbnVsbD4ge1xuICAgICAgICBjb25zdCByZXNwb25zZSA9IGF3YWl0IHRoaXMuY2xpZW50LnJlcXVlc3Qoe1xuICAgICAgICAgICAgbWV0aG9kOiBcIlBPU1RcIixcbiAgICAgICAgICAgIHBhdGg6IGAvdjEuMC9jb2xsZWN0aW9ucy8ke2NvbGxlY3Rpb25JZH0vZG9jdW1lbnRzL3NlYXJjaGAsXG4gICAgICAgICAgICBkYXRhOiB7IHNxbEV4cHJlc3Npb24gfVxuICAgICAgICB9KTtcblxuICAgICAgICBpZiAocmVzcG9uc2Uuc3VjY2VzcyAmJiByZXNwb25zZS5kYXRhKSB7XG4gICAgICAgICAgICByZXR1cm4gcGFyc2VTZWFyY2hSZXN1bHQocmVzcG9uc2UuZGF0YSk7XG4gICAgICAgIH1cbiAgICAgICAgcmV0dXJuIG51bGw7XG4gICAgfVxuXG4gICAgLyoqXG4gICAgICogRW51bWVyYXRlIGRvY3VtZW50cyBpbiBhIGNvbGxlY3Rpb24uXG4gICAgICovXG4gICAgYXN5bmMgZW51bWVyYXRlKHF1ZXJ5OiBTZWFyY2hRdWVyeSk6IFByb21pc2U8U2VhcmNoUmVzdWx0IHwgbnVsbD4ge1xuICAgICAgICByZXR1cm4gdGhpcy5zZWFyY2gocXVlcnkpO1xuICAgIH1cbn1cblxuLyoqXG4gKiBNZXRob2RzIGZvciBtYW5hZ2luZyBzY2hlbWFzLlxuICovXG5jbGFzcyBTY2hlbWFNZXRob2RzIHtcbiAgICBjb25zdHJ1Y3Rvcihwcml2YXRlIGNsaWVudDogTGF0dGljZUNsaWVudCkge31cblxuICAgIC8qKlxuICAgICAqIEdldCBhbGwgc2NoZW1hcy5cbiAgICAgKi9cbiAgICBhc3luYyByZWFkQWxsKCk6IFByb21pc2U8U2NoZW1hW10+IHtcbiAgICAgICAgY29uc3QgcmVzcG9uc2UgPSBhd2FpdCB0aGlzLmNsaWVudC5yZXF1ZXN0KHtcbiAgICAgICAgICAgIG1ldGhvZDogXCJHRVRcIixcbiAgICAgICAgICAgIHBhdGg6IFwiL3YxLjAvc2NoZW1hc1wiXG4gICAgICAgIH0pO1xuXG4gICAgICAgIGlmIChyZXNwb25zZS5zdWNjZXNzICYmIHJlc3BvbnNlLmRhdGEpIHtcbiAgICAgICAgICAgIHJldHVybiByZXNwb25zZS5kYXRhLm1hcCgoczogYW55KSA9PiBwYXJzZVNjaGVtYShzKSkuZmlsdGVyKChzOiBhbnkpID0+IHMgIT09IG51bGwpO1xuICAgICAgICB9XG4gICAgICAgIHJldHVybiBbXTtcbiAgICB9XG5cbiAgICAvKipcbiAgICAgKiBHZXQgYSBzY2hlbWEgYnkgSUQuXG4gICAgICovXG4gICAgYXN5bmMgcmVhZEJ5SWQoc2NoZW1hSWQ6IHN0cmluZyk6IFByb21pc2U8U2NoZW1hIHwgbnVsbD4ge1xuICAgICAgICBjb25zdCByZXNwb25zZSA9IGF3YWl0IHRoaXMuY2xpZW50LnJlcXVlc3Qoe1xuICAgICAgICAgICAgbWV0aG9kOiBcIkdFVFwiLFxuICAgICAgICAgICAgcGF0aDogYC92MS4wL3NjaGVtYXMvJHtzY2hlbWFJZH1gXG4gICAgICAgIH0pO1xuXG4gICAgICAgIGlmIChyZXNwb25zZS5zdWNjZXNzICYmIHJlc3BvbnNlLmRhdGEpIHtcbiAgICAgICAgICAgIHJldHVybiBwYXJzZVNjaGVtYShyZXNwb25zZS5kYXRhKTtcbiAgICAgICAgfVxuICAgICAgICByZXR1cm4gbnVsbDtcbiAgICB9XG5cbiAgICAvKipcbiAgICAgKiBHZXQgZWxlbWVudHMgZm9yIGEgc2NoZW1hLlxuICAgICAqL1xuICAgIGFzeW5jIGdldEVsZW1lbnRzKHNjaGVtYUlkOiBzdHJpbmcpOiBQcm9taXNlPFNjaGVtYUVsZW1lbnRbXT4ge1xuICAgICAgICBjb25zdCByZXNwb25zZSA9IGF3YWl0IHRoaXMuY2xpZW50LnJlcXVlc3Qoe1xuICAgICAgICAgICAgbWV0aG9kOiBcIkdFVFwiLFxuICAgICAgICAgICAgcGF0aDogYC92MS4wL3NjaGVtYXMvJHtzY2hlbWFJZH0vZWxlbWVudHNgXG4gICAgICAgIH0pO1xuXG4gICAgICAgIGlmIChyZXNwb25zZS5zdWNjZXNzICYmIHJlc3BvbnNlLmRhdGEpIHtcbiAgICAgICAgICAgIHJldHVybiByZXNwb25zZS5kYXRhLm1hcCgoZTogYW55KSA9PiBwYXJzZVNjaGVtYUVsZW1lbnQoZSkpLmZpbHRlcigoZTogYW55KSA9PiBlICE9PSBudWxsKTtcbiAgICAgICAgfVxuICAgICAgICByZXR1cm4gW107XG4gICAgfVxufVxuXG4vKipcbiAqIE1ldGhvZHMgZm9yIG1hbmFnaW5nIGluZGV4ZXMuXG4gKi9cbmNsYXNzIEluZGV4TWV0aG9kcyB7XG4gICAgY29uc3RydWN0b3IocHJpdmF0ZSBjbGllbnQ6IExhdHRpY2VDbGllbnQpIHt9XG5cbiAgICAvKipcbiAgICAgKiBHZXQgYWxsIGluZGV4IHRhYmxlIG1hcHBpbmdzLlxuICAgICAqL1xuICAgIGFzeW5jIGdldE1hcHBpbmdzKCk6IFByb21pc2U8SW5kZXhUYWJsZU1hcHBpbmdbXT4ge1xuICAgICAgICBjb25zdCByZXNwb25zZSA9IGF3YWl0IHRoaXMuY2xpZW50LnJlcXVlc3Qoe1xuICAgICAgICAgICAgbWV0aG9kOiBcIkdFVFwiLFxuICAgICAgICAgICAgcGF0aDogXCIvdjEuMC90YWJsZXNcIlxuICAgICAgICB9KTtcblxuICAgICAgICBpZiAocmVzcG9uc2Uuc3VjY2VzcyAmJiByZXNwb25zZS5kYXRhKSB7XG4gICAgICAgICAgICByZXR1cm4gcmVzcG9uc2UuZGF0YS5tYXAoKG06IGFueSkgPT4gcGFyc2VJbmRleFRhYmxlTWFwcGluZyhtKSkuZmlsdGVyKChtOiBhbnkpID0+IG0gIT09IG51bGwpO1xuICAgICAgICB9XG4gICAgICAgIHJldHVybiBbXTtcbiAgICB9XG59XG5cbiJdfQ==