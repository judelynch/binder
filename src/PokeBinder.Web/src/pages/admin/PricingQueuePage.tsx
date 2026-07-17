import { useState } from 'react'
import type { QueuedListing, ReclassifyPayload } from '../../lib/admin-types'
import {
  useApproveListing,
  useBulkClassificationAction,
  usePricingQueue,
  useReclassifyListing,
  useRejectListing,
  useRunPricingNow,
} from '../../lib/queries/pricing-admin'

const GRADED_STATUSES: ReclassifyPayload['gradedStatus'][] = ['Raw', 'Graded']
const RAW_CONDITIONS: ReclassifyPayload['rawCondition'][] = ['Unspecified', 'NM', 'LP', 'MP', 'HP', 'DMG']

function ConfidenceBadge({ score }: { score: number }) {
  const tone = score >= 55 ? 'text-good border-good/40 bg-good/10' : 'text-accent border-accent/40 bg-accent/10'
  return <span className={`rounded-full border px-2 py-0.5 text-xs font-semibold ${tone}`}>{score}</span>
}

function ReclassifyForm({
  listing,
  onCancel,
  onSubmit,
  isPending,
}: {
  listing: QueuedListing
  onCancel: () => void
  onSubmit: (payload: ReclassifyPayload) => void
  isPending: boolean
}) {
  const [gradedStatus, setGradedStatus] = useState<ReclassifyPayload['gradedStatus']>(listing.gradedStatus)
  const [grader, setGrader] = useState(listing.grader ?? '')
  const [grade, setGrade] = useState(listing.grade?.toString() ?? '')
  const [rawCondition, setRawCondition] = useState<ReclassifyPayload['rawCondition']>(listing.rawCondition)
  const [reason, setReason] = useState('')

  return (
    <div className="mt-3 rounded-lg border border-border bg-canvas p-3">
      <div className="grid grid-cols-2 gap-2 sm:grid-cols-4">
        <label className="text-xs text-ink-soft">
          Status
          <select
            value={gradedStatus}
            onChange={(e) => setGradedStatus(e.target.value as ReclassifyPayload['gradedStatus'])}
            className="mt-1 w-full rounded border border-border bg-surface px-2 py-1 text-sm text-ink"
          >
            {GRADED_STATUSES.map((s) => (
              <option key={s} value={s}>
                {s}
              </option>
            ))}
          </select>
        </label>
        {gradedStatus === 'Graded' ? (
          <>
            <label className="text-xs text-ink-soft">
              Grader
              <input
                value={grader}
                onChange={(e) => setGrader(e.target.value)}
                placeholder="PSA"
                className="mt-1 w-full rounded border border-border bg-surface px-2 py-1 text-sm text-ink"
              />
            </label>
            <label className="text-xs text-ink-soft">
              Grade
              <input
                value={grade}
                onChange={(e) => setGrade(e.target.value)}
                placeholder="10"
                inputMode="decimal"
                className="mt-1 w-full rounded border border-border bg-surface px-2 py-1 text-sm text-ink"
              />
            </label>
          </>
        ) : (
          <label className="text-xs text-ink-soft">
            Condition
            <select
              value={rawCondition}
              onChange={(e) => setRawCondition(e.target.value as ReclassifyPayload['rawCondition'])}
              className="mt-1 w-full rounded border border-border bg-surface px-2 py-1 text-sm text-ink"
            >
              {RAW_CONDITIONS.map((c) => (
                <option key={c} value={c}>
                  {c}
                </option>
              ))}
            </select>
          </label>
        )}
        <label className="text-xs text-ink-soft sm:col-span-1">
          Reason
          <input
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            placeholder="Optional note"
            className="mt-1 w-full rounded border border-border bg-surface px-2 py-1 text-sm text-ink"
          />
        </label>
      </div>
      <div className="mt-3 flex justify-end gap-2">
        <button type="button" onClick={onCancel} className="rounded-lg border border-border px-3 py-1.5 text-xs font-semibold text-ink-soft">
          Cancel
        </button>
        <button
          type="button"
          disabled={isPending || (gradedStatus === 'Graded' && (!grader.trim() || !grade.trim()))}
          onClick={() =>
            onSubmit({
              gradedStatus,
              grader: gradedStatus === 'Graded' ? grader.trim() : null,
              grade: gradedStatus === 'Graded' ? Number(grade) : null,
              rawCondition: gradedStatus === 'Graded' ? 'Unspecified' : rawCondition,
              reason: reason.trim() || undefined,
            })
          }
          className="rounded-lg bg-accent px-3 py-1.5 text-xs font-semibold text-accent-ink disabled:opacity-50"
        >
          {isPending ? 'Saving…' : 'Save & accept'}
        </button>
      </div>
    </div>
  )
}

