import { useState, useEffect, useMemo } from 'react'
import { useApp } from '../context/AppContext'
import { formatDate } from '../utils/api'
import ActionMenu from '../components/ActionMenu'
import CopyableId from '../components/CopyableId'
import TablePagination from '../components/TablePagination'
import JsonViewerModal from '../components/JsonViewerModal'
import DetailModal from '../components/DetailModal'
import './Audit.css'

export default function Audit() {
  const { api, setError } = useApp()
  const [entries, setEntries] = useState([])
  const [loading, setLoading] = useState(true)
  const [forbidden, setForbidden] = useState(false)
  const [page, setPage] = useState(0)
  const [pageSize, setPageSize] = useState(50)
  // How many entries to fetch from the server; those rows are then filtered client-side.
  const [fetchLimit, setFetchLimit] = useState(1000)
  const [filters, setFilters] = useState({
    time: '',
    eventType: '',
    method: '',
    path: '',
    response: '',
    authz: '',
    principal: '',
  })
  const [jsonRow, setJsonRow] = useState(null)
  const [viewRow, setViewRow] = useState(null)

  const principalOf = (e) => e.userId || e.credentialId || '-'

  const filteredEntries = useMemo(() => {
    let result = [...entries]
    if (filters.time) {
      const q = filters.time.toLowerCase()
      result = result.filter((e) => formatDate(e.createdUtc).toLowerCase().includes(q))
    }
    if (filters.eventType) {
      const q = filters.eventType.toLowerCase()
      result = result.filter((e) => (e.eventType || '').toLowerCase().includes(q))
    }
    if (filters.method) {
      const q = filters.method.toLowerCase()
      result = result.filter((e) => (e.method || '').toLowerCase().includes(q))
    }
    if (filters.path) {
      const q = filters.path.toLowerCase()
      result = result.filter((e) => (e.path || '').toLowerCase().includes(q))
    }
    if (filters.response) {
      const q = filters.response.toLowerCase()
      result = result.filter((e) => ((e.responseCode !== null && e.responseCode !== undefined) ? String(e.responseCode) : '').toLowerCase().includes(q))
    }
    if (filters.authz) {
      const q = filters.authz.toLowerCase()
      result = result.filter((e) => ((e.authzResult || '') + (e.denialReason ? ` (${e.denialReason})` : '')).toLowerCase().includes(q))
    }
    if (filters.principal) {
      const q = filters.principal.toLowerCase()
      result = result.filter((e) => (e.userId || e.credentialId || '').toLowerCase().includes(q))
    }
    return result
  }, [entries, filters])

  const totalPages = Math.max(1, Math.ceil(filteredEntries.length / pageSize))
  const pagedEntries = filteredEntries.slice(page * pageSize, (page + 1) * pageSize)

  const handleFilterChange = (column, value) => {
    setFilters((previous) => ({ ...previous, [column]: value }))
  }

  const onRowClick = (event, row) => {
    if (event.target.closest('button, a, input, select, textarea, label, .action-menu, .copyable-id, [role="button"]')) return
    setViewRow(row)
  }

  const buildFields = (e) => e ? [
    { label: 'Time', value: formatDate(e.createdUtc), title: 'When the audited event occurred (UTC)' },
    e.eventType ? { label: 'Event Type', value: e.eventType, title: 'The category of the event, such as Authorize or CreateUser' } : null,
    e.method ? { label: 'Method', value: e.method, title: 'The HTTP method of the request that triggered the event' } : null,
    e.path ? { label: 'Path', value: e.path, title: 'The request path that was accessed' } : null,
    (e.responseCode !== null && e.responseCode !== undefined) ? { label: 'Response Code', value: e.responseCode, title: 'The HTTP status code returned for the request' } : null,
    e.authResult ? { label: 'Auth Result', value: e.authResult, title: 'The authentication outcome for the request' } : null,
    e.authzResult ? { label: 'Authz Result', value: e.authzResult, title: 'The authorization decision for the request' } : null,
    e.denialReason ? { label: 'Denial Reason', value: e.denialReason, title: 'The reason the request was denied, if applicable' } : null,
    e.requiredPermission ? { label: 'Required Permission', value: e.requiredPermission, title: 'The permission that was required to perform the request' } : null,
    e.principalType ? { label: 'Principal Type', value: e.principalType, title: 'The kind of principal that performed the action, such as a user or credential' } : null,
    e.userId ? { label: 'User ID', value: e.userId, copyable: true, title: 'The user that performed the action' } : null,
    e.credentialId ? { label: 'Credential ID', value: e.credentialId, copyable: true, title: 'The credential used to perform the action' } : null,
    e.tenantId ? { label: 'Tenant ID', value: e.tenantId, copyable: true, title: 'The tenant the event pertains to' } : null,
    e.resourceType ? { label: 'Resource Type', value: e.resourceType, title: 'The type of resource the request targeted' } : null,
    e.resourceId ? { label: 'Resource ID', value: e.resourceId, title: 'The specific resource the request targeted' } : null,
    e.sourceIp ? { label: 'Source IP', value: e.sourceIp, title: 'The client IP address the request originated from' } : null,
    e.requestId ? { label: 'Request ID', value: e.requestId, copyable: true, title: 'The unique identifier of the request that produced this entry' } : null,
  ] : []

  const load = async () => {
    try {
      setLoading(true)
      const result = await api.getAudit({ maxResults: fetchLimit })
      setEntries(result?.objects || [])
      setForbidden(false)
    } catch (err) {
      if (err.status === 403) {
        setForbidden(true)
        setEntries([])
      } else {
        setError('Failed to load audit log: ' + err.message)
      }
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    load()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [api, fetchLimit])

  useEffect(() => {
    setPage(0)
  }, [filters])

  useEffect(() => {
    if (page > totalPages - 1) {
      setPage(Math.max(totalPages - 1, 0))
    }
  }, [page, totalPages])

  const handleDelete = async (entry) => {
    if (!confirm('Delete this audit log entry? This permanently removes the record of the event.')) {
      return
    }
    try {
      await api.deleteAuditEntry(entry.id)
      await load()
    } catch (err) {
      setError('Failed to delete audit entry: ' + err.message)
    }
  }

  return (
    <div className="audit">
      <div className="page-header">
        <div>
          <h1 className="page-title" title="The security audit log of requests and authorization decisions">Audit Log</h1>
          <p className="page-subtitle">The audit log records API requests, authorization outcomes, and administrative changes for security review.</p>
        </div>
      </div>

      {!forbidden ? (
        <div className="audit-filters">
          <div className="audit-filter">
            <label className="form-label" title="The maximum number of audit entries to fetch from the server before filtering">Load Size</label>
            <select
              className="input"
              value={fetchLimit}
              onChange={(e) => { setPage(0); setFetchLimit(Number.parseInt(e.target.value, 10)) }}
              title="Choose how many audit entries to fetch from the server; the per-column filters then narrow these rows"
            >
              {[100, 250, 500, 1000, 2500, 5000].map((n) => (
                <option key={n} value={n}>{n}</option>
              ))}
            </select>
          </div>
        </div>
      ) : null}

      {loading ? (
        <div className="loading">Loading...</div>
      ) : forbidden ? (
        <div className="inline-notice">You do not have permission to view this (administrator only).</div>
      ) : entries.length === 0 ? (
        <div className="empty-state">
          <p>No audit entries found.</p>
        </div>
      ) : (
        <div className="card">
          <TablePagination
            totalRecords={filteredEntries.length}
            currentPage={page}
            totalPages={totalPages}
            onPageChange={setPage}
            onRefresh={load}
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
                <th title="When the audited event occurred (UTC)">Time</th>
                <th title="The category of the event, such as Authorize or CreateUser">Event Type</th>
                <th title="The HTTP method of the request that triggered the event">Method</th>
                <th title="The request path that was accessed">Path</th>
                <th title="The HTTP status code returned for the request">Response</th>
                <th title="The authorization decision, including any reason the request was denied">Authz</th>
                <th title="The user or credential that performed the action">Principal</th>
                <th title="Actions you can perform on this audit entry">Actions</th>
              </tr>
              <tr className="filter-row">
                <td><input type="text" className="column-filter" placeholder="Filter..." value={filters.time} onChange={(e) => handleFilterChange('time', e.target.value)} title="Filter the list to rows whose Time contains this text" /></td>
                <td><input type="text" className="column-filter" placeholder="Filter..." value={filters.eventType} onChange={(e) => handleFilterChange('eventType', e.target.value)} title="Filter the list to rows whose Event Type contains this text" /></td>
                <td><input type="text" className="column-filter" placeholder="Filter..." value={filters.method} onChange={(e) => handleFilterChange('method', e.target.value)} title="Filter the list to rows whose Method contains this text" /></td>
                <td><input type="text" className="column-filter" placeholder="Filter..." value={filters.path} onChange={(e) => handleFilterChange('path', e.target.value)} title="Filter the list to rows whose Path contains this text" /></td>
                <td><input type="text" className="column-filter" placeholder="Filter..." value={filters.response} onChange={(e) => handleFilterChange('response', e.target.value)} title="Filter the list to rows whose Response code contains this text" /></td>
                <td><input type="text" className="column-filter" placeholder="Filter..." value={filters.authz} onChange={(e) => handleFilterChange('authz', e.target.value)} title="Filter the list to rows whose Authz result contains this text" /></td>
                <td><input type="text" className="column-filter" placeholder="Filter..." value={filters.principal} onChange={(e) => handleFilterChange('principal', e.target.value)} title="Filter the list to rows whose Principal contains this text" /></td>
                <td className="no-filter"></td>
              </tr>
            </thead>
            <tbody>
              {pagedEntries.length === 0 ? (
                <tr>
                  <td colSpan={8} className="empty-row">No audit entries match your filters.</td>
                </tr>
              ) : (
                pagedEntries.map((e) => (
                <tr key={e.id} className="clickable-row" title="Click to view details" onClick={(ev) => onRowClick(ev, e)}>
                  <td>{formatDate(e.createdUtc)}</td>
                  <td>{e.eventType || '-'}</td>
                  <td className="monospace" title="HTTP method">{e.method || '-'}</td>
                  <td className="monospace" title={e.path || ''}>{e.path || '-'}</td>
                  <td title="HTTP response status code">{e.responseCode ?? '-'}</td>
                  <td title={e.denialReason || 'Authorization result'}>
                    {e.authzResult || '-'}
                    {e.denialReason ? <span className="audit-denial"> ({e.denialReason})</span> : null}
                  </td>
                  <td title="The user or credential responsible for the event">
                    {principalOf(e) !== '-' ? <CopyableId value={principalOf(e)} /> : '-'}
                  </td>
                  <td>
                    <ActionMenu
                      items={[
                        {
                          label: 'View',
                          onClick: () => setViewRow(e),
                          title: 'Open the formatted details for this record',
                        },
                        {
                          label: 'View JSON',
                          onClick: () => setJsonRow(e),
                          title: 'View the raw JSON for this record',
                        },
                        {
                          label: 'Delete Entry',
                          onClick: () => handleDelete(e),
                          variant: 'danger',
                          title: 'Permanently delete this audit entry',
                        },
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
        isOpen={!!jsonRow}
        onClose={() => setJsonRow(null)}
        title={jsonRow ? `Audit entry: ${jsonRow.eventType}` : ''}
        identifier={jsonRow?.id}
        value={jsonRow}
      />

      <DetailModal
        isOpen={!!viewRow}
        onClose={() => setViewRow(null)}
        title={viewRow ? `Audit entry: ${viewRow.eventType}` : ''}
        fields={buildFields(viewRow)}
      />
    </div>
  )
}
