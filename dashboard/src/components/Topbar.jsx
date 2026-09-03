import { useApp } from '../context/AppContext'
import CopyButton from './CopyButton'
import { GithubIcon, LogoutIcon, MoonIcon, SunIcon } from './Icons'
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
        <div className="topbar-server-wrap">
          <span className="topbar-server" title={serverUrl}>
            {serverUrl}
          </span>
          {serverUrl ? <CopyButton value={serverUrl} className="topbar-copy-btn" /> : null}
        </div>
      </div>

      <div className="topbar-actions">
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
        <button
          className="topbar-btn topbar-btn-disconnect"
          onClick={disconnect}
          title="Disconnect"
          aria-label="Disconnect"
          type="button"
        >
          <LogoutIcon size={16} />
        </button>
      </div>
    </header>
  )
}
