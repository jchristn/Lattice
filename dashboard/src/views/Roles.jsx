import { useState, useEffect } from 'react'
import { useApp } from '../context/AppContext'
import CopyableId from '../components/CopyableId'
import TablePagination from '../components/TablePagination'
import JsonViewerModal from '../components/JsonViewerModal'
import './Roles.css'

export default function Roles() {
  const { api, setError } = useApp()
  const [roles, setRoles] = useState([])
  const [loading, setLoading] = useState(true)
  const [forbidden, setForbidden] = useState(false)
  const [page, setPage] = useState(0)
  const [pageSize, setPageSize] = useState(25)
  const [totalRecords, setTotalRecords] = useState(0)
  const [jsonRow, setJsonRow] = useState(null)

  const totalPages = Math.max(1, Math.ceil(totalRecords / pageSize))

  const onRowClick = (event, row) => {
    if (event.target.closest('button, a, input, select, textarea, label, .action-menu, .copyable-id, [role="button"]')) return
    setJsonRow(row)
  }

  const load = async () => {
    try {
      setLoading(true)
      const result = await api.getRoles({ maxResults: pageSize, skip: page * pageSize })
      setRoles(result?.objects || [])
      setTotalRecords(result?.totalRecords ?? (result?.objects?.length || 0))
      setForbidden(false)
    } catch (err) {
      if (err.status === 403) {
        setForbidden(true)
        setRoles([])
      } else {
        setError('Failed to load roles: ' + err.message)
      }
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    load()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [api, page, pageSize])

  if (loading) {
    return <div className="loading">Loading...</div>
  }

  return (
    <div className="roles">
      <div className="page-header">
        <div>
          <h1 className="page-title" title="The list of roles available for authorization">Roles</h1>
          <p className="page-subtitle">Roles bundle permissions together; assign them to users to grant access. This list is read-only.</p>
        </div>
      </div>

      {forbidden ? (
        <div className="inline-notice">You do not have permission to view this (administrator only).</div>
      ) : roles.length === 0 ? (
        <div className="empty-state">
          <p>No roles found.</p>
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
                <th title="The unique identifier of the role; use it when assigning the role via the API">ID</th>
                <th title="The human-readable name of the role, such as Viewer or Editor">Name</th>
                <th title="Built-in roles are predefined by the system and cannot be modified">Built-in</th>
                <th title="The tenant this role belongs to; blank means the role is global to all tenants">Tenant ID</th>
                <th title="Whether the role is active and can be assigned">Active</th>
              </tr>
            </thead>
            <tbody>
              {roles.map((r) => (
                <tr key={r.id} className="clickable-row" title="Click to view the full record as JSON" onClick={(e) => onRowClick(e, r)}>
                  <td><CopyableId value={r.id} /></td>
                  <td><strong>{r.name}</strong></td>
                  <td title={r.isBuiltIn ? 'Predefined system role' : 'Custom role'}>{r.isBuiltIn ? 'Yes' : 'No'}</td>
                  <td title={r.tenantId ? 'Scoped to a single tenant' : 'Global role available to all tenants'}>
                    {r.tenantId ? <CopyableId value={r.tenantId} /> : 'Global'}
                  </td>
                  <td title={r.active ? 'Role is active' : 'Role is inactive'}>{r.active ? 'Yes' : 'No'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <JsonViewerModal
        isOpen={!!jsonRow}
        onClose={() => setJsonRow(null)}
        title={jsonRow ? `Role: ${jsonRow.name}` : ''}
        identifier={jsonRow?.id}
        value={jsonRow}
      />
    </div>
  )
}
