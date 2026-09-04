import { useEffect, useMemo, useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { useApp } from '../context/AppContext'
import { formatDate } from '../utils/api'
import ActionMenu from '../components/ActionMenu'
import CopyableId from '../components/CopyableId'
import DetailModal from '../components/DetailModal'
import JsonViewerModal from '../components/JsonViewerModal'
import TablePagination from '../components/TablePagination'
import './Tables.css'

export default function Tables() {
  const [searchParams, setSearchParams] = useSearchParams()
  const navigate = useNavigate()
  const { api, setError } = useApp()
  const [tables, setTables] = useState([])
  const [loading, setLoading] = useState(true)
  const [page, setPage] = useState(0)
  const [pageSize, setPageSize] = useState(25)
  const [jsonViewer, setJsonViewer] = useState({ open: false, title: '', subtitle: '', identifier: '', value: null })
  const [viewRow, setViewRow] = useState(null)
  const [filters, setFilters] = useState({
    key: searchParams.get('key') || '',
    tableName: '',
  })
  const [sort, setSort] = useState({ column: 'key', direction: 'asc' })

  const loadTables = async () => {
    try {
      setLoading(true)
      const result = await api.getIndexTables({ maxResults: 1000 })
      setTables(result?.objects || [])
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

  // Row body click opens the formatted detail view; interactive controls inside the row are ignored.
  const onRowClick = (event, table) => {
    if (event.target.closest('button, a, input, select, textarea, label, .action-menu, .copyable-id, [role="button"]')) return
    setViewRow(table)
  }

  // Maps an index-table mapping record to labeled detail fields; null entries are skipped by DetailModal.
  const buildFields = (table) => {
    if (!table) return []
    return [
      { label: 'ID', value: table.id, copyable: true, title: 'Unique identifier for this index-table mapping' },
      { label: 'Field Key', value: table.key, title: 'Flattened field path that this index table stores values for' },
      { label: 'Table Name', value: table.tableName, copyable: true, title: 'Name of the physical database table backing this field key' },
      table.entryCount !== undefined && table.entryCount !== null
        ? { label: 'Entry Count', value: table.entryCount, title: 'Number of index entries stored in this table' }
        : null,
      table.createdUtc ? { label: 'Created (UTC)', value: formatDate(table.createdUtc), title: 'When this index table mapping was created (UTC)' } : null,
    ]
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
                <th className={`sortable ${sort.column === 'key' ? 'sorted' : ''}`} onClick={() => handleSort('key')} title="Flattened field path that this index table stores values for; click to sort by field key">
                  <span className="th-content">Field Key <span className="sort-icon">{getSortIcon('key')}</span></span>
                </th>
                <th className={`sortable ${sort.column === 'tableName' ? 'sorted' : ''}`} onClick={() => handleSort('tableName')} title="Name of the physical database table backing this field key; click to sort by table name">
                  <span className="th-content">Table Name <span className="sort-icon">{getSortIcon('tableName')}</span></span>
                </th>
                <th title="Per-row actions such as viewing the table's index entries, details, or raw JSON">Actions</th>
              </tr>
              <tr className="filter-row">
                <td><input type="text" className="column-filter" placeholder="Filter..." value={filters.key} onChange={(event) => handleFilterChange('key', event.target.value)} title="Filter the list to index tables whose field key contains this text" /></td>
                <td><input type="text" className="column-filter" placeholder="Filter..." value={filters.tableName} onChange={(event) => handleFilterChange('tableName', event.target.value)} title="Filter the list to index tables whose table name contains this text" /></td>
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
                  <tr key={mapping.id} className="clickable-row" title="Click to view this record's details" onClick={(event) => onRowClick(event, mapping)}>
                    <td className="monospace">{mapping.key}</td>
                    <td><CopyableId value={mapping.tableName} /></td>
                    <td>
                      <ActionMenu
                        items={[
                          { label: 'View', onClick: () => setViewRow(mapping), title: 'Open the formatted details for this record' },
                          { label: 'View JSON', onClick: () => handleViewJson(mapping), title: 'View the raw JSON for this record' },
                          { label: 'View Entries', onClick: () => handleViewEntries(mapping) },
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

      <DetailModal
        isOpen={!!viewRow}
        onClose={() => setViewRow(null)}
        title="Index Table Details"
        subtitle="This mapping shows how a flattened field key is assigned to a physical index table for search operations."
        fields={buildFields(viewRow)}
      />

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
