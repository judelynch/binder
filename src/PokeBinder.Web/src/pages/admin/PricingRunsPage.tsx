import { useState } from 'react'
import { usePricingRunHistory, useRunPricingNow } from '../../lib/queries/pricing-admin'

const TRIGGER_LABEL: Record<string, string> = {
  Nightly: 'Nightly',
  LoginCatchUp: 'Login catch-up',
  Manual: 'Manual',
}

export function PricingRunsPage() {
  const [page, setPage] = useState(1)
  const history = usePricingRunHistory(page)
  const runNow = useRunPricingNow()

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-3 rounded-2xl border border-border bg-surface p-5">
        <div>
          <h2 className="font-display text-lg italic text-ink">Run history</h2>
          <p className="text-xs text-ink-soft">Nightly, login catch-up, and manual pricing scrape runs.</p>
        </div>
        <button
          type="button"
          disabled={runNow.isPending}
          onClick={() => runNow.mutate()}
          className="rounded-lg bg-accent px-3 py-2 text-sm font-semibold text-accent-ink disabled:opacity-50"
        >
          {runNow.isPending ? 'Enqueuing…' : 'Run now'}
        </button>
      </div>

      <section className="rounded-2xl border border-border bg-surface p-5">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-sm">
            <thead>
              <tr className="border-b border-border text-xs uppercase tracking-wide text-ink-faint">
                <th className="py-2 pr-3 font-semibold">Started</th>
                <th className="py-2 pr-3 font-semibold">Trigger</th>
                <th className="py-2 pr-3 font-semibold">Status</th>
                <th className="py-2 pr-3 font-semibold">Cards</th>
                <th className="py-2 pr-3 font-semibold">Listings</th>
              </tr>
            </thead>
            <tbody>
              {history.data?.items.map((run) => (
                <tr key={run.id} className="border-b border-border/60">
                  <td className="py-2 pr-3 text-ink-soft">{new Date(run.startedAt).toLocaleString()}</td>
                  <td className="py-2 pr-3 text-ink-soft">{TRIGGER_LABEL[run.triggeredBy] ?? run.triggeredBy}</td>
                  <td className="py-2 pr-3">
                    <span
                      className={
                        run.status === 'Completed' ? 'text-good' : run.status === 'Failed' ? 'text-bad' : 'text-ink-soft'
                      }
                    >
                      {run.status}
                    </span>
                    {run.status === 'Failed' && run.errorMessage && (
                      <div className="text-xs text-ink-faint">{run.errorMessage}</div>
                    )}
                  </td>
                  <td className="py-2 pr-3 text-ink-soft [font-variant-numeric:tabular-nums]">{run.cardsProcessed}</td>
                  <td className="py-2 pr-3 text-ink-soft [font-variant-numeric:tabular-nums]">
                    {run.listingsAccepted} accepted / {run.listingsQuarantined} quarantined / {run.listingsRejected} rejected
                    <span className="text-ink-faint"> (of {run.listingsFound} found)</span>
                  </td>
                </tr>
              ))}
              {history.data?.items.length === 0 && (
                <tr>
                  <td colSpan={5} className="py-4 text-center text-xs text-ink-faint">
                    No pricing runs yet.
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
              disabled={page <= 1}
              onClick={() => setPage((p) => p - 1)}
              className="rounded border border-border px-2 py-1 disabled:opacity-30"
            >
              Prev
            </button>
            <button
              type="button"
              disabled={page * history.data.pageSize >= history.data.totalCount}
              onClick={() => setPage((p) => p + 1)}
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
