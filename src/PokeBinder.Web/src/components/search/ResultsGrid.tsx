import { useVirtualizer } from '@tanstack/react-virtual'
import { useMemo, useRef } from 'react'
import type { CardSearchResult } from '../../lib/search-types'
import { useColumnCount } from '../../lib/useColumnCount'
import { CardResultTile } from './CardResultTile'

const ROW_HEIGHT = 210

/** One selectable tile per (card, variant) pair — a card with three variants renders as three tiles. */
interface DisplayItem {
  card: CardSearchResult
  variantId: string
  variantTypeName: string
}

export function ResultsGrid({
  results,
  selectedIds,
  onToggleSelect,
  onOpenCard,
}: {
  results: CardSearchResult[]
  selectedIds: ReadonlySet<string>
  onToggleSelect: (variantId: string) => void
  onOpenCard?: (cardId: string) => void
}) {
  const parentRef = useRef<HTMLDivElement>(null)
  const columns = useColumnCount()

  const items = useMemo<DisplayItem[]>(
    () => results.flatMap((card) => card.variants.map((v) => ({ card, variantId: v.id, variantTypeName: v.variantTypeName }))),
    [results],
  )
  const rowCount = Math.ceil(items.length / columns)

  // Each tile's image is a fixed aspect-ratio (aspect-[5/7]), so its actual rendered height is a
  // function of column width — narrower columns (fewer columns per row, e.g. mobile) make tiles
  // *taller*, not shorter. A static row-height estimate is only ever right for one column count;
  // at every other breakpoint the real content either overflows a too-short row (rows visually
  // overlapping the next one — "some cards bigger than the rest") or leaves a gap in a too-tall
  // one. Dynamic measurement (measureElement) re-measures each row after it renders instead of
  // trusting a fixed guess.
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
                {rowItems.map((item) => (
                  <CardResultTile
                    key={item.variantId}
                    card={item.card}
                    variant={{ id: item.variantId, variantTypeName: item.variantTypeName }}
                    selected={selectedIds.has(item.variantId)}
                    onToggleSelect={() => onToggleSelect(item.variantId)}
                    onOpenDetail={onOpenCard ? () => onOpenCard(item.card.id) : undefined}
                  />
                ))}
              </div>
            </div>
          )
        })}
      </div>
    </div>
  )
}
