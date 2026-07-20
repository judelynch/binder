import { useVirtualizer } from '@tanstack/react-virtual'
import { useMemo, useRef } from 'react'
import type { CardSummary } from '../../lib/queries/cards'
import { useColumnCount } from '../../lib/useColumnCount'
import { OwnershipVariantTile } from './OwnershipVariantTile'

const ROW_HEIGHT = 210

/** One tile per (card, variant) pair - a card with three variants renders as three tiles, same as search/ResultsGrid.tsx. */
interface DisplayItem {
  card: CardSummary
  variantId: string
}

export function SetCardsGrid({
  cards,
  selectedIds,
  onToggleSelect,
  onOpenCard,
  priceByVariantId,
}: {
  cards: CardSummary[]
  selectedIds: ReadonlySet<string>
  onToggleSelect: (variantId: string) => void
  onOpenCard?: (cardId: string) => void
  priceByVariantId?: Map<string, number>
}) {
  const parentRef = useRef<HTMLDivElement>(null)
  const columns = useColumnCount()

  const items = useMemo<DisplayItem[]>(
    () => cards.flatMap((card) => card.variants.map((v) => ({ card, variantId: v.id }))),
    [cards],
  )
  const rowCount = Math.ceil(items.length / columns)

  // Dynamic row measurement, not a static estimate - see ResultsGrid.tsx's comment for why a
  // fixed guess breaks at different column counts (narrower columns make aspect-ratio tiles taller).
  const virtualizer = useVirtualizer({
    count: rowCount,
    getScrollElement: () => parentRef.current,
    estimateSize: () => ROW_HEIGHT,
    overscan: 4,
  })

  return (
    <div ref={parentRef} className="h-full overflow-y-auto">
      <div style={{ height: virtualizer.getTotalSize(), position: 'relative', width: '100%' }}>
        {virtualizer.getVirtualItems().map((virtualRow) => {
          const rowStart = virtualRow.index * columns
          const rowItems = items.slice(rowStart, rowStart + columns)
          return (
            <div
              key={virtualRow.key}
              data-index={virtualRow.index}
              ref={virtualizer.measureElement}
              style={{
                position: 'absolute',
                top: 0,
                left: 0,
                width: '100%',
                transform: `translateY(${virtualRow.start}px)`,
              }}
              className="px-1 pb-3"
            >
              <div className="grid gap-3" style={{ gridTemplateColumns: `repeat(${columns}, 1fr)` }}>
                {rowItems.map((item) => {
                  const variant = item.card.variants.find((v) => v.id === item.variantId)!
                  return (
                    <OwnershipVariantTile
                      key={item.variantId}
                      card={item.card}
                      variant={variant}
                      selected={selectedIds.has(item.variantId)}
                      onToggleSelect={() => onToggleSelect(item.variantId)}
                      onOpenDetail={onOpenCard ? () => onOpenCard(item.card.id) : undefined}
                      priceGbp={priceByVariantId?.get(item.variantId)}
                    />
                  )
                })}
              </div>
            </div>
          )
        })}
      </div>
    </div>
  )
}
