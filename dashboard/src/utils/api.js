/**
 * Lattice API client
 */
export class LatticeApi {
  constructor(baseUrl, token = null) {
    this.baseUrl = baseUrl.replace(/\/$/, '')
    this.token = token || null
    // Optional callback invoked whenever a request returns HTTP 401. Lets the
    // app clear auth state and return the user to the login screen.
    this.onUnauthorized = null
  }

  setToken(token) {
    this.token = token || null
  }

  buildUrl(path, query = null) {
    const normalizedPath = path.startsWith('/') ? path : `/${path}`
    const url = new URL(`${this.baseUrl}${normalizedPath}`)

    if (query) {
      Object.entries(query).forEach(([key, value]) => {
        if (value === null || value === undefined || value === '') return
        if (Array.isArray(value)) {
          value.forEach((entry) => {
            if (entry !== null && entry !== undefined && entry !== '') {
              url.searchParams.append(key, String(entry))
            }
          })
          return
        }

        url.searchParams.set(key, String(value))
      })
    }

    return url.toString()
  }

  async requestRaw(method, path, options = {}) {
    const {
      body = null,
      headers = {},
      query = null,
      signal = null,
      contentType = 'application/json',
    } = options

    const url = this.buildUrl(path, query)
    const requestHeaders = { ...headers }

    // Attach the bearer token unless an explicit Authorization header was passed.
    if (this.token && !Object.keys(requestHeaders).some((key) => key.toLowerCase() === 'authorization')) {
      requestHeaders['Authorization'] = `Bearer ${this.token}`
    }

    const fetchOptions = {
      method,
      headers: requestHeaders,
      signal,
    }

    if (body !== null && body !== undefined && body !== '') {
      if (!Object.keys(requestHeaders).some((key) => key.toLowerCase() === 'content-type') && contentType) {
        requestHeaders['Content-Type'] = contentType
      }

      if (typeof body === 'string') {
        fetchOptions.body = body
      } else if ((requestHeaders['Content-Type'] || requestHeaders['content-type'] || '').includes('application/json')) {
        fetchOptions.body = JSON.stringify(body)
      } else {
        fetchOptions.body = body
      }
    }

    const startedAt = performance.now()
    const response = await fetch(url, fetchOptions)
    const durationMs = performance.now() - startedAt

    // Surface 401s to the app so it can clear the token and re-prompt for login.
    // The handler only mutates local auth state (no further API calls), so this
    // cannot loop.
    if (response.status === 401 && typeof this.onUnauthorized === 'function') {
      this.onUnauthorized()
    }

    let text = ''
    if (response.status !== 204 && method !== 'HEAD') {
      text = await response.text()
    }

    let json = null
    if (text) {
      try {
        json = JSON.parse(text)
      } catch {
        json = null
      }
    }

    return {
      ok: response.ok,
      status: response.status,
      statusText: response.statusText,
      headers: Object.fromEntries(response.headers.entries()),
      requestId: response.headers.get('x-lattice-request-id'),
      contentType: response.headers.get('content-type') || '',
      text,
      json,
      durationMs,
      url,
    }
  }

  async request(method, path, body = null, options = {}) {
    const response = await this.requestRaw(method, path, { ...options, body })

    // Error (non-2xx): body is { error, detail? }. Fall back to status text
    // if the body isn't JSON. Surface status/detail/requestId on the error.
    if (!response.ok) {
      const errorBody = response.json
      const message =
        (errorBody && typeof errorBody === 'object' && errorBody.error) ||
        response.statusText ||
        `HTTP ${response.status}`
      const error = new Error(message)
      error.status = response.status
      error.requestId = response.requestId
      if (errorBody && typeof errorBody === 'object' && errorBody.detail !== undefined) {
        error.detail = errorBody.detail
      }
      throw error
    }

    // Success (2xx): the body IS the payload directly (no more `.data` unwrap).
    // Empty body (e.g. HEAD, 204, some DELETEs) → null; never JSON.parse('').
    if (response.json !== null && response.json !== undefined) {
      return response.json
    }

    if (response.text) {
      return response.text
    }

    return null
  }

