import { useEffect, useState } from 'react'
import { clearStoredToken, persistToken, readStoredToken, withAuthHeader } from './auth'
import { AuthContext } from './auth-context'

export function AuthProvider({ children }) {
  const [authToken, setAuthToken] = useState(() => readStoredToken())
  const [authenticatedUser, setAuthenticatedUser] = useState('')
  const [authChecking, setAuthChecking] = useState(false)
  const [authNotice, setAuthNotice] = useState('')
  const [authError, setAuthError] = useState('')

  useEffect(() => {
    const validateAuth = async () => {
      if (!authToken) {
        setAuthenticatedUser('')
        setAuthChecking(false)
        setAuthError('')
        return
      }

      setAuthChecking(true)

      try {
        const response = await fetch('/api/auth/me', {
          headers: withAuthHeader({}, authToken),
        })

        if (!response.ok) {
          throw new Error('Invalid credentials')
        }

        const data = await response.json()
        if (!data.authenticated) {
          throw new Error('Invalid credentials')
        }

        setAuthenticatedUser(data.username)
        setAuthError('')
        setAuthNotice(`Signed in as ${data.username}`)
      } catch {
        clearStoredToken()
        setAuthToken('')
        setAuthenticatedUser('')
        setAuthNotice('')
        setAuthError('Stored credentials are no longer valid.')
      } finally {
        setAuthChecking(false)
      }
    }

    validateAuth()
  }, [authToken])

  const authenticate = async ({ username, password, path }) => {
    if (!username.trim() || !password) {
      setAuthError('Username and password are required.')
      return { ok: false }
    }

    setAuthChecking(true)
    setAuthError('')
    setAuthNotice('')

    try {
      const response = await fetch(path, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          username: username.trim(),
          password,
        }),
      })

      const data = await response.json().catch(() => ({}))
      if (!response.ok) {
        throw new Error(data.error ?? 'Authentication failed.')
      }

      persistToken(data.token)
      setAuthToken(data.token)
      setAuthenticatedUser(data.username)
      setAuthNotice(`Signed in as ${data.username}`)
      return { ok: true }
    } catch (error) {
      setAuthError(error.message)
      setAuthenticatedUser('')
      return { ok: false }
    } finally {
      setAuthChecking(false)
    }
  }

  const logout = () => {
    clearStoredToken()
    setAuthToken('')
    setAuthenticatedUser('')
    setAuthError('')
    setAuthNotice('Signed out.')
  }

  return (
    <AuthContext.Provider
      value={{
        authToken,
        authenticatedUser,
        authChecking,
        authNotice,
        authError,
        isAuthenticated: authenticatedUser.length > 0,
        authenticate,
        logout,
      }}
    >
      {children}
    </AuthContext.Provider>
  )
}