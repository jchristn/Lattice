import { Outlet } from 'react-router-dom'
import { useApp } from '../context/AppContext'
import Topbar from '../components/Topbar'
import Sidebar from '../components/Sidebar'
import './Dashboard.css'

export default function Dashboard() {
  const { error, setError } = useApp()

  return (
    <div className="dashboard">
      <Topbar />
      {error && (
        <div className="error-banner">
          <span className="error-banner-message">{error}</span>
          <button className="error-banner-close" onClick={() => setError(null)}>
            &times;
          </button>
        </div>
      )}
      <div className="dashboard-body">
        <Sidebar />
        <main className="dashboard-content">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
