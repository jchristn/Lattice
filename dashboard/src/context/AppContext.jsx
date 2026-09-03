import { createContext, useContext, useState, useEffect, useMemo } from 'react'
import { LatticeApi } from '../utils/api'

const AppContext = createContext(null)

export function AppProvider({ children }) {
  const [serverUrl, setServerUrl] = useState(() => {
    return localStorage.getItem('lattice_server_url') || ''
  })
  const [token, setToken] = useState(() => {
    return localStorage.getItem('lattice_auth_token') || ''
  })
  const [principal, setPrincipal] = useState(null)
  const [theme, setTheme] = useState(() => {
    return localStorage.getItem('lattice_theme') || 'light'
  })
  const [error, setError] = useState(null)
  const [showTour, setShowTour] = useState(false)
  const [showSetupWizard, setShowSetupWizard] = useState(false)

  useEffect(() => {
    document.documentElement.setAttribute('data-theme', theme)
    localStorage.setItem('lattice_theme', theme)
  }, [theme])

  // Derive the API client synchronously from serverUrl + token so it is available on the same
  // render (avoids a race where a view mounts and calls the API before an effect sets it).
  const api = useMemo(
    () => (serverUrl ? new LatticeApi(serverUrl, token) : null),
    [serverUrl, token]
  )

  const isAuthenticated = !!token

  useEffect(() => {
    if (serverUrl) {
      localStorage.setItem('lattice_server_url', serverUrl)
    } else {
      localStorage.removeItem('lattice_server_url')
    }
  }, [serverUrl])

  useEffect(() => {
    if (token) {
      localStorage.setItem('lattice_auth_token', token)
    } else {
      localStorage.removeItem('lattice_auth_token')
    }
  }, [token])

  // Wire the 401 handler: any authenticated request that comes back 401 clears the
  // session and returns the user to the login screen. The handler only mutates local
  // state, so it cannot loop.
  useEffect(() => {
    if (!api) return undefined
    api.onUnauthorized = () => {
      setToken('')
      setPrincipal(null)
    }
    return () => {
      api.onUnauthorized = null
    }
  }, [api])

  // Hydrate the principal after a reload when we already hold a token. If the token is
  // stale the request 401s and the handler above clears it.
  useEffect(() => {
    let cancelled = false
    if (api && token && !principal) {
      api
        .whoami()
        .then((result) => {
          if (!cancelled) setPrincipal(result)
        })
        .catch(() => {})
    }
    return () => {
      cancelled = true
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [api, token])

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
      await testApi.health() // Test reachability (public, no auth required)
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
    setToken('')
    setPrincipal(null)
  }

  // Credentials login: exchange email/password/tenant for a session token, then
  // fetch the principal descriptor.
  const login = async (email, password, tenantId) => {
    if (!api) return false
    try {
      const result = await api.login(email, password, tenantId)
      const sessionToken = result?.token
      if (!sessionToken) {
        throw new Error('No token returned')
      }
      api.setToken(sessionToken)
      const who = await api.whoami()
      setToken(sessionToken)
      setPrincipal(who)
      setError(null)
      return true
    } catch (err) {
      setError('Login failed: ' + err.message)
      return false
    }
  }

  // Access-key login: use the provided key directly as the bearer value and validate
  // it via whoami before committing it.
  const loginWithAccessKey = async (accessKey) => {
    if (!api || !accessKey) return false
    try {
      api.setToken(accessKey)
      const who = await api.whoami()
      setToken(accessKey)
      setPrincipal(who)
      setError(null)
      return true
    } catch (err) {
      api.setToken(token || null)
      setError('Access key rejected: ' + err.message)
      return false
    }
  }

  const logout = async () => {
    if (api) {
      try {
        await api.logout()
      } catch {
        // Ignore logout errors; we clear local state regardless.
      }
    }
    setToken('')
    setPrincipal(null)
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
    token,
    principal,
    isAuthenticated,
    login,
    loginWithAccessKey,
    logout,
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
