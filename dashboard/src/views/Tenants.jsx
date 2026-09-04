import { useState, useEffect } from 'react'
import { useApp } from '../context/AppContext'
import { formatDate } from '../utils/api'
import Modal from '../components/Modal'
import ActionMenu from '../components/ActionMenu'
import CopyableId from '../components/CopyableId'
import TablePagination from '../components/TablePagination'
import './Tenants.css'

export default function Tenants() {
  const { api, setError } = useApp()
  const [tenants, setTenants] = useState([])
  const [loading, setLoading] = useState(true)
  const [forbidden, setForbidden] = useState(false)
  const [page, setPage] = useState(0)
  const [pageSize, setPageSize] = useState(25)
  const [totalRecords, setTotalRecords] = useState(0)
  const [showCreate, setShowCreate] = useState(false)
  const [name, setName] = useState('')
  const [saving, setSaving] = useState(false)

  const totalPages = Math.max(1, Math.ceil(totalRecords / pageSize))

  const load = async () => {
    try {
      setLoading(true)
      const result = await api.getTenants({ maxResults: pageSize, skip: page * pageSize })
      setTenants(result?.objects || [])
      setTotalRecords(result?.totalRecords ?? (result?.objects?.length || 0))
      setForbidden(false)
    } catch (err) {
      if (err.status === 403) {
        setForbidden(true)
        setTenants([])
      } else {
        setError('Failed to load tenants: ' + err.message)
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
      await api.createTenant({ name })
      setShowCreate(false)
      setName('')
      setPage(0)
      await load()
    } catch (err) {
      setError('Failed to create tenant: ' + err.message)
    } finally {
      setSaving(false)
    }
  }

  const handleDelete = async (tenant) => {
    if (!confirm(`Delete tenant "${tenant.name}"? All of its data, users, and credentials become inaccessible.`)) {
      return
    }
    try {
      await api.deleteTenant(tenant.id)
      await load()
    } catch (err) {
      if (err.status === 409) {
        setError('This tenant is protected and cannot be deleted.')
      } else {
        setError('Failed to delete tenant: ' + err.message)
      }
    }
  }

  if (loading) {
    return <div className="loading">Loading...</div>
  }

  return (
    <div className="tenants">
      <div className="page-header">
        <div>
          <h1 className="page-title" title="The list of tenants configured on this server">Tenants</h1>
          <p className="page-subtitle">Tenants are the top-level isolation boundary: each one owns its own collections, users, and credentials.</p>
        </div>
        {!forbidden ? (
          <div className="page-actions">
            <button
              className="btn btn-primary"
              onClick={() => setShowCreate(true)}
              title="Create a new tenant with its own isolated set of data and users"
            >
              + New Tenant
            </button>
          </div>
        ) : null}
      </div>

      {forbidden ? (
        <div className="inline-notice">You do not have permission to view this (administrator only).</div>
      ) : tenants.length === 0 ? (
        <div className="empty-state">
          <p>No tenants found. Create your first tenant to get started.</p>
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
                <th title="The unique identifier of the tenant; use it when referencing the tenant via the API">ID</th>
                <th title="The human-readable display name of the tenant">Name</th>
                <th title="Whether the tenant is currently active and able to accept requests">Active</th>
                <th title="Protected tenants are system-managed and cannot be deleted">Protected</th>
                <th title="When the tenant was first created">Created</th>
                <th title="Actions you can perform on this tenant">Actions</th>
              </tr>
            </thead>
            <tbody>
              {tenants.map((t) => (
                <tr key={t.id}>
                  <td><CopyableId value={t.id} /></td>
                  <td><strong>{t.name}</strong></td>
                  <td title={t.active ? 'This tenant is active' : 'This tenant is inactive'}>{t.active ? 'Yes' : 'No'}</td>
                  <td title={t.isProtected ? 'System-managed; cannot be deleted' : 'Can be deleted'}>{t.isProtected ? 'Yes' : 'No'}</td>
                  <td>{formatDate(t.createdUtc)}</td>
                  <td>
                    <ActionMenu
                      items={t.isProtected ? [] : [
                        {
                          label: 'Delete Tenant',
                          onClick: () => handleDelete(t),
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

      <Modal
        isOpen={showCreate}
        onClose={() => setShowCreate(false)}
        title="New Tenant"
        subtitle="Create a tenant to hold an isolated set of collections, users, and credentials."
      >
        <div className="form-group">
          <label className="form-label" title="A human-readable name for this tenant; shown throughout the console">Name *</label>
          <input
            type="text"
            className="input"
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder="e.g., Acme Corporation"
            title="Enter a descriptive name identifying this tenant"
          />
        </div>
        <div className="modal-actions">
          <button className="btn btn-secondary" onClick={() => setShowCreate(false)} title="Discard this new tenant and close the dialog">
            Cancel
          </button>
          <button className="btn btn-primary" onClick={handleCreate} disabled={!name.trim() || saving} title="Create the tenant with the name entered above">
            {saving ? 'Creating...' : 'Create'}
          </button>
        </div>
      </Modal>
    </div>
  )
}
