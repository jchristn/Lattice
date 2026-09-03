import { useApp } from '../context/AppContext'
import CopyButton from './CopyButton'
import { GithubIcon, LogoutIcon, MoonIcon, SunIcon } from './Icons'
import './Topbar.css'

export default function Topbar() {
  const { serverUrl, theme, toggleTheme, disconnect, logout, principal } = useApp()

  const principalLabel = principal?.email || (principal ? 'Credential' : '')

  const handleLogout = async () => {
    // Revoke the session but keep the server connection so the user lands on the
    // credentials screen rather than the server-URL screen.
    await logout()
  }

  return (
    <header className="topbar">
      <div className="topbar-brand">
        <img src="/logo.png" alt="Lattice" className="topbar-logo-img" />
        <span className="topbar-logo">Lattice</span>
      </div>

      <div className="topbar-info">
        <div className="topbar-server-wrap">
          <span className="topbar-server" title={serverUrl}>
            {serverUrl}
          </span>
          {serverUrl ? <CopyButton value={serverUrl} className="topbar-copy-btn" /> : null}
        </div>
      </div>

      <div className="topbar-actions">
        {principalLabel ? (
          <span className="topbar-principal" title={principalLabel}>
            {principalLabel}
          </span>
        ) : null}
        <a
          className="topbar-btn"
          href="https://github.com/jchristn/lattice"
          target="_blank"
          rel="noopener noreferrer"
          title="GitHub"
          aria-label="GitHub"
        >
          <GithubIcon size={16} />
        </a>
        <button
          className="topbar-btn"
          onClick={toggleTheme}
          title="Toggle theme"
          type="button"
        >
          {theme === 'light' ? <MoonIcon size={16} /> : <SunIcon size={16} />}
        </button>
        {principal ? (
          <button
            className="topbar-btn topbar-logout-btn"
            onClick={handleLogout}
            title="Log out"
            type="button"
          >
            <LogoutIcon size={16} />
            <span>Logout</span>
          </button>
        ) : null}
        <button
          className="topbar-btn topbar-btn-disconnect"
          onClick={disconnect}
          title="Disconnect from server"
          aria-label="Disconnect from server"
          type="button"
        >
          <LogoutIcon size={16} />
        </button>
      </div>
    </header>
  )
}
