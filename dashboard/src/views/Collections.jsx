import { useState, useEffect, useMemo } from 'react'
import { useNavigate } from 'react-router-dom'
import { useApp } from '../context/AppContext'
import { formatDate } from '../utils/api'
import Modal from '../components/Modal'
import ActionMenu from '../components/ActionMenu'
import CopyableId from '../components/CopyableId'
import TagInput from '../components/TagInput'
import KeyValueEditor from '../components/KeyValueEditor'
import './Collections.css'

export default function Collections() {
  const { api, setError } = useApp()
  const navigate = useNavigate()
  const [collections, setCollections] = useState([])
  const [loading, setLoading] = useState(true)
  const [showCreateModal, setShowCreateModal] = useState(false)
  const [showMetadataModal, setShowMetadataModal] = useState(false)
  const [selectedCollection, setSelectedCollection] = useState(null)
  const [newCollection, setNewCollection] = useState({
    name: '',
    description: '',
    documentsDirectory: '',
  })
  const [newLabels, setNewLabels] = useState([])
  const [newTags, setNewTags] = useState({})

  // Per-column filters
  const [filters, setFilters] = useState({
    name: '',
    description: '',
    documentsDirectory: '',
    createdUtc: '',
  })

  // Sorting state
  const [sort, setSort] = useState({ column: 'name', direction: 'asc' })

  // Filter and sort collections
  const filteredCollections = useMemo(() => {
    let result = [...collections]

    // Apply column filters
    if (filters.name) {
      const query = filters.name.toLowerCase()
      result = result.filter(c => c.name.toLowerCase().includes(query))
    }
    if (filters.description) {
      const query = filters.description.toLowerCase()
      result = result.filter(c => (c.description || '').toLowerCase().includes(query))
    }
    if (filters.documentsDirectory) {
      const query = filters.documentsDirectory.toLowerCase()
      result = result.filter(c => (c.documentsDirectory || '').toLowerCase().includes(query))
    }
    if (filters.createdUtc) {
      const query = filters.createdUtc.toLowerCase()
      result = result.filter(c => formatDate(c.createdUtc).toLowerCase().includes(query))
    }

    // Apply sorting
    if (sort.column) {
      result.sort((a, b) => {
        let aVal, bVal
        if (sort.column === 'createdUtc') {
          aVal = a.createdUtc || ''
          bVal = b.createdUtc || ''
        } else {
          aVal = a[sort.column] || ''
          bVal = b[sort.column] || ''
        }
        const comparison = aVal.localeCompare(bVal)
        return sort.direction === 'asc' ? comparison : -comparison
      })
    }

    return result
  }, [collections, filters, sort])

  const loadCollections = async () => {
    try {
      setLoading(true)
      const data = await api.getCollections()
      setCollections(data || [])
    } catch (err) {
      setError('Failed to load collections: ' + err.message)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    loadCollections()
  }, [api])

  const handleCreate = async () => {
    try {
      await api.createCollection({
        name: newCollection.name,
        description: newCollection.description || null,
        documentsDirectory: newCollection.documentsDirectory || null,
        labels: newLabels.length > 0 ? newLabels : null,
        tags: Object.keys(newTags).length > 0 ? newTags : null,
      })
      setShowCreateModal(false)
      setNewCollection({ name: '', description: '', documentsDirectory: '' })
      setNewLabels([])
      setNewTags({})
      await loadCollections()
    } catch (err) {
      setError('Failed to create collection: ' + err.message)
    }
  }

  const handleDelete = async (id) => {
    if (!confirm('Are you sure you want to delete this collection? All documents will be deleted.')) {
      return
    }
    try {
      await api.deleteCollection(id)
      await loadCollections()
    } catch (err) {
      setError('Failed to delete collection: ' + err.message)
    }
  }

  const handleViewDocuments = (collectionId) => {
    navigate(`/collections/${collectionId}/documents`)
  }

  const handleViewMetadata = (collection) => {
    setSelectedCollection(collection)
    setShowMetadataModal(true)
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
    <div className="collections">
      <div className="page-header">
        <h1 className="page-title">Collections</h1>
        <div className="page-actions">
          <button className="btn btn-primary" onClick={() => setShowCreateModal(true)}>
            + New Collection
          </button>
        </div>
      </div>

      {collections.length === 0 ? (
        <div className="empty-state">
          <p>No collections yet. Create your first collection to get started.</p>
        </div>
      ) : (
        <div className="card">
          <div className="table-results-count">
            Showing {filteredCollections.length} of {collections.length} collections
          </div>
          <table className="table">
            <thead>
              <tr>
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
                  className={`sortable ${sort.column === 'description' ? 'sorted' : ''}`}
                  onClick={() => handleSort('description')}
                >
                  <span className="th-content">
                    Description
                    <span className="sort-icon">{getSortIcon('description')}</span>
                  </span>
                </th>
                <th
                  className={`sortable ${sort.column === 'documentsDirectory' ? 'sorted' : ''}`}
                  onClick={() => handleSort('documentsDirectory')}
                >
                  <span className="th-content">
                    Documents Directory
                    <span className="sort-icon">{getSortIcon('documentsDirectory')}</span>
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
                    value={filters.name}
                    onChange={(e) => handleFilterChange('name', e.target.value)}
                  />
                </td>
                <td>
                  <input
                    type="text"
                    className="column-filter"
                    placeholder="Filter..."
                    value={filters.description}
                    onChange={(e) => handleFilterChange('description', e.target.value)}
                  />
                </td>
                <td>
                  <input
                    type="text"
                    className="column-filter"
                    placeholder="Filter..."
                    value={filters.documentsDirectory}
                    onChange={(e) => handleFilterChange('documentsDirectory', e.target.value)}
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
              {filteredCollections.length === 0 ? (
                <tr>
                  <td colSpan={5} className="empty-row">No collections match your filters.</td>
                </tr>
              ) : (
                filteredCollections.map((collection) => (
                  <tr key={collection.id}>
                    <td>
                      <strong>{collection.name}</strong>
                      <div className="collection-id">
                        <CopyableId value={collection.id} />
                      </div>
                    </td>
                    <td>{collection.description || '-'}</td>
                    <td className="monospace">{collection.documentsDirectory || '-'}</td>
                    <td>{formatDate(collection.createdUtc)}</td>
                    <td>
                      <ActionMenu
                        items={[
                          { label: 'View Metadata', onClick: () => handleViewMetadata(collection) },
                          { label: 'View Documents', onClick: () => handleViewDocuments(collection.id) },
                          { label: 'Delete Collection', onClick: () => handleDelete(collection.id), variant: 'danger' },
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
        title="New Collection"
      >
        <div className="form-group">
          <label className="form-label">Name *</label>
          <input
            type="text"
            className="input"
            value={newCollection.name}
            onChange={(e) => setNewCollection({ ...newCollection, name: e.target.value })}
            placeholder="Enter collection name"
          />
        </div>
        <div className="form-group">
          <label className="form-label">Description</label>
          <input
            type="text"
            className="input"
            value={newCollection.description}
            onChange={(e) => setNewCollection({ ...newCollection, description: e.target.value })}
            placeholder="Optional description"
          />
        </div>
        <div className="form-group">
          <label className="form-label">Documents Directory</label>
          <input
            type="text"
            className="input"
            value={newCollection.documentsDirectory}
            onChange={(e) => setNewCollection({ ...newCollection, documentsDirectory: e.target.value })}
            placeholder="e.g., ./documents/my-collection"
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
          <button
            className="btn btn-primary"
            onClick={handleCreate}
            disabled={!newCollection.name}
          >
            Create
          </button>
        </div>
      </Modal>

      <Modal
        isOpen={showMetadataModal}
        onClose={() => {
          setShowMetadataModal(false)
          setSelectedCollection(null)
        }}
        title="Collection Metadata"
      >
        {selectedCollection && (
          <>
            <div className="metadata-item">
              <label className="metadata-label">ID</label>
              <div className="metadata-value">
                <CopyableId value={selectedCollection.id} />
              </div>
            </div>
            <div className="metadata-item">
              <label className="metadata-label">Name</label>
              <div className="metadata-value">{selectedCollection.name}</div>
            </div>
            <div className="metadata-item">
              <label className="metadata-label">Description</label>
              <div className="metadata-value">{selectedCollection.description || '-'}</div>
            </div>
            <div className="metadata-item">
              <label className="metadata-label">Documents Directory</label>
              <div className="metadata-value monospace">{selectedCollection.documentsDirectory || '-'}</div>
            </div>
            <div className="metadata-item">
              <label className="metadata-label">Created</label>
              <div className="metadata-value">{formatDate(selectedCollection.createdUtc)}</div>
            </div>
            {selectedCollection.labels?.length > 0 && (
              <div className="metadata-item">
                <label className="metadata-label">Labels</label>
                <div className="metadata-value">
                  <div className="metadata-labels">
                    {selectedCollection.labels.map((label, i) => (
                      <span key={i} className="label-badge">{label}</span>
                    ))}
                  </div>
                </div>
              </div>
            )}
            {selectedCollection.tags && Object.keys(selectedCollection.tags).length > 0 && (
              <div className="metadata-item">
                <label className="metadata-label">Tags</label>
                <div className="metadata-value">
                  <div className="metadata-tags">
                    {Object.entries(selectedCollection.tags).map(([k, v]) => (
                      <span key={k} className="tag-item">
                        <span className="tag-key">{k}</span>
                        <span className="tag-sep">=</span>
                        <span className="tag-val">{v}</span>
                      </span>
                    ))}
                  </div>
                </div>
              </div>
            )}
          </>
        )}
      </Modal>
    </div>
  )
}
