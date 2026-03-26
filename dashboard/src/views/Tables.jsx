import { useEffect, useMemo, useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { useApp } from '../context/AppContext'
import ActionMenu from '../components/ActionMenu'
import CopyableId from '../components/CopyableId'
import JsonViewerModal from '../components/JsonViewerModal'
import Modal from '../components/Modal'
import TablePagination from '../components/TablePagination'
import './Tables.css'

export default function Tables() {
  const [searchParams, setSearchParams] = useSearchParams()
  const navigate = useNavigate()
  const { api, setError } = useApp()
  const [tables, setTables] = useState([])
  const [loading, setLoading] = useState(true)
  const [showDetailsModal, setShowDetailsModal] = useState(false)
  const [selectedTable, setSelectedTable] = useState(null)
  const [page, setPage] = useState(0)
  const [pageSize, setPageSize] = useState(25)
  const [jsonViewer, setJsonViewer] = useState({ open: false, title: '', subtitle: '', identifier: '', value: null })
  const [filters, setFilters] = useState({
    key: searchParams.get('key') || '',
    tableName: '',
  })
  const [sort, setSort] = useState({ column: 'key', direction: 'asc' })

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

  const filteredTables = useMemo(() => {
    let result = [...tables]

    if (filters.key) {
      const query = filters.key.toLowerCase()
      result = result.filter((table) => table.key.toLowerCase().includes(query))
    }
    if (filters.tableName) {
      const query = filters.tableName.toLowerCase()
      result = result.filter((table) => table.tableName.toLowerCase().includes(query))
    }

    result.sort((a, b) => {
      const aValue = a[sort.column] || ''
      const bValue = b[sort.column] || ''
      const comparison = aValue.localeCompare(bValue)
      return sort.direction === 'asc' ? comparison : -comparison
    })

    return result
  }, [tables, filters, sort])

  const totalPages = Math.max(1, Math.ceil(filteredTables.length / pageSize))
  const pagedTables = filteredTables.slice(page * pageSize, (page + 1) * pageSize)

  useEffect(() => {
    loadTables()
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
    if (column === 'key') {
      if (value) {
        setSearchParams({ key: value })
      } else {
        setSearchParams({})
      }
    }
  }

  const getSortIcon = (column) => {
    if (sort.column !== column) return '<>'
    return sort.direction === 'asc' ? '^' : 'v'
  }

  const handleViewDetails = (table) => {
    setSelectedTable(table)
    setShowDetailsModal(true)
  }

  const handleViewEntries = (table) => {
    navigate(`/entries?table=${encodeURIComponent(table.key)}`)
  }

  const handleViewJson = (table) => {
    setJsonViewer({
      open: true,
      title: 'Index Table Mapping JSON',
      subtitle: 'This mapping shows how a flattened field key is assigned to a physical index table for search operations.',
      identifier: table.id,
      value: table,
    })
  }

  if (loading) {
    return <div className="loading">Loading...</div>
  }

  return (
    <div className="tables">
      <div className="page-header">
        <div>
          <h1 className="page-title">Index Tables</h1>
          <p className="page-subtitle">Inspect how searchable field paths map to backing index tables so you can trace where flattened values are stored and queried.</p>
        </div>
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
          <TablePagination
            totalRecords={filteredTables.length}
            currentPage={page}
            totalPages={totalPages}
            onPageChange={setPage}
            onRefresh={loadTables}
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
                  <span className="th-content">Field Key <span className="sort-icon">{getSortIcon('key')}</span></span>
                </th>
                <th className={`sortable ${sort.column === 'tableName' ? 'sorted' : ''}`} onClick={() => handleSort('tableName')}>
                  <span className="th-content">Table Name <span className="sort-icon">{getSortIcon('tableName')}</span></span>
                </th>
                <th>Actions</th>
              </tr>
              <tr className="filter-row">
                <td><input type="text" className="column-filter" placeholder="Filter..." value={filters.key} onChange={(event) => handleFilterChange('key', event.target.value)} /></td>
                <td><input type="text" className="column-filter" placeholder="Filter..." value={filters.tableName} onChange={(event) => handleFilterChange('tableName', event.target.value)} /></td>
                <td className="no-filter"></td>
              </tr>
            </thead>
            <tbody>
              {pagedTables.length === 0 ? (
                <tr>
                  <td colSpan={3} className="empty-row">No tables match your filters.</td>
                </tr>
              ) : (
                pagedTables.map((mapping) => (
                  <tr key={mapping.id}>
                    <td className="monospace">{mapping.key}</td>
                    <td><CopyableId value={mapping.tableName} /></td>
                    <td>
                      <ActionMenu
                        items={[
                          { label: 'View Entries', onClick: () => handleViewEntries(mapping) },
                          { label: 'View Details', onClick: () => handleViewDetails(mapping) },
                          { label: 'View JSON', onClick: () => handleViewJson(mapping) },
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
        subtitle="Use this metadata to understand which generated table backs a given searchable field."
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
