import { CardSearchPanel } from '../components/search/CardSearchPanel'

export function CardsPage() {
  return (
    <div>
      <h1 className="font-display text-2xl font-semibold italic text-ink">Cards</h1>
      <p className="mt-1 text-sm text-ink-soft">Search the full card database and insert results straight into a binder.</p>
      <div className="mt-4 h-[calc(100vh-220px)] min-h-[480px] overflow-hidden rounded-xl border border-border bg-surface">
        <CardSearchPanel />
      </div>
    </div>
  )
}
