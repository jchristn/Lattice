import { useState } from 'react'
import { useApp } from '../context/AppContext'
import './Login.css'

export default function Login() {
  const {
    serverUrl,
    connect,
    disconnect,
    login,
    loginWithAccessKey,
    error,
    theme,
    toggleTheme,
  } = useApp()

  // Server-URL step
  const [url, setUrl] = useState(window.__LATTICE_CONFIG__?.serverUrl || 'http://localhost:8000')

  // Credentials step
  const [mode, setMode] = useState('credentials') // 'credentials' | 'accessKey'
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [tenantId, setTenantId] = useState('')
  const [accessKey, setAccessKey] = useState('')

  const [loading, setLoading] = useState(false)
  const [localError, setLocalError] = useState('')

  // Tenant selection step: populated when the credentials match users in more
  // than one tenant. email/password are preserved so the user doesn't retype
  // them when picking a tenant.
  const [tenantChoices, setTenantChoices] = useState(null)

  const handleConnect = async (e) => {
    e.preventDefault()
    setLoading(true)
    setLocalError('')
    const success = await connect(url)
    if (!success) {
      setLocalError(error || 'Failed to connect')
    }
    setLoading(false)
  }

  // Perform a credentials login for the given tenant (may be null to let the
  // server infer). Returns true on a completed sign-in. Handles the
  // tenant-selection prompt by stashing the returned tenants for the chooser.
  const attemptCredentialsLogin = async (chosenTenantId) => {
    const result = await login(email.trim(), password, chosenTenantId || null)

    if (result && typeof result === 'object' && result.tenantSelectionRequired) {
      setTenantChoices(result.tenants || [])
      setLocalError('')
      return false
    }

    if (!result) {
      setLocalError(error || 'Login failed')
      return false
    }

    return true
  }

  const handleLogin = async (e) => {
    e.preventDefault()
    setLoading(true)
    setLocalError('')

    if (mode === 'accessKey') {
      const success = await loginWithAccessKey(accessKey.trim())
      if (!success) {
        setLocalError(error || 'Login failed')
      }
      setLoading(false)
      return
    }

    await attemptCredentialsLogin(tenantId.trim() || null)
    setLoading(false)
  }

  const handleSelectTenant = async (chosenTenantId) => {
    setLoading(true)
    setLocalError('')
    setTenantId(chosenTenantId)
    const success = await attemptCredentialsLogin(chosenTenantId)
    if (!success) {
      // A follow-up selection should not normally re-prompt; if it fails, fall
      // back to the credentials form so the user can retry.
      setTenantChoices(null)
    }
    setLoading(false)
  }

  const handleCancelTenantSelection = () => {
    setTenantChoices(null)
    setLocalError('')
  }

  const renderServerStep = () => (
    <form onSubmit={handleConnect}>
      <div className="form-group">
        <label className="form-label" htmlFor="server-url">
          Server URL
        </label>
        <input
          id="server-url"
          type="url"
          className="input"
          value={url}
          onChange={(e) => setUrl(e.target.value)}
          placeholder="http://localhost:8000"
          required
        />
      </div>

      {localError && <div className="error-message">{localError}</div>}

      <button type="submit" className="btn btn-primary login-btn" disabled={loading}>
        {loading ? 'Connecting...' : 'Connect'}
      </button>
    </form>
  )

  const renderCredentialsStep = () => (
    <form onSubmit={handleLogin}>
      <div className="login-tabs">
        <button
          type="button"
          className={`login-tab ${mode === 'credentials' ? 'is-active' : ''}`}
          onClick={() => {
            setMode('credentials')
            setLocalError('')
          }}
        >
          Email &amp; password
        </button>
        <button
          type="button"
          className={`login-tab ${mode === 'accessKey' ? 'is-active' : ''}`}
          onClick={() => {
            setMode('accessKey')
            setLocalError('')
          }}
        >
          Access key
        </button>
      </div>

      {mode === 'credentials' ? (
        <>
          <div className="form-group">
            <label className="form-label" htmlFor="login-email">
              Email
            </label>
            <input
              id="login-email"
              type="email"
              className="input"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="you@example.com"
              autoComplete="username"
              required
            />
          </div>

          <div className="form-group">
            <label className="form-label" htmlFor="login-password">
              Password
            </label>
            <input
              id="login-password"
              type="password"
              className="input"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              autoComplete="current-password"
              required
            />
          </div>

          <div className="form-group">
            <label className="form-label" htmlFor="login-tenant">
              Tenant{' '}
              <span className="form-optional">
                (optional — leave blank to detect automatically)
              </span>
            </label>
            <input
              id="login-tenant"
              type="text"
              className="input"
              value={tenantId}
              onChange={(e) => setTenantId(e.target.value)}
              placeholder="Detected from your credentials"
            />
          </div>
        </>
      ) : (
        <div className="form-group">
          <label className="form-label" htmlFor="login-access-key">
            Access key
          </label>
          <input
            id="login-access-key"
            type="password"
            className="input"
            value={accessKey}
            onChange={(e) => setAccessKey(e.target.value)}
            placeholder="access_..."
            required
          />
        </div>
      )}

      {localError && <div className="error-message">{localError}</div>}

      <button type="submit" className="btn btn-primary login-btn" disabled={loading}>
        {loading ? 'Signing in...' : 'Sign in'}
      </button>

      <button type="button" className="login-back-btn" onClick={disconnect} disabled={loading}>
        Change server
      </button>
    </form>
  )

  const renderTenantStep = () => (
    <div>
      <p className="tenant-select-intro">
        Your account belongs to more than one tenant. Choose which one to sign in to.
      </p>

      <div className="tenant-list">
        {tenantChoices.map((tenant) => (
          <button
            key={tenant.tenantId}
            type="button"
            className="tenant-option"
            onClick={() => handleSelectTenant(tenant.tenantId)}
            disabled={loading}
          >
            <span className="tenant-option-name">
              {tenant.tenantName || tenant.tenantId}
            </span>
            <span className="tenant-option-id">{tenant.tenantId}</span>
          </button>
        ))}
      </div>

      {localError && <div className="error-message">{localError}</div>}

      <button
        type="button"
        className="login-back-btn"
        onClick={handleCancelTenantSelection}
        disabled={loading}
      >
        Back
      </button>
    </div>
  )

  const renderBody = () => {
    if (!serverUrl) return renderServerStep()
    if (tenantChoices) return renderTenantStep()
    return renderCredentialsStep()
  }

  let subtitle = 'JSON Document Store'
  if (serverUrl) {
    subtitle = tenantChoices ? 'Select a tenant' : 'Sign in to continue'
  }

  return (
    <div className="login-container">
      <div className="login-card">
        <div className="login-header">
          <img src="/logo.png" alt="Lattice" className="login-logo" />
          <h1>Lattice</h1>
          <p>{subtitle}</p>
        </div>

        {renderBody()}

        <button className="theme-toggle" onClick={toggleTheme} title="Toggle theme">
          {theme === 'light' ? '🌙' : '☀️'}
        </button>
      </div>
    </div>
  )
}