  // Authentication
  // POST /v1.0/token with { email, password } and an OPTIONAL tenantId. When
  // tenantId is falsy/empty it is omitted so the server can infer the tenant
  // from the credentials. The response is either a session token payload or a
  // { tenantSelectionRequired: true, tenants: [...] } prompt (both HTTP 200).
  async login(email, password, tenantId) {
    const body = { email, password }
    if (tenantId) {
      body.tenantId = tenantId
    }
    return this.request('POST', '/v1.0/token', body)
  }

  // GET /v1.0/whoami → current principal descriptor.
  async whoami() {
    return this.request('GET', '/v1.0/whoami')
  }

  // DELETE /v1.0/token → revoke the current session.
  async logout() {
    return this.request('DELETE', '/v1.0/token')
  }

  // GET /v1.0/health → unauthenticated reachability check.
  async health() {
    return this.request('GET', '/v1.0/health')
  }

  // Collections
  // Returns an EnumerationResult<Collection>: { ...pagination, objects: Collection[] }.
  async getCollections({ maxResults, skip } = {}) {
    return this.request('GET', '/v1.0/collections', null, { query: { maxResults, skip } })
  }

  async createCollection(data) {
    return this.request('PUT', '/v1.0/collections', data)
  }

  async getCollection(id) {
    return this.request('GET', `/v1.0/collections/${id}`)
  }

  async updateCollection(id, data) {
    return this.request('PUT', `/v1.0/collections/${id}`, data)
  }

  async deleteCollection(id) {
    return this.request('DELETE', `/v1.0/collections/${id}`)
  }

  async collectionExists(id) {
    try {
      await this.request('HEAD', `/v1.0/collections/${id}`)
      return true
    } catch {
      return false
    }
  }

  // Documents
  // Returns an EnumerationResult<Document>: { ...pagination, objects: Document[] }.
  async getDocuments(collectionId, { maxResults, skip } = {}) {
    return this.request('GET', `/v1.0/collections/${collectionId}/documents`, null, {
      query: { maxResults, skip },
    })
  }

  async createDocument(collectionId, data) {
    return this.request('PUT', `/v1.0/collections/${collectionId}/documents`, data)
  }

  async getDocument(collectionId, id) {
    return this.request('GET', `/v1.0/collections/${collectionId}/documents/${id}`)
  }

  async getDocumentContent(collectionId, id) {
    return this.request('GET', `/v1.0/collections/${collectionId}/documents/${id}`, null, {
      query: { includeContent: true },
    })
  }

  async deleteDocument(collectionId, id) {
    return this.request('DELETE', `/v1.0/collections/${collectionId}/documents/${id}`)
  }

  // Search
  async searchDocuments(collectionId, searchRequest) {
    return this.request('POST', `/v1.0/collections/${collectionId}/documents/search`, searchRequest)
  }

  // Schemas
  // Returns an EnumerationResult<Schema>: { ...pagination, objects: Schema[] }.
  async getSchemas({ maxResults, skip } = {}) {
    return this.request('GET', '/v1.0/schemas', null, { query: { maxResults, skip } })
  }

  async getSchema(id) {
    return this.request('GET', `/v1.0/schemas/${id}`)
  }

  // Returns an EnumerationResult<SchemaElement>: { ...pagination, objects: SchemaElement[] }.
  async getSchemaElements(schemaId, { maxResults, skip } = {}) {
    return this.request('GET', `/v1.0/schemas/${schemaId}/elements`, null, {
      query: { maxResults, skip },
    })
  }

  // Index Tables
  // Returns an EnumerationResult<IndexTableMapping>: { ...pagination, objects: IndexTableMapping[] }.
  async getIndexTables({ maxResults, skip } = {}) {
    return this.request('GET', '/v1.0/tables', null, { query: { maxResults, skip } })
  }

