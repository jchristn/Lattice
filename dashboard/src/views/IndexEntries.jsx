import { useState, useEffect, useMemo } from 'react'
import { useSearchParams, useNavigate } from 'react-router-dom'
import { useApp } from '../context/AppContext'
import CopyableId from '../components/CopyableId'
import ActionMenu from '../components/ActionMenu'
import './IndexEntries.css'

export default function IndexEntries() {
  const [searchParams, setSearchParams] = useSearchParams()
  const navigate = useNavigate()
  const { api, setError } = useApp()

  const [tables, setTables] = useState([])
  const [tablesLoaded, setTablesLoaded] = useState(false)
  const [selectedTableKey, setSelectedTableKey] = useState(searchParams.get('table') || '')
  const [manualTableKey, setManualTableKey] = useState('')

  const [entriesData, setEntriesData] = useState(null)
  const [loading, setLoading] = useState(true)
  const [entriesLoading, setEntriesLoading] = useState(false)

  // Pagination
  const [page, setPage] = useState(0)
  const entriesPerPage = 50

  // Per-column filters
  const [filters, setFilters] = useState({
    documentId: '',
    value: '',
  })

  // Sorting state
  const [sort, setSort] = useState({ column: 'documentId', direction: 'asc' })

  // Filter and sort entries (client-side for current page)
  const filteredEntries = useMemo(() => {
    if (!entriesData?.entries) return []

    let result = [...entriesData.entries]

    // Apply column filters
    if (filters.documentId) {
      const query = filters.documentId.toLowerCase()
      result = result.filter(e => e.documentId?.toLowerCase().includes(query))
    }
    if (filters.value) {
      const query = filters.value.toLowerCase()
      result = result.filter(e => (e.value ?? '').toLowerCase().includes(query))
    }

    // Apply sorting
    if (sort.column) {
      result.sort((a, b) => {
        let aVal = a[sort.column] ?? ''
        let bVal = b[sort.column] ?? ''
        if (typeof aVal === 'string' && typeof bVal === 'string') {
          const comparison = aVal.localeCompare(bVal)
          return sort.direction === 'asc' ? comparison : -comparison
        }
        if (aVal < bVal) return sort.direction === 'asc' ? -1 : 1
        if (aVal > bVal) return sort.direction === 'asc' ? 1 : -1
        return 0
      })
    }

    return result
  }, [entriesData, filters, sort])

  // Load all index tables
  useEffect(() => {
    const loadTables = async () => {
      try {
        const data = await api.getIndexTables()
        setTables(data || [])
        setTablesLoaded(true)
      } catch (err) {
        console.error('Failed to load index tables:', err)
        setTablesLoaded(true)
      } finally {
        setLoading(false)
      }
    }
    if (api) {
      loadTables()
    }
  }, [api])

  // Auto-select table from URL or first available
  useEffect(() => {
    if (tablesLoaded && !selectedTableKey && tables.length > 0) {
      const urlTable = searchParams.get('table')
      if (urlTable) {
        setSelectedTableKey(urlTable)
      }
    }
  }, [tablesLoaded, selectedTableKey, tables, searchParams])

  // Load entries for selected table
  useEffect(() => {
    const loadEntries = async () => {
      if (!selectedTableKey) {
        setEntriesData(null)
        return
      }
      try {
        setEntriesLoading(true)
        const data = await api.getTableEntries(selectedTableKey, page * entriesPerPage, entriesPerPage)
        setEntriesData(data)
      } catch (err) {
        setError('Failed to load index entries: ' + err.message)
        setEntriesData(null)
      } finally {
        setEntriesLoading(false)
      }
    }
    if (api && selectedTableKey) {
      loadEntries()
    }
  }, [api, selectedTableKey, page])

  const handleTableChange = (e) => {
    const newTableKey = e.target.value
    setSelectedTableKey(newTableKey)
    setPage(0)
    if (newTableKey) {
      setSearchParams({ table: newTableKey })
    } else {
      setSearchParams({})
    }
  }

  const handleManualKeySubmit = (e) => {
    e.preventDefault()
    const trimmedKey = manualTableKey.trim()
    if (trimmedKey) {
      setSelectedTableKey(trimmedKey)
      setPage(0)
      setSearchParams({ table: trimmedKey })
      setManualTableKey('')
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

  const handleViewDocument = (entry) => {
    // Navigate to documents view - we'd need to know the collection
    // For now, copy the document ID
    navigator.clipboard.writeText(entry.documentId)
  }

  const handlePageChange = (newPage) => {
    setPage(newPage)
  }

  if (loading) {
    return <div className="loading">Loading...</div>
  }

  const totalPages = entriesData ? Math.ceil(entriesData.totalCount / entriesPerPage) : 0
  const showingFrom = entriesData ? (page * entriesPerPage + 1) : 0
  const showingTo = entriesData ? Math.min((page + 1) * entriesPerPage, entriesData.totalCount) : 0

  return (
    <div className="index-entries">
      <div className="page-header">
        <div>
          <h1 className="page-title">Index Entries</h1>
          <div className="table-selector-row">
            <div className="table-selector">
              <label className="table-selector-label">Index Table:</label>
              <select
                className="table-selector-select"
                value={selectedTableKey}
                onChange={handleTableChange}
              >
                <option value="">Select an index table...</option>
                {tables.map((table) => (
                  <option key={table.id} value={table.key}>
                    {table.key}
                  </option>
                ))}
              </select>
            </div>
            <span className="table-selector-or">or</span>
            <form className="table-key-form" onSubmit={handleManualKeySubmit}>
              <input
                type="text"
                className="table-key-input"
                value={manualTableKey}
                onChange={(e) => setManualTableKey(e.target.value)}
                placeholder="Enter field key..."
              />
              <button
                type="submit"
                className="btn btn-secondary btn-sm"
                disabled={!manualTableKey.trim()}
              >
                Go
              </button>
            </form>
          </div>
        </div>
      </div>

      {!selectedTableKey ? (
        <div className="empty-state">
          <p>Select an index table to view its entries.</p>
        </div>
      ) : entriesLoading ? (
        <div className="loading">Loading entries...</div>
      ) : !entriesData ? (
        <div className="empty-state">
          <p>No data found for this index table.</p>
        </div>
      ) : entriesData.totalCount === 0 ? (
        <div className="empty-state">
          <p>No entries found in this index table.</p>
        </div>
      ) : (
        <div className="card">
          <div className="entries-header">
            <div className="table-results-count">
              Showing {showingFrom.toLocaleString()}-{showingTo.toLocaleString()} of {entriesData.totalCount.toLocaleString()} entries
            </div>
            {totalPages > 1 && (
              <div className="pagination-controls">
                <button
                  className="btn btn-secondary btn-sm"
                  onClick={() => handlePageChange(0)}
                  disabled={page === 0}
                >
                  First
                </button>
                <button
                  className="btn btn-secondary btn-sm"
                  onClick={() => handlePageChange(page - 1)}
                  disabled={page === 0}
                >
                  Previous
                </button>
                <span className="pagination-info">
                  Page {page + 1} of {totalPages.toLocaleString()}
                </span>
                <button
                  className="btn btn-secondary btn-sm"
                  onClick={() => handlePageChange(page + 1)}
                  disabled={page >= totalPages - 1}
                >
                  Next
                </button>
                <button
                  className="btn btn-secondary btn-sm"
                  onClick={() => handlePageChange(totalPages - 1)}
                  disabled={page >= totalPages - 1}
                >
                  Last
                </button>
              </div>
            )}
          </div>

          <table className="table">
            <thead>
              <tr>
                <th
                  className={`sortable ${sort.column === 'documentId' ? 'sorted' : ''}`}
                  onClick={() => handleSort('documentId')}
                >
                  <span className="th-content">
                    Document ID
                    <span className="sort-icon">{getSortIcon('documentId')}</span>
                  </span>
                </th>
                <th
                  className={`sortable ${sort.column === 'value' ? 'sorted' : ''}`}
                  onClick={() => handleSort('value')}
                >
                  <span className="th-content">
                    Value
                    <span className="sort-icon">{getSortIcon('value')}</span>
                  </span>
                </th>
                <th
                  className={`sortable ${sort.column === 'position' ? 'sorted' : ''}`}
                  onClick={() => handleSort('position')}
                >
                  <span className="th-content">
                    Position
                    <span className="sort-icon">{getSortIcon('position')}</span>
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
                    value={filters.documentId}
                    onChange={(e) => handleFilterChange('documentId', e.target.value)}
                  />
                </td>
                <td>
                  <input
                    type="text"
                    className="column-filter"
                    placeholder="Filter..."
                    value={filters.value}
                    onChange={(e) => handleFilterChange('value', e.target.value)}
                  />
                </td>
                <td className="no-filter"></td>
                <td className="no-filter"></td>
              </tr>
            </thead>
            <tbody>
              {filteredEntries.length === 0 ? (
                <tr>
                  <td colSpan={4} className="empty-row">No entries match your filters.</td>
                </tr>
              ) : (
                filteredEntries.map((entry) => (
                  <tr key={entry.id}>
                    <td><CopyableId value={entry.documentId} /></td>
                    <td className="entry-value-cell">
                      {entry.value ?? <em className="null-value">null</em>}
                    </td>
                    <td>{entry.position ?? '-'}</td>
                    <td>
                      <ActionMenu
                        items={[
                          { label: 'Copy Document ID', onClick: () => handleViewDocument(entry) },
                        ]}
                      />
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>

          {totalPages > 1 && (
            <div className="pagination-footer">
              <button
                className="btn btn-secondary btn-sm"
                onClick={() => handlePageChange(0)}
                disabled={page === 0}
              >
                First
              </button>
              <button
                className="btn btn-secondary btn-sm"
                onClick={() => handlePageChange(page - 1)}
                disabled={page === 0}
              >
                Previous
              </button>
              <span className="pagination-info">
                Page {page + 1} of {totalPages.toLocaleString()}
              </span>
              <button
                className="btn btn-secondary btn-sm"
                onClick={() => handlePageChange(page + 1)}
                disabled={page >= totalPages - 1}
              >
                Next
              </button>
              <button
                className="btn btn-secondary btn-sm"
                onClick={() => handlePageChange(totalPages - 1)}
                disabled={page >= totalPages - 1}
              >
                Last
              </button>
            </div>
          )}
        </div>
      )}
    </div>
  )
}
