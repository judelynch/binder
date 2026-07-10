import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from '../lib/auth-context'

export function RequireAdmin() {
  const { user } = useAuth()

  if (!user?.roles.includes('Admin')) {
    return <Navigate to="/" replace />
  }

  return <Outlet />
}
