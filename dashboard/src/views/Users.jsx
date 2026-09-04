import { useState, useEffect } from 'react'
import { useApp } from '../context/AppContext'
import { formatDate } from '../utils/api'
import Modal from '../components/Modal'
import ActionMenu from '../components/ActionMenu'
import CopyableId from '../components/CopyableId'
import TablePagination from '../components/TablePagination'
import JsonViewerModal from '../components/JsonViewerModal'
import DetailModal from '../components/DetailModal'
import './Users.css'

const EMPTY_FORM = { email: '', password: '', firstName: '', lastName: '', isTenantAdmin: false, isAdmin: false }

export default function Users() {
  const { api, setError, principal } = useApp()
  const isSystemAdmin = !!principal?.isAdmin
  const [users, setUsers] = useState([])
  const [loading, setLoading] = useState(true)
  const [forbidden, setForbidden] = useState(false)
  const [page, setPage] = useState(0)
  const [pageSize, setPageSize] = useState(25)
  const [totalRecords, setTotalRecords] = useState(0)
  const [showCreate, setShowCreate] = useState(false)
  const [form, setForm] = useState(EMPTY_FORM)
  const [saving, setSaving] = useState(false)
  const [jsonRow, setJsonRow] = useState(null)
  const [viewRow, setViewRow] = useState(null)

  const totalPages = Math.max(1, Math.ceil(totalRecords / pageSize))

  const onRowClick = (event, row) => {
    if (event.target.closest('button, a, input, select, textarea, label, .action-menu, .copyable-id, [role="button"]')) return
    setViewRow(row)
  }

  const buildFields = (u) => u ? [
    { label: 'ID', value: u.id, copyable: true, title: 'The unique identifier of the user' },
    { label: 'Email', value: u.email, title: 'The email address the user signs in with' },
    u.firstName ? { label: 'First Name', value: u.firstName, title: 'The user’s given name' } : null,
    u.lastName ? { label: 'Last Name', value: u.lastName, title: 'The user’s family name' } : null,
    { label: 'Tenant ID', value: u.tenantId, copyable: true, title: 'The tenant this user belongs to' },
    { label: 'Admin', value: u.isAdmin ? 'Yes' : 'No', title: 'System administrators can manage every tenant on the server' },
    { label: 'Tenant Admin', value: u.isTenantAdmin ? 'Yes' : 'No', title: 'Tenant administrators can manage users and settings within their own tenant' },
    { label: 'Active', value: u.active ? 'Yes' : 'No', title: 'Whether the account is active and allowed to sign in' },
    { label: 'Protected', value: u.isProtected ? 'Yes' : 'No', title: 'Protected users are system-managed and cannot be deleted' },
    { label: 'Created', value: formatDate(u.createdUtc), title: 'When the user account was created' },
    { label: 'Last Updated', value: formatDate(u.lastUpdateUtc), title: 'When the user account was last modified' },
  ] : []

  const load = async () => {
    try {
      setLoading(true)
      const result = await api.getUsers({ maxResults: pageSize, skip: page * pageSize })
      setUsers(result?.objects || [])
      setTotalRecords(result?.totalRecords ?? (result?.objects?.length || 0))
      setForbidden(false)
    } catch (err) {
      if (err.status === 403) {
        setForbidden(true)
        setUsers([])
      } else {
        setError('Failed to load users: ' + err.message)
      }
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    load()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [api, page, pageSize])

  const handleCreate = async () => {
    try {
      setSaving(true)
      const payload = {
        email: form.email,
        password: form.password,
        firstName: form.firstName || null,
        lastName: form.lastName || null,
        isTenantAdmin: form.isTenantAdmin,
      }
      if (isSystemAdmin) {
        payload.isAdmin = form.isAdmin
      }
      await api.createUser(payload)
      setShowCreate(false)
      setForm(EMPTY_FORM)
      setPage(0)
      await load()
    } catch (err) {
      setError('Failed to create user: ' + err.message)
    } finally {
      setSaving(false)
    }
  }

  const handleDelete = async (user) => {
    if (!confirm(`Delete user "${user.email}"? They will no longer be able to sign in.`)) {
      return
    }
    try {
      await api.deleteUser(user.id)
      await load()
    } catch (err) {
      if (err.status === 409) {
        setError('This user is protected and cannot be deleted.')
      } else {
        setError('Failed to delete user: ' + err.message)
      }
    }
  }

  if (loading) {
    return <div className="loading">Loading...</div>
  }

  return (
    <div className="users">
      <div className="page-header">
        <div>
          <h1 className="page-title" title="The list of user accounts on this server">Users</h1>
          <p className="page-subtitle">Users are people who sign in with an email and password; each belongs to a tenant and may hold administrative rights.</p>
        </div>
        {!forbidden ? (
          <div className="page-actions">
            <button
              className="btn btn-primary"
              onClick={() => setShowCreate(true)}
              title="Create a new user account that can sign in with email and password"
            >
              + New User
            </button>
          </div>
        ) : null}
      </div>

      {forbidden ? (
        <div className="inline-notice">You do not have permission to view this (administrator only).</div>
      ) : users.length === 0 ? (
        <div className="empty-state">
          <p>No users found. Create your first user to get started.</p>
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
                <th title="The unique identifier of the user; use it when referencing the user via the API">ID</th>
                <th title="The email address the user signs in with; must be unique within the tenant">Email</th>
                <th title="The tenant this user belongs to">Tenant ID</th>
                <th title="System administrators can manage every tenant on the server">Admin</th>
                <th title="Tenant administrators can manage users and settings within their own tenant">Tenant Admin</th>
                <th title="Whether the account is active and allowed to sign in">Active</th>
                <th title="When the user account was created">Created</th>
                <th title="Actions you can perform on this user">Actions</th>
              </tr>
            </thead>
            <tbody>
              {users.map((u) => (
                <tr key={u.id} className="clickable-row" title="Click to view details" onClick={(e) => onRowClick(e, u)}>
                  <td><CopyableId value={u.id} /></td>
                  <td>{u.email}</td>
                  <td>{u.tenantId ? <CopyableId value={u.tenantId} /> : '-'}</td>
                  <td title={u.isAdmin ? 'System administrator' : 'Not a system administrator'}>{u.isAdmin ? 'Yes' : 'No'}</td>
                  <td title={u.isTenantAdmin ? 'Tenant administrator' : 'Not a tenant administrator'}>{u.isTenantAdmin ? 'Yes' : 'No'}</td>
                  <td title={u.active ? 'Account is active' : 'Account is inactive'}>{u.active ? 'Yes' : 'No'}</td>
                  <td>{formatDate(u.createdUtc)}</td>
                  <td>
                    <ActionMenu
                      items={[
                        {
                          label: 'View',
                          onClick: () => setViewRow(u),
                          title: 'Open the formatted details for this record',
                        },
                        {
                          label: 'View JSON',
                          onClick: () => setJsonRow(u),
                          title: 'View the raw JSON for this record',
                        },
                        ...(u.isProtected ? [] : [
                          {
                            label: 'Delete User',
                            onClick: () => handleDelete(u),
                            variant: 'danger',
                            title: 'Permanently delete this user and its credentials',
                          },
                        ]),
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
        title="New User"
        subtitle="Create a user account that can sign in with an email and password."
      >
        <div className="form-group">
          <label className="form-label" title="The email address the new user will sign in with; must be unique within the tenant">Email *</label>
          <input
            type="email"
            className="input"
            value={form.email}
            onChange={(e) => setForm({ ...form, email: e.target.value })}
            placeholder="user@example.com"
            title="Enter the sign-in email address for this user"
          />
        </div>
        <div className="form-group">
          <label className="form-label" title="The initial password for this account; the user can change it later">Password *</label>
          <input
            type="password"
            className="input"
            value={form.password}
            onChange={(e) => setForm({ ...form, password: e.target.value })}
            placeholder="Enter an initial password"
            title="Set the initial password used to sign in"
          />
        </div>
        <div className="form-group">
          <label className="form-label" title="The user's given name, shown in the console">First Name</label>
          <input
            type="text"
            className="input"
            value={form.firstName}
            onChange={(e) => setForm({ ...form, firstName: e.target.value })}
            placeholder="Optional"
            title="Enter the user's first name (optional)"
          />
        </div>
        <div className="form-group">
          <label className="form-label" title="The user's family name, shown in the console">Last Name</label>
          <input
            type="text"
            className="input"
            value={form.lastName}
            onChange={(e) => setForm({ ...form, lastName: e.target.value })}
            placeholder="Optional"
            title="Enter the user's last name (optional)"
          />
        </div>
        <div className="form-group">
          <label className="checkbox-label" title="Grant this user administrative control over their own tenant's users and settings">
            <input
              type="checkbox"
              checked={form.isTenantAdmin}
              onChange={(e) => setForm({ ...form, isTenantAdmin: e.target.checked })}
              title="Make this user an administrator of their tenant"
            />
            Tenant administrator
          </label>
        </div>
        {isSystemAdmin ? (
          <div className="form-group">
            <label className="checkbox-label" title="Grant this user system-wide administrative control over every tenant on the server">
              <input
                type="checkbox"
                checked={form.isAdmin}
                onChange={(e) => setForm({ ...form, isAdmin: e.target.checked })}
                title="Make this user a system administrator across all tenants"
              />
              System administrator
            </label>
          </div>
        ) : null}
        <div className="modal-actions">
          <button className="btn btn-secondary" onClick={() => setShowCreate(false)} title="Discard this new user and close the dialog">
            Cancel
          </button>
          <button
            className="btn btn-primary"
            onClick={handleCreate}
            disabled={!form.email.trim() || !form.password || saving}
            title="Create the user account with the details entered above"
          >
            {saving ? 'Creating...' : 'Create'}
          </button>
        </div>
      </Modal>

      <JsonViewerModal
        isOpen={!!jsonRow}
        onClose={() => setJsonRow(null)}
        title={jsonRow ? `User: ${jsonRow.email}` : ''}
        identifier={jsonRow?.id}
        value={jsonRow}
      />

      <DetailModal
        isOpen={!!viewRow}
        onClose={() => setViewRow(null)}
        title={viewRow ? `User: ${viewRow.email}` : ''}
        fields={buildFields(viewRow)}
      />
    </div>
  )
}
