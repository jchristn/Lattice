import { useState, useEffect, useMemo } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useApp } from '../context/AppContext'
import { formatDate } from '../utils/api'

// Format bytes to human-readable string
function formatBytes(bytes) {
  if (bytes === 0) return '0 B'
  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i]
}
import Modal from '../components/Modal'
import ActionMenu from '../components/ActionMenu'
import CopyableId from '../components/CopyableId'
import TagInput from '../components/TagInput'
import KeyValueEditor from '../components/KeyValueEditor'
import './Documents.css'

export default function Documents() {
  const { collectionId } = useParams()
  const navigate = useNavigate()
  const { api, setError } = useApp()
  const [collections, setCollections] = useState([])
  const [collection, setCollection] = useState(null)
  const [documents, setDocuments] = useState([])
  const [loading, setLoading] = useState(true)
  const [showCreateModal, setShowCreateModal] = useState(false)
  const [showMetadataModal, setShowMetadataModal] = useState(false)
  const [showDataModal, setShowDataModal] = useState(false)
  const [selectedDocument, setSelectedDocument] = useState(null)
  const [newDocument, setNewDocument] = useState({
    name: '',
    content: '{\n  \n}',
  })
  const [newLabels, setNewLabels] = useState([])
  const [newTags, setNewTags] = useState({})

  // Per-column filters
  const [filters, setFilters] = useState({
    id: '',
    name: '',
    contentLength: '',
    schemaId: '',
    createdUtc: '',
  })

  // Sorting state
  const [sort, setSort] = useState({ column: 'name', direction: 'asc' })

  // Filter and sort documents
  const filteredDocuments = useMemo(() => {
    let result = [...documents]

    // Apply column filters
    if (filters.id) {
      const query = filters.id.toLowerCase()
      result = result.filter(d => d.id.toLowerCase().includes(query))
    }
    if (filters.name) {
      const query = filters.name.toLowerCase()
      result = result.filter(d => (d.name || '').toLowerCase().includes(query))
    }
    if (filters.contentLength) {
      const query = filters.contentLength.toLowerCase()
      result = result.filter(d => formatBytes(d.contentLength || 0).toLowerCase().includes(query))
    }
    if (filters.schemaId) {
      const query = filters.schemaId.toLowerCase()
      result = result.filter(d => (d.schemaId || '').toLowerCase().includes(query))
    }
    if (filters.createdUtc) {
      const query = filters.createdUtc.toLowerCase()
      result = result.filter(d => formatDate(d.createdUtc).toLowerCase().includes(query))
    }

    // Apply sorting
    if (sort.column) {
      result.sort((a, b) => {
        let aVal, bVal
        if (sort.column === 'createdUtc') {
          aVal = a.createdUtc || ''
          bVal = b.createdUtc || ''
          const comparison = aVal.localeCompare(bVal)
          return sort.direction === 'asc' ? comparison : -comparison
        } else if (sort.column === 'contentLength') {
          aVal = a.contentLength || 0
          bVal = b.contentLength || 0
          const comparison = aVal - bVal
          return sort.direction === 'asc' ? comparison : -comparison
        } else {
          aVal = a[sort.column] || ''
          bVal = b[sort.column] || ''
          const comparison = aVal.localeCompare(bVal)
          return sort.direction === 'asc' ? comparison : -comparison
        }
      })
    }

    return result
  }, [documents, filters, sort])

  useEffect(() => {
    const loadCollections = async () => {
      try {
        const data = await api.getCollections()
        setCollections(data || [])
        // Auto-select first collection if none selected
        if (!collectionId && data?.length > 0) {
          navigate(`/collections/${data[0].id}/documents`, { replace: true })
        }
      } catch (err) {
        console.error('Failed to load collections:', err)
      }
    }
    if (api) {
      loadCollections()
    }
  }, [api, collectionId, navigate])

  const loadData = async () => {
    if (!collectionId) {
      setLoading(false)
      return
    }
    try {
      setLoading(true)
      const [collectionData, documentsData] = await Promise.all([
        api.getCollection(collectionId),
        api.getDocuments(collectionId),
      ])
      setCollection(collectionData)
      setDocuments(documentsData || [])
    } catch (err) {
      setError('Failed to load data: ' + err.message)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    loadData()
  }, [api, collectionId])

  const handleCollectionChange = (e) => {
    const newCollectionId = e.target.value
    if (newCollectionId) {
      navigate(`/collections/${newCollectionId}/documents`)
    }
  }

  const handleCreate = async () => {
    try {
      const content = JSON.parse(newDocument.content)
      await api.createDocument(collectionId, {
        name: newDocument.name || null,
        content: content,
        labels: newLabels.length > 0 ? newLabels : null,
        tags: Object.keys(newTags).length > 0 ? newTags : null,
      })
      setShowCreateModal(false)
      setNewDocument({ name: '', content: '{\n  \n}' })
      setNewLabels([])
      setNewTags({})
      await loadData()
    } catch (err) {
      if (err instanceof SyntaxError) {
        setError('Invalid JSON: ' + err.message)
      } else {
        setError('Failed to create document: ' + err.message)
      }
    }
  }

  const handleDelete = async (id) => {
    if (!confirm('Are you sure you want to delete this document?')) {
      return
    }
    try {
      await api.deleteDocument(collectionId, id)
      await loadData()
    } catch (err) {
      setError('Failed to delete document: ' + err.message)
    }
  }

  const handleViewMetadata = async (id) => {
    try {
      const doc = await api.getDocument(collectionId, id, false)
      setSelectedDocument(doc)
      setShowMetadataModal(true)
    } catch (err) {
      setError('Failed to load document: ' + err.message)
    }
  }

  const handleViewData = async (id) => {
    try {
      const content = await api.getDocumentContent(collectionId, id)
      setSelectedDocument({ content })
      setShowDataModal(true)
    } catch (err) {
      setError('Failed to load document: ' + err.message)
    }
  }

  const handleSort = (column) => {
    setSort(prev => ({
      column,
      direction: prev.column === column && prev.direction === 'asc' ? 'desc' : 'asc'
    }))
  }

  const handleFilterChange = (column, value) => {
    setFilters(prev => ({ ...prev, [column]: value }))
  }

  const getSortIcon = (column) => {
    if (sort.column !== column) return '↕'
    return sort.direction === 'asc' ? '↑' : '↓'
  }

  if (loading) {
    return <div className="loading">Loading...</div>
  }

  return (
    <div className="documents">
      <div className="page-header">
        <div>
          <h1 className="page-title">Documents</h1>
          <div className="collection-selector">
            <label className="collection-selector-label">Collection:</label>
            <select
              className="collection-selector-select"
              value={collectionId || ''}
              onChange={handleCollectionChange}
            >
              <option value="">Select a collection...</option>
              {collections.map((col) => (
                <option key={col.id} value={col.id}>
                  {col.name}
                </option>
              ))}
            </select>
          </div>
        </div>
        {collectionId && (
          <div className="page-actions">
            <button className="btn btn-primary" onClick={() => setShowCreateModal(true)}>
              + New Document
            </button>
          </div>
        )}
      </div>

      {!collectionId ? (
        <div className="empty-state">
          <p>Select a collection to view its documents.</p>
        </div>
      ) : documents.length === 0 ? (
        <div className="empty-state">
          <p>No documents in this collection. Add your first document.</p>
        </div>
      ) : (
        <div className="card">
          <div className="table-results-count">
            Showing {filteredDocuments.length} of {documents.length} documents
          </div>
          <table className="table">
            <thead>
              <tr>
                <th
                  className={`sortable ${sort.column === 'id' ? 'sorted' : ''}`}
                  onClick={() => handleSort('id')}
                >
                  <span className="th-content">
                    ID
                    <span className="sort-icon">{getSortIcon('id')}</span>
                  </span>
                </th>
                <th
                  className={`sortable ${sort.column === 'name' ? 'sorted' : ''}`}
                  onClick={() => handleSort('name')}
                >
                  <span className="th-content">
                    Name
                    <span className="sort-icon">{getSortIcon('name')}</span>
                  </span>
                </th>
                <th
                  className={`sortable ${sort.column === 'contentLength' ? 'sorted' : ''}`}
                  onClick={() => handleSort('contentLength')}
                >
                  <span className="th-content">
                    Size
                    <span className="sort-icon">{getSortIcon('contentLength')}</span>
                  </span>
                </th>
                <th
                  className={`sortable ${sort.column === 'schemaId' ? 'sorted' : ''}`}
                  onClick={() => handleSort('schemaId')}
                >
                  <span className="th-content">
                    Schema ID
                    <span className="sort-icon">{getSortIcon('schemaId')}</span>
                  </span>
                </th>
                <th
                  className={`sortable ${sort.column === 'createdUtc' ? 'sorted' : ''}`}
                  onClick={() => handleSort('createdUtc')}
                >
                  <span className="th-content">
                    Created
                    <span className="sort-icon">{getSortIcon('createdUtc')}</span>
                  </span>
                </th>
                <th>Actions</th>
              </tr>
              <tr className="filter-row">
                <td>
                  <input
                    type="text"
                    className="column-filter"
                    placeholder="Filter..."
                    value={filters.id}
                    onChange={(e) => handleFilterChange('id', e.target.value)}
                  />
                </td>
                <td>
                  <input
                    type="text"
                    className="column-filter"
                    placeholder="Filter..."
                    value={filters.name}
                    onChange={(e) => handleFilterChange('name', e.target.value)}
                  />
                </td>
                <td>
                  <input
                    type="text"
                    className="column-filter"
                    placeholder="Filter..."
                    value={filters.contentLength}
                    onChange={(e) => handleFilterChange('contentLength', e.target.value)}
                  />
                </td>
                <td>
                  <input
                    type="text"
                    className="column-filter"
                    placeholder="Filter..."
                    value={filters.schemaId}
                    onChange={(e) => handleFilterChange('schemaId', e.target.value)}
                  />
                </td>
                <td>
                  <input
                    type="text"
                    className="column-filter"
                    placeholder="Filter..."
                    value={filters.createdUtc}
                    onChange={(e) => handleFilterChange('createdUtc', e.target.value)}
                  />
                </td>
                <td className="no-filter"></td>
              </tr>
            </thead>
            <tbody>
              {filteredDocuments.length === 0 ? (
                <tr>
                  <td colSpan={6} className="empty-row">No documents match your filters.</td>
                </tr>
              ) : (
                filteredDocuments.map((doc) => (
                  <tr key={doc.id}>
                    <td><CopyableId value={doc.id} /></td>
                    <td>{doc.name || '-'}</td>
                    <td>{formatBytes(doc.contentLength || 0)}</td>
                    <td>{doc.schemaId ? <CopyableId value={doc.schemaId} /> : '-'}</td>
                    <td>{formatDate(doc.createdUtc)}</td>
                    <td>
                      <ActionMenu
                        items={[
                          { label: 'View Metadata', onClick: () => handleViewMetadata(doc.id) },
                          { label: 'View Data', onClick: () => handleViewData(doc.id) },
                          { label: 'Delete Document', onClick: () => handleDelete(doc.id), variant: 'danger' },
                        ]}
                      />
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      )}

      <Modal
        isOpen={showCreateModal}
        onClose={() => setShowCreateModal(false)}
        title="New Document"
      >
        <div className="form-group">
          <label className="form-label">Name (optional)</label>
          <input
            type="text"
            className="input"
            value={newDocument.name}
            onChange={(e) => setNewDocument({ ...newDocument, name: e.target.value })}
            placeholder="Enter document name"
          />
        </div>
        <div className="form-group">
          <label className="form-label">Content (JSON) *</label>
          <textarea
            className="textarea json-editor"
            value={newDocument.content}
            onChange={(e) => setNewDocument({ ...newDocument, content: e.target.value })}
            placeholder='{"key": "value"}'
            rows={10}
          />
        </div>
        <div className="form-group">
          <label className="form-label">Labels</label>
          <TagInput
            value={newLabels}
            onChange={setNewLabels}
            placeholder="Add labels..."
          />
        </div>
        <div className="form-group">
          <label className="form-label">Tags</label>
          <KeyValueEditor
            value={newTags}
            onChange={setNewTags}
            keyPlaceholder="Tag name"
            valuePlaceholder="Tag value"
          />
        </div>
        <div className="modal-actions">
          <button className="btn btn-secondary" onClick={() => setShowCreateModal(false)}>
            Cancel
          </button>
          <button className="btn btn-primary" onClick={handleCreate}>
            Create
          </button>
        </div>
      </Modal>

      <Modal
        isOpen={showMetadataModal}
        onClose={() => {
          setShowMetadataModal(false)
          setSelectedDocument(null)
        }}
        title="Document Metadata"
        wide
      >
        {selectedDocument && (
          <>
            <div className="doc-detail">
              <strong>ID:</strong> <CopyableId value={selectedDocument.id} />
            </div>
            <div className="doc-detail">
              <strong>Name:</strong> {selectedDocument.name || '-'}
            </div>
            <div className="doc-detail">
              <strong>Schema ID:</strong> {selectedDocument.schemaId ? <CopyableId value={selectedDocument.schemaId} /> : '-'}
            </div>
            <div className="doc-detail">
              <strong>Content Length:</strong> {formatBytes(selectedDocument.contentLength || 0)} ({(selectedDocument.contentLength || 0).toLocaleString()} bytes)
            </div>
            <div className="doc-detail">
              <strong>SHA256 Hash:</strong> {selectedDocument.sha256Hash ? <CopyableId value={selectedDocument.sha256Hash} /> : '-'}
            </div>
            <div className="doc-detail">
              <strong>Created:</strong> {formatDate(selectedDocument.createdUtc)}
            </div>
            {selectedDocument.labels?.length > 0 && (
              <div className="doc-detail">
                <strong>Labels:</strong>
                <div className="doc-labels">
                  {selectedDocument.labels.map((label, i) => (
                    <span key={i} className="label-badge">{label}</span>
                  ))}
                </div>
              </div>
            )}
            {selectedDocument.tags && Object.keys(selectedDocument.tags).length > 0 && (
              <div className="doc-detail">
                <strong>Tags:</strong>
                <div className="doc-tags">
                  {Object.entries(selectedDocument.tags).map(([k, v]) => (
                    <span key={k} className="tag-item">
                      <span className="tag-key">{k}</span>
                      <span className="tag-sep">=</span>
                      <span className="tag-val">{v}</span>
                    </span>
                  ))}
                </div>
              </div>
            )}
          </>
        )}
      </Modal>

      <Modal
        isOpen={showDataModal}
        onClose={() => {
          setShowDataModal(false)
          setSelectedDocument(null)
        }}
        title="Document Data"
      >
        {selectedDocument && (
          <pre className="json-preview">
            {selectedDocument.content
              ? JSON.stringify(selectedDocument.content, null, 2)
              : '(content not loaded)'}
          </pre>
        )}
      </Modal>
    </div>
  )
}
