import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useApp } from '../context/AppContext'
import { formatDate } from '../utils/api'
import ActionMenu from '../components/ActionMenu'
import CopyableId from '../components/CopyableId'
import JsonViewerModal from '../components/JsonViewerModal'
import TablePagination from '../components/TablePagination'
import './Schemas.css'

export default function Schemas() {
  const navigate = useNavigate()
  const { api, setError } = useApp()
  const [schemas, setSchemas] = useState([])
  const [loading, setLoading] = useState(true)
  const [page, setPage] = useState(0)
  const [pageSize, setPageSize] = useState(25)
  const [filters, setFilters] = useState({
    id: '',
    hash: '',
    createdUtc: '',
  })
  const [sort, setSort] = useState({ column: 'id', direction: 'asc' })
  const [jsonViewer, setJsonViewer] = useState({ open: false, title: '', subtitle: '', identifier: '', value: null })

  const loadSchemas = async () => {
    try {
      setLoading(true)
      const data = await api.getSchemas()
      setSchemas(data || [])
    } catch (err) {
      setError('Failed to load schemas: ' + err.message)
    } finally {
      setLoading(false)
    }
  }

  const filteredSchemas = useMemo(() => {
    const result = [...schemas]

    if (filters.id) {
      const query = filters.id.toLowerCase()
      result.splice(0, result.length, ...result.filter((schema) => schema.id.toLowerCase().includes(query)))
    }
    if (filters.hash) {
      const query = filters.hash.toLowerCase()
      result.splice(0, result.length, ...result.filter((schema) => schema.hash.toLowerCase().includes(query)))
    }
    if (filters.createdUtc) {
      const query = filters.createdUtc.toLowerCase()
      result.splice(0, result.length, ...result.filter((schema) => formatDate(schema.createdUtc).toLowerCase().includes(query)))
    }

    result.sort((a, b) => {
      const aValue = sort.column === 'createdUtc' ? (a.createdUtc || '') : (a[sort.column] || '')
      const bValue = sort.column === 'createdUtc' ? (b.createdUtc || '') : (b[sort.column] || '')
      const comparison = aValue.localeCompare(bValue)
      return sort.direction === 'asc' ? comparison : -comparison
    })

    return result
  }, [schemas, filters, sort])

  const totalPages = Math.max(1, Math.ceil(filteredSchemas.length / pageSize))
  const pagedSchemas = filteredSchemas.slice(page * pageSize, (page + 1) * pageSize)

  useEffect(() => {
    loadSchemas()
  }, [api])

  useEffect(() => {
    setPage(0)
  }, [filters, sort])

  useEffect(() => {
    if (page > totalPages - 1) {
      setPage(Math.max(totalPages - 1, 0))
    }
  }, [page, totalPages])

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

  const handleViewJson = (schema) => {
    setJsonViewer({
      open: true,
      title: 'Schema JSON',
      subtitle: 'This object captures the inferred structure that Lattice uses to group similar documents.',
      identifier: schema.id,
      value: schema,
    })
  }

  if (loading) {
    return <div className="loading">Loading...</div>
  }

  return (
    <div className="schemas">
      <div className="page-header">
        <div>
          <h1 className="page-title">Schemas</h1>
          <p className="page-subtitle">Review the inferred structures generated from ingested documents so you can understand document shape, reuse, and downstream indexing behavior.</p>
        </div>
      </div>

      {schemas.length === 0 ? (
        <div className="empty-state">
          <p>No schemas found. Schemas are created automatically when documents are ingested.</p>
        </div>
      ) : (
        <div className="card">
          <div className="table-results-count">
            Showing {filteredSchemas.length} of {schemas.length} schemas
          </div>
          <TablePagination
            totalRecords={filteredSchemas.length}
            currentPage={page}
            totalPages={totalPages}
            onPageChange={setPage}
            onRefresh={loadSchemas}
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
                <th className={`sortable ${sort.column === 'hash' ? 'sorted' : ''}`} onClick={() => handleSort('hash')}>
                  <span className="th-content">Hash <span className="sort-icon">{getSortIcon('hash')}</span></span>
                </th>
                <th className={`sortable ${sort.column === 'createdUtc' ? 'sorted' : ''}`} onClick={() => handleSort('createdUtc')}>
                  <span className="th-content">Created <span className="sort-icon">{getSortIcon('createdUtc')}</span></span>
                </th>
                <th>Actions</th>
              </tr>
              <tr className="filter-row">
                <td><input type="text" className="column-filter" placeholder="Filter..." value={filters.id} onChange={(event) => handleFilterChange('id', event.target.value)} /></td>
                <td><input type="text" className="column-filter" placeholder="Filter..." value={filters.hash} onChange={(event) => handleFilterChange('hash', event.target.value)} /></td>
                <td><input type="text" className="column-filter" placeholder="Filter..." value={filters.createdUtc} onChange={(event) => handleFilterChange('createdUtc', event.target.value)} /></td>
                <td className="no-filter"></td>
              </tr>
            </thead>
            <tbody>
              {pagedSchemas.length === 0 ? (
                <tr>
                  <td colSpan={4} className="empty-row">No schemas match your filters.</td>
                </tr>
              ) : (
                pagedSchemas.map((schema) => (
                  <tr key={schema.id}>
                    <td><CopyableId value={schema.id} /></td>
                    <td><CopyableId value={schema.hash} /></td>
                    <td>{formatDate(schema.createdUtc)}</td>
                    <td>
                      <ActionMenu
                        items={[
                          { label: 'View Elements', onClick: () => navigate(`/schemas/${schema.id}/elements`) },
                          { label: 'View JSON', onClick: () => handleViewJson(schema) },
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
