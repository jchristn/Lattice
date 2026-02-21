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
  const [showTour, setShowTour] = useState(false)
  const [showSetupWizard, setShowSetupWizard] = useState(false)

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

  // Auto-trigger tour for first-time visitors after connecting
  useEffect(() => {
    if (api && !localStorage.getItem('lattice_tour_completed')) {
      setShowTour(true)
    }
  }, [api])

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

  const startTour = () => {
    setShowTour(true)
  }

  const completeTour = () => {
    setShowTour(false)
    localStorage.setItem('lattice_tour_completed', 'true')
    // Auto-trigger setup wizard after tour if not completed
    if (!localStorage.getItem('lattice_setup_completed')) {
      setShowSetupWizard(true)
    }
  }

  const startSetupWizard = () => {
    setShowSetupWizard(true)
  }

  const completeSetupWizard = () => {
    setShowSetupWizard(false)
    localStorage.setItem('lattice_setup_completed', 'true')
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
    showTour,
    showSetupWizard,
    startTour,
    completeTour,
    startSetupWizard,
    completeSetupWizard,
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
