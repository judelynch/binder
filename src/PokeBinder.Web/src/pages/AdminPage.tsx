import { NavLink, Outlet } from 'react-router-dom'

const tabClasses = ({ isActive }: { isActive: boolean }) =>
  `rounded-lg px-3 py-2 text-sm font-semibold transition-colors ${
    isActive ? 'bg-accent text-accent-ink' : 'text-ink-soft hover:bg-surface-2 hover:text-ink'
  }`

export function AdminPage() {
  return (
    <div>
      <h1 className="font-display text-2xl italic text-ink">Admin</h1>
      <nav className="mt-4 flex gap-2 border-b border-border pb-3">
        <NavLink to="/admin/sync" className={tabClasses}>
          Data sync
        </NavLink>
        <NavLink to="/admin/cards" className={tabClasses}>
          Sets &amp; cards
        </NavLink>
        <NavLink to="/admin/variants" className={tabClasses}>
          Variants
        </NavLink>
        <NavLink to="/admin/pricing/queue" className={tabClasses}>
          Pricing queue
        </NavLink>
        <NavLink to="/admin/pricing/runs" className={tabClasses}>
          Pricing runs
        </NavLink>
      </nav>
      <div className="mt-5">
        <Outlet />
      </div>
    </div>
  )
}
