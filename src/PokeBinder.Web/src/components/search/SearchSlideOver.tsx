import { useEffect } from 'react'
import { CardSearchPanel } from './CardSearchPanel'

export function SearchSlideOver({
  binderId,
  startSlotId,
  onClose,
}: {
  binderId: string
  startSlotId?: string
  onClose: () => void
}) {
  useEffect(() => {
    const onKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose()
    }
    document.addEventListener('keydown', onKeyDown)
    return () => document.removeEventListener('keydown', onKeyDown)
  }, [onClose])

  return (
    <div className="fixed inset-0 z-50">
      <button aria-label="Close search" className="absolute inset-0 bg-black/60" onClick={onClose} />
      <div className="absolute inset-y-0 right-0 flex w-full flex-col border-l border-border bg-surface shadow-2xl sm:w-[90vw] lg:w-[75vw] xl:w-[65vw]">
        <div className="flex items-center justify-between border-b border-border px-4 py-3">
          <h2 className="font-display text-base font-semibold text-ink">Add cards</h2>
          <button
            type="button"
            aria-label="Close"
            onClick={onClose}
            className="flex h-8 w-8 items-center justify-center rounded-lg border border-border text-ink-soft"
          >
            ×
          </button>
        </div>
        <div className="min-h-0 flex-1">
          <CardSearchPanel defaultBinderId={binderId} defaultStartSlotId={startSlotId} />
        </div>
      </div>
    </div>
  )
}
