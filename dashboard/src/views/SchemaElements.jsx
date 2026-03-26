import { useEffect, useMemo, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useApp } from '../context/AppContext'
import ActionMenu from '../components/ActionMenu'
import JsonViewerModal from '../components/JsonViewerModal'
import TablePagination from '../components/TablePagination'
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
  const [page, setPage] = useState(0)
  const [pageSize, setPageSize] = useState(25)
  const [jsonViewer, setJsonViewer] = useState({ open: false, title: '', subtitle: '', identifier: '', value: null })
  const [filters, setFilters] = useState({
    key: '',
    dataType: '',
    nullable: '',
  })
  const [sort, setSort] = useState({ column: 'key', direction: 'asc' })

  const filteredElements = useMemo(() => {
    let result = [...elements]

    if (filters.key) {
      const query = filters.key.toLowerCase()
      result = result.filter((element) => element.key.toLowerCase().includes(query))
    }
    if (filters.dataType) {
      const query = filters.dataType.toLowerCase()
      result = result.filter((element) => element.dataType.toLowerCase().includes(query))
    }
    if (filters.nullable) {
      const query = filters.nullable.toLowerCase()
      result = result.filter((element) => (element.nullable ? 'yes' : 'no').includes(query))
    }

    result.sort((a, b) => {
      const aValue = sort.column === 'nullable' ? (a.nullable ? 'yes' : 'no') : (a[sort.column] || '')
      const bValue = sort.column === 'nullable' ? (b.nullable ? 'yes' : 'no') : (b[sort.column] || '')
      const comparison = aValue.localeCompare(bValue)
      return sort.direction === 'asc' ? comparison : -comparison
    })

    return result
  }, [elements, filters, sort])

  const totalPages = Math.max(1, Math.ceil(filteredElements.length / pageSize))
  const pagedElements = filteredElements.slice(page * pageSize, (page + 1) * pageSize)

  useEffect(() => {
    const loadSchemas = async () => {
      try {
        const data = await api.getSchemas()
        setSchemas(data || [])
      } catch (err) {
        console.error('Failed to load schemas:', err)
      } finally {
        setSchemasLoaded(true)
      }
    }

    if (api) {
      loadSchemas()
    }
  }, [api])

  useEffect(() => {
    if (schemasLoaded && !schemaId && schemas.length > 0) {
      navigate(`/schemas/${schemas[0].id}/elements`, { replace: true })
    }
  }, [schemasLoaded, schemaId, schemas, navigate])

  const loadElements = async () => {
    if (!schemaId) {
      setLoading(false)
      setElements([])
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

  useEffect(() => {
    if (api && schemaId) {
      loadElements()
    } else if (!schemaId) {
      setLoading(false)
    }
  }, [api, schemaId])

  useEffect(() => {
    setPage(0)
  }, [filters, sort, schemaId])

  useEffect(() => {
    if (page > totalPages - 1) {
      setPage(Math.max(totalPages - 1, 0))
    }
  }, [page, totalPages])

  const handleSchemaChange = (event) => {
    const newSchemaId = event.target.value
    if (newSchemaId) {
      navigate(`/schemas/${newSchemaId}/elements`)
    }
  }

  const handleManualIdSubmit = (event) => {
    event.preventDefault()
    const trimmedId = manualSchemaId.trim()
    if (trimmedId) {
      navigate(`/schemas/${trimmedId}/elements`)
      setManualSchemaId('')
    }
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

  const handleViewIndexTables = (element) => {
    navigate(`/tables?key=${encodeURIComponent(element.key)}`)
  }

  const handleViewJson = (element) => {
    setJsonViewer({
      open: true,
      title: 'Schema Element JSON',
      subtitle: 'This schema element explains one flattened field and its inferred type characteristics.',
      identifier: element.id,
      value: element,
    })
  }

  if (loading) {
    return <div className="loading">Loading...</div>
  }

  return (
    <div className="schema-elements">
      <div className="page-header">
        <div>
          <h1 className="page-title">Schema Elements</h1>
          <p className="page-subtitle">Inspect the individual flattened fields within a schema so you can verify inferred types, nullability, and downstream index-table mappings.</p>
          <div className="schema-selector-row">
            <div className="schema-selector">
              <label className="schema-selector-label">Schema:</label>
              <select className="schema-selector-select" value={schemaId || ''} onChange={handleSchemaChange}>
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
                onChange={(event) => setManualSchemaId(event.target.value)}
                placeholder="Paste schema ID..."
              />
              <button type="submit" className="btn btn-secondary btn-sm" disabled={!manualSchemaId.trim()}>
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
          <TablePagination
            totalRecords={filteredElements.length}
            currentPage={page}
            totalPages={totalPages}
            onPageChange={setPage}
            onRefresh={loadElements}
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
                <th className={`sortable ${sort.column === 'key' ? 'sorted' : ''}`} onClick={() => handleSort('key')}>
                  <span className="th-content">Key <span className="sort-icon">{getSortIcon('key')}</span></span>
                </th>
                <th className={`sortable ${sort.column === 'dataType' ? 'sorted' : ''}`} onClick={() => handleSort('dataType')}>
                  <span className="th-content">Data Type <span className="sort-icon">{getSortIcon('dataType')}</span></span>
                </th>
                <th className={`sortable ${sort.column === 'nullable' ? 'sorted' : ''}`} onClick={() => handleSort('nullable')}>
                  <span className="th-content">Nullable <span className="sort-icon">{getSortIcon('nullable')}</span></span>
                </th>
                <th>Actions</th>
              </tr>
              <tr className="filter-row">
                <td><input type="text" className="column-filter" placeholder="Filter..." value={filters.key} onChange={(event) => handleFilterChange('key', event.target.value)} /></td>
                <td><input type="text" className="column-filter" placeholder="Filter..." value={filters.dataType} onChange={(event) => handleFilterChange('dataType', event.target.value)} /></td>
                <td><input type="text" className="column-filter" placeholder="Filter..." value={filters.nullable} onChange={(event) => handleFilterChange('nullable', event.target.value)} /></td>
                <td className="no-filter"></td>
              </tr>
            </thead>
            <tbody>
              {pagedElements.length === 0 ? (
                <tr>
                  <td colSpan={4} className="empty-row">No elements match your filters.</td>
                </tr>
              ) : (
                pagedElements.map((element) => (
                  <tr key={element.id}>
                    <td className="monospace">{element.key}</td>
                    <td>{element.dataType}</td>
                    <td>{element.nullable ? 'Yes' : 'No'}</td>
                    <td>
                      <ActionMenu
                        items={[
                          { label: 'View Index Tables', onClick: () => handleViewIndexTables(element) },
                          { label: 'View JSON', onClick: () => handleViewJson(element) },
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
