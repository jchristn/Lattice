import { useState, useEffect } from 'react'
import { useApp } from '../context/AppContext'
import { formatDate } from '../utils/api'
import ActionMenu from '../components/ActionMenu'
import CopyableId from '../components/CopyableId'
import TablePagination from '../components/TablePagination'
import JsonViewerModal from '../components/JsonViewerModal'
import './Audit.css'

export default function Audit() {
  const { api, setError } = useApp()
  const [entries, setEntries] = useState([])
  const [loading, setLoading] = useState(true)
  const [forbidden, setForbidden] = useState(false)
  const [page, setPage] = useState(0)
  const [pageSize, setPageSize] = useState(50)
  const [totalRecords, setTotalRecords] = useState(0)
  // Draft filter inputs vs. the applied event-type filter used in the query.
  const [eventTypeDraft, setEventTypeDraft] = useState('')
  const [eventType, setEventType] = useState('')
  const [jsonRow, setJsonRow] = useState(null)

  const totalPages = Math.max(1, Math.ceil(totalRecords / pageSize))

  const onRowClick = (event, row) => {
    if (event.target.closest('button, a, input, select, textarea, label, .action-menu, .copyable-id, [role="button"]')) return
    setJsonRow(row)
  }

  const load = async () => {
    try {
      setLoading(true)
      const result = await api.getAudit({
        eventType: eventType || undefined,
        maxResults: pageSize,
        skip: page * pageSize,
      })
      setEntries(result?.objects || [])
      setTotalRecords(result?.totalRecords ?? (result?.objects?.length || 0))
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
  }, [api, page, pageSize, eventType])

  const applyFilters = () => {
    setPage(0)
    setEventType(eventTypeDraft.trim())
  }

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

  const principalOf = (e) => e.userId || e.credentialId || '-'

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
            <label className="form-label" title="Filter the log to a single event type, such as Authorize or CreateUser">Event Type</label>
            <input
              type="text"
              className="input"
              value={eventTypeDraft}
              onChange={(e) => setEventTypeDraft(e.target.value)}
              onKeyDown={(e) => { if (e.key === 'Enter') applyFilters() }}
              placeholder="e.g., Authorize"
              title="Enter an event type to filter the audit log by"
            />
          </div>
          <div className="audit-filter">
            <label className="form-label" title="The maximum number of audit entries to fetch per page">Max Results</label>
            <select
              className="input"
              value={pageSize}
              onChange={(e) => { setPage(0); setPageSize(Number.parseInt(e.target.value, 10)) }}
              title="Choose how many audit entries to load per page"
            >
              {[25, 50, 100, 250, 500, 1000].map((n) => (
                <option key={n} value={n}>{n}</option>
              ))}
            </select>
          </div>
          <button className="btn btn-primary audit-apply" onClick={applyFilters} title="Apply the event-type filter and reload the audit log">
            Apply
          </button>
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
            totalRecords={totalRecords}
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
            </thead>
            <tbody>
              {entries.map((e) => (
                <tr key={e.id} className="clickable-row" title="Click to view the full record as JSON" onClick={(ev) => onRowClick(ev, e)}>
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
                          label: 'Delete Entry',
                          onClick: () => handleDelete(e),
                          variant: 'danger',
                        },
                      ]}
                    />
                  </td>
                </tr>
              ))}
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
    </div>
  )
}
