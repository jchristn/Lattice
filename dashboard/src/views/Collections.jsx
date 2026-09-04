import { useState, useEffect, useMemo } from 'react'
import usePersistedPageSize from '../hooks/usePersistedPageSize'
import { useNavigate } from 'react-router-dom'
import { useApp } from '../context/AppContext'
import { formatDate } from '../utils/api'
import Modal from '../components/Modal'
import ActionMenu from '../components/ActionMenu'
import CopyableId from '../components/CopyableId'
import JsonViewerModal from '../components/JsonViewerModal'
import DetailModal from '../components/DetailModal'
import TablePagination from '../components/TablePagination'
import TagInput from '../components/TagInput'
import KeyValueEditor from '../components/KeyValueEditor'
import './Collections.css'

// Schema enforcement mode labels (string values match server enum serialization)
const ENFORCEMENT_MODES = {
  'none': { label: 'None', description: 'No validation' },
  'strict': { label: 'Strict', description: 'All constraints must pass' },
  'flexible': { label: 'Flexible', description: 'Warns but allows' },
  'partial': { label: 'Partial', description: 'Only validates constrained fields' },
}

// Indexing mode labels (string values match server enum serialization)
const INDEXING_MODES = {
  'all': { label: 'All', description: 'Index all fields' },
  'selective': { label: 'Selective', description: 'Index only specified fields' },
  'none': { label: 'None', description: 'No indexing' },
}

