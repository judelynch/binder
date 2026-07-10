import { useAuth } from '../lib/auth-context'

export function HomePage() {
  const { user } = useAuth()

  return (
    <div>
      <h1 className="text-2xl font-semibold text-slate-900">Welcome back{user ? `, ${user.email}` : ''}</h1>
      <p className="mt-2 text-slate-600">This is the authenticated shell. Binders and cards land in later phases.</p>
    </div>
  )
}
