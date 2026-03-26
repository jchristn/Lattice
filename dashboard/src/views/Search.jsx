import { useEffect, useMemo, useState } from 'react'
import { useApp } from '../context/AppContext'
import { formatDate } from '../utils/api'
import ActionMenu from '../components/ActionMenu'
import CopyableId from '../components/CopyableId'
import JsonViewerModal from '../components/JsonViewerModal'
import Modal from '../components/Modal'
import TablePagination from '../components/TablePagination'
import TagInput from '../components/TagInput'
import KeyValueEditor from '../components/KeyValueEditor'
import './Search.css'

export default function Search() {
  const { api, setError } = useApp()
  const [collections, setCollections] = useState([])
  const [selectedCollection, setSelectedCollection] = useState('')
  const [sqlExpression, setSqlExpression] = useState('SELECT * FROM documents')
  const [filterLabels, setFilterLabels] = useState([])
  const [filterTags, setFilterTags] = useState({})
  const [results, setResults] = useState(null)
  const [loading, setLoading] = useState(false)
  const [showMetadataModal, setShowMetadataModal] = useState(false)
  const [selectedDocument, setSelectedDocument] = useState(null)
  const [page, setPage] = useState(0)
  const [pageSize, setPageSize] = useState(25)
  const [jsonViewer, setJsonViewer] = useState({ open: false, title: '', subtitle: '', identifier: '', value: null })

  useEffect(() => {
    const loadCollections = async () => {
      try {
        const data = await api.getCollections()
        setCollections(data || [])
        if (data?.length > 0) {
          setSelectedCollection(data[0].id)
        }
      } catch (err) {
        setError('Failed to load collections: ' + err.message)
      }
    }

    loadCollections()
  }, [api])

  const pagedDocuments = useMemo(() => {
    const documents = results?.documents || []
    return documents.slice(page * pageSize, (page + 1) * pageSize)
  }, [results, page, pageSize])

  const totalPages = Math.max(1, Math.ceil((results?.documents?.length || 0) / pageSize))

  useEffect(() => {
    if (page > totalPages - 1) {
      setPage(Math.max(totalPages - 1, 0))
    }
  }, [page, totalPages])

  const executeSearch = async () => {
    if (!selectedCollection) {
      setError('Please select a collection')
      return
    }

    try {
      setLoading(true)
      const searchRequest = { includeContent: true }

      if (sqlExpression.trim()) {
        searchRequest.sqlExpression = sqlExpression
      }
      if (filterLabels.length > 0) {
        searchRequest.labels = filterLabels
      }
      if (Object.keys(filterTags).length > 0) {
        searchRequest.tags = filterTags
      }

      const result = await api.searchDocuments(selectedCollection, searchRequest)
      setResults(result)
      setPage(0)
    } catch (err) {
      setError('Search failed: ' + err.message)
    } finally {
      setLoading(false)
    }
  }

  const handleSearch = async (event) => {
    event.preventDefault()
    await executeSearch()
  }

  const handleViewMetadata = async (document) => {
    try {
      const fullDoc = await api.getDocument(selectedCollection, document.id)
      setSelectedDocument(fullDoc)
      setShowMetadataModal(true)
    } catch (err) {
      setError('Failed to load document metadata: ' + err.message)
    }
  }

  const handleViewData = async (document) => {
    try {
      const contentData = await api.getDocumentContent(selectedCollection, document.id)
      setJsonViewer({
        open: true,
        title: 'Search Result JSON',
        subtitle: 'This is the full document payload returned for the selected search result.',
        identifier: document.id,
        value: contentData,
      })
    } catch (err) {
      setError('Failed to load document data: ' + err.message)
    }
  }

  const handleViewJson = (document) => {
    setJsonViewer({
      open: true,
      title: 'Search Result Metadata JSON',
      subtitle: 'This is the result object returned by the search endpoint for the selected document.',
      identifier: document.id,
      value: document,
    })
  }

  return (
    <div className="search">
      <div className="page-header">
        <div>
          <h1 className="page-title">Search Documents</h1>
          <p className="page-subtitle">Run label, tag, and SQL-style searches against a collection to validate query behavior and inspect the exact documents returned.</p>
        </div>
      </div>

      <div className="card search-form-card">
        <form onSubmit={handleSearch}>
          <div className="search-row">
            <div className="form-group search-collection">
              <label className="form-label">Collection</label>
              <select className="input" value={selectedCollection} onChange={(event) => setSelectedCollection(event.target.value)}>
                {collections.map((collection) => (
                  <option key={collection.id} value={collection.id}>
                    {collection.name}
                  </option>
                ))}
              </select>
            </div>
          </div>

          <div className="filter-section">
            <h3 className="filter-section-title">First-pass Filters</h3>
            <p className="filter-section-hint">Documents must match all specified labels and tags before the SQL expression is applied.</p>

            <div className="filter-row">
              <div className="form-group filter-labels">
                <label className="form-label">Filter by Labels</label>
                <TagInput value={filterLabels} onChange={setFilterLabels} placeholder="Add labels to filter by..." />
              </div>
            </div>

            <div className="filter-row">
              <div className="form-group filter-tags">
                <label className="form-label">Filter by Tags</label>
                <KeyValueEditor value={filterTags} onChange={setFilterTags} keyPlaceholder="Tag name" valuePlaceholder="Tag value" />
              </div>
            </div>
          </div>

          <div className="form-group">
            <label className="form-label">SQL Expression (optional)</label>
            <textarea
              className="textarea sql-input"
              value={sqlExpression}
              onChange={(event) => setSqlExpression(event.target.value)}
              placeholder="SELECT * FROM documents WHERE Person.First = 'Joel'"
              rows={3}
            />
            <p className="form-hint">
              Leave empty to return all documents. Supports `=`, `!=`, `&gt;`, `&lt;`, `&gt;=`, `&lt;=`, `LIKE`, `IS NULL`, `IS NOT NULL`, `ORDER BY`, `LIMIT`, and `OFFSET`.
            </p>
          </div>

          <button type="submit" className="btn btn-primary" disabled={loading}>
            {loading ? 'Searching...' : 'Search'}
          </button>
        </form>
      </div>

      {results && (
        <div className="search-results">
          <div className="results-header">
            <h2>Results</h2>
            <span className="results-count">
              {results.totalRecords} document{results.totalRecords !== 1 ? 's' : ''} found
            </span>
          </div>

          {results.documents?.length === 0 ? (
            <div className="empty-state">
              <p>No documents match your search criteria.</p>
            </div>
          ) : (
            <div className="card">
              <div className="table-results-count">
                Showing {results.documents.length} loaded results from {results.totalRecords} total matches
              </div>
              <TablePagination
                totalRecords={results.documents.length}
                currentPage={page}
                totalPages={totalPages}
                onPageChange={setPage}
                onRefresh={executeSearch}
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
                    <th>ID</th>
                    <th>Name</th>
                    <th>Created</th>
                    <th>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {pagedDocuments.map((document) => (
                    <tr key={document.id}>
                      <td><CopyableId value={document.id} /></td>
                      <td>{document.name || '-'}</td>
                      <td>{formatDate(document.createdUtc)}</td>
                      <td>
                        <ActionMenu
                          items={[
                            { label: 'View Metadata', onClick: () => handleViewMetadata(document) },
                            { label: 'View Data', onClick: () => handleViewData(document) },
                            { label: 'View JSON', onClick: () => handleViewJson(document) },
                          ]}
                        />
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}

          {results.totalRecords > 0 && (
            <div className="results-pagination">
              <span>
                Showing {results.documents?.length || 0} loaded results of {results.totalRecords}
                {!results.endOfResults && ` (${results.recordsRemaining} remaining on the server result set)`}
              </span>
            </div>
          )}
        </div>
      )}

      <Modal
        isOpen={showMetadataModal}
        onClose={() => {
          setShowMetadataModal(false)
          setSelectedDocument(null)
        }}
        title="Document Metadata"
        subtitle="This metadata explains why the document matched and how it is stored within the selected collection."
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
              <strong>Collection ID:</strong> <CopyableId value={selectedDocument.collectionId} />
            </div>
            <div className="doc-detail">
              <strong>Schema ID:</strong> {selectedDocument.schemaId ? <CopyableId value={selectedDocument.schemaId} /> : '-'}
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
