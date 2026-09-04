import { useApp } from '../context/AppContext'
import CopyButton from './CopyButton'
import { GithubIcon, LogoutIcon, MoonIcon, SunIcon } from './Icons'
import './Topbar.css'

export default function Topbar() {
  const { serverUrl, theme, toggleTheme, logout, principal } = useApp()

  const principalLabel = principal?.email || (principal ? 'Credential' : '')

  const handleSignOut = async () => {
    // Revoke the session and return the user to the login screen.
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
          <span className="topbar-principal" title={`Signed in as ${principalLabel}`}>
            {principalLabel}
          </span>
        ) : null}
        <a
          className="topbar-btn"
          href="https://github.com/jchristn/lattice"
          target="_blank"
          rel="noopener noreferrer"
          title="Open the Lattice project on GitHub in a new tab"
          aria-label="Open the Lattice project on GitHub"
        >
          <GithubIcon size={16} />
        </a>
        <button
          className="topbar-btn"
          onClick={toggleTheme}
          title={theme === 'light' ? 'Switch to dark theme' : 'Switch to light theme'}
          aria-label="Toggle color theme"
          type="button"
        >
          {theme === 'light' ? <MoonIcon size={16} /> : <SunIcon size={16} />}
        </button>
        <button
          className="topbar-btn topbar-signout-btn"
          onClick={handleSignOut}
          title="Sign out and return to the login screen"
          aria-label="Sign out"
          type="button"
        >
          <LogoutIcon size={16} />
        </button>
      </div>
    </header>
  )
}
