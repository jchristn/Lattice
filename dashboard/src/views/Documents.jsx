import { useEffect, useMemo, useState } from 'react'
import { useNavigate, useParams, useSearchParams } from 'react-router-dom'
import { useApp } from '../context/AppContext'
import { formatDate } from '../utils/api'
import ActionMenu from '../components/ActionMenu'
import CopyableId from '../components/CopyableId'
import JsonViewerModal from '../components/JsonViewerModal'
import Modal from '../components/Modal'
import TablePagination from '../components/TablePagination'
import TagInput from '../components/TagInput'
import KeyValueEditor from '../components/KeyValueEditor'
import './Documents.css'

function formatBytes(bytes) {
  if (bytes === 0) return '0 B'
  const units = ['B', 'KB', 'MB', 'GB']
  const index = Math.floor(Math.log(bytes) / Math.log(1024))
  return `${parseFloat((bytes / Math.pow(1024, index)).toFixed(1))} ${units[index]}`
}

export default function Documents() {
  const { collectionId } = useParams()
  const [searchParams, setSearchParams] = useSearchParams()
  const navigate = useNavigate()
  const { api, setError } = useApp()
  const [collections, setCollections] = useState([])
  const [collection, setCollection] = useState(null)
  const [documents, setDocuments] = useState([])
  const [loading, setLoading] = useState(true)
  const [showCreateModal, setShowCreateModal] = useState(false)
  const [showMetadataModal, setShowMetadataModal] = useState(false)
  const [selectedDocument, setSelectedDocument] = useState(null)
  const [jsonViewer, setJsonViewer] = useState({ open: false, title: '', subtitle: '', identifier: '', value: null })
  const [page, setPage] = useState(0)
  const [pageSize, setPageSize] = useState(25)
  const [newDocument, setNewDocument] = useState({
    name: '',
    content: '{\n  \n}',
  })
  const [newLabels, setNewLabels] = useState([])
  const [newTags, setNewTags] = useState({})
  const [filters, setFilters] = useState({
    id: '',
    name: '',
    contentLength: '',
    schemaId: '',
    createdUtc: '',
  })
  const [sort, setSort] = useState({ column: 'name', direction: 'asc' })

  const loadCollections = async () => {
    try {
      const data = await api.getCollections()
      setCollections(data || [])
      if (!collectionId && data?.length > 0) {
        navigate(`/collections/${data[0].id}/documents`, { replace: true })
      }
    } catch (err) {
      console.error('Failed to load collections:', err)
    }
  }

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

  const filteredDocuments = useMemo(() => {
    let result = [...documents]

    if (filters.id) {
      const query = filters.id.toLowerCase()
      result = result.filter((document) => document.id.toLowerCase().includes(query))
    }
    if (filters.name) {
      const query = filters.name.toLowerCase()
      result = result.filter((document) => (document.name || '').toLowerCase().includes(query))
    }
    if (filters.contentLength) {
      const query = filters.contentLength.toLowerCase()
      result = result.filter((document) => formatBytes(document.contentLength || 0).toLowerCase().includes(query))
    }
    if (filters.schemaId) {
      const query = filters.schemaId.toLowerCase()
      result = result.filter((document) => (document.schemaId || '').toLowerCase().includes(query))
    }
    if (filters.createdUtc) {
      const query = filters.createdUtc.toLowerCase()
      result = result.filter((document) => formatDate(document.createdUtc).toLowerCase().includes(query))
    }

    result.sort((a, b) => {
      if (sort.column === 'createdUtc') {
        const comparison = (a.createdUtc || '').localeCompare(b.createdUtc || '')
        return sort.direction === 'asc' ? comparison : -comparison
      }
      if (sort.column === 'contentLength') {
        const comparison = (a.contentLength || 0) - (b.contentLength || 0)
        return sort.direction === 'asc' ? comparison : -comparison
      }

      const comparison = (a[sort.column] || '').localeCompare(b[sort.column] || '')
      return sort.direction === 'asc' ? comparison : -comparison
    })

    return result
  }, [documents, filters, sort])

  const totalPages = Math.max(1, Math.ceil(filteredDocuments.length / pageSize))
  const pagedDocuments = filteredDocuments.slice(page * pageSize, (page + 1) * pageSize)

  useEffect(() => {
    if (api) {
      loadCollections()
    }
  }, [api, collectionId, navigate])

  useEffect(() => {
    loadData()
  }, [api, collectionId])

  useEffect(() => {
    setPage(0)
  }, [filters, sort, collectionId])

  useEffect(() => {
    if (page > totalPages - 1) {
      setPage(Math.max(totalPages - 1, 0))
    }
  }, [page, totalPages])

  useEffect(() => {
    if (searchParams.get('create') === '1' && collectionId) {
      setShowCreateModal(true)
      const nextParams = new URLSearchParams(searchParams)
      nextParams.delete('create')
      setSearchParams(nextParams, { replace: true })
    }
  }, [searchParams, collectionId, setSearchParams])

  const resetCreateForm = () => {
    setNewDocument({ name: '', content: '{\n  \n}' })
    setNewLabels([])
    setNewTags({})
  }

  const handleCollectionChange = (event) => {
    const nextCollectionId = event.target.value
    if (nextCollectionId) {
      navigate(`/collections/${nextCollectionId}/documents`)
    }
  }

  const handleCreate = async () => {
    try {
      const content = JSON.parse(newDocument.content)
      await api.createDocument(collectionId, {
        name: newDocument.name || null,
        content,
        labels: newLabels.length > 0 ? newLabels : null,
        tags: Object.keys(newTags).length > 0 ? newTags : null,
      })
      setShowCreateModal(false)
      resetCreateForm()
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
      const document = await api.getDocument(collectionId, id)
      setSelectedDocument(document)
      setShowMetadataModal(true)
    } catch (err) {
      setError('Failed to load document: ' + err.message)
    }
  }

  const handleViewData = async (id, name) => {
    try {
      const content = await api.getDocumentContent(collectionId, id)
      setJsonViewer({
        open: true,
        title: 'Document JSON',
        subtitle: 'This is the full JSON payload stored for the selected document.',
        identifier: id,
        value: content,
      })
    } catch (err) {
      setError('Failed to load document: ' + err.message)
    }
  }

  const handleViewDocumentJson = (document) => {
    setJsonViewer({
      open: true,
      title: 'Document Metadata JSON',
      subtitle: 'This object shows the document metadata returned by the dashboard list view.',
      identifier: document.id,
      value: document,
    })
  }

  const handleSort = (column) => {
    setSort((previous) => ({
      column,
      direction: previous.column === column && previous.direction === 'asc' ? 'desc' : 'asc',
    }))
  }

  const handleFilterChange = (column, value) => {
    setFilters((previous) => ({ ...previous, [column]: value }))
  }

  const getSortIcon = (column) => {
    if (sort.column !== column) return '<>'
    return sort.direction === 'asc' ? '^' : 'v'
  }

  if (loading) {
    return <div className="loading">Loading...</div>
  }

  return (
    <div className="documents">
      <div className="page-header">
        <div>
          <h1 className="page-title">Documents</h1>
          <p className="page-subtitle">Open a collection to inspect its stored documents, validate schema assignment, and create new documents with labels and tags.</p>
          <div className="collection-selector">
            <label className="collection-selector-label">Collection:</label>
            <select className="collection-selector-select" value={collectionId || ''} onChange={handleCollectionChange}>
              <option value="">Select a collection...</option>
              {collections.map((item) => (
                <option key={item.id} value={item.id}>
                  {item.name}
                </option>
              ))}
            </select>
          </div>
        </div>
        {collectionId ? (
          <div className="page-actions">
            <button className="btn btn-primary" type="button" onClick={() => setShowCreateModal(true)}>
              + New Document
            </button>
          </div>
        ) : null}
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
            {collection ? ` in ${collection.name}` : ''}
          </div>
          <TablePagination
            totalRecords={filteredDocuments.length}
            currentPage={page}
            totalPages={totalPages}
            onPageChange={setPage}
            onRefresh={loadData}
            disabled={loading}
            pageSize={pageSize}
            onPageSizeChange={(value) => {
              setPageSize(value)
              setPage(0)
            }}
          />
          <table className="table">
            <thead>
              <tr>
                <th className={`sortable ${sort.column === 'id' ? 'sorted' : ''}`} onClick={() => handleSort('id')}>
                  <span className="th-content">ID <span className="sort-icon">{getSortIcon('id')}</span></span>
                </th>
                <th className={`sortable ${sort.column === 'name' ? 'sorted' : ''}`} onClick={() => handleSort('name')}>
                  <span className="th-content">Name <span className="sort-icon">{getSortIcon('name')}</span></span>
                </th>
                <th className={`sortable ${sort.column === 'contentLength' ? 'sorted' : ''}`} onClick={() => handleSort('contentLength')}>
                  <span className="th-content">Size <span className="sort-icon">{getSortIcon('contentLength')}</span></span>
                </th>
                <th className={`sortable ${sort.column === 'schemaId' ? 'sorted' : ''}`} onClick={() => handleSort('schemaId')}>
                  <span className="th-content">Schema ID <span className="sort-icon">{getSortIcon('schemaId')}</span></span>
                </th>
                <th className={`sortable ${sort.column === 'createdUtc' ? 'sorted' : ''}`} onClick={() => handleSort('createdUtc')}>
                  <span className="th-content">Created <span className="sort-icon">{getSortIcon('createdUtc')}</span></span>
                </th>
                <th>Actions</th>
              </tr>
              <tr className="filter-row">
                <td><input type="text" className="column-filter" placeholder="Filter..." value={filters.id} onChange={(event) => handleFilterChange('id', event.target.value)} /></td>
                <td><input type="text" className="column-filter" placeholder="Filter..." value={filters.name} onChange={(event) => handleFilterChange('name', event.target.value)} /></td>
                <td><input type="text" className="column-filter" placeholder="Filter..." value={filters.contentLength} onChange={(event) => handleFilterChange('contentLength', event.target.value)} /></td>
                <td><input type="text" className="column-filter" placeholder="Filter..." value={filters.schemaId} onChange={(event) => handleFilterChange('schemaId', event.target.value)} /></td>
                <td><input type="text" className="column-filter" placeholder="Filter..." value={filters.createdUtc} onChange={(event) => handleFilterChange('createdUtc', event.target.value)} /></td>
                <td className="no-filter"></td>
              </tr>
            </thead>
            <tbody>
              {pagedDocuments.length === 0 ? (
                <tr>
                  <td colSpan={6} className="empty-row">No documents match your filters.</td>
                </tr>
              ) : (
                pagedDocuments.map((document) => (
                  <tr key={document.id}>
                    <td><CopyableId value={document.id} /></td>
                    <td>{document.name || '-'}</td>
                    <td>{formatBytes(document.contentLength || 0)}</td>
                    <td>{document.schemaId ? <CopyableId value={document.schemaId} /> : '-'}</td>
                    <td>{formatDate(document.createdUtc)}</td>
                    <td>
                      <ActionMenu
                        items={[
                          { label: 'View Metadata', onClick: () => handleViewMetadata(document.id) },
                          { label: 'View Data', onClick: () => handleViewData(document.id, document.name) },
                          { label: 'View JSON', onClick: () => handleViewDocumentJson(document) },
                          { label: 'Delete Document', onClick: () => handleDelete(document.id), variant: 'danger' },
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
        subtitle="Create a JSON document in the selected collection so it can be indexed, searched, and analyzed."
      >
        <div className="form-group">
          <label className="form-label">Name (optional)</label>
          <input type="text" className="input" value={newDocument.name} onChange={(event) => setNewDocument({ ...newDocument, name: event.target.value })} placeholder="Enter document name" />
        </div>
        <div className="form-group">
          <label className="form-label">Content (JSON) *</label>
          <textarea className="textarea json-editor" value={newDocument.content} onChange={(event) => setNewDocument({ ...newDocument, content: event.target.value })} placeholder='{"key": "value"}' rows={10} />
        </div>
        <div className="form-group">
          <label className="form-label">Labels</label>
          <TagInput value={newLabels} onChange={setNewLabels} placeholder="Add labels..." />
        </div>
        <div className="form-group">
          <label className="form-label">Tags</label>
          <KeyValueEditor value={newTags} onChange={setNewTags} keyPlaceholder="Tag name" valuePlaceholder="Tag value" />
        </div>
        <div className="modal-actions">
          <button className="btn btn-secondary" type="button" onClick={() => setShowCreateModal(false)}>
            Cancel
          </button>
          <button className="btn btn-primary" type="button" onClick={handleCreate}>
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
        subtitle="Review identifiers, schema assignment, and stored document attributes for the selected record."
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
                  {selectedDocument.labels.map((label, index) => (
                    <span key={index} className="label-badge">{label}</span>
                  ))}
                </div>
              </div>
            )}
            {selectedDocument.tags && Object.keys(selectedDocument.tags).length > 0 && (
              <div className="doc-detail">
                <strong>Tags:</strong>
                <div className="doc-tags">
                  {Object.entries(selectedDocument.tags).map(([key, value]) => (
                    <span key={key} className="tag-item">
                      <span className="tag-key">{key}</span>
                      <span className="tag-sep">=</span>
                      <span className="tag-val">{value}</span>
                    </span>
                  ))}
                </div>
              </div>
            )}
          </>
        )}
      </Modal>

      <JsonViewerModal
        isOpen={jsonViewer.open}
        onClose={() => setJsonViewer({ open: false, title: '', subtitle: '', identifier: '', value: null })}
        title={jsonViewer.title}
        subtitle={jsonViewer.subtitle}
        identifier={jsonViewer.identifier}
        value={jsonViewer.value}
      />
    </div>
  )
}
