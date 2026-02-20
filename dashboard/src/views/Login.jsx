import { useState } from 'react'
import { useApp } from '../context/AppContext'
import './Login.css'

export default function Login() {
  const { connect, error, theme, toggleTheme } = useApp()
  const [url, setUrl] = useState(window.__LATTICE_CONFIG__?.serverUrl || 'http://localhost:8000')
  const [loading, setLoading] = useState(false)
  const [localError, setLocalError] = useState('')

  const handleSubmit = async (e) => {
    e.preventDefault()
    setLoading(true)
    setLocalError('')

    const success = await connect(url)
    if (!success) {
      setLocalError(error || 'Failed to connect')
    }

    setLoading(false)
  }

  return (
    <div className="login-container">
      <div className="login-card">
        <div className="login-header">
          <img src="/logo.png" alt="Lattice" className="login-logo" />
          <h1>Lattice</h1>
          <p>JSON Document Store</p>
        </div>

        <form onSubmit={handleSubmit}>
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

          {localError && (
            <div className="error-message">{localError}</div>
          )}

          <button type="submit" className="btn btn-primary login-btn" disabled={loading}>
            {loading ? 'Connecting...' : 'Connect'}
          </button>
        </form>

        <button className="theme-toggle" onClick={toggleTheme} title="Toggle theme">
          {theme === 'light' ? '🌙' : '☀️'}
        </button>
      </div>
    </div>
  )
}
