import { useState, useEffect, useMemo } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useApp } from '../context/AppContext'
import CopyableId from '../components/CopyableId'
import './SchemaElements.css'

export default function SchemaElements() {
  const { schemaId } = useParams()
  const navigate = useNavigate()
  const { api, setError } = useApp()
  const [schemas, setSchemas] = useState([])
  const [elements, setElements] = useState([])
  const [loading, setLoading] = useState(true)
  const [schemasLoaded, setSchemasLoaded] = useState(false)
  const [manualSchemaId, setManualSchemaId] = useState('')

  // Per-column filters
  const [filters, setFilters] = useState({
    key: '',
    dataType: '',
    nullable: '',
  })

  // Sorting state
  const [sort, setSort] = useState({ column: 'key', direction: 'asc' })

  // Filter and sort elements
  const filteredElements = useMemo(() => {
    let result = [...elements]

    // Apply column filters
    if (filters.key) {
      const query = filters.key.toLowerCase()
      result = result.filter(e => e.key.toLowerCase().includes(query))
    }
    if (filters.dataType) {
      const query = filters.dataType.toLowerCase()
      result = result.filter(e => e.dataType.toLowerCase().includes(query))
    }
    if (filters.nullable) {
      const query = filters.nullable.toLowerCase()
      const nullableStr = (e) => e.nullable ? 'yes' : 'no'
      result = result.filter(e => nullableStr(e).includes(query))
    }

    // Apply sorting
    if (sort.column) {
      result.sort((a, b) => {
        let aVal, bVal
        if (sort.column === 'nullable') {
          aVal = a.nullable ? 'yes' : 'no'
          bVal = b.nullable ? 'yes' : 'no'
        } else {
          aVal = a[sort.column] || ''
          bVal = b[sort.column] || ''
        }
        const comparison = aVal.localeCompare(bVal)
        return sort.direction === 'asc' ? comparison : -comparison
      })
    }

    return result
  }, [elements, filters, sort])

  // Load all schemas
  useEffect(() => {
    const loadSchemas = async () => {
      try {
        const data = await api.getSchemas()
        setSchemas(data || [])
        setSchemasLoaded(true)
      } catch (err) {
        console.error('Failed to load schemas:', err)
        setSchemasLoaded(true)
      }
    }
    if (api) {
      loadSchemas()
    }
  }, [api])

  // Auto-navigate to first schema if none selected
  useEffect(() => {
    if (schemasLoaded && !schemaId && schemas.length > 0) {
      navigate(`/schemas/${schemas[0].id}/elements`, { replace: true })
    }
  }, [schemasLoaded, schemaId, schemas, navigate])

  // Load elements for selected schema
  useEffect(() => {
    const loadElements = async () => {
      if (!schemaId) {
        setLoading(false)
        return
      }
      try {
        setLoading(true)
        const elementsData = await api.getSchemaElements(schemaId)
        setElements(elementsData || [])
      } catch (err) {
        setError('Failed to load schema elements: ' + err.message)
      } finally {
        setLoading(false)
      }
    }
    if (api && schemaId) {
      loadElements()
    } else if (!schemaId) {
      setLoading(false)
    }
  }, [api, schemaId])

  const handleSchemaChange = (e) => {
    const newSchemaId = e.target.value
    if (newSchemaId) {
      navigate(`/schemas/${newSchemaId}/elements`)
    }
  }

  const handleManualIdSubmit = (e) => {
    e.preventDefault()
    const trimmedId = manualSchemaId.trim()
    if (trimmedId) {
      navigate(`/schemas/${trimmedId}/elements`)
      setManualSchemaId('')
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
    <div className="schema-elements">
      <div className="page-header">
        <div>
          <h1 className="page-title">Schema Elements</h1>
          <div className="schema-selector-row">
            <div className="schema-selector">
              <label className="schema-selector-label">Schema:</label>
              <select
                className="schema-selector-select"
                value={schemaId || ''}
                onChange={handleSchemaChange}
              >
                <option value="">Select a schema...</option>
                {schemas.map((schema) => (
                  <option key={schema.id} value={schema.id}>
                    {schema.id}
                  </option>
                ))}
              </select>
            </div>
            <span className="schema-selector-or">or</span>
            <form className="schema-id-form" onSubmit={handleManualIdSubmit}>
              <input
                type="text"
                className="schema-id-input"
                value={manualSchemaId}
                onChange={(e) => setManualSchemaId(e.target.value)}
                placeholder="Paste schema ID..."
              />
              <button
                type="submit"
                className="btn btn-secondary btn-sm"
                disabled={!manualSchemaId.trim()}
              >
                Go
              </button>
            </form>
          </div>
        </div>
      </div>

      {!schemaId ? (
        <div className="empty-state">
          <p>Select a schema to view its elements.</p>
        </div>
      ) : elements.length === 0 ? (
        <div className="empty-state">
          <p>No elements found for this schema.</p>
        </div>
      ) : (
        <div className="card">
          <div className="table-results-count">
            Showing {filteredElements.length} of {elements.length} elements
          </div>
          <table className="table">
            <thead>
              <tr>
                <th
                  className={`sortable ${sort.column === 'key' ? 'sorted' : ''}`}
                  onClick={() => handleSort('key')}
                >
                  <span className="th-content">
                    Key
                    <span className="sort-icon">{getSortIcon('key')}</span>
                  </span>
                </th>
                <th
                  className={`sortable ${sort.column === 'dataType' ? 'sorted' : ''}`}
                  onClick={() => handleSort('dataType')}
                >
                  <span className="th-content">
                    Data Type
                    <span className="sort-icon">{getSortIcon('dataType')}</span>
                  </span>
                </th>
                <th
                  className={`sortable ${sort.column === 'nullable' ? 'sorted' : ''}`}
                  onClick={() => handleSort('nullable')}
                >
                  <span className="th-content">
                    Nullable
                    <span className="sort-icon">{getSortIcon('nullable')}</span>
                  </span>
                </th>
              </tr>
              <tr className="filter-row">
                <td>
                  <input
                    type="text"
                    className="column-filter"
                    placeholder="Filter..."
                    value={filters.key}
                    onChange={(e) => handleFilterChange('key', e.target.value)}
                  />
                </td>
                <td>
                  <input
                    type="text"
                    className="column-filter"
                    placeholder="Filter..."
                    value={filters.dataType}
                    onChange={(e) => handleFilterChange('dataType', e.target.value)}
                  />
                </td>
                <td>
                  <input
                    type="text"
                    className="column-filter"
                    placeholder="Filter..."
                    value={filters.nullable}
                    onChange={(e) => handleFilterChange('nullable', e.target.value)}
                  />
                </td>
              </tr>
            </thead>
            <tbody>
              {filteredElements.length === 0 ? (
                <tr>
                  <td colSpan={3} className="empty-row">No elements match your filters.</td>
                </tr>
              ) : (
                filteredElements.map((element) => (
                  <tr key={element.id}>
                    <td className="monospace">{element.key}</td>
                    <td>{element.dataType}</td>
                    <td>{element.nullable ? 'Yes' : 'No'}</td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
