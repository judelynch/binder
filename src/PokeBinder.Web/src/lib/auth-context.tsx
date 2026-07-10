import {
  createContext,
  useContext,
  useEffect,
  useState,
  type ReactNode,
} from 'react'
import { api, setAuthToken } from './api'
import type { AuthResponse, AuthUser } from './types'

const TOKEN_STORAGE_KEY = 'pokebinder.token'

interface AuthContextValue {
  user: AuthUser | null
  isLoading: boolean
  login: (email: string, password: string) => Promise<void>
  register: (email: string, password: string) => Promise<void>
  logout: () => void
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    const storedToken = localStorage.getItem(TOKEN_STORAGE_KEY)
    if (!storedToken) {
      setIsLoading(false)
      return
    }

    setAuthToken(storedToken)
    api
      .get<AuthUser>('/auth/me')
      .then((response) => setUser(response.data))
      .catch(() => {
        localStorage.removeItem(TOKEN_STORAGE_KEY)
        setAuthToken(null)
      })
      .finally(() => setIsLoading(false))
  }, [])

  function applyAuthResponse(data: AuthResponse) {
    localStorage.setItem(TOKEN_STORAGE_KEY, data.token)
    setAuthToken(data.token)
    setUser({ userId: data.userId, email: data.email, roles: data.roles })
  }

  async function login(email: string, password: string) {
    const response = await api.post<AuthResponse>('/auth/login', { email, password })
    applyAuthResponse(response.data)
  }

  async function register(email: string, password: string) {
    const response = await api.post<AuthResponse>('/auth/register', { email, password })
    applyAuthResponse(response.data)
  }

  function logout() {
    localStorage.removeItem(TOKEN_STORAGE_KEY)
    setAuthToken(null)
    setUser(null)
  }

  return (
    <AuthContext.Provider value={{ user, isLoading, login, register, logout }}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider')
  }
  return context
}
