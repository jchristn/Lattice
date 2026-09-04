import { useState, useEffect, useMemo } from 'react'
import { useApp } from '../context/AppContext'
import { formatDate } from '../utils/api'
import Modal from '../components/Modal'
import ActionMenu from '../components/ActionMenu'
import CopyableId from '../components/CopyableId'
import CopyButton from '../components/CopyButton'
import TablePagination from '../components/TablePagination'
import JsonViewerModal from '../components/JsonViewerModal'
import DetailModal from '../components/DetailModal'
import './Credentials.css'

export default function Credentials() {
  const { api, setError } = useApp()
  const [credentials, setCredentials] = useState([])
  const [loading, setLoading] = useState(true)
  const [forbidden, setForbidden] = useState(false)
  const [page, setPage] = useState(0)
  const [pageSize, setPageSize] = useState(25)
  const [filters, setFilters] = useState({
    id: '',
    name: '',
    userId: '',
    last4: '',
    active: '',
    created: '',
  })
  const [showCreate, setShowCreate] = useState(false)
  const [form, setForm] = useState({ name: '', userId: '' })
  const [saving, setSaving] = useState(false)
  // Holds the freshly created access key, shown exactly once after creation.
  const [createdKey, setCreatedKey] = useState(null)
  const [jsonRow, setJsonRow] = useState(null)
  const [viewRow, setViewRow] = useState(null)
  const [editing, setEditing] = useState(null)
  const [editForm, setEditForm] = useState({ name: '', active: true })

  const filteredCredentials = useMemo(() => {
    let result = [...credentials]
    if (filters.id) {
      const query = filters.id.toLowerCase()
      result = result.filter((c) => (c.id || '').toLowerCase().includes(query))
    }
    if (filters.name) {
      const query = filters.name.toLowerCase()
      result = result.filter((c) => (c.name || '-').toLowerCase().includes(query))
    }
    if (filters.userId) {
      const query = filters.userId.toLowerCase()
      result = result.filter((c) => (c.userId || '-').toLowerCase().includes(query))
    }
    if (filters.last4) {
      const query = filters.last4.toLowerCase()
      result = result.filter((c) => (c.accessKeyLast4 ? `…${c.accessKeyLast4}` : '-').toLowerCase().includes(query))
    }
    if (filters.active) {
      const query = filters.active.toLowerCase()
      result = result.filter((c) => (c.active ? 'yes' : 'no').includes(query))
    }
    if (filters.created) {
      const query = filters.created.toLowerCase()
      result = result.filter((c) => formatDate(c.createdUtc).toLowerCase().includes(query))
    }
    return result
  }, [credentials, filters])

  const totalPages = Math.max(1, Math.ceil(filteredCredentials.length / pageSize))
  const pagedCredentials = filteredCredentials.slice(page * pageSize, (page + 1) * pageSize)

  const handleFilterChange = (column, value) => {
    setFilters((previous) => ({ ...previous, [column]: value }))
  }

  const openEdit = (row) => {
    setEditForm({ name: row.name || '', active: !!row.active })
    setEditing(row)
  }

  const onRowClick = (event, row) => {
    if (event.target.closest('button, a, input, select, textarea, label, .action-menu, .copyable-id, [role="button"]')) return
    openEdit(row)
  }

  const buildFields = (c) => c ? [
    { label: 'ID', value: c.id, copyable: true, title: 'The unique identifier of the credential' },
    c.name ? { label: 'Name', value: c.name, title: 'The human-readable label describing this credential’s purpose' } : null,
    { label: 'User ID', value: c.userId, copyable: true, title: 'The user this credential authenticates as and inherits permissions from' },
    { label: 'Tenant ID', value: c.tenantId, copyable: true, title: 'The tenant this credential belongs to' },
    c.accessKey ? { label: 'Access Key', value: c.accessKey, copyable: true, title: 'The full access key used as a bearer token; treat it as a secret' } : null,
    c.accessKeyLast4 ? { label: 'Access key (last 4)', value: c.accessKeyLast4, title: 'The last four characters of the access key, for identifying it without exposing the secret' } : null,
    { label: 'Active', value: c.active ? 'Yes' : 'No', title: 'Whether the credential is active and accepted for authentication' },
    { label: 'Protected', value: c.isProtected ? 'Yes' : 'No', title: 'Protected credentials are system-managed and cannot be deleted' },
    { label: 'Created', value: formatDate(c.createdUtc), title: 'When the credential was created' },
    { label: 'Last Updated', value: formatDate(c.lastUpdateUtc), title: 'When the credential was last modified' },
  ] : []

  const load = async () => {
    try {
      setLoading(true)
      const result = await api.getCredentials({ maxResults: 1000 })
      setCredentials(result?.objects || [])
      setForbidden(false)
    } catch (err) {
      if (err.status === 403) {
        setForbidden(true)
        setCredentials([])
      } else {
        setError('Failed to load credentials: ' + err.message)
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

  const handleCreate = async () => {
    try {
      setSaving(true)
      const payload = {
        name: form.name || null,
        userId: form.userId || null,
      }
      const result = await api.createCredential(payload)
      // The access key is returned only once; surface it for copying.
      setCreatedKey(result?.accessKey || '(no access key returned)')
      setForm({ name: '', userId: '' })
      setPage(0)
      await load()
    } catch (err) {
      setError('Failed to create credential: ' + err.message)
    } finally {
      setSaving(false)
    }
  }

  const closeCreate = () => {
    setShowCreate(false)
    setCreatedKey(null)
    setForm({ name: '', userId: '' })
  }

  const handleUpdate = async () => {
    if (!editing) return
    try {
      setSaving(true)
      await api.updateCredential(editing.id, { name: editForm.name || null, active: editForm.active })
      setEditing(null)
      await load()
    } catch (err) {
      setError('Failed to update credential: ' + err.message)
    } finally {
      setSaving(false)
    }
  }

  const handleDelete = async (credential) => {
    if (!confirm(`Delete credential "${credential.name || credential.id}"? Applications using its access key will stop authenticating.`)) {
      return
    }
    try {
      await api.deleteCredential(credential.id)
      await load()
    } catch (err) {
      if (err.status === 409) {
        setError('This credential is protected and cannot be deleted.')
      } else {
        setError('Failed to delete credential: ' + err.message)
      }
    }
  }

  if (loading) {
    return <div className="loading">Loading...</div>
  }

  return (
    <div className="credentials">
      <div className="page-header">
        <div>
          <h1 className="page-title" title="The list of access-key credentials on this server">Credentials</h1>
          <p className="page-subtitle">Credentials are access keys that let applications and scripts authenticate to the API on behalf of a user.</p>
        </div>
        {!forbidden ? (
          <div className="page-actions">
            <button
              className="btn btn-primary"
              onClick={() => setShowCreate(true)}
              title="Generate a new access key credential for programmatic API access"
            >
              + New Credential
            </button>
          </div>
        ) : null}
      </div>

      {forbidden ? (
        <div className="inline-notice">You do not have permission to view this (administrator only).</div>
      ) : credentials.length === 0 ? (
        <div className="empty-state">
          <p>No credentials found. Create your first credential to authenticate applications.</p>
        </div>
      ) : (
        <div className="card">
          <TablePagination
            totalRecords={filteredCredentials.length}
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
                <th title="The unique identifier of the credential; use it when referencing the credential via the API">ID</th>
                <th title="The human-readable label given to this credential to describe its purpose">Name</th>
                <th title="The user this credential authenticates as and inherits permissions from">User ID</th>
                <th title="The last four characters of the access key, for identifying it without exposing the secret">Last4</th>
                <th title="Whether the credential is active and accepted for authentication">Active</th>
                <th title="When the credential was created">Created</th>
                <th title="Actions you can perform on this credential">Actions</th>
              </tr>
              <tr className="filter-row">
                <td><input type="text" className="column-filter" placeholder="Filter..." value={filters.id} onChange={(e) => handleFilterChange('id', e.target.value)} title="Filter the list to rows whose ID contains this text" /></td>
                <td><input type="text" className="column-filter" placeholder="Filter..." value={filters.name} onChange={(e) => handleFilterChange('name', e.target.value)} title="Filter the list to rows whose Name contains this text" /></td>
                <td><input type="text" className="column-filter" placeholder="Filter..." value={filters.userId} onChange={(e) => handleFilterChange('userId', e.target.value)} title="Filter the list to rows whose User ID contains this text" /></td>
                <td><input type="text" className="column-filter" placeholder="Filter..." value={filters.last4} onChange={(e) => handleFilterChange('last4', e.target.value)} title="Filter the list to rows whose Last4 contains this text" /></td>
                <td><input type="text" className="column-filter" placeholder="Filter..." value={filters.active} onChange={(e) => handleFilterChange('active', e.target.value)} title="Filter the list to rows whose Active contains this text" /></td>
                <td><input type="text" className="column-filter" placeholder="Filter..." value={filters.created} onChange={(e) => handleFilterChange('created', e.target.value)} title="Filter the list to rows whose Created contains this text" /></td>
                <td className="no-filter"></td>
              </tr>
            </thead>
            <tbody>
              {pagedCredentials.length === 0 ? (
                <tr>
                  <td colSpan={7} className="empty-row">No credentials match your filters.</td>
                </tr>
              ) : (
              pagedCredentials.map((c) => (
                <tr key={c.id} className="clickable-row" title="Click to edit" onClick={(e) => onRowClick(e, c)}>
                  <td><CopyableId value={c.id} /></td>
                  <td>{c.name || '-'}</td>
                  <td>{c.userId ? <CopyableId value={c.userId} /> : '-'}</td>
                  <td className="monospace" title="The last four characters of this credential's access key">{c.accessKeyLast4 ? `…${c.accessKeyLast4}` : '-'}</td>
                  <td title={c.active ? 'Credential is active' : 'Credential is inactive'}>{c.active ? 'Yes' : 'No'}</td>
                  <td>{formatDate(c.createdUtc)}</td>
                  <td>
                    <ActionMenu
                      items={[
                        {
                          label: 'Edit',
                          onClick: () => openEdit(c),
                          title: 'Edit this credential’s name and active status',
                        },
                        {
                          label: 'View',
                          onClick: () => setViewRow(c),
                          title: 'Open the formatted details for this record',
                        },
                        {
                          label: 'View JSON',
                          onClick: () => setJsonRow(c),
                          title: 'View the raw JSON for this record',
                        },
                        {
                          label: 'Delete Credential',
                          onClick: () => handleDelete(c),
                          variant: 'danger',
                          title: 'Permanently delete this credential; apps using its access key stop working',
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
        onClose={closeCreate}
        title={createdKey ? 'Credential Created' : 'New Credential'}
        subtitle={createdKey ? 'Your new access key is shown below.' : 'Generate an access key that an application can use to authenticate to the API.'}
        wide
      >
        {createdKey ? (
          <>
            <div className="key-warning" title="This access key is displayed only once and cannot be retrieved again">
              Copy this now — it will not be shown again.
            </div>
            <div className="form-group">
              <label className="form-label" title="The full access key; store it securely, as it grants API access as the associated user">Access Key</label>
              <div className="key-reveal">
                <code className="key-value" title="The full secret access key">{createdKey}</code>
                <CopyButton value={createdKey} title="Copy the access key to your clipboard" />
              </div>
            </div>
            <div className="modal-actions">
              <button className="btn btn-primary" onClick={closeCreate} title="Close the dialog now that you have copied the access key">
                Done
              </button>
            </div>
          </>
        ) : (
          <>
            <div className="form-group">
              <label className="form-label" title="An optional label describing what this credential is for; shown in the credentials list">Name</label>
              <input
                type="text"
                className="input"
                value={form.name}
                onChange={(e) => setForm({ ...form, name: e.target.value })}
                placeholder="e.g., CI pipeline key"
                title="Enter an optional descriptive name for this credential"
              />
            </div>
            <div className="form-group">
              <label className="form-label" title="The user this credential authenticates as; leave blank to use your own account">User ID</label>
              <input
                type="text"
                className="input"
                value={form.userId}
                onChange={(e) => setForm({ ...form, userId: e.target.value })}
                placeholder="Defaults to the current user"
                title="Enter the ID of the user this credential should act as, or leave blank for yourself"
              />
            </div>
            <div className="modal-actions">
              <button className="btn btn-secondary" onClick={closeCreate} title="Discard this new credential and close the dialog">
                Cancel
              </button>
              <button className="btn btn-primary" onClick={handleCreate} disabled={saving} title="Generate the access key credential">
                {saving ? 'Creating...' : 'Create'}
              </button>
            </div>
          </>
        )}
      </Modal>

      <Modal
        isOpen={!!editing}
        onClose={() => setEditing(null)}
        title="Edit Credential"
        subtitle="Update the credential’s name and whether it is active."
        wide
      >
        <div className="form-group">
          <label className="form-label" title="An optional label describing what this credential is for; shown in the credentials list">Name</label>
          <input
            type="text"
            className="input"
            value={editForm.name}
            onChange={(e) => setEditForm({ ...editForm, name: e.target.value })}
            placeholder="e.g., CI pipeline key"
            title="Edit the optional descriptive name for this credential"
          />
        </div>
        {editing?.accessKey ? (
          <div className="form-group">
            <label className="form-label" title="The full access key used as a bearer token; treat it as a secret. This value is read-only and cannot be changed.">Access Key</label>
            <div className="key-reveal">
              <code className="key-value" title="The full secret access key; read-only">{editing.accessKey}</code>
              <CopyButton value={editing.accessKey} title="Copy the access key to your clipboard" />
            </div>
          </div>
        ) : null}
        <div className="form-group">
          <label className="checkbox-label" title="Whether the credential is active and accepted for authentication; inactive credentials are rejected">
            <input
              type="checkbox"
              checked={editForm.active}
              onChange={(e) => setEditForm({ ...editForm, active: e.target.checked })}
              title="Toggle whether this credential is currently active"
            />
            Active
          </label>
        </div>
        <div className="modal-actions">
          <button className="btn btn-secondary" onClick={() => setEditing(null)} title="Discard your changes and close the dialog">
            Cancel
          </button>
          <button className="btn btn-primary" onClick={handleUpdate} disabled={saving} title="Save the changes to this credential">
            {saving ? 'Saving...' : 'Save'}
          </button>
        </div>
      </Modal>

      <JsonViewerModal
        isOpen={!!jsonRow}
        onClose={() => setJsonRow(null)}
        title={jsonRow ? `Credential: ${jsonRow.name || jsonRow.id}` : ''}
        identifier={jsonRow?.id}
        value={jsonRow}
      />

      <DetailModal
        isOpen={!!viewRow}
        onClose={() => setViewRow(null)}
        title={viewRow ? `Credential: ${viewRow.name || viewRow.id}` : ''}
        fields={buildFields(viewRow)}
      />
    </div>
  )
}
