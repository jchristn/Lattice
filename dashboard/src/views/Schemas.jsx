import { useState, useEffect, useMemo } from 'react'
import { useNavigate } from 'react-router-dom'
import { useApp } from '../context/AppContext'
import { formatDate } from '../utils/api'
import CopyableId from '../components/CopyableId'
import ActionMenu from '../components/ActionMenu'
import './Schemas.css'

export default function Schemas() {
  const navigate = useNavigate()
  const { api, setError } = useApp()
  const [schemas, setSchemas] = useState([])
  const [loading, setLoading] = useState(true)

  // Per-column filters
  const [filters, setFilters] = useState({
    id: '',
    hash: '',
    createdUtc: '',
  })

  // Sorting state
  const [sort, setSort] = useState({ column: 'id', direction: 'asc' })

  // Filter and sort schemas
  const filteredSchemas = useMemo(() => {
    let result = [...schemas]

    // Apply column filters
    if (filters.id) {
      const query = filters.id.toLowerCase()
      result = result.filter(s => s.id.toLowerCase().includes(query))
    }
    if (filters.hash) {
      const query = filters.hash.toLowerCase()
      result = result.filter(s => s.hash.toLowerCase().includes(query))
    }
    if (filters.createdUtc) {
      const query = filters.createdUtc.toLowerCase()
      result = result.filter(s => formatDate(s.createdUtc).toLowerCase().includes(query))
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
  }, [schemas, filters, sort])

  useEffect(() => {
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
    loadSchemas()
  }, [api])

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
    <div className="schemas">
      <div className="page-header">
        <h1 className="page-title">Schemas</h1>
        <p className="page-subtitle">Inferred document schemas</p>
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
                  className={`sortable ${sort.column === 'hash' ? 'sorted' : ''}`}
                  onClick={() => handleSort('hash')}
                >
                  <span className="th-content">
                    Hash
                    <span className="sort-icon">{getSortIcon('hash')}</span>
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
                    value={filters.hash}
                    onChange={(e) => handleFilterChange('hash', e.target.value)}
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
              {filteredSchemas.length === 0 ? (
                <tr>
                  <td colSpan={4} className="empty-row">No schemas match your filters.</td>
                </tr>
              ) : (
                filteredSchemas.map((schema) => (
                  <tr key={schema.id}>
                    <td><CopyableId value={schema.id} /></td>
                    <td><CopyableId value={schema.hash} /></td>
                    <td>{formatDate(schema.createdUtc)}</td>
                    <td>
                      <ActionMenu
                        items={[
                          { label: 'View Elements', onClick: () => navigate(`/schemas/${schema.id}/elements`) },
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
    </div>
  )
}
