export function StatCard({
  label,
  value,
  sub,
  accent = false,
}: {
  label: string
  value: string
  sub?: string
  accent?: boolean
}) {
  return (
    <div
      className={`rounded-xl border p-4 ${
        accent ? 'border-accent bg-accent' : 'border-border bg-surface'
      }`}
    >
      <div
        className={`text-[11px] font-semibold uppercase tracking-wide ${
          accent ? 'text-accent-ink/70' : 'text-ink-faint'
        }`}
      >
        {label}
      </div>
      <div
        className={`mt-1.5 font-display text-2xl font-semibold [font-variant-numeric:tabular-nums] ${
          accent ? 'text-accent-ink' : 'text-ink'
        }`}
      >
        {value}
      </div>
      {sub && (
        <div className={`mt-0.5 text-xs ${accent ? 'text-accent-ink/70' : 'text-ink-soft'}`}>{sub}</div>
      )}
    </div>
  )
}
