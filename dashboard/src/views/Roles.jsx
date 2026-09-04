import { useState, useEffect, useMemo } from 'react'
import usePersistedPageSize from '../hooks/usePersistedPageSize'
import { useApp } from '../context/AppContext'
import { formatDate } from '../utils/api'
import Modal from '../components/Modal'
import ActionMenu from '../components/ActionMenu'
import CopyableId from '../components/CopyableId'
import TablePagination from '../components/TablePagination'
import JsonViewerModal from '../components/JsonViewerModal'
import DetailModal from '../components/DetailModal'
import './Roles.css'

// Values accepted by the server's permission grant editor. Sent verbatim.
const PERMISSION_TYPES = ['permit', 'deny']
const RESOURCE_TYPES = [
  'all', 'tenant', 'user', 'credential', 'session', 'role', 'permission',
  'assignment', 'audit', 'collection', 'document', 'schema', 'index', 'requestHistory',
]
const OPERATION_TYPES = ['all', 'create', 'read', 'write', 'update', 'delete', 'execute', 'admin']

// A fresh grant row for the permission editor.
const emptyGrant = () => ({ permissionType: 'permit', resourceTypes: [], operationTypes: [] })

// A brand-new role form (create mode).
const emptyForm = () => ({ name: '', permissions: [emptyGrant()] })

// A built-in role is global (no tenant) and system-defined; it cannot be edited or deleted.
const isEditable = (role) => !role.isBuiltIn && role.tenantId !== null

