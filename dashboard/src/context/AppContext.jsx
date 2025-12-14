import { createContext, useContext, useState, useEffect } from 'react'
import { LatticeApi } from '../utils/api'

const AppContext = createContext(null)

export function AppProvider({ children }) {
  const [serverUrl, setServerUrl] = useState(() => {
    return localStorage.getItem('lattice_server_url') || ''
  })
  const [theme, setTheme] = useState(() => {
    return localStorage.getItem('lattice_theme') || 'light'
  })
  const [api, setApi] = useState(null)
  const [error, setError] = useState(null)

  useEffect(() => {
    document.documentElement.setAttribute('data-theme', theme)
    localStorage.setItem('lattice_theme', theme)
  }, [theme])

  useEffect(() => {
    if (serverUrl) {
      localStorage.setItem('lattice_server_url', serverUrl)
      setApi(new LatticeApi(serverUrl))
    } else {
      localStorage.removeItem('lattice_server_url')
      setApi(null)
    }
  }, [serverUrl])

  const toggleTheme = () => {
    setTheme(prev => prev === 'light' ? 'dark' : 'light')
  }

  const connect = async (url) => {
    try {
      const testApi = new LatticeApi(url)
      await testApi.getCollections() // Test connection
      setServerUrl(url)
      setError(null)
      return true
    } catch (err) {
      setError('Failed to connect to server: ' + err.message)
      return false
    }
  }

  const disconnect = () => {
    setServerUrl('')
    setApi(null)
  }

  const value = {
    serverUrl,
    theme,
    api,
    error,
    setError,
    toggleTheme,
    connect,
    disconnect,
  }

  return (
    <AppContext.Provider value={value}>
      {children}
    </AppContext.Provider>
  )
}

export function useApp() {
  const context = useContext(AppContext)
  if (!context) {
    throw new Error('useApp must be used within AppProvider')
  }
  return context
}
