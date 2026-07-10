import { NavLink, Outlet } from 'react-router-dom'
import { useAuth } from '../lib/auth-context'

const linkClasses = ({ isActive }: { isActive: boolean }) =>
  `block rounded-md px-3 py-2 text-sm font-medium ${
    isActive ? 'bg-slate-900 text-white' : 'text-slate-600 hover:bg-slate-100'
  }`

export function AppShell() {
  const { user, logout } = useAuth()
  const isAdmin = user?.roles.includes('Admin')

  return (
    <div className="flex min-h-screen">
      <aside className="flex w-56 flex-col justify-between border-r border-slate-200 p-4">
        <div>
          <div className="mb-6 px-3 text-lg font-semibold text-slate-900">PokéBinder</div>
          <nav className="space-y-1">
            <NavLink to="/" end className={linkClasses}>
              Home
            </NavLink>
            <NavLink to="/binders" className={linkClasses}>
              Binders
            </NavLink>
            {isAdmin && (
              <NavLink to="/admin" className={linkClasses}>
                Admin
              </NavLink>
            )}
          </nav>
        </div>
        <div className="space-y-2 px-3">
          <div className="truncate text-xs text-slate-500">{user?.email}</div>
          <button
            onClick={logout}
            className="w-full rounded-md border border-slate-300 px-3 py-1.5 text-sm text-slate-700 hover:bg-slate-100"
          >
            Log out
          </button>
        </div>
      </aside>
      <main className="flex-1 p-6">
        <Outlet />
      </main>
    </div>
  )
}
