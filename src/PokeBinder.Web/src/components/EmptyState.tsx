export function EmptyState({
  title,
  message,
  actionLabel,
  onAction,
}: {
  title: string
  message: string
  actionLabel?: string
  onAction?: () => void
}) {
  return (
    <div className="flex flex-col items-center justify-center rounded-xl border border-dashed border-border px-6 py-14 text-center">
      <div className="font-display text-lg font-semibold text-ink">{title}</div>
      <p className="mt-1.5 max-w-sm text-sm text-ink-soft">{message}</p>
      {actionLabel && onAction && (
        <button
          type="button"
          onClick={onAction}
          className="mt-5 rounded-lg bg-accent px-4 py-2 text-sm font-semibold text-accent-ink hover:opacity-90"
        >
          {actionLabel}
        </button>
      )}
    </div>
  )
}
