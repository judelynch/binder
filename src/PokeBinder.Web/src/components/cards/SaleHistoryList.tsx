import type { PriceHistoryPoint } from '../../lib/pricing-types'

const FORMAT_LABELS: Record<PriceHistoryPoint['listingFormat'], string> = {
  Auction: 'Auction',
  BuyItNow: 'Buy It Now',
  BestOfferAccepted: 'Best offer',
}

export function SaleHistoryList({ sales }: { sales: PriceHistoryPoint[] }) {
  if (sales.length === 0) {
    return <p className="text-xs text-ink-faint">No accepted sales recorded yet for this variant.</p>
  }

  const newestFirst = [...sales].sort((a, b) => new Date(b.soldDate).getTime() - new Date(a.soldDate).getTime())

  return (
    <div className="space-y-2">
      {newestFirst.map((sale, i) => (
        <div key={i} className="flex items-center gap-3 rounded-lg border border-border bg-surface-2 p-2.5">
          <div className="h-14 w-10 shrink-0 overflow-hidden rounded bg-canvas">
            {sale.thumbnailUrl && <img src={sale.thumbnailUrl} alt="" loading="lazy" className="h-full w-full object-cover" />}
          </div>
          <div className="min-w-0 flex-1">
            <div className="truncate text-xs font-semibold text-ink">{sale.title}</div>
            <div className="mt-0.5 flex flex-wrap gap-1.5 text-[10.5px] text-ink-faint">
              <span>{new Date(sale.soldDate).toLocaleDateString()}</span>
              <span>·</span>
              <span>{FORMAT_LABELS[sale.listingFormat]}</span>
              <span>·</span>
              <span>{sale.gradedStatus === 'Graded' ? `${sale.grader ?? '?'} ${sale.grade ?? '?'}` : `Raw · ${sale.rawCondition}`}</span>
            </div>
          </div>
          <div className="shrink-0 text-right">
            <div className="text-sm font-semibold text-ink [font-variant-numeric:tabular-nums]">£{sale.itemPriceGbp.toFixed(2)}</div>
            {sale.postagePriceGbp != null && (
              <div className="text-[10.5px] text-ink-faint [font-variant-numeric:tabular-nums]">
                + £{sale.postagePriceGbp.toFixed(2)} postage
              </div>
            )}
          </div>
        </div>
      ))}
    </div>
  )
}
