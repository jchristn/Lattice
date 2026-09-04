import { useState, useEffect } from 'react'
import { useApp } from '../context/AppContext'
import { formatDate } from '../utils/api'
import Modal from '../components/Modal'
import ActionMenu from '../components/ActionMenu'
import CopyableId from '../components/CopyableId'
import TablePagination from '../components/TablePagination'
import JsonViewerModal from '../components/JsonViewerModal'
import './Assignments.css'

const EMPTY_FORM = { userId: '', roleName: '', roleId: '', resourceScope: '', resourceId: '' }

export default function Assignments() {
  const { api, setError } = useApp()
  const [assignments, setAssignments] = useState([])
  const [loading, setLoading] = useState(true)
  const [forbidden, setForbidden] = useState(false)
  const [page, setPage] = useState(0)
  const [pageSize, setPageSize] = useState(25)
  const [totalRecords, setTotalRecords] = useState(0)
  const [showCreate, setShowCreate] = useState(false)
  const [form, setForm] = useState(EMPTY_FORM)
  const [saving, setSaving] = useState(false)
  const [jsonRow, setJsonRow] = useState(null)

  const totalPages = Math.max(1, Math.ceil(totalRecords / pageSize))

  const onRowClick = (event, row) => {
    if (event.target.closest('button, a, input, select, textarea, label, .action-menu, .copyable-id, [role="button"]')) return
    setJsonRow(row)
  }

  const load = async () => {
    try {
      setLoading(true)
      const result = await api.getAssignments({ maxResults: pageSize, skip: page * pageSize })
      setAssignments(result?.objects || [])
      setTotalRecords(result?.totalRecords ?? (result?.objects?.length || 0))
      setForbidden(false)
    } catch (err) {
      if (err.status === 403) {
        setForbidden(true)
        setAssignments([])
      } else {
        setError('Failed to load role assignments: ' + err.message)
      }
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    load()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [api, page, pageSize])

  const canSubmit = form.userId.trim() && (form.roleName.trim() || form.roleId.trim())

  const handleCreate = async () => {
    try {
      setSaving(true)
      await api.createAssignment({
        userId: form.userId,
        roleName: form.roleName || null,
        roleId: form.roleId || null,
        resourceScope: form.resourceScope || null,
        resourceId: form.resourceId || null,
      })
      setShowCreate(false)
      setForm(EMPTY_FORM)
      setPage(0)
      await load()
    } catch (err) {
      setError('Failed to create role assignment: ' + err.message)
    } finally {
      setSaving(false)
    }
  }

  const handleDelete = async (assignment) => {
    if (!confirm('Delete this role assignment? The user will lose the access it granted.')) {
      return
    }
    try {
      await api.deleteAssignment(assignment.id)
      await load()
    } catch (err) {
      if (err.status === 409) {
        setError('This role assignment is protected and cannot be deleted.')
      } else {
        setError('Failed to delete role assignment: ' + err.message)
      }
    }
  }

  if (loading) {
    return <div className="loading">Loading...</div>
  }

  return (
    <div className="assignments">
      <div className="page-header">
        <div>
          <h1 className="page-title" title="The list of role assignments that grant users access">Role Assignments</h1>
          <p className="page-subtitle">Role assignments grant a user a role, optionally scoped to a specific resource, to control exactly what they can access.</p>
        </div>
        {!forbidden ? (
          <div className="page-actions">
            <button
              className="btn btn-primary"
              onClick={() => setShowCreate(true)}
              title="Grant a user a role, optionally limited to a specific resource"
            >
              + New Assignment
            </button>
          </div>
        ) : null}
      </div>

      {forbidden ? (
        <div className="inline-notice">You do not have permission to view this (administrator only).</div>
      ) : assignments.length === 0 ? (
        <div className="empty-state">
          <p>No role assignments found. Create one to grant a user access.</p>
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
                <th title="The unique identifier of this role assignment">ID</th>
                <th title="The user who is granted the role by this assignment">User ID</th>
                <th title="The role granted to the user, such as Viewer or Editor">Role</th>
                <th title="The kind of resource this grant applies to, such as a collection; blank means it applies everywhere">Scope</th>
                <th title="The specific resource the grant is limited to; blank means all resources of the scope">Resource ID</th>
                <th title="When this role assignment was created">Created</th>
                <th title="Actions you can perform on this assignment">Actions</th>
              </tr>
            </thead>
            <tbody>
              {assignments.map((a) => (
                <tr key={a.id} className="clickable-row" title="Click to view the full record as JSON" onClick={(e) => onRowClick(e, a)}>
                  <td><CopyableId value={a.id} /></td>
                  <td>{a.userId ? <CopyableId value={a.userId} /> : '-'}</td>
                  <td title="The role granted to the user">{a.roleName || a.roleId || '-'}</td>
                  <td title="The resource scope this grant is limited to">{a.resourceScope || 'All'}</td>
                  <td>{a.resourceId ? <CopyableId value={a.resourceId} /> : '-'}</td>
                  <td>{formatDate(a.createdUtc)}</td>
                  <td>
                    <ActionMenu
                      items={[
                        {
                          label: 'View Details',
                          onClick: () => setJsonRow(a),
                          title: 'View this role assignment’s full record as JSON',
                        },
                        {
                          label: 'Delete Assignment',
                          onClick: () => handleDelete(a),
                          variant: 'danger',
                          title: 'Remove this role assignment; the user loses the access it granted',
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

      <Modal
        isOpen={showCreate}
        onClose={() => setShowCreate(false)}
        title="New Role Assignment"
        subtitle="Grant a user a role, optionally scoped to a specific resource."
      >
        <div className="form-group">
          <label className="form-label" title="The ID of the user who should receive this role">User ID *</label>
          <input
            type="text"
            className="input"
            value={form.userId}
            onChange={(e) => setForm({ ...form, userId: e.target.value })}
            placeholder="The user to grant access to"
            title="Enter the ID of the user this assignment applies to"
          />
        </div>
        <div className="form-group">
          <label className="form-label" title="The name of the role to grant, such as Viewer, Editor, or CollectionAdmin; provide this or a role ID">Role Name</label>
          <input
            type="text"
            className="input"
            value={form.roleName}
            onChange={(e) => setForm({ ...form, roleName: e.target.value })}
            placeholder="e.g., Viewer, Editor, CollectionAdmin"
            title="Enter the role name to grant (either a role name or a role ID is required)"
          />
        </div>
        <div className="form-group">
          <label className="form-label" title="The ID of the role to grant; use this instead of a role name if you prefer to reference the role by ID">Role ID</label>
          <input
            type="text"
            className="input"
            value={form.roleId}
            onChange={(e) => setForm({ ...form, roleId: e.target.value })}
            placeholder="Alternative to Role Name"
            title="Enter the role ID to grant (either a role name or a role ID is required)"
          />
        </div>
        <div className="form-group">
          <label className="form-label" title="Limits the grant to a type of resource, such as a collection; leave blank to apply everywhere">Resource Scope</label>
          <input
            type="text"
            className="input"
            value={form.resourceScope}
            onChange={(e) => setForm({ ...form, resourceScope: e.target.value })}
            placeholder="Optional, e.g., collection"
            title="Enter the resource scope to limit this grant to (optional)"
          />
        </div>
        <div className="form-group">
          <label className="form-label" title="Limits the grant to a single specific resource by its ID; leave blank to apply to all resources of the scope">Resource ID</label>
          <input
            type="text"
            className="input"
            value={form.resourceId}
            onChange={(e) => setForm({ ...form, resourceId: e.target.value })}
            placeholder="Optional, a specific resource"
            title="Enter the specific resource ID to limit this grant to (optional)"
          />
        </div>
        <div className="modal-actions">
          <button className="btn btn-secondary" onClick={() => setShowCreate(false)} title="Discard this new assignment and close the dialog">
            Cancel
          </button>
          <button className="btn btn-primary" onClick={handleCreate} disabled={!canSubmit || saving} title="Create the role assignment with the details entered above">
            {saving ? 'Creating...' : 'Create'}
          </button>
        </div>
      </Modal>

      <JsonViewerModal
        isOpen={!!jsonRow}
        onClose={() => setJsonRow(null)}
        title={jsonRow ? `Assignment: ${jsonRow.id}` : ''}
        identifier={jsonRow?.id}
        value={jsonRow}
      />
    </div>
  )
}
