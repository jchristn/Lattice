import { useState } from 'react'
import { useApp } from '../context/AppContext'
import { GithubIcon } from '../components/Icons'
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

    await attemptCredentialsLogin(null)
    setLoading(false)
  }

  const handleSelectTenant = async (chosenTenantId) => {
    setLoading(true)
    setLocalError('')
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
        <label className="form-label" htmlFor="server-url" title="Base URL of the Lattice server this dashboard should connect to">
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
          title="Enter the full base URL of the Lattice server, including protocol and port"
        />
      </div>

      {localError && <div className="error-message">{localError}</div>}

      <button type="submit" className="btn btn-primary login-btn" disabled={loading} title="Connect to the entered server URL and continue to sign-in">
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
          title="Sign in using your email address and password"
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
          title="Sign in using a pre-issued access key instead of a password"
        >
          Access key
        </button>
      </div>

      {mode === 'credentials' ? (
        <>
          <div className="form-group">
            <label className="form-label" htmlFor="login-email" title="Email address of the account you are signing in with">
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
              title="Enter the email address associated with your Lattice account"
            />
          </div>

          <div className="form-group">
            <label className="form-label" htmlFor="login-password" title="Password for the account you are signing in with">
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
              title="Enter your account password"
            />
          </div>
        </>
      ) : (
        <div className="form-group">
          <label className="form-label" htmlFor="login-access-key" title="Pre-issued access key used to authenticate without a password">
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
            title="Paste the access key issued for your account to sign in"
          />
        </div>
      )}

      {localError && <div className="error-message">{localError}</div>}

      <button type="submit" className="btn btn-primary login-btn" disabled={loading} title="Submit your credentials and sign in to the Lattice server">
        {loading ? 'Signing in...' : 'Sign in'}
      </button>

      <button type="button" className="login-back-btn" onClick={disconnect} disabled={loading} title="Disconnect from this server and return to the server URL entry step">
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
            title={`Sign in to the ${tenant.tenantName || tenant.tenantId} tenant`}
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
        title="Return to the sign-in form without choosing a tenant"
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

        <div className="login-top-actions">
          <button
            type="button"
            className="theme-toggle"
            onClick={toggleTheme}
            title="Switch between light and dark color themes"
            aria-label="Toggle color theme"
          >
            {theme === 'light' ? '🌙' : '☀️'}
          </button>
          <a
            className="login-github"
            href="https://github.com/jchristn/lattice"
            target="_blank"
            rel="noopener noreferrer"
            title="Open the Lattice project on GitHub in a new tab"
            aria-label="Open the Lattice project on GitHub"
          >
            <GithubIcon size={18} />
          </a>
        </div>
      </div>
    </div>
  )
}
