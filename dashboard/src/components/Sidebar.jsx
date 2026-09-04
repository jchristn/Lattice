import { NavLink, useLocation } from 'react-router-dom'
import { useApp } from '../context/AppContext'
import {
  BracketsIcon,
  BuildingIcon,
  FileIcon,
  FolderIcon,
  HistoryIcon,
  KeyIcon,
  ListIcon,
  PulseIcon,
  SchemaIcon,
  ScrollIcon,
  SearchIcon,
  ShieldIcon,
  TableIcon,
  TerminalIcon,
  UserCheckIcon,
  UserIcon,
} from './Icons'
import './Sidebar.css'

export default function Sidebar() {
  const location = useLocation()
  const { startTour, startSetupWizard, principal } = useApp()
  const isDocumentsPage = location.pathname === '/documents' || location.pathname.endsWith('/documents')
  const isSchemaElementsPage = location.pathname === '/schema-elements' || location.pathname.endsWith('/elements')

  // Only administrators (system or tenant) may see the CONFIGURE group, which
  // exposes identity, credential, and authorization management surfaces.
  const canConfigure = !!(principal?.isAdmin || principal?.isTenantAdmin)

  return (
    <aside className="sidebar">
      <nav className="sidebar-nav">
        <div className="sidebar-group">
          <div className="sidebar-group-label" title="Collections and the documents stored inside them">Data</div>
          <NavLink
            to="/collections"
            title="Browse and manage document collections"
            className={() => `sidebar-link ${location.pathname === '/collections' ? 'active' : ''}`}
          >
            <span className="sidebar-icon"><FolderIcon size={16} /></span>
            Collections
          </NavLink>
          <NavLink
            to="/documents"
            title="Browse, inspect, and manage individual documents"
            className={() => `sidebar-link ${isDocumentsPage ? 'active' : ''}`}
          >
            <span className="sidebar-icon"><FileIcon size={16} /></span>
            Documents
          </NavLink>
        </div>

        <div className="sidebar-group">
          <div className="sidebar-group-label" title="Schemas and indexes that describe and accelerate your data">Structure</div>
          <NavLink
            to="/schemas"
            title="Define and review schemas that describe document shape"
            className={() => `sidebar-link ${location.pathname === '/schemas' ? 'active' : ''}`}
          >
            <span className="sidebar-icon"><SchemaIcon size={16} /></span>
            Schemas
          </NavLink>
          <NavLink
            to="/schema-elements"
            title="Inspect the individual field elements that make up a schema"
            className={() => `sidebar-link ${isSchemaElementsPage ? 'active' : ''}`}
          >
            <span className="sidebar-icon"><BracketsIcon size={16} /></span>
            Schema Elements
          </NavLink>
          <NavLink
            to="/tables"
            title="View the index tables that back fast field lookups"
            className={({ isActive }) => `sidebar-link ${isActive ? 'active' : ''}`}
          >
            <span className="sidebar-icon"><TableIcon size={16} /></span>
            Index Tables
          </NavLink>
          <NavLink
            to="/entries"
            title="Browse the individual entries stored inside index tables"
            className={({ isActive }) => `sidebar-link ${isActive ? 'active' : ''}`}
          >
            <span className="sidebar-icon"><ListIcon size={16} /></span>
            Index Entries
          </NavLink>
        </div>

        <div className="sidebar-group">
          <div className="sidebar-group-label" title="Query documents across collections">Search</div>
          <NavLink
            to="/search"
            title="Run structured searches against document collections"
            className={({ isActive }) => `sidebar-link ${isActive ? 'active' : ''}`}
          >
            <span className="sidebar-icon"><SearchIcon size={16} /></span>
            Search
          </NavLink>
        </div>

        <div className="sidebar-group">
          <div className="sidebar-group-label" title="Operational tooling for inspecting and exercising the API">Manage</div>
          <NavLink
            to="/request-history"
            title="Review a log of past API requests and their outcomes"
            className={({ isActive }) => `sidebar-link ${isActive ? 'active' : ''}`}
          >
            <span className="sidebar-icon"><HistoryIcon size={16} /></span>
            Request History
          </NavLink>
          <NavLink
            to="/api-explorer"
            title="Interactively build and send requests to the Lattice API"
            className={({ isActive }) => `sidebar-link ${isActive ? 'active' : ''}`}
          >
            <span className="sidebar-icon"><TerminalIcon size={16} /></span>
            API Explorer
          </NavLink>
          <NavLink
            to="/observability"
            title="Monitor server health, metrics, and telemetry"
            className={({ isActive }) => `sidebar-link ${isActive ? 'active' : ''}`}
          >
            <span className="sidebar-icon"><PulseIcon size={16} /></span>
            Observability
          </NavLink>
        </div>

        {canConfigure ? (
          <div className="sidebar-group">
            <div className="sidebar-group-label" title="Administrative settings for tenants, identities, and access control">Configure</div>
            <NavLink
              to="/tenants"
              title="Manage tenants, the top-level isolation boundary for data and users"
              className={({ isActive }) => `sidebar-link ${isActive ? 'active' : ''}`}
            >
              <span className="sidebar-icon"><BuildingIcon size={16} /></span>
              Tenants
            </NavLink>
            <NavLink
              to="/users"
              title="Manage user accounts that sign in with email and password"
              className={({ isActive }) => `sidebar-link ${isActive ? 'active' : ''}`}
            >
              <span className="sidebar-icon"><UserIcon size={16} /></span>
              Users
            </NavLink>
            <NavLink
              to="/credentials"
              title="Manage access keys that authenticate applications and scripts"
              className={({ isActive }) => `sidebar-link ${isActive ? 'active' : ''}`}
            >
              <span className="sidebar-icon"><KeyIcon size={16} /></span>
              Credentials
            </NavLink>
            <NavLink
              to="/roles"
              title="Review the roles that grant sets of permissions"
              className={({ isActive }) => `sidebar-link ${isActive ? 'active' : ''}`}
            >
              <span className="sidebar-icon"><ShieldIcon size={16} /></span>
              Roles
            </NavLink>
            <NavLink
              to="/assignments"
              title="Grant users roles over specific resources to control access"
              className={({ isActive }) => `sidebar-link ${isActive ? 'active' : ''}`}
            >
              <span className="sidebar-icon"><UserCheckIcon size={16} /></span>
              Role Assignments
            </NavLink>
            <NavLink
              to="/audit"
              title="Review the security audit log of authorization decisions and changes"
              className={({ isActive }) => `sidebar-link ${isActive ? 'active' : ''}`}
            >
              <span className="sidebar-icon"><ScrollIcon size={16} /></span>
              Audit Log
            </NavLink>
          </div>
        ) : null}
      </nav>

      <div className="sidebar-footer">
        <button className="sidebar-footer-link" onClick={startTour} title="Replay the guided product tour of the dashboard">
          Take Tour
        </button>
        <button className="sidebar-footer-link" onClick={startSetupWizard} title="Open the step-by-step setup wizard to configure your workspace">
          Setup Wizard
        </button>
      </div>
    </aside>
  )
}
