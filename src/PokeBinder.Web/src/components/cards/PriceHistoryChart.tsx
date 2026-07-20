import type { PriceHistoryPoint } from '../../lib/pricing-types'

const WIDTH = 320
const HEIGHT = 96
const PAD_X = 4
const PAD_Y = 10

/** Plain SVG line chart of individual accepted sales over time - no charting library, just enough to show the trend. */
export function PriceHistoryChart({ points: unsorted }: { points: PriceHistoryPoint[] }) {
  if (unsorted.length < 2) {
    return <p className="text-xs text-ink-faint">Not enough sales yet to chart a trend.</p>
  }

  // Sorted defensively rather than trusting caller order - the line is drawn by connecting points
  // in array order, so anything other than chronological would draw a jagged mess.
  const points = [...unsorted].sort((a, b) => new Date(a.soldDate).getTime() - new Date(b.soldDate).getTime())
  const dates = points.map((p) => new Date(p.soldDate).getTime())
  const prices = points.map((p) => p.itemPriceGbp)
  const minDate = Math.min(...dates)
  const maxDate = Math.max(...dates)
  const minPrice = Math.min(...prices)
  const maxPrice = Math.max(...prices)
  const dateSpan = maxDate - minDate || 1
  const priceSpan = maxPrice - minPrice || 1

  const x = (t: number) => PAD_X + ((t - minDate) / dateSpan) * (WIDTH - PAD_X * 2)
  const y = (p: number) => HEIGHT - PAD_Y - ((p - minPrice) / priceSpan) * (HEIGHT - PAD_Y * 2)

  const linePath = points.map((p, i) => `${i === 0 ? 'M' : 'L'} ${x(new Date(p.soldDate).getTime())} ${y(p.itemPriceGbp)}`).join(' ')
  const areaPath = `${linePath} L ${x(maxDate)} ${HEIGHT - PAD_Y} L ${x(minDate)} ${HEIGHT - PAD_Y} Z`

  return (
    <div>
      <svg viewBox={`0 0 ${WIDTH} ${HEIGHT}`} className="h-24 w-full" preserveAspectRatio="none">
        <line x1={PAD_X} y1={HEIGHT - PAD_Y} x2={WIDTH - PAD_X} y2={HEIGHT - PAD_Y} stroke="var(--color-border)" strokeWidth="1" />
        <path d={areaPath} fill="var(--color-accent)" fillOpacity="0.12" stroke="none" />
        <path d={linePath} fill="none" stroke="var(--color-accent)" strokeWidth="1.5" vectorEffect="non-scaling-stroke" />
        {points.map((p, i) => (
          <circle key={i} cx={x(new Date(p.soldDate).getTime())} cy={y(p.itemPriceGbp)} r="2" fill="var(--color-accent)" />
        ))}
      </svg>
      <div className="mt-1 flex items-center justify-between text-[10px] text-ink-faint">
        <span>{new Date(minDate).toLocaleDateString()}</span>
        <span className="[font-variant-numeric:tabular-nums]">
          £{minPrice.toFixed(2)} – £{maxPrice.toFixed(2)}
        </span>
        <span>{new Date(maxDate).toLocaleDateString()}</span>
      </div>
    </div>
  )
}
