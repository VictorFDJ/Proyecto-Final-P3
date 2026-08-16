import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import { api } from '../lib/api'
import type { AuthResponse, User } from '../types'

interface AuthContextValue {
  user: User | null; token: string | null; loading: boolean
  login(email: string, password: string): Promise<void>
  register(name: string, email: string, password: string): Promise<void>
  logout(): void; updateUser(user: User): void
}

const AuthContext = createContext<AuthContextValue | null>(null)
const TOKEN_KEY = 'mi-presupuesto-token'
const USER_KEY = 'mi-presupuesto-user'

export function AuthProvider({ children }: { children: ReactNode }) {
  const [token, setToken] = useState<string | null>(() => localStorage.getItem(TOKEN_KEY))
  const [user, setUser] = useState<User | null>(() => {
    try { return JSON.parse(localStorage.getItem(USER_KEY) ?? 'null') } catch { return null }
  })
  const [loading] = useState(false)

  const save = (response: AuthResponse) => {
    localStorage.setItem(TOKEN_KEY, response.token)
    localStorage.setItem(USER_KEY, JSON.stringify(response.user))
    setToken(response.token); setUser(response.user)
  }
  const logout = () => {
    localStorage.removeItem(TOKEN_KEY); localStorage.removeItem(USER_KEY)
    setToken(null); setUser(null)
  }
  useEffect(() => {
    const handler = () => logout()
    window.addEventListener('auth:unauthorized', handler)
    return () => window.removeEventListener('auth:unauthorized', handler)
  }, [])
  const value = useMemo<AuthContextValue>(() => ({
    user, token, loading,
    login: async (email, password) => save(await api<AuthResponse>('/auth/login', {
      method: 'POST', body: JSON.stringify({ email, password }),
    })),
    register: async (name, email, password) => save(await api<AuthResponse>('/auth/register', {
      method: 'POST', body: JSON.stringify({ name, email, password }),
    })),
    logout,
    updateUser: next => { setUser(next); localStorage.setItem(USER_KEY, JSON.stringify(next)) },
  }), [user, token, loading])
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const value = useContext(AuthContext)
  if (!value) throw new Error('useAuth debe usarse dentro de AuthProvider')
  return value
}
