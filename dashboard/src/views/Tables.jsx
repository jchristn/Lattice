import { useState, useEffect, useMemo } from 'react'
import { useSearchParams, useNavigate } from 'react-router-dom'
import { useApp } from '../context/AppContext'
import CopyableId from '../components/CopyableId'
import ActionMenu from '../components/ActionMenu'
import Modal from '../components/Modal'
import './Tables.css'

export default function Tables() {
  const [searchParams, setSearchParams] = useSearchParams()
  const navigate = useNavigate()
  const { api, setError } = useApp()
  const [tables, setTables] = useState([])
  const [loading, setLoading] = useState(true)
  const [showDetailsModal, setShowDetailsModal] = useState(false)
  const [selectedTable, setSelectedTable] = useState(null)

  // Per-column filters - initialize from URL params
  const [filters, setFilters] = useState({
    key: searchParams.get('key') || '',
    tableName: '',
  })

  // Sorting state: { column: string, direction: 'asc' | 'desc' }
  const [sort, setSort] = useState({ column: 'key', direction: 'asc' })

  // Filter and sort tables
  const filteredTables = useMemo(() => {
    let result = [...tables]

    // Apply column filters
    if (filters.key) {
      const query = filters.key.toLowerCase()
      result = result.filter(t => t.key.toLowerCase().includes(query))
    }
    if (filters.tableName) {
      const query = filters.tableName.toLowerCase()
      result = result.filter(t => t.tableName.toLowerCase().includes(query))
    }

    // Apply sorting
    if (sort.column) {
      result.sort((a, b) => {
        const aVal = a[sort.column] || ''
        const bVal = b[sort.column] || ''
        const comparison = aVal.localeCompare(bVal)
        return sort.direction === 'asc' ? comparison : -comparison
      })
    }

    return result
  }, [tables, filters, sort])

  useEffect(() => {
    const loadTables = async () => {
      try {
        setLoading(true)
        const data = await api.getIndexTables()
        setTables(data || [])
      } catch (err) {
        setError('Failed to load tables: ' + err.message)
      } finally {
        setLoading(false)
      }
    }
    loadTables()
  }, [api])

  const handleSort = (column) => {
    setSort(prev => ({
      column,
      direction: prev.column === column && prev.direction === 'asc' ? 'desc' : 'asc'
    }))
  }

  const handleFilterChange = (column, value) => {
    setFilters(prev => ({ ...prev, [column]: value }))
    // Update URL params for key filter
    if (column === 'key') {
      if (value) {
        setSearchParams({ key: value })
      } else {
        setSearchParams({})
      }
    }
  }

  const getSortIcon = (column) => {
    if (sort.column !== column) return '↕'
    return sort.direction === 'asc' ? '↑' : '↓'
  }

  const handleViewDetails = (table) => {
    setSelectedTable(table)
    setShowDetailsModal(true)
  }

  const handleViewEntries = (table) => {
    navigate(`/entries?table=${encodeURIComponent(table.key)}`)
  }

  if (loading) {
    return <div className="loading">Loading...</div>
  }

  return (
    <div className="tables">
      <div className="page-header">
        <h1 className="page-title">Index Tables</h1>
        <p className="page-subtitle">Field-to-table mappings for search indices</p>
      </div>

      {tables.length === 0 ? (
        <div className="empty-state">
          <p>No index tables found. Tables are created automatically when documents are ingested.</p>
        </div>
      ) : (
        <div className="card">
          <div className="table-results-count">
            Showing {filteredTables.length} of {tables.length} tables
          </div>
          <table className="table">
            <thead>
              <tr>
                <th
                  className={`sortable ${sort.column === 'key' ? 'sorted' : ''}`}
                  onClick={() => handleSort('key')}
                >
                  <span className="th-content">
                    Field Key
                    <span className="sort-icon">{getSortIcon('key')}</span>
                  </span>
                </th>
                <th
                  className={`sortable ${sort.column === 'tableName' ? 'sorted' : ''}`}
                  onClick={() => handleSort('tableName')}
                >
                  <span className="th-content">
                    Table Name
                    <span className="sort-icon">{getSortIcon('tableName')}</span>
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
                    value={filters.key}
                    onChange={(e) => handleFilterChange('key', e.target.value)}
                  />
                </td>
                <td>
                  <input
                    type="text"
                    className="column-filter"
                    placeholder="Filter..."
                    value={filters.tableName}
                    onChange={(e) => handleFilterChange('tableName', e.target.value)}
                  />
                </td>
                <td className="no-filter"></td>
              </tr>
            </thead>
            <tbody>
              {filteredTables.length === 0 ? (
                <tr>
                  <td colSpan={3} className="empty-row">No tables match your filters.</td>
                </tr>
              ) : (
                filteredTables.map((mapping) => (
                  <tr key={mapping.id}>
                    <td className="monospace">{mapping.key}</td>
                    <td><CopyableId value={mapping.tableName} /></td>
                    <td>
                      <ActionMenu
                        items={[
                          { label: 'View Entries', onClick: () => handleViewEntries(mapping) },
                          { label: 'View Details', onClick: () => handleViewDetails(mapping) },
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
        isOpen={showDetailsModal}
        onClose={() => {
          setShowDetailsModal(false)
          setSelectedTable(null)
        }}
        title="Table Details"
      >
        {selectedTable && (
          <>
            <div className="table-detail">
              <strong>ID:</strong> <CopyableId value={selectedTable.id} />
            </div>
            <div className="table-detail">
              <strong>Field Key:</strong> <span className="monospace">{selectedTable.key}</span>
            </div>
            <div className="table-detail">
              <strong>Table Name:</strong> <CopyableId value={selectedTable.tableName} />
            </div>
          </>
        )}
      </Modal>
    </div>
  )
}
