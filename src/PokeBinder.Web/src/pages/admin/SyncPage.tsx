import { useMemo, useState } from 'react'
import { useApplySync, useDryRunSync, useSyncHistory, useSyncJob } from '../../lib/queries/admin'
import type { SyncDiffSummary, SyncManualConflict } from '../../lib/admin-types'

function FieldChangeTable({ summary }: { summary: SyncDiffSummary }) {
  if (summary.changedFieldCounts.length === 0) return null
  return (
    <div className="mt-3">
      <div className="text-xs font-semibold uppercase tracking-wide text-ink-faint">Changed fields</div>
      <div className="mt-1.5 flex flex-wrap gap-2">
        {summary.changedFieldCounts.map((f) => (
          <span key={f.field} className="rounded-lg border border-border bg-canvas px-2.5 py-1 text-xs text-ink-soft">
            {f.field}: <span className="font-semibold text-ink">{f.count}</span>
          </span>
        ))}
      </div>
    </div>
  )
}

function ManualConflictList({
  conflicts,
  confirmed,
  onToggle,
}: {
  conflicts: SyncManualConflict[]
  confirmed: Set<string>
  onToggle: (conflict: SyncManualConflict) => void
}) {
  if (conflicts.length === 0) return null
  return (
    <div className="mt-4 rounded-lg border border-accent/40 bg-accent/10 p-3">
      <div className="text-xs font-semibold uppercase tracking-wide text-accent">
        Manual-origin conflicts ({conflicts.length})
      </div>
      <p className="mt-1 text-xs text-ink-soft">
        These were hand-added or hand-corrected, so sync leaves them alone by default. Check any you want the
        upstream data to overwrite on apply.
      </p>
      <ul className="mt-2.5 space-y-1.5">
        {conflicts.map((c) => {
          const key = `${c.entityType}:${c.entityId}`
          return (
            <li key={key} className="flex items-start gap-2 text-sm">
              <input
                type="checkbox"
                className="mt-0.5"
                checked={confirmed.has(key)}
                onChange={() => onToggle(c)}
                aria-label={`Allow overwrite of ${c.name}`}
              />
              <div>
                <div className="text-ink">
                  {c.name} <span className="text-xs text-ink-faint">({c.entityType})</span>
                </div>
                <div className="text-xs text-ink-faint">Would change: {c.changedFields.join(', ')}</div>
              </div>
            </li>
          )
        })}
      </ul>
    </div>
  )
}