  // Returns an EnumerationResult<IndexTableEntry>: { ...pagination, objects: IndexTableEntry[] }.
  async getTableEntries(tableName, skip = 0, maxResults = 100) {
    return this.request('GET', `/v1.0/tables/${encodeURIComponent(tableName)}/entries`, null, {
      query: { skip, maxResults },
    })
  }

  // Schema Constraints
  async getCollectionConstraints(collectionId) {
    return this.request('GET', `/v1.0/collections/${collectionId}/constraints`)
  }

  async updateCollectionConstraints(collectionId, data) {
    return this.request('PUT', `/v1.0/collections/${collectionId}/constraints`, data)
  }

  // Indexing Configuration
  async getCollectionIndexedFields(collectionId) {
    return this.request('GET', `/v1.0/collections/${collectionId}/indexing`)
  }

  async updateCollectionIndexing(collectionId, data) {
    return this.request('PUT', `/v1.0/collections/${collectionId}/indexing`, data)
  }

  // Index Rebuild
  async rebuildIndexes(collectionId, options = {}) {
    return this.request('POST', `/v1.0/collections/${collectionId}/indexes/rebuild`, options)
  }

  // Diagnostics / OpenAPI
  async getOpenApiSpec() {
    const response = await this.requestRaw('GET', '/openapi.json')
    if (!response.ok || !response.json) {
      throw new Error(response.text || `HTTP ${response.status}`)
    }
    return response.json
  }

  async searchRequestHistory(params = {}) {
    return this.request('GET', '/v1.0/requesthistory', null, { query: params })
  }

  async getRequestHistoryEntry(requestId) {
    return this.request('GET', `/v1.0/requesthistory/${requestId}`)
  }

  async getRequestHistoryDetail(requestId) {
    return this.request('GET', `/v1.0/requesthistory/${requestId}/detail`)
  }

  async getRequestHistorySummary(params = {}) {
    return this.request('GET', '/v1.0/requesthistory/summary', null, { query: params })
  }

  async deleteRequestHistoryEntry(requestId) {
    return this.request('DELETE', `/v1.0/requesthistory/${requestId}`)
  }

  async bulkDeleteRequestHistory(filter = {}) {
    return this.request('DELETE', '/v1.0/requesthistory/bulk', filter)
  }

  // Tenants
  // Returns an EnumerationResult<Tenant>: { ...pagination, objects: Tenant[] }.
  async getTenants({ maxResults, skip } = {}) {
    return this.request('GET', '/v1.0/tenants', null, { query: { maxResults, skip } })
  }

  async createTenant(data) {
    return this.request('PUT', '/v1.0/tenants', data)
  }

  async getTenant(id) {
    return this.request('GET', `/v1.0/tenants/${id}`)
  }

  async updateTenant(id, data) {
    return this.request('PUT', `/v1.0/tenants/${id}`, data)
  }

  async deleteTenant(id) {
    return this.request('DELETE', `/v1.0/tenants/${id}`)
  }

  // Users
  // Returns an EnumerationResult<User>: { ...pagination, objects: User[] }.
  async getUsers({ tenantId, maxResults, skip } = {}) {
    return this.request('GET', '/v1.0/users', null, { query: { tenantId, maxResults, skip } })
  }

  async createUser(data) {
    return this.request('PUT', '/v1.0/users', data)
  }

  async getUser(id) {
    return this.request('GET', `/v1.0/users/${id}`)
  }

  async updateUser(id, data) {
    return this.request('PUT', `/v1.0/users/${id}`, data)
  }

  async deleteUser(id) {
    return this.request('DELETE', `/v1.0/users/${id}`)
  }

  // Credentials
  // Returns an EnumerationResult<Credential>: { ...pagination, objects: Credential[] }.
  async getCredentials({ tenantId, maxResults, skip } = {}) {
    return this.request('GET', '/v1.0/credentials', null, { query: { tenantId, maxResults, skip } })
  }

