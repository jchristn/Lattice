import { useState, useEffect, useMemo } from 'react'
import { useApp } from '../context/AppContext'
import { formatDate } from '../utils/api'
import Modal from '../components/Modal'
import ActionMenu from '../components/ActionMenu'
import CopyableId from '../components/CopyableId'
import TablePagination from '../components/TablePagination'
import JsonViewerModal from '../components/JsonViewerModal'
import DetailModal from '../components/DetailModal'
import './Assignments.css'

const EMPTY_FORM = { userId: '', roleName: '', roleId: '', resourceScope: '', resourceId: '' }

export default function Assignments() {
  const { api, setError } = useApp()
  const [assignments, setAssignments] = useState([])
  const [loading, setLoading] = useState(true)
  const [forbidden, setForbidden] = useState(false)
  const [page, setPage] = useState(0)
  const [pageSize, setPageSize] = useState(25)
  const [filters, setFilters] = useState({
    id: '',
    userId: '',
    role: '',
    scope: '',
    resourceId: '',
    created: '',
  })
  const [showCreate, setShowCreate] = useState(false)
  const [form, setForm] = useState(EMPTY_FORM)
  const [saving, setSaving] = useState(false)
  const [jsonRow, setJsonRow] = useState(null)
  const [viewRow, setViewRow] = useState(null)

  const filteredAssignments = useMemo(() => {
    let result = [...assignments]
    if (filters.id) {
      const q = filters.id.toLowerCase()
      result = result.filter((a) => (a.id || '').toLowerCase().includes(q))
    }
    if (filters.userId) {
      const q = filters.userId.toLowerCase()
      result = result.filter((a) => (a.userId || '').toLowerCase().includes(q))
    }
    if (filters.role) {
      const q = filters.role.toLowerCase()
      result = result.filter((a) => (a.roleName || a.roleId || '').toLowerCase().includes(q))
    }
    if (filters.scope) {
      const q = filters.scope.toLowerCase()
      result = result.filter((a) => (a.resourceScope || 'All').toLowerCase().includes(q))
    }
    if (filters.resourceId) {
      const q = filters.resourceId.toLowerCase()
      result = result.filter((a) => (a.resourceId || '').toLowerCase().includes(q))
    }
    if (filters.created) {
      const q = filters.created.toLowerCase()
      result = result.filter((a) => formatDate(a.createdUtc).toLowerCase().includes(q))
    }
    return result
  }, [assignments, filters])

  const totalPages = Math.max(1, Math.ceil(filteredAssignments.length / pageSize))
  const pagedAssignments = filteredAssignments.slice(page * pageSize, (page + 1) * pageSize)

  const handleFilterChange = (column, value) => {
    setFilters((previous) => ({ ...previous, [column]: value }))
  }

  const onRowClick = (event, row) => {
    if (event.target.closest('button, a, input, select, textarea, label, .action-menu, .copyable-id, [role="button"]')) return
    setViewRow(row)
  }

  const buildFields = (a) => a ? [
    { label: 'ID', value: a.id, copyable: true, title: 'The unique identifier of this role assignment' },
    { label: 'User ID', value: a.userId, copyable: true, title: 'The user who is granted the role by this assignment' },
    { label: 'Role', value: a.roleName || a.roleId, title: 'The role granted to the user, such as Viewer or Editor' },
    { label: 'Role ID', value: a.roleId, copyable: true, title: 'The unique identifier of the granted role' },
    a.resourceScope ? { label: 'Resource Scope', value: a.resourceScope, title: 'The kind of resource this grant applies to; blank means it applies everywhere' } : null,
    a.resourceId ? { label: 'Resource ID', value: a.resourceId, copyable: true, title: 'The specific resource the grant is limited to; blank means all resources of the scope' } : null,
    { label: 'Inherits to Children', value: a.inheritsToChildren ? 'Yes' : 'No', title: 'Whether this grant also applies to child resources of the scoped resource' },
    { label: 'Tenant ID', value: a.tenantId, copyable: true, title: 'The tenant this assignment belongs to' },
    { label: 'Active', value: a.active ? 'Yes' : 'No', title: 'Whether the assignment is active and currently granting access' },
    { label: 'Created', value: formatDate(a.createdUtc), title: 'When this role assignment was created' },
  ] : []

  const load = async () => {
    try {
      setLoading(true)
      const result = await api.getAssignments({ maxResults: 1000 })
      setAssignments(result?.objects || [])
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
  }, [api])

  useEffect(() => {
    setPage(0)
  }, [filters])

  useEffect(() => {
    if (page > totalPages - 1) {
      setPage(Math.max(totalPages - 1, 0))
    }
  }, [page, totalPages])

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
            totalRecords={filteredAssignments.length}
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
              <tr className="filter-row">
                <td><input type="text" className="column-filter" placeholder="Filter..." value={filters.id} onChange={(e) => handleFilterChange('id', e.target.value)} title="Filter the list to rows whose ID contains this text" /></td>
                <td><input type="text" className="column-filter" placeholder="Filter..." value={filters.userId} onChange={(e) => handleFilterChange('userId', e.target.value)} title="Filter the list to rows whose User ID contains this text" /></td>
                <td><input type="text" className="column-filter" placeholder="Filter..." value={filters.role} onChange={(e) => handleFilterChange('role', e.target.value)} title="Filter the list to rows whose Role contains this text" /></td>
                <td><input type="text" className="column-filter" placeholder="Filter..." value={filters.scope} onChange={(e) => handleFilterChange('scope', e.target.value)} title="Filter the list to rows whose Scope contains this text" /></td>
                <td><input type="text" className="column-filter" placeholder="Filter..." value={filters.resourceId} onChange={(e) => handleFilterChange('resourceId', e.target.value)} title="Filter the list to rows whose Resource ID contains this text" /></td>
                <td><input type="text" className="column-filter" placeholder="Filter..." value={filters.created} onChange={(e) => handleFilterChange('created', e.target.value)} title="Filter the list to rows whose Created date contains this text" /></td>
                <td className="no-filter"></td>
              </tr>
            </thead>
            <tbody>
              {pagedAssignments.length === 0 ? (
                <tr>
                  <td colSpan={7} className="empty-row">No role assignments match your filters.</td>
                </tr>
              ) : (
                pagedAssignments.map((a) => (
                <tr key={a.id} className="clickable-row" title="Click to view details" onClick={(e) => onRowClick(e, a)}>
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
                          label: 'View',
                          onClick: () => setViewRow(a),
                          title: 'Open the formatted details for this record',
                        },
                        {
                          label: 'View JSON',
                          onClick: () => setJsonRow(a),
                          title: 'View the raw JSON for this record',
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
                ))
              )}
            </tbody>
          </table>
        </div>
      )}

      <Modal
        isOpen={showCreate}
        onClose={() => setShowCreate(false)}
        title="New Role Assignment"
        subtitle="Grant a user a role, optionally scoped to a specific resource."
        wide
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

      <DetailModal
        isOpen={!!viewRow}
        onClose={() => setViewRow(null)}
        title={viewRow ? `Assignment: ${viewRow.id}` : ''}
        fields={buildFields(viewRow)}
      />
    </div>
  )
}