// Data type options for constraints
const DATA_TYPES = ['string', 'integer', 'number', 'boolean', 'array', 'object']
export default function Collections() {
  const { api, setError } = useApp()
  const navigate = useNavigate()
  const [collections, setCollections] = useState([])
  const [loading, setLoading] = useState(true)
  const [showCreateModal, setShowCreateModal] = useState(false)
  const [showEditModal, setShowEditModal] = useState(false)
  const [editForm, setEditForm] = useState({ id: '', name: '', description: '' })
  const [showMetadataModal, setShowMetadataModal] = useState(false)
  const [showConstraintsModal, setShowConstraintsModal] = useState(false)
  const [showIndexingModal, setShowIndexingModal] = useState(false)
  const [showRebuildModal, setShowRebuildModal] = useState(false)
  const [selectedCollection, setSelectedCollection] = useState(null)
  const [constraints, setConstraints] = useState({ mode: 'none', fields: [] })
  const [indexing, setIndexing] = useState({ mode: 'all', fields: [] })
  const [rebuildProgress, setRebuildProgress] = useState(null)
  const [rebuildResult, setRebuildResult] = useState(null)
  const [page, setPage] = useState(0)
  const [pageSize, setPageSize] = usePersistedPageSize('collections', 25)
  const [saving, setSaving] = useState(false)
  const [jsonViewer, setJsonViewer] = useState({ open: false, title: '', subtitle: '', identifier: '', value: null })
  const [newCollection, setNewCollection] = useState({
    name: '',
    description: '',
    documentsDirectory: '',
    schemaEnforcementMode: 'none',
    indexingMode: 'all',
  })
  const [newLabels, setNewLabels] = useState([])
  const [newTags, setNewTags] = useState({})
  const [newConstraints, setNewConstraints] = useState([])
  const [newIndexedFields, setNewIndexedFields] = useState([])

  // Per-column filters
  const [filters, setFilters] = useState({
    id: '',
    name: '',
    description: '',
    createdUtc: '',
  })

  // Sorting state
  const [sort, setSort] = useState({ column: 'name', direction: 'asc' })

  // Filter and sort collections
  const filteredCollections = useMemo(() => {
    let result = [...collections]

    // Apply column filters
    if (filters.id) {
      const query = filters.id.toLowerCase()
      result = result.filter(c => (c.id || '').toLowerCase().includes(query))
    }
    if (filters.name) {
      const query = filters.name.toLowerCase()
      result = result.filter(c => c.name.toLowerCase().includes(query))
    }
    if (filters.description) {
      const query = filters.description.toLowerCase()
      result = result.filter(c => (c.description || '').toLowerCase().includes(query))
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

  const totalPages = Math.max(1, Math.ceil(filteredCollections.length / pageSize))
  const pagedCollections = filteredCollections.slice(page * pageSize, (page + 1) * pageSize)

  const loadCollections = async () => {
    try {
      setLoading(true)
      const result = await api.getCollections({ maxResults: 1000 })
      setCollections(result?.objects || [])
    } catch (err) {
      setError('Failed to load collections: ' + err.message)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    loadCollections()
  }, [api])

  useEffect(() => {
    setPage(0)
  }, [filters, sort])

  useEffect(() => {
    if (page > totalPages - 1) {
      setPage(Math.max(totalPages - 1, 0))
    }
  }, [page, totalPages])

  const handleCreate = async () => {
    try {
      await api.createCollection({
        name: newCollection.name,
        description: newCollection.description || null,
        documentsDirectory: newCollection.documentsDirectory || null,
        labels: newLabels.length > 0 ? newLabels : null,
        tags: Object.keys(newTags).length > 0 ? newTags : null,
        schemaEnforcementMode: newCollection.schemaEnforcementMode,
        fieldConstraints: newConstraints.length > 0 ? newConstraints : null,
        indexingMode: newCollection.indexingMode,
        indexedFields: newIndexedFields.length > 0 ? newIndexedFields : null,
      })
      setShowCreateModal(false)
      setNewCollection({ name: '', description: '', documentsDirectory: '', schemaEnforcementMode: 'none', indexingMode: 'all' })
      setNewLabels([])
      setNewTags({})
      setNewConstraints([])
      setNewIndexedFields([])
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

  const handleAddDocument = (collectionId) => {
    navigate(`/collections/${collectionId}/documents?create=1`)
  }

  const handleViewMetadata = (collection) => {
    setSelectedCollection(collection)
    setShowMetadataModal(true)
  }

  const handleEditCollection = (collection) => {
    setEditForm({
      id: collection.id,
      name: collection.name || '',
      description: collection.description || '',
    })
    setShowEditModal(true)
  }

  const handleSaveEdit = async () => {
    try {
      setSaving(true)
      await api.updateCollection(editForm.id, {
        name: editForm.name,
        description: editForm.description || null,
      })
      setShowEditModal(false)
      await loadCollections()
    } catch (err) {
      setError('Failed to update collection: ' + err.message)
    } finally {
      setSaving(false)
    }
  }

  // Row body click opens the edit modal; interactive controls inside the row are ignored.
  const onRowClick = (event, collection) => {
    if (event.target.closest('button, a, input, select, textarea, label, .action-menu, .copyable-id, [role="button"]')) return
    handleEditCollection(collection)
  }

  const handleViewConstraints = async (collection) => {
    setSelectedCollection(collection)
    // Set defaults first, then try to load
    setConstraints({
      mode: collection.schemaEnforcementMode || 'none',
      fields: [],
    })
    setShowConstraintsModal(true)

    try {
      const data = await api.getCollectionConstraints(collection.id)
      if (data && Array.isArray(data)) {
        setConstraints({
          mode: collection.schemaEnforcementMode || 'none',
          fields: data,
        })
      }
    } catch (err) {
      // If API fails, modal is already open with empty constraints - that's fine
      console.warn('Failed to load constraints:', err.message)
    }
  }

  const handleSaveConstraints = async () => {
    try {
      setSaving(true)
      await api.updateCollectionConstraints(selectedCollection.id, {
        schemaEnforcementMode: constraints.mode,
        fieldConstraints: constraints.fields,
      })
      await loadCollections()
      setShowConstraintsModal(false)
    } catch (err) {
      setError('Failed to save constraints: ' + err.message)
    } finally {
      setSaving(false)
    }
  }

  const handleAddConstraint = () => {
    setConstraints(prev => ({
      ...prev,
      fields: [...prev.fields, {
        fieldPath: '',
        dataType: 'string',
        required: false,
        nullable: true,
      }],
    }))
  }

  const handleUpdateConstraint = (index, field, value) => {
    setConstraints(prev => ({
      ...prev,
      fields: prev.fields.map((c, i) =>
        i === index ? { ...c, [field]: value } : c
      ),
    }))
  }

  const handleRemoveConstraint = (index) => {
    setConstraints(prev => ({
      ...prev,
      fields: prev.fields.filter((_, i) => i !== index),
    }))
  }

  const handleViewIndexing = async (collection) => {
    setSelectedCollection(collection)
    // Set defaults first, then try to load
    setIndexing({
      mode: collection.indexingMode || 'all',
      fields: [],
    })
    setShowIndexingModal(true)

    try {
      const data = await api.getCollectionIndexedFields(collection.id)
      if (data && Array.isArray(data)) {
        setIndexing({
          mode: collection.indexingMode || 'all',
          fields: data.map(f => f.fieldPath || f),
        })
      }
    } catch (err) {
      // If API fails, modal is already open with empty fields - that's fine
      console.warn('Failed to load indexing config:', err.message)
    }
  }

  const handleSaveIndexing = async (rebuild = false) => {
    try {
      setSaving(true)
      await api.updateCollectionIndexing(selectedCollection.id, {
        indexingMode: indexing.mode,
        indexedFields: indexing.fields,
        rebuildIndexes: rebuild,
      })
      await loadCollections()
      setShowIndexingModal(false)
    } catch (err) {
      setError('Failed to save indexing config: ' + err.message)
    } finally {
      setSaving(false)
    }
  }

  const handleAddIndexedField = () => {
    setIndexing(prev => ({
      ...prev,
      fields: [...prev.fields, ''],
    }))
  }

  const handleUpdateIndexedField = (index, value) => {
    setIndexing(prev => ({
      ...prev,
      fields: prev.fields.map((f, i) => i === index ? value : f),
    }))
  }

  const handleRemoveIndexedField = (index) => {
    setIndexing(prev => ({
      ...prev,
      fields: prev.fields.filter((_, i) => i !== index),
    }))
  }

  const handleRebuildIndexes = async (collection) => {
    setSelectedCollection(collection)
    setRebuildProgress(null)
    setRebuildResult(null)
    setShowRebuildModal(true)
  }

  const handleStartRebuild = async (dropUnused = true) => {
    try {
      setRebuildProgress({ phase: 'Starting...', percent: 0 })
      const result = await api.rebuildIndexes(selectedCollection.id, { dropUnusedIndexes: dropUnused })
      // Handle the result - ensure we have a valid object
      setRebuildResult(result || {
        documentsProcessed: 0,
        indexesCreated: 0,
        indexesDropped: 0,
        valuesInserted: 0,
        duration: 0,
        errors: [],
        success: true,
      })
      setRebuildProgress(null)
    } catch (err) {
      // Show error in the modal instead of just setting global error
      setRebuildResult({
        documentsProcessed: 0,
        indexesCreated: 0,
        indexesDropped: 0,
        valuesInserted: 0,
        duration: 0,
        errors: [err.message || 'Unknown error occurred'],
        success: false,
      })
      setRebuildProgress(null)
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

  const handleViewJson = (collection) => {
    setJsonViewer({
      open: true,
      title: 'Collection JSON',
      subtitle: 'This object contains the collection metadata, indexing configuration, and schema enforcement settings used by the dashboard.',
      identifier: collection.id,
      value: collection,
    })
  }

  if (loading) {
    return <div className="loading">Loading...</div>
  }

  return (
    <div className="collections">
      <div className="page-header">
        <div>
          <h1 className="page-title">Collections</h1>
          <p className="page-subtitle">Manage document collections, review storage/indexing settings, and jump into the documents contained within each collection.</p>
        </div>
        <div className="page-actions">
          <button className="btn btn-primary" onClick={() => setShowCreateModal(true)} title="Open a form to define and create a new document collection">
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
          <TablePagination
            totalRecords={filteredCollections.length}
            currentPage={page}
            totalPages={totalPages}
            onPageChange={setPage}
            onRefresh={loadCollections}
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
                <th
                  className={`sortable ${sort.column === 'id' ? 'sorted' : ''}`}
                  onClick={() => handleSort('id')}
                  title="The collection's unique identifier; click to sort by ID"
                >
                  <span className="th-content">
                    ID
                    <span className="sort-icon">{getSortIcon('id')}</span>
                  </span>
                </th>
                <th
                  className={`sortable ${sort.column === 'name' ? 'sorted' : ''}`}
                  onClick={() => handleSort('name')}
                  title="The collection's display name; click to sort collections alphabetically by name"
                >
                  <span className="th-content">
                    Name
                    <span className="sort-icon">{getSortIcon('name')}</span>
                  </span>
                </th>
                <th
                  className={`sortable ${sort.column === 'description' ? 'sorted' : ''}`}
                  onClick={() => handleSort('description')}
                  title="Optional human-readable description of the collection; click to sort by description"
                >
                  <span className="th-content">
                    Description
                    <span className="sort-icon">{getSortIcon('description')}</span>
                  </span>
                </th>
                <th
                  className={`sortable ${sort.column === 'createdUtc' ? 'sorted' : ''}`}
                  onClick={() => handleSort('createdUtc')}
                  title="When the collection was created (UTC); click to sort by creation time"
                >
                  <span className="th-content">
                    Created
                    <span className="sort-icon">{getSortIcon('createdUtc')}</span>
                  </span>
                </th>
                <th title="Per-row actions such as viewing documents, editing constraints, rebuilding indexes, or deleting the collection">Actions</th>
              </tr>
              <tr className="filter-row">
                <td>
                  <input
                    type="text"
                    className="column-filter"
                    placeholder="Filter..."
                    value={filters.id}
                    onChange={(e) => handleFilterChange('id', e.target.value)}
                    title="Type to filter the list to collections whose ID contains this text"
                  />
                </td>
                <td>
                  <input
                    type="text"
                    className="column-filter"
                    placeholder="Filter..."
                    value={filters.name}
                    onChange={(e) => handleFilterChange('name', e.target.value)}
                    title="Type to filter the list to collections whose name contains this text"
                  />
                </td>
                <td>
                  <input
                    type="text"
                    className="column-filter"
                    placeholder="Filter..."
                    value={filters.description}
                    onChange={(e) => handleFilterChange('description', e.target.value)}
                    title="Type to filter the list to collections whose description contains this text"
                  />
                </td>
                <td>
                  <input
                    type="text"
                    className="column-filter"
                    placeholder="Filter..."
                    value={filters.createdUtc}
                    onChange={(e) => handleFilterChange('createdUtc', e.target.value)}
                    title="Type to filter the list to collections whose formatted creation date contains this text"
                  />
                </td>
                <td className="no-filter"></td>
              </tr>
            </thead>
            <tbody>
              {pagedCollections.length === 0 ? (
                <tr>
                  <td colSpan={5} className="empty-row">No collections match your filters.</td>
                </tr>
              ) : (
                pagedCollections.map((collection) => (
                  <tr key={collection.id} className="clickable-row" title="Click to edit this collection" onClick={(event) => onRowClick(event, collection)}>
                    <td><CopyableId value={collection.id} /></td>
                    <td>{collection.name}</td>
                    <td>{collection.description || '-'}</td>
                    <td>{formatDate(collection.createdUtc)}</td>
                    <td>
                      <ActionMenu
                        items={[
                          { label: 'Edit Collection', onClick: () => handleEditCollection(collection) },
                          { label: 'View Metadata', onClick: () => handleViewMetadata(collection) },
                          { label: 'View Documents', onClick: () => handleViewDocuments(collection.id) },
                          { label: 'Add Document', onClick: () => handleAddDocument(collection.id) },
                          { label: 'Schema Constraints', onClick: () => handleViewConstraints(collection) },
                          { label: 'Indexing Config', onClick: () => handleViewIndexing(collection) },
                          { label: 'Rebuild Indexes', onClick: () => handleRebuildIndexes(collection) },
                          { label: 'View JSON', onClick: () => handleViewJson(collection) },
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
        subtitle="Create a collection to define where documents live and how schema enforcement and indexing should behave."
        wide
      >
        <div className="form-group">
          <label className="form-label" title="Required unique name used to identify this collection throughout the dashboard">Name *</label>
          <input
            type="text"
            className="input"
            value={newCollection.name}
            onChange={(e) => setNewCollection({ ...newCollection, name: e.target.value })}
            placeholder="Enter collection name"
            title="Enter a required unique name for the new collection"
          />
        </div>
        <div className="form-group">
          <label className="form-label" title="Optional description shown alongside the collection to explain its purpose">Description</label>
          <input
            type="text"
            className="input"
            value={newCollection.description}
            onChange={(e) => setNewCollection({ ...newCollection, description: e.target.value })}
            placeholder="Optional description"
            title="Enter an optional description explaining what this collection holds"
          />
        </div>
        <div className="form-group">
          <label className="form-label" title="Optional server filesystem path where this collection's documents will be stored">Documents Directory</label>
          <input
            type="text"
            className="input"
            value={newCollection.documentsDirectory}
            onChange={(e) => setNewCollection({ ...newCollection, documentsDirectory: e.target.value })}
            placeholder="e.g., ./documents/my-collection"
            title="Enter the server directory path where documents for this collection should be persisted"
          />
        </div>
        <div className="form-group">
          <label className="form-label" title="Free-form labels attached to the collection that can later be used to filter and search">Labels</label>
          <TagInput
            value={newLabels}
            onChange={setNewLabels}
            placeholder="Add labels..."
          />
        </div>
        <div className="form-group">
          <label className="form-label" title="Key/value metadata pairs stored on the collection for organization and filtering">Tags</label>
          <KeyValueEditor
            value={newTags}
            onChange={setNewTags}
            keyPlaceholder="Tag name"
            valuePlaceholder="Tag value"
          />
        </div>

        <div className="form-section-divider">Schema & Indexing</div>

        <div className="form-group">
          <label className="form-label" title="Controls how strictly documents are validated against field constraints when ingested">Schema Enforcement Mode</label>
          <select
            className="input"
            value={newCollection.schemaEnforcementMode}
            onChange={(e) => setNewCollection({ ...newCollection, schemaEnforcementMode: e.target.value })}
            title="Choose how strictly incoming documents are validated against the field constraints"
          >
            {Object.entries(ENFORCEMENT_MODES).map(([val, { label, description }]) => (
              <option key={val} value={val}>{label} - {description}</option>
            ))}
          </select>
        </div>

        {newCollection.schemaEnforcementMode !== 'none' && (
          <div className="form-group">
            <div className="form-label-row">
              <label className="form-label" title="Per-field type and validation rules enforced on documents added to this collection">Field Constraints</label>
              <button
                className="btn btn-sm btn-secondary"
                onClick={() => setNewConstraints([...newConstraints, {
                  fieldPath: '',
                  dataType: 'string',
                  required: false,
                  nullable: true,
                }])}
                title="Add another field constraint row to validate a specific document field"
              >
                + Add Field
              </button>
            </div>

            {newConstraints.length === 0 ? (
              <div className="empty-constraints">No field constraints defined</div>
            ) : (
              <div className="constraints-list">
                {newConstraints.map((constraint, idx) => (
                  <div key={idx} className="constraint-item">
                    <div className="constraint-row">
                      <input
                        type="text"
                        className="input constraint-field-path"
                        placeholder="Field path (e.g., user.email)"
                        value={constraint.fieldPath || ''}
                        onChange={(e) => {
                          const updated = [...newConstraints]
                          updated[idx] = { ...constraint, fieldPath: e.target.value }
                          setNewConstraints(updated)
                        }}
                        title="Dot-notation path to the document field this constraint applies to (e.g., user.email)"
                      />
                      <select
                        className="input constraint-type"
                        value={constraint.dataType || 'string'}
                        onChange={(e) => {
                          const updated = [...newConstraints]
                          updated[idx] = { ...constraint, dataType: e.target.value }
                          setNewConstraints(updated)
                        }}
                        title="Expected data type that values at this field path must match"
                      >
                        {DATA_TYPES.map(t => (
                          <option key={t} value={t}>{t}</option>
                        ))}
                      </select>
                      <button
                        className="btn btn-sm btn-danger"
                        onClick={() => setNewConstraints(newConstraints.filter((_, i) => i !== idx))}
                        title="Remove this field constraint from the collection"
                      >
                        Remove
                      </button>
                    </div>
                    <div className="constraint-options">
                      <label className="checkbox-label" title="When checked, documents must include a value at this field path">
                        <input
                          type="checkbox"
                          checked={constraint.required || false}
                          onChange={(e) => {
                            const updated = [...newConstraints]
                            updated[idx] = { ...constraint, required: e.target.checked }
                            setNewConstraints(updated)
                          }}
                          title="Require this field to be present in every document"
                        />
                        Required
                      </label>
                      <label className="checkbox-label" title="When checked, this field is allowed to hold a null value">
                        <input
                          type="checkbox"
                          checked={constraint.nullable ?? true}
                          onChange={(e) => {
                            const updated = [...newConstraints]
                            updated[idx] = { ...constraint, nullable: e.target.checked }
                            setNewConstraints(updated)
                          }}
                          title="Allow this field to contain a null value"
                        />
                        Nullable
                      </label>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        )}

        <div className="form-group">
          <label className="form-label" title="Determines which document fields get indexed for search: all fields, only selected fields, or none">Indexing Mode</label>
          <select
            className="input"
            value={newCollection.indexingMode}
            onChange={(e) => setNewCollection({ ...newCollection, indexingMode: e.target.value })}
            title="Choose whether all fields, only specified fields, or no fields are indexed for search"
          >
            {Object.entries(INDEXING_MODES).map(([val, { label, description }]) => (
              <option key={val} value={val}>{label} - {description}</option>
            ))}
          </select>
        </div>

        {newCollection.indexingMode === 'selective' && (
          <div className="form-group">
            <div className="form-label-row">
              <label className="form-label" title="Explicit list of field paths to index when indexing mode is set to selective">Indexed Fields</label>
              <button
                className="btn btn-sm btn-secondary"
                onClick={() => setNewIndexedFields([...newIndexedFields, ''])}
                title="Add another field path to the list of fields that will be indexed"
              >
                + Add Field
              </button>
            </div>

            {newIndexedFields.length === 0 ? (
              <div className="empty-constraints">No indexed fields defined</div>
            ) : (
              <div className="indexed-fields-list">
                {newIndexedFields.map((field, idx) => (
                  <div key={idx} className="indexed-field-item">
                    <input
                      type="text"
                      className="input"
                      placeholder="Field path (e.g., user.name)"
                      value={field}
                      onChange={(e) => {
                        const updated = [...newIndexedFields]
                        updated[idx] = e.target.value
                        setNewIndexedFields(updated)
                      }}
                      title="Dot-notation path of a document field to include in the search index (e.g., user.name)"
                    />
                    <button
                      className="btn btn-sm btn-danger"
                      onClick={() => setNewIndexedFields(newIndexedFields.filter((_, i) => i !== idx))}
                      title="Remove this field from the list of indexed fields"
                    >
                      Remove
                    </button>
                  </div>
                ))}
              </div>
            )}
          </div>
        )}

        <div className="modal-actions">
          <button className="btn btn-secondary" onClick={() => setShowCreateModal(false)} title="Discard this new collection and close the dialog without saving">
            Cancel
          </button>
          <button
            className="btn btn-primary"
            onClick={handleCreate}
            disabled={!newCollection.name}
            title="Create the collection with the settings entered above"
          >
            Create
          </button>
        </div>
      </Modal>

      <Modal
        isOpen={showEditModal}
        onClose={() => setShowEditModal(false)}
        title="Edit Collection"
        subtitle="Update the collection's name and description. Other settings are managed from their own dialogs."
      >
        <div className="form-group">
          <label className="form-label" title="Required unique name used to identify this collection throughout the dashboard">Name *</label>
          <input
            type="text"
            className="input"
            value={editForm.name}
            onChange={(e) => setEditForm({ ...editForm, name: e.target.value })}
            placeholder="Enter collection name"
            title="Enter a required unique name for this collection"
          />
        </div>
        <div className="form-group">
          <label className="form-label" title="Optional description shown alongside the collection to explain its purpose">Description</label>
          <textarea
            className="textarea"
            value={editForm.description}
            onChange={(e) => setEditForm({ ...editForm, description: e.target.value })}
            placeholder="Optional description"
            rows={3}
            title="Enter an optional description explaining what this collection holds"
          />
        </div>
        <div className="modal-actions">
          <button className="btn btn-secondary" onClick={() => setShowEditModal(false)} title="Discard changes and close the dialog without saving">
            Cancel
          </button>
          <button
            className="btn btn-primary"
            onClick={handleSaveEdit}
            disabled={saving || !editForm.name}
            title="Save the updated name and description for this collection"
          >
            {saving ? 'Saving...' : 'Save'}
          </button>
        </div>
      </Modal>

      <DetailModal
        isOpen={showMetadataModal}
        onClose={() => {
          setShowMetadataModal(false)
          setSelectedCollection(null)
        }}
        title="Collection Metadata"
        subtitle="Identifiers and collection-level metadata."
        fields={selectedCollection ? [
          { label: 'ID', value: selectedCollection.id, copyable: true, title: 'System-generated unique identifier for this collection' },
          { label: 'Name', value: selectedCollection.name, title: 'The collection’s display name' },
          selectedCollection.description ? { label: 'Description', value: selectedCollection.description, title: 'Optional description explaining the collection’s purpose' } : null,
          { label: 'Documents Directory', value: selectedCollection.documentsDirectory || '—', mono: true, title: 'Server filesystem path where this collection’s documents are stored' },
          selectedCollection.labels?.length > 0 ? {
            label: 'Labels', inline: true, title: 'Labels attached to this collection for organization and filtering',
            node: <div className="detail-chips">{selectedCollection.labels.map((label, i) => <span key={i} className="label-badge">{label}</span>)}</div>,
          } : null,
          selectedCollection.tags && Object.keys(selectedCollection.tags).length > 0 ? {
            label: 'Tags', inline: true, title: 'Key/value metadata pairs stored on this collection',
            node: (
              <div className="detail-chips">
                {Object.entries(selectedCollection.tags).map(([k, v]) => (
                  <span key={k} className="tag-item">
                    <span className="tag-key">{k}</span>
                    <span className="tag-sep">=</span>
                    <span className="tag-val">{v}</span>
                  </span>
                ))}
              </div>
            ),
          } : null,
          { label: 'Created', value: formatDate(selectedCollection.createdUtc), title: 'When this collection was created' },
        ] : []}
      />

      {/* Schema Constraints Modal */}
      <Modal
        isOpen={showConstraintsModal}
        onClose={() => {
          setShowConstraintsModal(false)
          setSelectedCollection(null)
        }}
        title="Schema Constraints"
        wide
      >
        {selectedCollection && (
          <>
            <div className="form-group">
              <label className="form-label" title="Controls how strictly documents are validated against the field constraints below">Enforcement Mode</label>
              <select
                className="input"
                value={constraints.mode}
                onChange={(e) => setConstraints(prev => ({ ...prev, mode: e.target.value }))}
                title="Choose how strictly documents in this collection are validated against the field constraints"
              >
                {Object.entries(ENFORCEMENT_MODES).map(([val, { label, description }]) => (
                  <option key={val} value={val}>{label} - {description}</option>
                ))}
              </select>
            </div>

            <div className="form-group">
              <div className="form-label-row">
                <label className="form-label" title="Per-field type and validation rules enforced on documents in this collection">Field Constraints</label>
                <button className="btn btn-sm btn-secondary" onClick={handleAddConstraint} title="Add another field constraint row to validate a specific document field">
                  + Add Field
                </button>
              </div>

              {constraints.fields.length === 0 ? (
                <div className="empty-constraints">No field constraints defined</div>
              ) : (
                <div className="constraints-list">
                  {constraints.fields.map((constraint, idx) => (
                    <div key={idx} className="constraint-item">
                      <div className="constraint-row">
                        <input
                          type="text"
                          className="input constraint-field-path"
                          placeholder="Field path (e.g., user.email)"
                          value={constraint.fieldPath || ''}
                          onChange={(e) => handleUpdateConstraint(idx, 'fieldPath', e.target.value)}
                          title="Dot-notation path to the document field this constraint applies to (e.g., user.email)"
                        />
                        <select
                          className="input constraint-type"
                          value={constraint.dataType || 'string'}
                          onChange={(e) => handleUpdateConstraint(idx, 'dataType', e.target.value)}
                          title="Expected data type that values at this field path must match"
                        >
                          {DATA_TYPES.map(t => (
                            <option key={t} value={t}>{t}</option>
                          ))}
                        </select>
                        <button
                          className="btn btn-sm btn-danger"
                          onClick={() => handleRemoveConstraint(idx)}
                          title="Remove this field constraint"
                        >
                          Remove
                        </button>
                      </div>
                      <div className="constraint-options">
                        <label className="checkbox-label" title="When checked, documents must include a value at this field path">
                          <input
                            type="checkbox"
                            checked={constraint.required || false}
                            onChange={(e) => handleUpdateConstraint(idx, 'required', e.target.checked)}
                            title="Require this field to be present in every document"
                          />
                          Required
                        </label>
                        <label className="checkbox-label" title="When checked, this field is allowed to hold a null value">
                          <input
                            type="checkbox"
                            checked={constraint.nullable ?? true}
                            onChange={(e) => handleUpdateConstraint(idx, 'nullable', e.target.checked)}
                            title="Allow this field to contain a null value"
                          />
                          Nullable
                        </label>
                      </div>
                      {(constraint.dataType === 'string') && (
                        <div className="constraint-row">
                          <input
                            type="text"
                            className="input"
                            placeholder="Regex pattern (optional)"
                            value={constraint.regexPattern || ''}
                            onChange={(e) => handleUpdateConstraint(idx, 'regexPattern', e.target.value)}
                            title="Optional regular expression that string values at this field must match"
                          />
                        </div>
                      )}
                      {(constraint.dataType === 'integer' || constraint.dataType === 'number') && (
                        <div className="constraint-row">
                          <input
                            type="number"
                            className="input"
                            placeholder="Min value"
                            value={constraint.minValue ?? ''}
                            onChange={(e) => handleUpdateConstraint(idx, 'minValue', e.target.value ? parseFloat(e.target.value) : null)}
                            title="Smallest numeric value allowed for this field (inclusive)"
                          />
                          <input
                            type="number"
                            className="input"
                            placeholder="Max value"
                            value={constraint.maxValue ?? ''}
                            onChange={(e) => handleUpdateConstraint(idx, 'maxValue', e.target.value ? parseFloat(e.target.value) : null)}
                            title="Largest numeric value allowed for this field (inclusive)"
                          />
                        </div>
                      )}
                      {(constraint.dataType === 'string' || constraint.dataType === 'array') && (
                        <div className="constraint-row">
                          <input
                            type="number"
                            className="input"
                            placeholder="Min length"
                            value={constraint.minLength ?? ''}
                            onChange={(e) => handleUpdateConstraint(idx, 'minLength', e.target.value ? parseInt(e.target.value) : null)}
                            title="Minimum number of characters (string) or items (array) allowed for this field"
                          />
                          <input
                            type="number"
                            className="input"
                            placeholder="Max length"
                            value={constraint.maxLength ?? ''}
                            onChange={(e) => handleUpdateConstraint(idx, 'maxLength', e.target.value ? parseInt(e.target.value) : null)}
                            title="Maximum number of characters (string) or items (array) allowed for this field"
                          />
                        </div>
                      )}
                    </div>
                  ))}
                </div>
              )}
            </div>

            <div className="modal-actions">
              <button className="btn btn-secondary" onClick={() => setShowConstraintsModal(false)} title="Close without saving changes to the schema constraints">
                Cancel
              </button>
              <button
                className="btn btn-primary"
                onClick={handleSaveConstraints}
                disabled={saving}
                title="Save the enforcement mode and field constraints for this collection"
              >
                {saving ? 'Saving...' : 'Save'}
              </button>
            </div>
          </>
        )}
      </Modal>

      {/* Indexing Configuration Modal */}
      <Modal
        isOpen={showIndexingModal}
        onClose={() => {
          setShowIndexingModal(false)
          setSelectedCollection(null)
        }}
        title="Indexing Configuration"
        wide
      >
        {selectedCollection && (
          <>
            <div className="form-group">
              <label className="form-label" title="Determines which document fields are indexed for search: all, only selected, or none">Indexing Mode</label>
              <select
                className="input"
                value={indexing.mode}
                onChange={(e) => setIndexing(prev => ({ ...prev, mode: e.target.value }))}
                title="Choose whether all fields, only specified fields, or no fields are indexed for search"
              >
                {Object.entries(INDEXING_MODES).map(([val, { label, description }]) => (
                  <option key={val} value={val}>{label} - {description}</option>
                ))}
              </select>
            </div>

            {indexing.mode === 'selective' && (
              <div className="form-group">
                <div className="form-label-row">
                  <label className="form-label" title="Explicit list of field paths to index when indexing mode is selective">Indexed Fields</label>
                  <button className="btn btn-sm btn-secondary" onClick={handleAddIndexedField} title="Add another field path to the list of fields that will be indexed">
                    + Add Field
                  </button>
                </div>

                {indexing.fields.length === 0 ? (
                  <div className="empty-constraints">No indexed fields defined</div>
                ) : (
                  <div className="indexed-fields-list">
                    {indexing.fields.map((field, idx) => (
                      <div key={idx} className="indexed-field-item">
                        <input
                          type="text"
                          className="input"
                          placeholder="Field path (e.g., user.name)"
                          value={field}
                          onChange={(e) => handleUpdateIndexedField(idx, e.target.value)}
                          title="Dot-notation path of a document field to include in the search index (e.g., user.name)"
                        />
                        <button
                          className="btn btn-sm btn-danger"
                          onClick={() => handleRemoveIndexedField(idx)}
                          title="Remove this field from the list of indexed fields"
                        >
                          Remove
                        </button>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            )}

            <div className="modal-actions">
              <button className="btn btn-secondary" onClick={() => setShowIndexingModal(false)} title="Close without saving changes to the indexing configuration">
                Cancel
              </button>
              <button
                className="btn btn-primary"
                onClick={() => handleSaveIndexing(false)}
                disabled={saving}
                title="Save the indexing configuration without rebuilding existing indexes"
              >
                {saving ? 'Saving...' : 'Save'}
              </button>
              <button
                className="btn btn-primary"
                onClick={() => handleSaveIndexing(true)}
                disabled={saving}
                title="Save the indexing configuration and immediately rebuild all indexes for existing documents"
              >
                {saving ? 'Saving...' : 'Save & Rebuild'}
              </button>
            </div>
          </>
        )}
      </Modal>

      {/* Rebuild Indexes Modal */}
      <Modal
        isOpen={showRebuildModal}
        onClose={() => {
          if (!rebuildProgress) {
            setShowRebuildModal(false)
            setSelectedCollection(null)
            setRebuildResult(null)
          }
        }}
        title="Rebuild Indexes"
      >
        {selectedCollection && (
          <>
            {!rebuildProgress && !rebuildResult && (
              <>
                <p className="rebuild-info">
                  This will rebuild all indexes for the collection "{selectedCollection.name}".
                  Existing documents will be re-indexed according to the current indexing configuration.
                </p>
                <div className="modal-actions">
                  <button className="btn btn-secondary" onClick={() => setShowRebuildModal(false)} title="Close this dialog without rebuilding any indexes">
                    Cancel
                  </button>
                  <button className="btn btn-primary" onClick={() => handleStartRebuild(false)} title="Re-index all documents while keeping every existing index, even unused ones">
                    Rebuild (Keep All)
                  </button>
                  <button className="btn btn-warning" onClick={() => handleStartRebuild(true)} title="Re-index all documents and drop indexes no longer referenced by the current configuration">
                    Rebuild (Drop Unused)
                  </button>
                </div>
              </>
            )}

            {rebuildProgress && (
              <div className="rebuild-progress">
                <div className="progress-phase">{rebuildProgress.phase}</div>
                <div className="progress-bar">
                  <div
                    className="progress-fill"
                    style={{ width: `${rebuildProgress.percent}%` }}
                  />
                </div>
                <div className="progress-percent">{rebuildProgress.percent}%</div>
              </div>
            )}

            {rebuildResult && (
              <div className="rebuild-result">
                <div className={`rebuild-status ${rebuildResult.success ? 'success' : 'error'}`}>
                  {rebuildResult.success ? 'Rebuild Complete' : 'Rebuild Failed'}
                </div>
                <div className="rebuild-stats">
                  <div className="stat-item">
                    <span className="stat-label">Documents Processed:</span>
                    <span className="stat-value">{rebuildResult.documentsProcessed}</span>
                  </div>
                  <div className="stat-item">
                    <span className="stat-label">Indexes Created:</span>
                    <span className="stat-value">{rebuildResult.indexesCreated}</span>
                  </div>
                  <div className="stat-item">
                    <span className="stat-label">Indexes Dropped:</span>
                    <span className="stat-value">{rebuildResult.indexesDropped}</span>
                  </div>
                  <div className="stat-item">
                    <span className="stat-label">Values Inserted:</span>
                    <span className="stat-value">{rebuildResult.valuesInserted}</span>
                  </div>
                  <div className="stat-item">
                    <span className="stat-label">Duration:</span>
                    <span className="stat-value">{rebuildResult.duration}ms</span>
                  </div>
                </div>
                {rebuildResult.errors?.length > 0 && (
                  <div className="rebuild-errors">
                    <div className="errors-label">Errors:</div>
                    {rebuildResult.errors.map((err, i) => (
                      <div key={i} className="error-item">{err}</div>
                    ))}
                  </div>
                )}
                <div className="modal-actions">
                  <button
                    className="btn btn-primary"
                    onClick={() => {
                      setShowRebuildModal(false)
                      setSelectedCollection(null)
                      setRebuildResult(null)
                    }}
                    title="Dismiss the rebuild results and close this dialog"
                  >
                    Close
                  </button>
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
