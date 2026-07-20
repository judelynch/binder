import type { CardVariantPrice } from '../../lib/pricing-types'
import { useVariantPriceHistory } from '../../lib/queries/cards'
import { PriceHistoryChart } from './PriceHistoryChart'
import { SaleHistoryList } from './SaleHistoryList'

export function CardVariantPriceCard({
  cardId,
  variantTypeName,
  price,
}: {
  cardId: string
  variantTypeName: string
  price: CardVariantPrice
}) {
  const { data: history } = useVariantPriceHistory(cardId, price.cardVariantId)
  const psa10 = price.gradedBuckets.find((b) => b.grader === 'PSA' && b.grade === 10)
  const psa9 = price.gradedBuckets.find((b) => b.grader === 'PSA' && b.grade === 9)

  const hasAnyData = price.rawBuckets.length > 0 || price.gradedBuckets.length > 0

  return (
    <div className="rounded-lg border border-border bg-surface-2 p-3">
      <div className="flex items-center justify-between">
        <span className="text-xs font-semibold text-ink">{variantTypeName}</span>
        {price.bestAvailableItemOnlyGbp != null && (
          <span className="text-sm font-semibold text-accent [font-variant-numeric:tabular-nums]">
            from £{price.bestAvailableItemOnlyGbp.toFixed(2)}
          </span>
        )}
      </div>

      {!hasAnyData ? (
        <p className="mt-2 text-xs text-ink-faint">No price data yet for this variant.</p>
      ) : (
        <>
          {price.rawBuckets.length > 0 && (
            <div className="mt-2 space-y-1">
              {price.rawBuckets.map((b) => (
                <div key={`${b.condition}-${b.windowDays}`} className="flex items-baseline justify-between text-xs">
                  <span className="text-ink-soft">
                    {b.condition ?? 'Unspecified'} <span className="text-ink-faint">({b.windowDays}d, {b.sampleCount} sales)</span>
                  </span>
                  <span className="font-semibold text-ink [font-variant-numeric:tabular-nums]">£{b.itemOnlyMedianGbp.toFixed(2)}</span>
                </div>
              ))}
            </div>
          )}

          {(psa10 || psa9) && (
            <div className="mt-2 flex items-center gap-4 border-t border-border pt-2">
              {psa10 && (
                <div>
                  <div className="text-[10.5px] text-ink-faint">PSA 10</div>
                  <div className="text-sm font-semibold text-ink [font-variant-numeric:tabular-nums]">£{psa10.itemOnlyMedianGbp.toFixed(2)}</div>
                </div>
              )}
              {psa9 && (
                <div>
                  <div className="text-[10.5px] text-ink-faint">PSA 9</div>
                  <div className="text-sm font-semibold text-ink [font-variant-numeric:tabular-nums]">£{psa9.itemOnlyMedianGbp.toFixed(2)}</div>
                </div>
              )}
            </div>
          )}
        </>
      )}

      <div className="mt-3 border-t border-border pt-2">
        <div className="mb-1 text-[10.5px] font-semibold uppercase tracking-wide text-ink-faint">Price trend</div>
        <PriceHistoryChart points={history ?? []} />
      </div>

      <div className="mt-3 border-t border-border pt-2">
        <div className="mb-1.5 text-[10.5px] font-semibold uppercase tracking-wide text-ink-faint">
          All sales ({history?.length ?? 0})
        </div>
        <SaleHistoryList sales={history ?? []} />
      </div>
    </div>
  )
}
