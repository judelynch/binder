import { useEffect, type ReactNode } from 'react'

const SIZE_CLASSES = {
  md: 'md:max-w-md',
  lg: 'md:max-w-2xl',
} as const

export function Modal({
  title,
  onClose,
  children,
  size = 'md',
}: {
  title: string
  onClose: () => void
  children: ReactNode
  size?: keyof typeof SIZE_CLASSES
}) {
  useEffect(() => {
    const onKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose()
    }
    document.addEventListener('keydown', onKeyDown)
    return () => document.removeEventListener('keydown', onKeyDown)
  }, [onClose])

  return (
    <div className="fixed inset-0 z-50 flex items-end justify-center md:items-center">
      <button aria-label="Close dialog" className="absolute inset-0 bg-black/60" onClick={onClose} />
      <div
        role="dialog"
        aria-modal="true"
        aria-label={title}
        className={`relative flex max-h-[92vh] w-full flex-col overflow-y-auto rounded-t-2xl border-t border-border bg-surface p-5 shadow-2xl md:max-h-[85vh] md:w-full md:rounded-2xl md:border ${SIZE_CLASSES[size]}`}
      >
        <div className="mb-4 flex items-center justify-between">
          <h2 className="font-display text-lg font-semibold text-ink">{title}</h2>
          <button
            type="button"
            aria-label="Close"
            onClick={onClose}
            className="flex h-8 w-8 items-center justify-center rounded-lg border border-border text-ink-soft"
          >
            ×
          </button>
        </div>
        {children}
      </div>
    </div>
  )
}
