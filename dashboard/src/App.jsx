import { Routes, Route, Navigate } from 'react-router-dom'
import { useApp } from './context/AppContext'
import Login from './views/Login'
import Dashboard from './views/Dashboard'
import Collections from './views/Collections'
import Documents from './views/Documents'
import Search from './views/Search'
import Schemas from './views/Schemas'
import SchemaElements from './views/SchemaElements'
import Tables from './views/Tables'

function App() {
  const { serverUrl } = useApp()

  if (!serverUrl) {
    return <Login />
  }

  return (
    <Routes>
      <Route path="/" element={<Dashboard />}>
        <Route index element={<Navigate to="/collections" replace />} />
        <Route path="collections" element={<Collections />} />
        <Route path="documents" element={<Documents />} />
        <Route path="collections/:collectionId/documents" element={<Documents />} />
        <Route path="schemas" element={<Schemas />} />
        <Route path="schema-elements" element={<SchemaElements />} />
        <Route path="schemas/:schemaId/elements" element={<SchemaElements />} />
        <Route path="tables" element={<Tables />} />
        <Route path="search" element={<Search />} />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}

export default App
