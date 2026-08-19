import { createContext, useContext, useEffect, useState, type ReactNode } from 'react'
import { get, post, setToken } from '../api/client'
import type { AuthResponse, User } from '../types'

interface AuthState {
  user: User | null
  loading: boolean
  login: (email: string, password: string) => Promise<User>
  register: (body: RegisterBody) => Promise<User>
  logout: () => void
}

export interface RegisterBody {
  fullName: string
  email: string
  password: string
  userType: 'Developer' | 'Manager'
  functionId?: number | null
  roleId?: number | null
}

const AuthCtx = createContext<AuthState | undefined>(undefined)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    // Restore session from a stored token, if present.
    get<User>('/api/auth/me')
      .then(setUser)
      .catch(() => setToken(null))
      .finally(() => setLoading(false))
  }, [])

  const login = async (email: string, password: string) => {
    const res = await post<AuthResponse>('/api/auth/login', { email, password })
    setToken(res.token)
    setUser(res.user)
    return res.user
  }

  const register = async (body: RegisterBody) => {
    const res = await post<AuthResponse>('/api/auth/register', body)
    setToken(res.token)
    setUser(res.user)
    return res.user
  }

  const logout = () => {
    setToken(null)
    setUser(null)
  }

  return (
    <AuthCtx.Provider value={{ user, loading, login, register, logout }}>
      {children}
    </AuthCtx.Provider>
  )
}

export function useAuth() {
  const ctx = useContext(AuthCtx)
  if (!ctx) throw new Error('useAuth must be used within AuthProvider')
  return ctx
}
