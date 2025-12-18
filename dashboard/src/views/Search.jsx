import { useState, useEffect } from 'react'
import { useApp } from '../context/AppContext'
import { formatDate } from '../utils/api'
import Modal from '../components/Modal'
import ActionMenu from '../components/ActionMenu'
import TagInput from '../components/TagInput'
import KeyValueEditor from '../components/KeyValueEditor'
import './Search.css'

export default function Search() {
  const { api, setError } = useApp()
  const [collections, setCollections] = useState([])
  const [selectedCollection, setSelectedCollection] = useState('')
  const [sqlExpression, setSqlExpression] = useState('')
  const [filterLabels, setFilterLabels] = useState([])
  const [filterTags, setFilterTags] = useState({})
  const [results, setResults] = useState(null)
  const [loading, setLoading] = useState(false)
  const [showMetadataModal, setShowMetadataModal] = useState(false)
  const [showDataModal, setShowDataModal] = useState(false)
  const [selectedDocument, setSelectedDocument] = useState(null)
  const [documentData, setDocumentData] = useState(null)

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

  const handleSearch = async (e) => {
    e.preventDefault()
    if (!selectedCollection) {
      setError('Please select a collection')
      return
    }

    try {
      setLoading(true)
      const searchRequest = {
        includeContent: true,
      }

      // Add SQL expression if provided
      if (sqlExpression.trim()) {
        searchRequest.sqlExpression = sqlExpression
      }

      // Add label filters (first-pass filter)
      if (filterLabels.length > 0) {
        searchRequest.labels = filterLabels
      }

      // Add tag filters (first-pass filter)
      if (Object.keys(filterTags).length > 0) {
        searchRequest.tags = filterTags
      }

      const result = await api.searchDocuments(selectedCollection, searchRequest)
      setResults(result)
    } catch (err) {
      setError('Search failed: ' + err.message)
    } finally {
      setLoading(false)
    }
  }

  const handleViewMetadata = async (doc) => {
    try {
      const fullDoc = await api.getDocument(selectedCollection, doc.id)
      setSelectedDocument(fullDoc)
      setShowMetadataModal(true)
    } catch (err) {
      setError('Failed to load document metadata: ' + err.message)
    }
  }

  const handleViewData = async (doc) => {
    try {
      // getDocumentContent returns the raw JSON content directly (not a document object)
      const contentData = await api.getDocumentContent(selectedCollection, doc.id)
      setSelectedDocument(doc)
      setDocumentData(contentData)
      setShowDataModal(true)
    } catch (err) {
      setError('Failed to load document data: ' + err.message)
    }
  }

  return (
    <div className="search">
      <div className="page-header">
        <h1 className="page-title">Search Documents</h1>
      </div>

      <div className="card search-form-card">
        <form onSubmit={handleSearch}>
          <div className="search-row">
            <div className="form-group search-collection">
              <label className="form-label">Collection</label>
              <select
                className="input"
                value={selectedCollection}
                onChange={(e) => setSelectedCollection(e.target.value)}
              >
                {collections.map((col) => (
                  <option key={col.id} value={col.id}>
                    {col.name}
                  </option>
                ))}
              </select>
            </div>
          </div>

          <div className="filter-section">
            <h3 className="filter-section-title">First-pass Filters</h3>
            <p className="filter-section-hint">Documents must match ALL specified labels and tags</p>

            <div className="filter-row">
              <div className="form-group filter-labels">
                <label className="form-label">Filter by Labels</label>
                <TagInput
                  value={filterLabels}
                  onChange={setFilterLabels}
                  placeholder="Add labels to filter by..."
                />
              </div>
            </div>

            <div className="filter-row">
              <div className="form-group filter-tags">
                <label className="form-label">Filter by Tags</label>
                <KeyValueEditor
                  value={filterTags}
                  onChange={setFilterTags}
                  keyPlaceholder="Tag name"
                  valuePlaceholder="Tag value"
                />
              </div>
            </div>
          </div>

          <div className="form-group">
            <label className="form-label">SQL Expression (optional)</label>
            <textarea
              className="textarea sql-input"
              value={sqlExpression}
              onChange={(e) => setSqlExpression(e.target.value)}
              placeholder="SELECT * FROM documents WHERE Person.First = 'Joel'"
              rows={3}
            />
            <p className="form-hint">
              Leave empty to return all documents. Supports: =, !=, &gt;, &lt;, &gt;=, &lt;=, LIKE, IS NULL, IS NOT NULL, ORDER BY, LIMIT, OFFSET
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
                  {results.documents?.map((doc) => (
                    <tr key={doc.id}>
                      <td className="monospace">{doc.id}</td>
                      <td>{doc.name || '-'}</td>
                      <td>{formatDate(doc.createdUtc)}</td>
                      <td>
                        <ActionMenu
                          items={[
                            { label: 'View Metadata', onClick: () => handleViewMetadata(doc) },
                            { label: 'View Data', onClick: () => handleViewData(doc) },
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
                Showing {results.documents?.length || 0} of {results.totalRecords}
                {!results.endOfResults && ` (${results.recordsRemaining} remaining)`}
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
      >
        {selectedDocument && (
          <>
            <div className="doc-detail">
              <strong>ID:</strong> <span className="monospace">{selectedDocument.id}</span>
            </div>
            <div className="doc-detail">
              <strong>Name:</strong> {selectedDocument.name || '-'}
            </div>
            <div className="doc-detail">
              <strong>Collection ID:</strong> <span className="monospace">{selectedDocument.collectionId}</span>
            </div>
            <div className="doc-detail">
              <strong>Schema ID:</strong> <span className="monospace">{selectedDocument.schemaId || '-'}</span>
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
          setDocumentData(null)
        }}
        title="Document Data"
      >
        {selectedDocument && (
          <>
            <div className="doc-detail">
              <strong>Document:</strong> <span className="monospace">{selectedDocument.name || selectedDocument.id}</span>
            </div>
            <div className="doc-detail">
              <strong>Data:</strong>
              <pre className="json-preview">
                {documentData
                  ? JSON.stringify(documentData, null, 2)
                  : '(no data available)'}
              </pre>
            </div>
          </>
        )}
      </Modal>
    </div>
  )
}
