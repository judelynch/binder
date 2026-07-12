import { useState } from 'react'
import { Modal } from '../Modal'
import { api } from '../../lib/api'
import { useBinders } from '../../lib/queries/binders'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import type { BulkAssignResult } from '../../lib/binder-types'

export function InsertIntoBinderPanel({
  cardVariantIds,
  defaultBinderId,
  defaultStartSlotId,
  onClose,
  onInserted,
}: {
  cardVariantIds: string[]
  defaultBinderId?: string
  defaultStartSlotId?: string
  onClose: () => void
  onInserted: () => void
}) {
  const { data: binders } = useBinders()
  const queryClient = useQueryClient()
  const [binderId, setBinderId] = useState(defaultBinderId ?? '')
  const [strategy, setStrategy] = useState<'skip' | 'overwrite'>('skip')
  const [result, setResult] = useState<BulkAssignResult | null>(null)

  const insert = useMutation({
    mutationFn: async () => {
      let startSlotId = defaultStartSlotId
      if (!startSlotId) {
        const spread = (await api.get(`/binders/${binderId}/spread/0`)).data
        startSlotId = spread.rightPanel?.slots?.[0]?.slotId
        if (!startSlotId) throw new Error('This binder has no pages yet — add pages before inserting.')
      }
      return (
        await api.post<BulkAssignResult>(`/binders/${binderId}/slots/bulk-assign`, {
          cardVariantIds,
          startSlotId,
          occupiedStrategy: strategy,
        })
      ).data
    },
    onSuccess: (data) => {
      setResult(data)
      queryClient.invalidateQueries({ queryKey: ['spread', binderId] })
      queryClient.invalidateQueries({ queryKey: ['binders'] })
      queryClient.invalidateQueries({ queryKey: ['dashboard'] })
      onInserted()
    },
  })

  return (
    <Modal title="Insert into binder" onClose={onClose}>
      {result ? (
        <div className="space-y-3 text-sm text-ink">
          <p>
            Placed <span className="font-semibold">{result.placed}</span> card{result.placed === 1 ? '' : 's'}
            {result.pagesAdded > 0 && (
              <>
                {' '}
                and added <span className="font-semibold">{result.pagesAdded}</span> page{result.pagesAdded === 1 ? '' : 's'}
              </>
            )}
            .
          </p>
          {result.skipped > 0 && <p className="text-ink-soft">Skipped past {result.skipped} occupied slot(s) along the way.</p>}
          <button type="button" onClick={onClose} className="w-full rounded-lg bg-accent py-2 text-sm font-semibold text-accent-ink">
            Done
          </button>
        </div>
      ) : (
        <div className="space-y-4">
          <p className="text-sm text-ink-soft">
            Inserting <span className="font-semibold text-ink">{cardVariantIds.length}</span> card
            {cardVariantIds.length === 1 ? '' : 's'} in the current sort order.
          </p>

          <div>
            <label htmlFor="insert-binder" className="mb-1.5 block text-xs font-semibold text-ink-soft">
              Target binder
            </label>
            <select
              id="insert-binder"
              value={binderId}
              onChange={(e) => setBinderId(e.target.value)}
              className="w-full rounded-lg border border-border bg-canvas px-3 py-2 text-sm text-ink"
            >
              <option value="" disabled>
                Choose a binder…
              </option>
              {binders?.map((b) => (
                <option key={b.id} value={b.id}>
                  {b.name}
                </option>
              ))}
            </select>
            {!defaultStartSlotId && (
              <p className="mt-1 text-xs text-ink-faint">Starts from the first slot of page 1.</p>
            )}
          </div>

          <div>
            <span className="mb-1.5 block text-xs font-semibold text-ink-soft">If a slot is already filled</span>
            <div className="flex gap-2">
              <button
                type="button"
                onClick={() => setStrategy('skip')}
                aria-pressed={strategy === 'skip'}
                className={`flex-1 rounded-lg border px-3 py-2 text-xs font-semibold ${strategy === 'skip' ? 'border-accent text-accent' : 'border-border text-ink-soft'}`}
              >
                Skip to next empty slot
              </button>
              <button
                type="button"
                onClick={() => setStrategy('overwrite')}
                aria-pressed={strategy === 'overwrite'}
                className={`flex-1 rounded-lg border px-3 py-2 text-xs font-semibold ${strategy === 'overwrite' ? 'border-accent text-accent' : 'border-border text-ink-soft'}`}
              >
                Overwrite
              </button>
            </div>
          </div>

          {insert.isError && <p className="text-xs text-bad">{(insert.error as Error).message}</p>}

          <button
            type="button"
            disabled={!binderId || insert.isPending}
            onClick={() => insert.mutate()}
            className="w-full rounded-lg bg-accent py-2 text-sm font-semibold text-accent-ink disabled:opacity-50"
          >
            {insert.isPending ? 'Inserting…' : 'Insert'}
          </button>
        </div>
      )}
    </Modal>
  )
}