export function PricingQueuePage() {
  const [page, setPage] = useState(1)
  const [selected, setSelected] = useState<Set<string>>(new Set())
  const [reclassifying, setReclassifying] = useState<string | null>(null)

  const queue = usePricingQueue(page)
  const approve = useApproveListing()
  const reclassify = useReclassifyListing()
  const reject = useRejectListing()
  const bulk = useBulkClassificationAction()
  const runNow = useRunPricingNow()

  const items = queue.data?.items ?? []

  function toggle(id: string) {
    setSelected((prev) => {
      const next = new Set(prev)
      if (next.has(id)) next.delete(id)
      else next.add(id)
      return next
    })
  }

  const isBusy = approve.isPending || reclassify.isPending || reject.isPending || bulk.isPending

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-3 rounded-2xl border border-border bg-surface p-5">
        <div>
          <h2 className="font-display text-lg italic text-ink">Review queue</h2>
          <p className="text-xs text-ink-soft">
            Listings the classifier couldn't confidently place — approve, correct, or reject each before it counts toward a price.
          </p>
        </div>
        <button
          type="button"
          disabled={runNow.isPending}
          onClick={() => runNow.mutate()}
          className="rounded-lg border border-border px-3 py-2 text-sm font-semibold text-ink-soft hover:text-ink disabled:opacity-50"
        >
          {runNow.isPending ? 'Enqueuing…' : 'Run now'}
        </button>
      </div>

      <div className="space-y-3">
        {items.length === 0 && (
          <div className="rounded-2xl border border-border bg-surface p-8 text-center text-sm text-ink-faint">
            Nothing waiting on review.
          </div>
        )}
        {items.map((listing) => (
          <div key={listing.classificationId} className="rounded-2xl border border-border bg-surface p-4">
            <div className="flex items-start gap-3">
              <input
                type="checkbox"
                className="mt-1"
                checked={selected.has(listing.classificationId)}
                onChange={() => toggle(listing.classificationId)}
                aria-label={`Select ${listing.title}`}
              />
              <div className="h-16 w-16 shrink-0 overflow-hidden rounded-lg bg-canvas">
                {listing.thumbnailUrl && (
                  <img src={listing.thumbnailUrl} alt="" loading="lazy" className="h-full w-full object-cover" />
                )}
              </div>
              <div className="min-w-0 flex-1">
                <div className="flex flex-wrap items-center gap-2">
                  <span className="truncate text-sm font-semibold text-ink">{listing.title}</span>
                  <ConfidenceBadge score={listing.confidenceScore} />
                </div>
                <div className="mt-0.5 text-xs text-ink-soft">
                  {listing.cardName} · {listing.setNumber} · {listing.variantTypeName} · £
                  {listing.itemPriceGbp.toFixed(2)}
                  {listing.postagePriceGbp != null && ` + £${listing.postagePriceGbp.toFixed(2)} postage`}
                </div>
                <div className="mt-1 flex flex-wrap gap-1.5 text-xs text-ink-faint">
                  <span className="rounded border border-border px-1.5 py-0.5">
                    {listing.gradedStatus === 'Graded' ? `${listing.grader ?? '?'} ${listing.grade ?? '?'}` : `Raw · ${listing.rawCondition}`}
                  </span>
                  <span className="rounded border border-border px-1.5 py-0.5">{listing.variantMatch}</span>
                  {listing.bestOfferAccepted && <span className="rounded border border-border px-1.5 py-0.5">Best offer</span>}
                  {listing.language !== 'English' && <span className="rounded border border-border px-1.5 py-0.5">{listing.language}</span>}
                </div>
              </div>
              <div className="flex shrink-0 flex-col gap-1.5">
                <button
                  type="button"
                  disabled={isBusy}
                  onClick={() => approve.mutate(listing.classificationId)}
                  className="rounded-lg border border-good/50 px-3 py-1.5 text-xs font-semibold text-good disabled:opacity-40"
                >
                  Approve
                </button>
                <button
                  type="button"
                  disabled={isBusy}
                  onClick={() => setReclassifying((prev) => (prev === listing.classificationId ? null : listing.classificationId))}
                  className="rounded-lg border border-border px-3 py-1.5 text-xs font-semibold text-ink-soft hover:text-ink disabled:opacity-40"
                >
                  Reclassify
                </button>
                <button
                  type="button"
                  disabled={isBusy}
                  onClick={() => reject.mutate({ classificationId: listing.classificationId })}
                  className="rounded-lg border border-bad/50 px-3 py-1.5 text-xs font-semibold text-bad disabled:opacity-40"
                >
                  Reject
                </button>
              </div>
            </div>
            {reclassifying === listing.classificationId && (
              <ReclassifyForm
                listing={listing}
                isPending={reclassify.isPending}
                onCancel={() => setReclassifying(null)}
                onSubmit={(payload) =>
                  reclassify.mutate(
                    { classificationId: listing.classificationId, payload },
                    { onSuccess: () => setReclassifying(null) },
                  )
                }
              />
            )}
          </div>
        ))}
      </div>

      {queue.data && queue.data.totalCount > queue.data.pageSize && (
        <div className="flex items-center justify-end gap-2 text-xs">
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
            disabled={page * queue.data.pageSize >= queue.data.totalCount}
            onClick={() => setPage((p) => p + 1)}
            className="rounded border border-border px-2 py-1 disabled:opacity-30"
          >
            Next
          </button>
        </div>
      )}

      {selected.size > 0 && (
        <div className="sticky bottom-0 z-10 flex flex-wrap items-center justify-between gap-2 rounded-xl border border-border bg-surface px-4 py-3 shadow-lg">
          <span className="text-sm font-semibold text-ink">{selected.size} selected</span>
          <div className="flex items-center gap-2">
            <button
              type="button"
              onClick={() => setSelected(new Set())}
              className="text-xs font-semibold text-ink-soft"
            >
              Clear
            </button>
            <button
              type="button"
              disabled={isBusy}
              onClick={() =>
                bulk.mutate(
                  { classificationIds: Array.from(selected), action: 'Approve' },
                  { onSuccess: () => setSelected(new Set()) },
                )
              }
              className="rounded-lg border border-good/50 px-3 py-1.5 text-xs font-semibold text-good disabled:opacity-40"
            >
              Approve all
            </button>
            <button
              type="button"
              disabled={isBusy}
              onClick={() =>
                bulk.mutate(
                  { classificationIds: Array.from(selected), action: 'Reject' },
                  { onSuccess: () => setSelected(new Set()) },
                )
              }
              className="rounded-lg border border-bad/50 px-3 py-1.5 text-xs font-semibold text-bad disabled:opacity-40"
            >
              Reject all
            </button>
          </div>
        </div>
      )}
    </div>
  )
}
