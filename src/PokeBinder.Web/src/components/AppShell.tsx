import { useEffect, useState } from 'react'
import { NavLink, Outlet } from 'react-router-dom'
import { useAuth } from '../lib/auth-context'

const navLinkClasses = ({ isActive }: { isActive: boolean }) =>
  `block rounded-lg px-3 py-2 text-sm font-semibold transition-colors ${
    isActive ? 'bg-accent text-accent-ink' : 'text-ink-soft hover:bg-surface-2 hover:text-ink'
  }`

function NavLinks({ isAdmin, onNavigate }: { isAdmin: boolean; onNavigate?: () => void }) {
  return (
    <nav className="flex flex-col gap-1">
      <NavLink to="/" end className={navLinkClasses} onClick={onNavigate}>
        Home
      </NavLink>
      <NavLink to="/binders" className={navLinkClasses} onClick={onNavigate}>
        Binders
      </NavLink>
      {isAdmin && (
        <NavLink to="/admin" className={navLinkClasses} onClick={onNavigate}>
          Admin
        </NavLink>
      )}
    </nav>
  )
}

function UserFooter({ email, onLogout }: { email: string | undefined; onLogout: () => void }) {
  return (
    <div className="space-y-2">
      <div className="truncate text-xs text-ink-faint">{email}</div>
      <button
        onClick={onLogout}
        className="w-full rounded-lg border border-border py-1.5 text-xs font-semibold text-ink-soft transition-colors hover:border-accent hover:text-ink"
      >
        Log out
      </button>
    </div>
  )
}

export function AppShell() {
  const { user, logout } = useAuth()
  const isAdmin = user?.roles.includes('Admin') ?? false
  const [drawerOpen, setDrawerOpen] = useState(false)

  useEffect(() => {
    if (!drawerOpen) return
    const onKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') setDrawerOpen(false)
    }
    document.addEventListener('keydown', onKeyDown)
    return () => document.removeEventListener('keydown', onKeyDown)
  }, [drawerOpen])

  return (
    <div className="min-h-screen bg-canvas">
      {/* Mobile top bar */}
      <div className="flex items-center justify-between border-b border-border bg-surface-2 px-4 py-3 md:hidden">
        <div className="font-display text-lg italic text-ink">PokéBinder</div>
        <button
          aria-label="Open navigation"
          aria-expanded={drawerOpen}
          onClick={() => setDrawerOpen(true)}
          className="flex h-9 w-9 items-center justify-center rounded-lg border border-border"
        >
          <span className="sr-only">Menu</span>
          <div className="space-y-1">
            <span className="block h-0.5 w-4 rounded bg-ink-soft" />
            <span className="block h-0.5 w-4 rounded bg-ink-soft" />
            <span className="block h-0.5 w-4 rounded bg-ink-soft" />
          </div>
        </button>
      </div>

      {/* Mobile drawer */}
      {drawerOpen && (
        <div className="fixed inset-0 z-50 md:hidden">
          <button
            aria-label="Close navigation"
            className="absolute inset-0 bg-black/60"
            onClick={() => setDrawerOpen(false)}
          />
          <div className="absolute inset-y-0 left-0 flex w-64 flex-col justify-between border-r border-border bg-surface-2 p-4 shadow-2xl">
            <div>
              <div className="mb-6 flex items-center justify-between px-1">
                <div className="font-display text-lg italic text-ink">PokéBinder</div>
                <button
                  aria-label="Close navigation"
                  onClick={() => setDrawerOpen(false)}
                  className="flex h-8 w-8 items-center justify-center rounded-lg border border-border text-ink-soft"
                >
                  ×
                </button>
              </div>
              <NavLinks isAdmin={isAdmin} onNavigate={() => setDrawerOpen(false)} />
            </div>
            <UserFooter email={user?.email} onLogout={logout} />
          </div>
        </div>
      )}

      <div className="mx-auto flex max-w-[1400px]">
        {/* Desktop sidebar */}
        <aside className="sticky top-0 hidden h-screen w-56 shrink-0 flex-col justify-between border-r border-border bg-surface-2 p-5 md:flex">
          <div>
            <div className="mb-8 px-1 font-display text-xl italic text-ink">PokéBinder</div>
            <NavLinks isAdmin={isAdmin} />
          </div>
          <UserFooter email={user?.email} onLogout={logout} />
        </aside>

        <main className="min-w-0 flex-1 px-4 py-6 md:px-8 md:py-8">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