export default function Roles() {
  const { api, setError } = useApp()
  const [roles, setRoles] = useState([])
  const [loading, setLoading] = useState(true)
  const [forbidden, setForbidden] = useState(false)
  const [page, setPage] = useState(0)
  const [pageSize, setPageSize] = usePersistedPageSize('roles', 25)
  const [filters, setFilters] = useState({
    id: '',
    name: '',
    builtIn: '',
    tenantId: '',
    active: '',
    created: '',
  })
  const [jsonRow, setJsonRow] = useState(null)
  const [viewRow, setViewRow] = useState(null)
  // Grants for the currently viewed role, fetched on demand; null means unavailable/omit.
  const [viewPermissions, setViewPermissions] = useState(null)

  // Create/Edit modal state. editingId is null for create, or the role id for edit.
  const [showEditor, setShowEditor] = useState(false)
  const [editingId, setEditingId] = useState(null)
  const [form, setForm] = useState(emptyForm())
  const [editorLoading, setEditorLoading] = useState(false)
  const [saving, setSaving] = useState(false)

  const filteredRoles = useMemo(() => {
    let result = [...roles]
    if (filters.id) {
      const q = filters.id.toLowerCase()
      result = result.filter((r) => (r.id || '').toLowerCase().includes(q))
    }
    if (filters.name) {
      const q = filters.name.toLowerCase()
      result = result.filter((r) => (r.name || '').toLowerCase().includes(q))
    }
    if (filters.builtIn) {
      const q = filters.builtIn.toLowerCase()
      result = result.filter((r) => (r.isBuiltIn ? 'yes' : 'no').includes(q))
    }
    if (filters.tenantId) {
      const q = filters.tenantId.toLowerCase()
      result = result.filter((r) => (r.tenantId || 'Global').toLowerCase().includes(q))
    }
    if (filters.active) {
      const q = filters.active.toLowerCase()
      result = result.filter((r) => (r.active ? 'yes' : 'no').includes(q))
    }
    if (filters.created) {
      const q = filters.created.toLowerCase()
      result = result.filter((r) => formatDate(r.createdUtc).toLowerCase().includes(q))
    }
    return result
  }, [roles, filters])

  const totalPages = Math.max(1, Math.ceil(filteredRoles.length / pageSize))
  const pagedRoles = filteredRoles.slice(page * pageSize, (page + 1) * pageSize)

  const handleFilterChange = (column, value) => {
    setFilters((previous) => ({ ...previous, [column]: value }))
  }

  const onRowClick = (event, row) => {
    if (event.target.closest('button, a, input, select, textarea, label, .action-menu, .copyable-id, [role="button"]')) return
    // Editable (custom) roles open the edit dialog; built-in roles open the formatted detail view.
    if (isEditable(row)) {
      openEdit(row)
    } else {
      openView(row)
    }
  }

  // Open the formatted detail view, fetching the role's grants to include in the Permissions field.
  const openView = async (role) => {
    setViewPermissions(null)
    setViewRow(role)
    try {
      const full = await api.getRole(role.id)
      setViewPermissions(full?.permissions ?? null)
    } catch {
      setViewPermissions(null)
    }
  }

  const buildFields = (r) => r ? [
    { label: 'ID', value: r.id, copyable: true, title: 'The unique identifier of the role' },
    { label: 'Name', value: r.name, title: 'The human-readable name of the role, such as Viewer or Editor' },
    { label: 'Tenant ID', value: r.tenantId || 'Global (built-in)', copyable: !!r.tenantId, title: 'The tenant this role belongs to; global roles apply across all tenants' },
    { label: 'Built-in', value: r.isBuiltIn ? 'Yes' : 'No', title: 'Built-in roles are predefined by the system and cannot be modified or deleted' },
    { label: 'Active', value: r.active ? 'Yes' : 'No', title: 'Whether the role is active and can be assigned' },
    { label: 'Protected', value: r.isProtected ? 'Yes' : 'No', title: 'Protected roles are system-managed and cannot be deleted' },
    { label: 'Created', value: formatDate(r.createdUtc), title: 'When the role was first created' },
    { label: 'Last Updated', value: formatDate(r.lastUpdateUtc), title: 'When the role was last modified' },
    viewPermissions && viewPermissions.length ? {
      label: 'Permissions',
      full: true,
      title: 'The grants this role confers — each permits or denies a set of operations on a set of resource types',
      node: (
        <div className="role-grant-list">
          {viewPermissions.map((p, i) => (
            <div className="role-grant" key={i}>
              <span className={`role-grant-effect ${p.permissionType === 'deny' ? 'deny' : 'permit'}`}>
                {p.permissionType === 'deny' ? 'Deny' : 'Permit'}
              </span>
              <div className="role-grant-body">
                <div className="role-grant-line">
                  <span className="role-grant-key">Resources</span>
                  <span className="role-grant-chips">
                    {(p.resourceTypes || []).length
                      ? (p.resourceTypes || []).map((rt) => <span className="role-grant-chip" key={rt}>{rt}</span>)
                      : <span className="role-grant-none">—</span>}
                  </span>
                </div>
                <div className="role-grant-line">
                  <span className="role-grant-key">Operations</span>
                  <span className="role-grant-chips">
                    {(p.operationTypes || []).length
                      ? (p.operationTypes || []).map((ot) => <span className="role-grant-chip" key={ot}>{ot}</span>)
                      : <span className="role-grant-none">—</span>}
                  </span>
                </div>
              </div>
            </div>
          ))}
        </div>
      ),
    } : (viewPermissions ? { label: 'Permissions', value: 'This role grants no permissions.', full: true, title: 'The grants this role confers' } : null),
  ] : []

  const load = async () => {
    try {
      setLoading(true)
      const result = await api.getRoles({ maxResults: 1000 })
      setRoles(result?.objects || [])
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
  }, [api])

  useEffect(() => {
    setPage(0)
  }, [filters])

  useEffect(() => {
    if (page > totalPages - 1) {
      setPage(Math.max(totalPages - 1, 0))
    }
  }, [page, totalPages])

  const openCreate = () => {
    setEditingId(null)
    setForm(emptyForm())
    setShowEditor(true)
  }

  const openEdit = async (role) => {
    setEditingId(role.id)
    setForm({ name: role.name, permissions: [] })
    setShowEditor(true)
    setEditorLoading(true)
    try {
      // Fetch the role's current grants so the editor is prefilled with what it currently grants.
      const full = await api.getRole(role.id)
      const grants = (full?.permissions || []).map((p) => ({
        permissionType: p.permissionType || 'permit',
        resourceTypes: Array.isArray(p.resourceTypes) ? p.resourceTypes : [],
        operationTypes: Array.isArray(p.operationTypes) ? p.operationTypes : [],
      }))
      setForm({ name: full?.name ?? role.name, permissions: grants.length ? grants : [emptyGrant()] })
    } catch (err) {
      setError('Failed to load role permissions: ' + err.message)
      setForm({ name: role.name, permissions: [emptyGrant()] })
    } finally {
      setEditorLoading(false)
    }
  }

  const closeEditor = () => {
    setShowEditor(false)
    setEditingId(null)
    setForm(emptyForm())
  }

  const addGrant = () => {
    setForm((f) => ({ ...f, permissions: [...f.permissions, emptyGrant()] }))
  }

  const removeGrant = (index) => {
    setForm((f) => ({ ...f, permissions: f.permissions.filter((_, i) => i !== index) }))
  }

  const setGrantField = (index, field, value) => {
    setForm((f) => ({
      ...f,
      permissions: f.permissions.map((g, i) => (i === index ? { ...g, [field]: value } : g)),
    }))
  }

  const toggleGrantValue = (index, field, value) => {
    setForm((f) => ({
      ...f,
      permissions: f.permissions.map((g, i) => {
        if (i !== index) return g
        const current = g[field]
        const next = current.includes(value)
          ? current.filter((v) => v !== value)
          : [...current, value]
        return { ...g, [field]: next }
      }),
    }))
  }

  const handleSave = async () => {
    const payload = {
      name: form.name.trim(),
      permissions: form.permissions.map((g) => ({
        permissionType: g.permissionType,
        resourceTypes: g.resourceTypes,
        operationTypes: g.operationTypes,
      })),
    }
    try {
      setSaving(true)
      if (editingId) {
        await api.updateRole(editingId, payload)
      } else {
        await api.createRole(payload)
        setPage(0)
      }
      closeEditor()
      await load()
    } catch (err) {
      if (err.status === 409) {
        setError(editingId
          ? 'Built-in roles cannot be modified, or that name is already in use.'
          : 'A role with that name already exists.')
      } else {
        setError((editingId ? 'Failed to update role: ' : 'Failed to create role: ') + err.message)
      }
    } finally {
      setSaving(false)
    }
  }

  const handleDelete = async (role) => {
    if (!confirm(`Delete role "${role.name}"? Any assignments that grant it will lose these permissions.`)) {
      return
    }
    try {
      await api.deleteRole(role.id)
      await load()
    } catch (err) {
      if (err.status === 409) {
        setError('Built-in roles cannot be deleted.')
      } else {
        setError('Failed to delete role: ' + err.message)
      }
    }
  }

  if (loading) {
    return <div className="loading">Loading...</div>
  }

  return (
    <div className="roles">
      <div className="page-header">
        <div>
          <h1 className="page-title" title="The list of roles available for authorization">Roles</h1>
          <p className="page-subtitle">Roles bundle permissions together; assign them to users to grant access. Built-in roles are read-only; create custom roles to tailor access.</p>
        </div>
        {!forbidden ? (
          <div className="page-actions">
            <button
              className="btn btn-primary"
              onClick={openCreate}
              title="Create a new custom role and choose the permissions it grants"
            >
              + New Role
            </button>
          </div>
        ) : null}
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
            totalRecords={filteredRoles.length}
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
                <th title="Built-in roles are predefined by the system and cannot be modified or deleted">Built-in</th>
                <th title="The tenant this role belongs to; Global means the role applies across all tenants">Tenant ID</th>
                <th title="Whether the role is active and can be assigned">Active</th>
                <th title="When the role was first created">Created</th>
                <th title="Edit or delete actions available for custom roles">Actions</th>
              </tr>
              <tr className="filter-row">
                <td><input type="text" className="column-filter" placeholder="Filter..." value={filters.id} onChange={(e) => handleFilterChange('id', e.target.value)} title="Filter the list to rows whose ID contains this text" /></td>
                <td><input type="text" className="column-filter" placeholder="Filter..." value={filters.name} onChange={(e) => handleFilterChange('name', e.target.value)} title="Filter the list to rows whose Name contains this text" /></td>
                <td><input type="text" className="column-filter" placeholder="Filter..." value={filters.builtIn} onChange={(e) => handleFilterChange('builtIn', e.target.value)} title="Filter the list to rows whose Built-in value contains this text" /></td>
                <td><input type="text" className="column-filter" placeholder="Filter..." value={filters.tenantId} onChange={(e) => handleFilterChange('tenantId', e.target.value)} title="Filter the list to rows whose Tenant ID contains this text" /></td>
                <td><input type="text" className="column-filter" placeholder="Filter..." value={filters.active} onChange={(e) => handleFilterChange('active', e.target.value)} title="Filter the list to rows whose Active value contains this text" /></td>
                <td><input type="text" className="column-filter" placeholder="Filter..." value={filters.created} onChange={(e) => handleFilterChange('created', e.target.value)} title="Filter the list to rows whose Created date contains this text" /></td>
                <td className="no-filter"></td>
              </tr>
            </thead>
            <tbody>
              {pagedRoles.length === 0 ? (
                <tr>
                  <td colSpan={7} className="empty-row">No roles match your filters.</td>
                </tr>
              ) : (
                pagedRoles.map((r) => (
                <tr key={r.id} className="clickable-row" title={isEditable(r) ? 'Click to edit this custom role and its permissions' : 'Click to view this role’s details'} onClick={(e) => onRowClick(e, r)}>
                  <td><CopyableId value={r.id} /></td>
                  <td>{r.name}</td>
                  <td title={r.isBuiltIn ? 'Predefined system role; read-only' : 'Custom role you can edit or delete'}>
                    {r.isBuiltIn ? <span className="badge badge-builtin" title="This role is built in and read-only">Built-in</span> : 'No'}
                  </td>
                  <td title={r.tenantId ? 'Scoped to a single tenant' : 'Global role available to all tenants'}>
                    {r.tenantId ? <CopyableId value={r.tenantId} /> : 'Global'}
                  </td>
                  <td title={r.active ? 'Role is active' : 'Role is inactive'}>{r.active ? 'Yes' : 'No'}</td>
                  <td>{formatDate(r.createdUtc)}</td>
                  <td>
                    <ActionMenu
                      items={isEditable(r) ? [
                        {
                          label: 'View',
                          onClick: () => openView(r),
                          title: 'Open the formatted details for this record',
                        },
                        {
                          label: 'View JSON',
                          onClick: () => setJsonRow(r),
                          title: 'View the raw JSON for this record',
                        },
                        {
                          label: 'Edit Role',
                          onClick: () => openEdit(r),
                          title: 'Edit this role’s name and the permissions it grants',
                        },
                        {
                          label: 'Delete Role',
                          onClick: () => handleDelete(r),
                          variant: 'danger',
                          title: 'Permanently delete this custom role',
                        },
                      ] : [
                        {
                          label: 'View',
                          onClick: () => openView(r),
                          title: 'Open the formatted details for this record',
                        },
                        {
                          label: 'View JSON',
                          onClick: () => setJsonRow(r),
                          title: 'View the raw JSON for this record',
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
        isOpen={showEditor}
        onClose={closeEditor}
        title={editingId ? 'Edit Role' : 'New Role'}
        subtitle="Name the role and choose the permission grants it bundles together."
        extraWide
      >
        {editorLoading ? (
          <div className="loading">Loading permissions...</div>
        ) : (
          <>
            <div className="form-group">
              <label className="form-label" title="A human-readable name for this role; shown wherever the role is assigned">Name *</label>
              <input
                type="text"
                className="input"
                value={form.name}
                onChange={(e) => setForm({ ...form, name: e.target.value })}
                placeholder="e.g., Collection Editor"
                title="Enter a descriptive name identifying this role"
              />
            </div>

            <div className="form-group">
              <div className="perm-editor-head">
                <label className="form-label" title="Each grant permits or denies a set of operations on a set of resource types">Permissions</label>
                <button
                  type="button"
                  className="btn btn-secondary btn-sm"
                  onClick={addGrant}
                  title="Add another permission grant to this role"
                >
                  + Add Grant
                </button>
              </div>

              {form.permissions.length === 0 ? (
                <p className="perm-empty" title="This role currently grants no permissions">No permission grants. Add at least one grant.</p>
              ) : (
                <div className="perm-grants">
                  {form.permissions.map((grant, index) => (
                    <div className="perm-grant" key={index}>
                      <div className="perm-grant-top">
                        <div className="perm-field">
                          <label className="perm-field-label" title="Whether this grant permits (allows) or denies the selected operations">Effect</label>
                          <select
                            className="input"
                            value={grant.permissionType}
                            onChange={(e) => setGrantField(index, 'permissionType', e.target.value)}
                            title="Choose whether this grant permits or denies access"
                          >
                            {PERMISSION_TYPES.map((pt) => (
                              <option key={pt} value={pt} title={pt === 'permit' ? 'Allow the selected operations' : 'Explicitly deny the selected operations'}>
                                {pt === 'permit' ? 'Permit' : 'Deny'}
                              </option>
                            ))}
                          </select>
                        </div>
                        <button
                          type="button"
                          className="btn btn-danger btn-sm perm-remove"
                          onClick={() => removeGrant(index)}
                          title="Remove this permission grant from the role"
                        >
                          Remove
                        </button>
                      </div>

                      <div className="perm-field">
                        <label className="perm-field-label" title="The resource types this grant applies to; choose one or more">Resource types</label>
                        <div className="perm-checks" role="group" aria-label="Resource types">
                          {RESOURCE_TYPES.map((rt) => (
                            <label className="perm-check" key={rt} title={`Apply this grant to the ${rt} resource type`}>
                              <input
                                type="checkbox"
                                checked={grant.resourceTypes.includes(rt)}
                                onChange={() => toggleGrantValue(index, 'resourceTypes', rt)}
                                title={`Include the ${rt} resource type in this grant`}
                              />
                              {rt}
                            </label>
                          ))}
                        </div>
                      </div>

                      <div className="perm-field">
                        <label className="perm-field-label" title="The operations this grant covers; write expands to create, update, and delete">Operations</label>
                        <div className="perm-checks" role="group" aria-label="Operations">
                          {OPERATION_TYPES.map((ot) => (
                            <label className="perm-check" key={ot} title={ot === 'write' ? 'Shorthand for create, update, and delete' : `Include the ${ot} operation in this grant`}>
                              <input
                                type="checkbox"
                                checked={grant.operationTypes.includes(ot)}
                                onChange={() => toggleGrantValue(index, 'operationTypes', ot)}
                                title={`Include the ${ot} operation in this grant`}
                              />
                              {ot}
                            </label>
                          ))}
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>

            <div className="modal-actions">
              <button className="btn btn-secondary" onClick={closeEditor} title="Discard changes and close the dialog">
                Cancel
              </button>
              <button
                className="btn btn-primary"
                onClick={handleSave}
                disabled={!form.name.trim() || saving}
                title={editingId ? 'Save the updated name and permissions for this role' : 'Create the role with the name and permissions entered above'}
              >
                {saving ? 'Saving...' : editingId ? 'Save Changes' : 'Create'}
              </button>
            </div>
          </>
        )}
      </Modal>

      <JsonViewerModal
        isOpen={!!jsonRow}
        onClose={() => setJsonRow(null)}
        title={jsonRow ? `Role: ${jsonRow.name}` : ''}
        identifier={jsonRow?.id}
        value={jsonRow}
      />

      <DetailModal
        isOpen={!!viewRow}
        onClose={() => { setViewRow(null); setViewPermissions(null) }}
        title={viewRow ? `Role: ${viewRow.name}` : ''}
        fields={buildFields(viewRow)}
      />
    </div>
  )
}
