import { useVirtualizer } from '@tanstack/react-virtual'
import { useRef } from 'react'
import type { CardSearchResult } from '../../lib/search-types'
import { useColumnCount } from '../../lib/useColumnCount'
import { CardResultTile } from './CardResultTile'

const ROW_HEIGHT = 210

export function ResultsGrid({
  results,
  selectedIds,
  onToggleSelect,
  selectedVariants,
  onSelectVariant,
}: {
  results: CardSearchResult[]
  selectedIds: ReadonlySet<string>
  onToggleSelect: (id: string) => void
  selectedVariants: Record<string, string>
  onSelectVariant: (cardId: string, variantId: string) => void
}) {
  const parentRef = useRef<HTMLDivElement>(null)
  const columns = useColumnCount()
  const rowCount = Math.ceil(results.length / columns)

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
          const rowCards = results.slice(rowStart, rowStart + columns)
          return (
            <div
              key={virtualRow.key}
              style={{
                position: 'absolute',
                top: 0,
                left: 0,
                width: '100%',
                height: virtualRow.size,
                transform: `translateY(${virtualRow.start}px)`,
              }}
              className="px-1"
            >
              <div className="grid gap-3" style={{ gridTemplateColumns: `repeat(${columns}, 1fr)` }}>
                {rowCards.map((card) => (
                  <CardResultTile
                    key={card.id}
                    card={card}
                    selected={selectedIds.has(card.id)}
                    onToggleSelect={() => onToggleSelect(card.id)}
                    selectedVariantId={selectedVariants[card.id]}
                    onSelectVariant={(variantId) => onSelectVariant(card.id, variantId)}
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
