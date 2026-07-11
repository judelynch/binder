import { useParams } from 'react-router-dom'

export function BinderDetailPage() {
  const { id } = useParams<{ id: string }>()

  return (
    <div>
      <h1 className="font-display text-2xl font-semibold italic text-ink">Binder view</h1>
      <p className="mt-2 text-sm text-ink-soft">
        The full spread view for binder <span className="text-ink">{id}</span> lands in a later phase.
      </p>
    </div>
  )
}
