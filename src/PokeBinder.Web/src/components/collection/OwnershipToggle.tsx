import type { OwnedVariantSummary } from '../../lib/queries/cards'
import { useSetOwnership, useUnsetOwnership } from '../../lib/queries/collection'

const CONDITIONS = ['NM', 'LP', 'MP', 'HP', 'DMG'] as const

/**
 * A single variant's ownership control, used on the card detail page - a click toggles
 * owned/unowned, and once owned it also exposes a quantity stepper and condition select. The
 * set-detail grid uses a different, selection-first flow (OwnershipVariantTile + the bulk
 * mark-owned/unowned toolbar) rather than this per-variant control.
 */
export function OwnershipToggle({ variant }: { variant: OwnedVariantSummary }) {
  const setOwnership = useSetOwnership()
  const unsetOwnership = useUnsetOwnership()
  const isPending = setOwnership.isPending || unsetOwnership.isPending

  function toggleOwned() {
    if (variant.owned) {
      unsetOwnership.mutate(variant.id)
    } else {
      setOwnership.mutate({ cardVariantId: variant.id, quantity: 1 })
    }
  }

  function setQuantity(quantity: number) {
    if (quantity < 1) return
    setOwnership.mutate({ cardVariantId: variant.id, quantity, condition: variant.condition })
  }

  function setCondition(condition: string) {
    setOwnership.mutate({ cardVariantId: variant.id, quantity: variant.quantity || 1, condition })
  }

  return (
    <div className="flex flex-col gap-1.5">
      <button
        type="button"
        onClick={toggleOwned}
        disabled={isPending}
        aria-pressed={variant.owned}
        aria-label={`${variant.owned ? 'Remove' : 'Mark'} ${variant.variantTypeName} as owned`}
        className={`rounded-md border px-2 py-1 text-[10.5px] font-semibold uppercase tracking-wide transition-colors disabled:opacity-50 ${
          variant.owned ? 'border-accent bg-accent text-accent-ink' : 'border-border bg-surface-2 text-ink-soft'
        }`}
      >
        {variant.variantTypeName}
      </button>

      {variant.owned && (
        <div className="flex items-center gap-2 text-xs text-ink-soft">
          <div className="flex items-center gap-1">
            <button
              type="button"
              onClick={() => setQuantity(variant.quantity - 1)}
              disabled={isPending || variant.quantity <= 1}
              className="flex h-6 w-6 items-center justify-center rounded border border-border disabled:opacity-30"
            >
              −
            </button>
            <span className="w-5 text-center [font-variant-numeric:tabular-nums]">{variant.quantity}</span>
            <button
              type="button"
              onClick={() => setQuantity(variant.quantity + 1)}
              disabled={isPending}
              className="flex h-6 w-6 items-center justify-center rounded border border-border disabled:opacity-30"
            >
              +
            </button>
          </div>
          <select
            value={variant.condition ?? ''}
            onChange={(e) => setCondition(e.target.value)}
            disabled={isPending}
            className="rounded border border-border bg-canvas px-1.5 py-1 text-[10.5px] text-ink"
          >
            <option value="" disabled>
              Condition
            </option>
            {CONDITIONS.map((c) => (
              <option key={c} value={c}>
                {c}
              </option>
            ))}
          </select>
        </div>
      )}
    </div>
  )
}
