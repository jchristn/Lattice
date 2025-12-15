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

// Schema enforcement mode labels
const ENFORCEMENT_MODES = {
  0: { label: 'None', description: 'No validation' },
  1: { label: 'Strict', description: 'All constraints must pass' },
  2: { label: 'Flexible', description: 'Warns but allows' },
  3: { label: 'Partial', description: 'Only validates constrained fields' },
}

// Indexing mode labels
const INDEXING_MODES = {
  0: { label: 'All', description: 'Index all fields' },
  1: { label: 'Selective', description: 'Index only specified fields' },
  2: { label: 'None', description: 'No indexing' },
}

// Data type options for constraints
const DATA_TYPES = ['string', 'integer', 'number', 'boolean', 'array', 'object']

export default function Collections() {
  const { api, setError } = useApp()
  const navigate = useNavigate()
  const [collections, setCollections] = useState([])
  const [loading, setLoading] = useState(true)
  const [showCreateModal, setShowCreateModal] = useState(false)
  const [showMetadataModal, setShowMetadataModal] = useState(false)
  const [showConstraintsModal, setShowConstraintsModal] = useState(false)
  const [showIndexingModal, setShowIndexingModal] = useState(false)
  const [showRebuildModal, setShowRebuildModal] = useState(false)
  const [selectedCollection, setSelectedCollection] = useState(null)
  const [constraints, setConstraints] = useState({ mode: 0, fields: [] })
  const [indexing, setIndexing] = useState({ mode: 0, fields: [] })
  const [rebuildProgress, setRebuildProgress] = useState(null)
  const [rebuildResult, setRebuildResult] = useState(null)
  const [saving, setSaving] = useState(false)
  const [newCollection, setNewCollection] = useState({
    name: '',
    description: '',
    documentsDirectory: '',
    schemaEnforcementMode: 0,
    indexingMode: 0,
  })
  const [newLabels, setNewLabels] = useState([])
  const [newTags, setNewTags] = useState({})
  const [newConstraints, setNewConstraints] = useState([])
  const [newIndexedFields, setNewIndexedFields] = useState([])

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
        schemaEnforcementMode: newCollection.schemaEnforcementMode,
        fieldConstraints: newConstraints.length > 0 ? newConstraints : null,
        indexingMode: newCollection.indexingMode,
        indexedFields: newIndexedFields.length > 0 ? newIndexedFields : null,
      })
      setShowCreateModal(false)
      setNewCollection({ name: '', description: '', documentsDirectory: '', schemaEnforcementMode: 0, indexingMode: 0 })
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

  const handleViewMetadata = (collection) => {
    setSelectedCollection(collection)
    setShowMetadataModal(true)
  }

  const handleViewConstraints = async (collection) => {
    setSelectedCollection(collection)
    // Set defaults first, then try to load
    setConstraints({
      mode: collection.schemaEnforcementMode || 0,
      fields: [],
    })
    setShowConstraintsModal(true)

    try {
      const data = await api.getCollectionConstraints(collection.id)
      if (data && Array.isArray(data)) {
        setConstraints({
          mode: collection.schemaEnforcementMode || 0,
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
      mode: collection.indexingMode || 0,
      fields: [],
    })
    setShowIndexingModal(true)

    try {
      const data = await api.getCollectionIndexedFields(collection.id)
      if (data && Array.isArray(data)) {
        setIndexing({
          mode: collection.indexingMode || 0,
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
                          { label: 'Schema Constraints', onClick: () => handleViewConstraints(collection) },
                          { label: 'Indexing Config', onClick: () => handleViewIndexing(collection) },
                          { label: 'Rebuild Indexes', onClick: () => handleRebuildIndexes(collection) },
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

        <div className="form-section-divider">Schema & Indexing</div>

        <div className="form-group">
          <label className="form-label">Schema Enforcement Mode</label>
          <select
            className="input"
            value={newCollection.schemaEnforcementMode}
            onChange={(e) => setNewCollection({ ...newCollection, schemaEnforcementMode: parseInt(e.target.value) })}
          >
            {Object.entries(ENFORCEMENT_MODES).map(([val, { label, description }]) => (
              <option key={val} value={val}>{label} - {description}</option>
            ))}
          </select>
        </div>

        {newCollection.schemaEnforcementMode > 0 && (
          <div className="form-group">
            <div className="form-label-row">
              <label className="form-label">Field Constraints</label>
              <button
                className="btn btn-sm btn-secondary"
                onClick={() => setNewConstraints([...newConstraints, {
                  fieldPath: '',
                  dataType: 'string',
                  required: false,
                  nullable: true,
                }])}
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
                      />
                      <select
                        className="input constraint-type"
                        value={constraint.dataType || 'string'}
                        onChange={(e) => {
                          const updated = [...newConstraints]
                          updated[idx] = { ...constraint, dataType: e.target.value }
                          setNewConstraints(updated)
                        }}
                      >
                        {DATA_TYPES.map(t => (
                          <option key={t} value={t}>{t}</option>
                        ))}
                      </select>
                      <button
                        className="btn btn-sm btn-danger"
                        onClick={() => setNewConstraints(newConstraints.filter((_, i) => i !== idx))}
                      >
                        Remove
                      </button>
                    </div>
                    <div className="constraint-options">
                      <label className="checkbox-label">
                        <input
                          type="checkbox"
                          checked={constraint.required || false}
                          onChange={(e) => {
                            const updated = [...newConstraints]
                            updated[idx] = { ...constraint, required: e.target.checked }
                            setNewConstraints(updated)
                          }}
                        />
                        Required
                      </label>
                      <label className="checkbox-label">
                        <input
                          type="checkbox"
                          checked={constraint.nullable ?? true}
                          onChange={(e) => {
                            const updated = [...newConstraints]
                            updated[idx] = { ...constraint, nullable: e.target.checked }
                            setNewConstraints(updated)
                          }}
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
          <label className="form-label">Indexing Mode</label>
          <select
            className="input"
            value={newCollection.indexingMode}
            onChange={(e) => setNewCollection({ ...newCollection, indexingMode: parseInt(e.target.value) })}
          >
            {Object.entries(INDEXING_MODES).map(([val, { label, description }]) => (
              <option key={val} value={val}>{label} - {description}</option>
            ))}
          </select>
        </div>

        {newCollection.indexingMode === 1 && (
          <div className="form-group">
            <div className="form-label-row">
              <label className="form-label">Indexed Fields</label>
              <button
                className="btn btn-sm btn-secondary"
                onClick={() => setNewIndexedFields([...newIndexedFields, ''])}
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
                    />
                    <button
                      className="btn btn-sm btn-danger"
                      onClick={() => setNewIndexedFields(newIndexedFields.filter((_, i) => i !== idx))}
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

      {/* Schema Constraints Modal */}
      <Modal
        isOpen={showConstraintsModal}
        onClose={() => {
          setShowConstraintsModal(false)
          setSelectedCollection(null)
        }}
        title="Schema Constraints"
      >
        {selectedCollection && (
          <>
            <div className="form-group">
              <label className="form-label">Enforcement Mode</label>
              <select
                className="input"
                value={constraints.mode}
                onChange={(e) => setConstraints(prev => ({ ...prev, mode: parseInt(e.target.value) }))}
              >
                {Object.entries(ENFORCEMENT_MODES).map(([val, { label, description }]) => (
                  <option key={val} value={val}>{label} - {description}</option>
                ))}
              </select>
            </div>

            <div className="form-group">
              <div className="form-label-row">
                <label className="form-label">Field Constraints</label>
                <button className="btn btn-sm btn-secondary" onClick={handleAddConstraint}>
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
                        />
                        <select
                          className="input constraint-type"
                          value={constraint.dataType || 'string'}
                          onChange={(e) => handleUpdateConstraint(idx, 'dataType', e.target.value)}
                        >
                          {DATA_TYPES.map(t => (
                            <option key={t} value={t}>{t}</option>
                          ))}
                        </select>
                        <button
                          className="btn btn-sm btn-danger"
                          onClick={() => handleRemoveConstraint(idx)}
                        >
                          Remove
                        </button>
                      </div>
                      <div className="constraint-options">
                        <label className="checkbox-label">
                          <input
                            type="checkbox"
                            checked={constraint.required || false}
                            onChange={(e) => handleUpdateConstraint(idx, 'required', e.target.checked)}
                          />
                          Required
                        </label>
                        <label className="checkbox-label">
                          <input
                            type="checkbox"
                            checked={constraint.nullable ?? true}
                            onChange={(e) => handleUpdateConstraint(idx, 'nullable', e.target.checked)}
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
                          />
                          <input
                            type="number"
                            className="input"
                            placeholder="Max value"
                            value={constraint.maxValue ?? ''}
                            onChange={(e) => handleUpdateConstraint(idx, 'maxValue', e.target.value ? parseFloat(e.target.value) : null)}
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
                          />
                          <input
                            type="number"
                            className="input"
                            placeholder="Max length"
                            value={constraint.maxLength ?? ''}
                            onChange={(e) => handleUpdateConstraint(idx, 'maxLength', e.target.value ? parseInt(e.target.value) : null)}
                          />
                        </div>
                      )}
                    </div>
                  ))}
                </div>
              )}
            </div>

            <div className="modal-actions">
              <button className="btn btn-secondary" onClick={() => setShowConstraintsModal(false)}>
                Cancel
              </button>
              <button
                className="btn btn-primary"
                onClick={handleSaveConstraints}
                disabled={saving}
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
      >
        {selectedCollection && (
          <>
            <div className="form-group">
              <label className="form-label">Indexing Mode</label>
              <select
                className="input"
                value={indexing.mode}
                onChange={(e) => setIndexing(prev => ({ ...prev, mode: parseInt(e.target.value) }))}
              >
                {Object.entries(INDEXING_MODES).map(([val, { label, description }]) => (
                  <option key={val} value={val}>{label} - {description}</option>
                ))}
              </select>
            </div>

            {indexing.mode === 1 && (
              <div className="form-group">
                <div className="form-label-row">
                  <label className="form-label">Indexed Fields</label>
                  <button className="btn btn-sm btn-secondary" onClick={handleAddIndexedField}>
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
                        />
                        <button
                          className="btn btn-sm btn-danger"
                          onClick={() => handleRemoveIndexedField(idx)}
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
              <button className="btn btn-secondary" onClick={() => setShowIndexingModal(false)}>
                Cancel
              </button>
              <button
                className="btn btn-primary"
                onClick={() => handleSaveIndexing(false)}
                disabled={saving}
              >
                {saving ? 'Saving...' : 'Save'}
              </button>
              <button
                className="btn btn-primary"
                onClick={() => handleSaveIndexing(true)}
                disabled={saving}
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
                  <button className="btn btn-secondary" onClick={() => setShowRebuildModal(false)}>
                    Cancel
                  </button>
                  <button className="btn btn-primary" onClick={() => handleStartRebuild(false)}>
                    Rebuild (Keep All)
                  </button>
                  <button className="btn btn-warning" onClick={() => handleStartRebuild(true)}>
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
                  >
                    Close
                  </button>
                </div>
              </div>
            )}
          </>
        )}
      </Modal>
    </div>
  )
}