export function SyncPage() {
  const dryRun = useDryRunSync()
  const apply = useApplySync()
  const [jobId, setJobId] = useState<string | null>(null)
  const [confirmed, setConfirmed] = useState<Set<string>>(new Set())
  const [historyPage, setHistoryPage] = useState(1)

  const job = useSyncJob(jobId)
  const history = useSyncHistory(historyPage)

  const summary = dryRun.data

  function toggleConfirm(conflict: SyncManualConflict) {
    const key = `${conflict.entityType}:${conflict.entityId}`
    setConfirmed((prev) => {
      const next = new Set(prev)
      if (next.has(key)) next.delete(key)
      else next.add(key)
      return next
    })
  }

  const confirmedCardIds = useMemo(
    () =>
      summary?.manualConflicts
        .filter((c) => c.entityType === 'Card' && confirmed.has(`Card:${c.entityId}`))
        .map((c) => c.entityId) ?? [],
    [summary, confirmed],
  )
  const confirmedSetIds = useMemo(
    () =>
      summary?.manualConflicts
        .filter((c) => c.entityType === 'Set' && confirmed.has(`Set:${c.entityId}`))
        .map((c) => c.entityId) ?? [],
    [summary, confirmed],
  )

  function handleApply() {
    apply.mutate(
      { confirmedOverrideCardIds: confirmedCardIds, confirmedOverrideSetIds: confirmedSetIds },
      { onSuccess: (result) => setJobId(result.jobId) },
    )
  }

  const running = job.data?.status === 'Running'
  const progressPercent =
    job.data && job.data.totalSets > 0 ? Math.round((job.data.setsProcessed / job.data.totalSets) * 100) : 0

  return (
    <div className="space-y-6">
      <section className="rounded-2xl border border-border bg-surface p-5">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div>
            <h2 className="font-display text-lg italic text-ink">Data sync</h2>
            <p className="text-xs text-ink-soft">Pulls the latest pokemon-tcg-data and reconciles it with the catalog.</p>
          </div>
          <div className="flex gap-2">
            <button
              type="button"
              disabled={dryRun.isPending}
              onClick={() => {
                setConfirmed(new Set())
                dryRun.mutate()
              }}
              className="rounded-lg border border-border px-3 py-2 text-sm font-semibold text-ink-soft hover:text-ink disabled:opacity-50"
            >
              {dryRun.isPending ? 'Checking…' : 'Check for updates'}
            </button>
            <button
              type="button"
              disabled={!summary || apply.isPending || running}
              onClick={handleApply}
              className="rounded-lg bg-accent px-3 py-2 text-sm font-semibold text-accent-ink disabled:opacity-50"
            >
              {apply.isPending || running ? 'Applying…' : 'Apply sync'}
            </button>
          </div>
        </div>

        {summary && (
          <div className="mt-4 border-t border-border pt-4">
            <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
              <div className="rounded-lg bg-canvas p-3">
                <div className="text-xs text-ink-faint">New sets</div>
                <div className="text-lg font-semibold text-ink [font-variant-numeric:tabular-nums]">{summary.setsAdded}</div>
              </div>
              <div className="rounded-lg bg-canvas p-3">
                <div className="text-xs text-ink-faint">New cards</div>
                <div className="text-lg font-semibold text-ink [font-variant-numeric:tabular-nums]">{summary.cardsAdded}</div>
              </div>
              <div className="rounded-lg bg-canvas p-3">
                <div className="text-xs text-ink-faint">Sets updated</div>
                <div className="text-lg font-semibold text-ink [font-variant-numeric:tabular-nums]">{summary.setsUpdated}</div>
              </div>
              <div className="rounded-lg bg-canvas p-3">
                <div className="text-xs text-ink-faint">Cards updated</div>
                <div className="text-lg font-semibold text-ink [font-variant-numeric:tabular-nums]">{summary.cardsUpdated}</div>
              </div>
            </div>

            {summary.newSets.length > 0 && (
              <div className="mt-3">
                <div className="text-xs font-semibold uppercase tracking-wide text-ink-faint">New sets</div>
                <div className="mt-1.5 flex flex-wrap gap-2">
                  {summary.newSets.map((s) => (
                    <span key={s.setId} className="rounded-lg border border-border bg-canvas px-2.5 py-1 text-xs text-ink-soft">
                      {s.name}
                    </span>
                  ))}
                </div>
              </div>
            )}

            <FieldChangeTable summary={summary} />
            <ManualConflictList conflicts={summary.manualConflicts} confirmed={confirmed} onToggle={toggleConfirm} />
          </div>
        )}

        {jobId && job.data && (
          <div className="mt-4 border-t border-border pt-4">
            {running ? (
              <div>
                <div className="flex items-center justify-between text-xs text-ink-soft">
                  <span>
                    Syncing… {job.data.setsProcessed}/{job.data.totalSets || '?'} sets, {job.data.cardsProcessed} cards processed
                  </span>
                  <span>{progressPercent}%</span>
                </div>
                <div className="mt-1.5 h-1.5 overflow-hidden rounded-full bg-border">
                  <div className="h-full rounded-full bg-accent transition-all" style={{ width: `${progressPercent}%` }} />
                </div>
              </div>
            ) : job.data.status === 'Completed' ? (
              <div className="rounded-lg bg-good/10 p-3 text-sm text-ink">
                Sync complete: {job.data.setsAdded} sets added, {job.data.setsUpdated} updated, {job.data.cardsAdded} cards
                added, {job.data.cardsUpdated} updated.
                {job.data.remainingManualConflicts.length > 0 && (
                  <div className="mt-1 text-xs text-ink-soft">
                    {job.data.remainingManualConflicts.length} manual-origin row(s) were left untouched.
                  </div>
                )}
              </div>
            ) : (
              <div className="rounded-lg bg-bad/10 p-3 text-sm text-bad">Sync failed: {job.data.errorMessage}</div>
            )}
          </div>
        )}
      </section>

      <section className="rounded-2xl border border-border bg-surface p-5">
        <h2 className="font-display text-lg italic text-ink">Sync history</h2>
        <div className="mt-3 overflow-x-auto">
          <table className="w-full text-left text-sm">
            <thead>
              <tr className="border-b border-border text-xs uppercase tracking-wide text-ink-faint">
                <th className="py-2 pr-3 font-semibold">When</th>
                <th className="py-2 pr-3 font-semibold">Who</th>
                <th className="py-2 pr-3 font-semibold">Status</th>
                <th className="py-2 pr-3 font-semibold">Sets</th>
                <th className="py-2 pr-3 font-semibold">Cards</th>
              </tr>
            </thead>
            <tbody>
              {history.data?.items.map((run) => (
                <tr key={run.id} className="border-b border-border/60">
                  <td className="py-2 pr-3 text-ink-soft">{new Date(run.startedAt).toLocaleString()}</td>
                  <td className="py-2 pr-3 text-ink-soft">{run.runByEmail}</td>
                  <td className="py-2 pr-3">
                    <span
                      className={
                        run.status === 'Completed' ? 'text-good' : run.status === 'Failed' ? 'text-bad' : 'text-ink-soft'
                      }
                    >
                      {run.status}
                    </span>
                  </td>
                  <td className="py-2 pr-3 text-ink-soft [font-variant-numeric:tabular-nums]">
                    +{run.setsAdded} / ~{run.setsUpdated}
                  </td>
                  <td className="py-2 pr-3 text-ink-soft [font-variant-numeric:tabular-nums]">
                    +{run.cardsAdded} / ~{run.cardsUpdated}
                  </td>
                </tr>
              ))}
              {history.data?.items.length === 0 && (
                <tr>
                  <td colSpan={5} className="py-4 text-center text-xs text-ink-faint">
                    No completed sync runs yet.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
        {history.data && history.data.totalCount > history.data.pageSize && (
          <div className="mt-3 flex items-center justify-end gap-2 text-xs">
            <button
              type="button"
              disabled={historyPage <= 1}
              onClick={() => setHistoryPage((p) => p - 1)}
              className="rounded border border-border px-2 py-1 disabled:opacity-30"
            >
              Prev
            </button>
            <button
              type="button"
              disabled={historyPage * history.data.pageSize >= history.data.totalCount}
              onClick={() => setHistoryPage((p) => p + 1)}
              className="rounded border border-border px-2 py-1 disabled:opacity-30"
            >
              Next
            </button>
          </div>
        )}
      </section>
    </div>
  )
}
