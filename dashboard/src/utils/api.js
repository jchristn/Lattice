/**
 * Lattice API client
 */
export class LatticeApi {
  constructor(baseUrl) {
    this.baseUrl = baseUrl.replace(/\/$/, '')
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

    if (response.status === 204 || method === 'HEAD') {
      if (!response.ok) {
        throw new Error(`HTTP ${response.status}`)
      }
      return null
    }

    const responseData = response.json

    // Handle WatsonWebserver ResponseContext wrapper
    if (responseData && typeof responseData === 'object' && 'success' in responseData) {
      if (!responseData.success) {
        throw new Error(responseData.errorMessage || `HTTP ${response.status}`)
      }
      return responseData.data
    }

    if (!response.ok) {
      throw new Error(responseData?.error || response.text || `HTTP ${response.status}`)
    }

    if (responseData !== null) {
      return responseData
    }

    return response.text
  }

  // Collections
  async getCollections() {
    return this.request('GET', '/v1.0/collections')
  }

  async createCollection(data) {
    return this.request('PUT', '/v1.0/collections', data)
  }

  async getCollection(id) {
    return this.request('GET', `/v1.0/collections/${id}`)
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
  async getDocuments(collectionId) {
    return this.request('GET', `/v1.0/collections/${collectionId}/documents`)
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
  async getSchemas() {
    return this.request('GET', '/v1.0/schemas')
  }

  async getSchema(id) {
    return this.request('GET', `/v1.0/schemas/${id}`)
  }

  async getSchemaElements(schemaId) {
    return this.request('GET', `/v1.0/schemas/${schemaId}/elements`)
  }

  // Index Tables
  async getIndexTables() {
    return this.request('GET', '/v1.0/tables')
  }

  async getTableEntries(tableName, skip = 0, limit = 100) {
    return this.request('GET', `/v1.0/tables/${encodeURIComponent(tableName)}/entries`, null, {
      query: { skip, limit },
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
