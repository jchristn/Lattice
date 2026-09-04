import { useState, useEffect } from 'react'
import { useApp } from '../context/AppContext'
import { formatDate } from '../utils/api'
import Modal from '../components/Modal'
import ActionMenu from '../components/ActionMenu'
import CopyableId from '../components/CopyableId'
import CopyButton from '../components/CopyButton'
import TablePagination from '../components/TablePagination'
import JsonViewerModal from '../components/JsonViewerModal'
import './Credentials.css'

export default function Credentials() {
  const { api, setError } = useApp()
  const [credentials, setCredentials] = useState([])
  const [loading, setLoading] = useState(true)
  const [forbidden, setForbidden] = useState(false)
  const [page, setPage] = useState(0)
  const [pageSize, setPageSize] = useState(25)
  const [totalRecords, setTotalRecords] = useState(0)
  const [showCreate, setShowCreate] = useState(false)
  const [form, setForm] = useState({ name: '', userId: '' })
  const [saving, setSaving] = useState(false)
  // Holds the freshly created access key, shown exactly once after creation.
  const [createdKey, setCreatedKey] = useState(null)
  const [jsonRow, setJsonRow] = useState(null)

  const totalPages = Math.max(1, Math.ceil(totalRecords / pageSize))

  const onRowClick = (event, row) => {
    if (event.target.closest('button, a, input, select, textarea, label, .action-menu, .copyable-id, [role="button"]')) return
    setJsonRow(row)
  }

  const load = async () => {
    try {
      setLoading(true)
      const result = await api.getCredentials({ maxResults: pageSize, skip: page * pageSize })
      setCredentials(result?.objects || [])
      setTotalRecords(result?.totalRecords ?? (result?.objects?.length || 0))
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
  }, [api, page, pageSize])

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
                <th title="The unique identifier of the credential; use it when referencing the credential via the API">ID</th>
                <th title="The human-readable label given to this credential to describe its purpose">Name</th>
                <th title="The user this credential authenticates as and inherits permissions from">User ID</th>
                <th title="The last four characters of the access key, for identifying it without exposing the secret">Last4</th>
                <th title="Whether the credential is active and accepted for authentication">Active</th>
                <th title="When the credential was created">Created</th>
                <th title="Actions you can perform on this credential">Actions</th>
              </tr>
            </thead>
            <tbody>
              {credentials.map((c) => (
                <tr key={c.id} className="clickable-row" title="Click to view the full record as JSON" onClick={(e) => onRowClick(e, c)}>
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
                          label: 'View Details',
                          onClick: () => setJsonRow(c),
                          title: 'View this credential’s full record as JSON',
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
              ))}
            </tbody>
          </table>
        </div>
      )}

      <Modal
        isOpen={showCreate}
        onClose={closeCreate}
        title={createdKey ? 'Credential Created' : 'New Credential'}
        subtitle={createdKey ? 'Your new access key is shown below.' : 'Generate an access key that an application can use to authenticate to the API.'}
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

      <JsonViewerModal
        isOpen={!!jsonRow}
        onClose={() => setJsonRow(null)}
        title={jsonRow ? `Credential: ${jsonRow.name || jsonRow.id}` : ''}
        identifier={jsonRow?.id}
        value={jsonRow}
      />
    </div>
  )
}
