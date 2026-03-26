import { useApp } from '../context/AppContext'
import CopyButton from './CopyButton'
import { MoonIcon, SunIcon } from './Icons'
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
          type="button"
        >
          Disconnect
        </button>
      </div>
    </header>
  )
}
