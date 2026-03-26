import { useEffect, useMemo, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { useApp } from '../context/AppContext'
import { copyToClipboard } from '../utils/clipboard'
import ActionMenu from '../components/ActionMenu'
import CopyableId from '../components/CopyableId'
import JsonViewerModal from '../components/JsonViewerModal'
import TablePagination from '../components/TablePagination'
import './IndexEntries.css'

export default function IndexEntries() {
  const [searchParams, setSearchParams] = useSearchParams()
  const { api, setError } = useApp()
  const [tables, setTables] = useState([])
  const [tablesLoaded, setTablesLoaded] = useState(false)
  const [selectedTableKey, setSelectedTableKey] = useState(searchParams.get('table') || '')
  const [manualTableKey, setManualTableKey] = useState('')
  const [entriesData, setEntriesData] = useState(null)
  const [loading, setLoading] = useState(true)
  const [entriesLoading, setEntriesLoading] = useState(false)
  const [page, setPage] = useState(0)
  const [pageSize, setPageSize] = useState(50)
  const [jsonViewer, setJsonViewer] = useState({ open: false, title: '', subtitle: '', identifier: '', value: null })
  const [filters, setFilters] = useState({
    documentId: '',
    value: '',
  })
  const [sort, setSort] = useState({ column: 'documentId', direction: 'asc' })

  const loadTables = async () => {
    try {
      const data = await api.getIndexTables()
      setTables(data || [])
    } catch (err) {
      console.error('Failed to load index tables:', err)
    } finally {
      setTablesLoaded(true)
      setLoading(false)
    }
  }

  const loadEntries = async (tableKey = selectedTableKey, nextPage = page, nextPageSize = pageSize) => {
    if (!tableKey) {
      setEntriesData(null)
      return
    }

    try {
      setEntriesLoading(true)
      const data = await api.getTableEntries(tableKey, nextPage * nextPageSize, nextPageSize)
      setEntriesData(data)
    } catch (err) {
      setError('Failed to load index entries: ' + err.message)
      setEntriesData(null)
    } finally {
      setEntriesLoading(false)
    }
  }

  const filteredEntries = useMemo(() => {
    if (!entriesData?.entries) return []

    let result = [...entriesData.entries]

    if (filters.documentId) {
      const query = filters.documentId.toLowerCase()
      result = result.filter((entry) => entry.documentId?.toLowerCase().includes(query))
    }
    if (filters.value) {
      const query = filters.value.toLowerCase()
      result = result.filter((entry) => `${entry.value ?? ''}`.toLowerCase().includes(query))
    }

    result.sort((a, b) => {
      const aValue = a[sort.column] ?? ''
      const bValue = b[sort.column] ?? ''
      if (typeof aValue === 'string' && typeof bValue === 'string') {
        const comparison = aValue.localeCompare(bValue)
        return sort.direction === 'asc' ? comparison : -comparison
      }
      if (aValue < bValue) return sort.direction === 'asc' ? -1 : 1
      if (aValue > bValue) return sort.direction === 'asc' ? 1 : -1
      return 0
    })

    return result
  }, [entriesData, filters, sort])

  const totalPages = entriesData ? Math.max(1, Math.ceil(entriesData.totalCount / pageSize)) : 1
  const showingFrom = entriesData?.totalCount ? (page * pageSize + 1) : 0
  const showingTo = entriesData?.totalCount ? Math.min((page + 1) * pageSize, entriesData.totalCount) : 0

  useEffect(() => {
    if (api) {
      loadTables()
    }
  }, [api])

  useEffect(() => {
    if (tablesLoaded && !selectedTableKey && tables.length > 0) {
      const urlTable = searchParams.get('table')
      if (urlTable) {
        setSelectedTableKey(urlTable)
      }
    }
  }, [tablesLoaded, selectedTableKey, tables, searchParams])

  useEffect(() => {
    if (api && selectedTableKey) {
      loadEntries()
    }
  }, [api, selectedTableKey, page, pageSize])

  useEffect(() => {
    if (page > totalPages - 1) {
      setPage(Math.max(totalPages - 1, 0))
    }
  }, [page, totalPages])

  const handleTableChange = (event) => {
    const newTableKey = event.target.value
    setSelectedTableKey(newTableKey)
    setPage(0)
    if (newTableKey) {
      setSearchParams({ table: newTableKey })
    } else {
      setSearchParams({})
    }
  }

  const handleManualKeySubmit = (event) => {
    event.preventDefault()
    const trimmedKey = manualTableKey.trim()
    if (trimmedKey) {
      setSelectedTableKey(trimmedKey)
      setPage(0)
      setSearchParams({ table: trimmedKey })
      setManualTableKey('')
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

  const handleViewJson = (entry) => {
    setJsonViewer({
      open: true,
      title: 'Index Entry JSON',
      subtitle: 'This record is the raw indexed value row stored for a flattened field in the selected index table.',
      identifier: entry.id,
      value: entry,
    })
  }

  const handleCopyDocumentId = async (entry) => {
    try {
      await copyToClipboard(entry.documentId)
    } catch (err) {
      setError('Failed to copy document ID: ' + err.message)
    }
  }

  if (loading) {
    return <div className="loading">Loading...</div>
  }

  return (
    <div className="index-entries">
      <div className="page-header">
        <div>
          <h1 className="page-title">Index Entries</h1>
          <p className="page-subtitle">Review the raw index rows stored for a field key so you can validate document-to-value mappings and troubleshoot search behavior.</p>
          <div className="table-selector-row">
            <div className="table-selector">
              <label className="table-selector-label">Index Table:</label>
              <select className="table-selector-select" value={selectedTableKey} onChange={handleTableChange}>
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
                onChange={(event) => setManualTableKey(event.target.value)}
                placeholder="Enter field key..."
              />
              <button type="submit" className="btn btn-secondary btn-sm" disabled={!manualTableKey.trim()}>
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
          <div className="table-results-count">
            Showing {showingFrom.toLocaleString()}-{showingTo.toLocaleString()} of {entriesData.totalCount.toLocaleString()} entries
          </div>
          <TablePagination
            totalRecords={entriesData.totalCount}
            currentPage={page}
            totalPages={totalPages}
            onPageChange={setPage}
            onRefresh={() => loadEntries(selectedTableKey, page, pageSize)}
            disabled={entriesLoading}
            pageSize={pageSize}
            onPageSizeChange={(value) => {
              setPageSize(value)
              setPage(0)
            }}
          />
          <table className="table">
            <thead>
              <tr>
                <th className={`sortable ${sort.column === 'documentId' ? 'sorted' : ''}`} onClick={() => handleSort('documentId')}>
                  <span className="th-content">Document ID <span className="sort-icon">{getSortIcon('documentId')}</span></span>
                </th>
                <th className={`sortable ${sort.column === 'value' ? 'sorted' : ''}`} onClick={() => handleSort('value')}>
                  <span className="th-content">Value <span className="sort-icon">{getSortIcon('value')}</span></span>
                </th>
                <th className={`sortable ${sort.column === 'position' ? 'sorted' : ''}`} onClick={() => handleSort('position')}>
                  <span className="th-content">Position <span className="sort-icon">{getSortIcon('position')}</span></span>
                </th>
                <th>Actions</th>
              </tr>
              <tr className="filter-row">
                <td><input type="text" className="column-filter" placeholder="Filter..." value={filters.documentId} onChange={(event) => handleFilterChange('documentId', event.target.value)} /></td>
                <td><input type="text" className="column-filter" placeholder="Filter..." value={filters.value} onChange={(event) => handleFilterChange('value', event.target.value)} /></td>
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
                    <td className="entry-value-cell">{entry.value ?? <em className="null-value">null</em>}</td>
                    <td>{entry.position ?? '-'}</td>
                    <td>
                      <ActionMenu
                        items={[
                          { label: 'Copy Document ID', onClick: () => handleCopyDocumentId(entry) },
                          { label: 'View JSON', onClick: () => handleViewJson(entry) },
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
