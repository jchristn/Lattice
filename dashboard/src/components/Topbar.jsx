import { useApp } from '../context/AppContext'
import './Topbar.css'

export default function Topbar() {
  const { serverUrl, theme, toggleTheme, disconnect } = useApp()

  return (
    <header className="topbar">
      <div className="topbar-brand">
        <img src="/logo.png" alt="Lattice" className="topbar-logo-img" />
        <span className="topbar-logo">Lattice</span>
      </div>

      <div className="topbar-info">
        <span className="topbar-server" title={serverUrl}>
          {serverUrl}
        </span>
      </div>

      <div className="topbar-actions">
        <button
          className="topbar-btn"
          onClick={toggleTheme}
          title="Toggle theme"
        >
          {theme === 'light' ? '🌙' : '☀️'}
        </button>
        <button
          className="topbar-btn topbar-btn-disconnect"
          onClick={disconnect}
          title="Disconnect"
        >
          Disconnect
        </button>
      </div>
    </header>
  )
}
