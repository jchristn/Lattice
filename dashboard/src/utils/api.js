/**
 * Lattice API client
 */
export class LatticeApi {
  constructor(baseUrl) {
    this.baseUrl = baseUrl.replace(/\/$/, '')
  }

  async request(method, path, body = null) {
    const url = `${this.baseUrl}${path}`
    const options = {
      method,
      headers: {
        'Content-Type': 'application/json',
      },
    }

    if (body) {
      options.body = JSON.stringify(body)
    }

    const response = await fetch(url, options)

    // For HEAD requests or 204 No Content
    if (response.status === 204 || method === 'HEAD') {
      if (!response.ok) {
        throw new Error(`HTTP ${response.status}`)
      }
      return null
    }

    const responseData = await response.json()

    // Handle WatsonWebserver ResponseContext wrapper
    if (responseData && typeof responseData === 'object' && 'success' in responseData) {
      if (!responseData.success) {
        throw new Error(responseData.errorMessage || `HTTP ${response.status}`)
      }
      return responseData.data
    }

    if (!response.ok) {
      throw new Error(responseData.error || `HTTP ${response.status}`)
    }

    return responseData
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
    // This endpoint returns raw JSON content directly (not wrapped)
    const url = `${this.baseUrl}/v1.0/collections/${collectionId}/documents/${id}?includeContent=true`
    const response = await fetch(url, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
      },
    })

    if (!response.ok) {
      const errorData = await response.json()
      throw new Error(errorData.error || `HTTP ${response.status}`)
    }

    return response.json()
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
