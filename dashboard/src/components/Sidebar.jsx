import { NavLink, useLocation } from 'react-router-dom'
import './Sidebar.css'

export default function Sidebar() {
  const location = useLocation()
  const isDocumentsPage = location.pathname === '/documents' || location.pathname.endsWith('/documents')
  const isSchemaElementsPage = location.pathname === '/schema-elements' || location.pathname.endsWith('/elements')

  return (
    <aside className="sidebar">
      <nav className="sidebar-nav">
        <NavLink
          to="/collections"
          className={() => `sidebar-link ${location.pathname === '/collections' ? 'active' : ''}`}
        >
          <span className="sidebar-icon">&#128193;</span>
          Collections
        </NavLink>

        <NavLink
          to="/documents"
          className={() => `sidebar-link ${isDocumentsPage ? 'active' : ''}`}
        >
          <span className="sidebar-icon">&#128196;</span>
          Documents
        </NavLink>

        <NavLink
          to="/schemas"
          className={() => `sidebar-link ${location.pathname === '/schemas' ? 'active' : ''}`}
        >
          <span className="sidebar-icon">&#128221;</span>
          Schemas
        </NavLink>

        <NavLink
          to="/schema-elements"
          className={() => `sidebar-link ${isSchemaElementsPage ? 'active' : ''}`}
        >
          <span className="sidebar-icon">&#128203;</span>
          Schema Elements
        </NavLink>

        <NavLink
          to="/tables"
          className={({ isActive }) => `sidebar-link ${isActive ? 'active' : ''}`}
        >
          <span className="sidebar-icon">&#128202;</span>
          Index Tables
        </NavLink>

        <NavLink
          to="/search"
          className={({ isActive }) => `sidebar-link ${isActive ? 'active' : ''}`}
        >
          <span className="sidebar-icon">&#128269;</span>
          Search
        </NavLink>
      </nav>
    </aside>
  )
}