  // The create response returns the full access key exactly once (accessKey).
  async createCredential(data) {
    return this.request('PUT', '/v1.0/credentials', data)
  }

  async getCredential(id) {
    return this.request('GET', `/v1.0/credentials/${id}`)
  }

  async updateCredential(id, data) {
    return this.request('PUT', `/v1.0/credentials/${id}`, data)
  }

  async deleteCredential(id) {
    return this.request('DELETE', `/v1.0/credentials/${id}`)
  }

  // Roles
  // Returns an EnumerationResult<Role>: { ...pagination, objects: Role[] }.
  async getRoles({ maxResults, skip } = {}) {
    return this.request('GET', '/v1.0/roles', null, { query: { maxResults, skip } })
  }

  // Create a custom role. Body { name, permissions: [ { permissionType, resourceTypes, operationTypes } ] }.
  // Returns the created role including its permissions. 409 if the name already exists.
  async createRole(data) {
    return this.request('PUT', '/v1.0/roles', data)
  }

  // Returns the role including its permissions array.
  async getRole(id) {
    return this.request('GET', `/v1.0/roles/${id}`)
  }

  // Update a custom role. Body { name?, permissions? } renames and/or replaces grants. 409 for built-in roles.
  async updateRole(id, data) {
    return this.request('PUT', `/v1.0/roles/${id}`, data)
  }

  // Delete a custom role. 409 for built-in roles.
  async deleteRole(id) {
    return this.request('DELETE', `/v1.0/roles/${id}`)
  }

  // Role Assignments
  // Returns an EnumerationResult<Assignment>: { ...pagination, objects: Assignment[] }.
  async getAssignments({ tenantId, maxResults, skip } = {}) {
    return this.request('GET', '/v1.0/assignments', null, { query: { tenantId, maxResults, skip } })
  }

  async createAssignment(data) {
    return this.request('PUT', '/v1.0/assignments', data)
  }

  async deleteAssignment(id) {
    return this.request('DELETE', `/v1.0/assignments/${id}`)
  }

  // Audit Log
  // Returns an EnumerationResult<AuditEntry>: { ...pagination, objects: AuditEntry[] }.
  async getAudit({ eventType, tenantId, maxResults, skip } = {}) {
    return this.request('GET', '/v1.0/audit', null, {
      query: { eventType, tenantId, maxResults, skip },
    })
  }

  async getAuditEntry(id) {
    return this.request('GET', `/v1.0/audit/${id}`)
  }

  async deleteAuditEntry(id) {
    return this.request('DELETE', `/v1.0/audit/${id}`)
  }
}

/**
 * Convert keys from camelCase to PascalCase
 */
export function toPascalCase(obj) {
  if (Array.isArray(obj)) {
    return obj.map(toPascalCase)
  }
  if (obj !== null && typeof obj === 'object') {
    return Object.keys(obj).reduce((result, key) => {
      const pascalKey = key.charAt(0).toUpperCase() + key.slice(1)
      result[pascalKey] = toPascalCase(obj[key])
      return result
    }, {})
  }
  return obj
}

/**
 * Convert keys from PascalCase to camelCase
 */
export function toCamelCase(obj) {
  if (Array.isArray(obj)) {
    return obj.map(toCamelCase)
  }
  if (obj !== null && typeof obj === 'object') {
    return Object.keys(obj).reduce((result, key) => {
      const camelKey = key.charAt(0).toLowerCase() + key.slice(1)
      result[camelKey] = toCamelCase(obj[key])
      return result
    }, {})
  }
  return obj
}

/**
 * Format a date string
 */
export function formatDate(dateString) {
  if (!dateString) return '-'
  const date = new Date(dateString)
  return date.toLocaleDateString() + ' ' + date.toLocaleTimeString()
}
