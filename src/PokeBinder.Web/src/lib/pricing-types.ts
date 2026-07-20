export interface PriceBucket {
  gradedStatus: 'Raw' | 'Graded'
  grader: string | null
  grade: number | null
  condition: 'Unspecified' | 'NM' | 'LP' | 'MP' | 'HP' | 'DMG' | null
  windowDays: number
  itemOnlyMedianGbp: number
  deliveredMedianGbp: number
  sampleCount: number
  lastSaleDate: string
}

export interface CardVariantPrice {
  cardVariantId: string
  bestAvailableItemOnlyGbp: number | null
  bestAvailableDeliveredGbp: number | null
  rawBuckets: PriceBucket[]
  gradedBuckets: PriceBucket[]
  lastScrapedAt: string | null
}

export interface BinderPriceSummary {
  ownedValueGbp: number | null
  missingCostGbp: number | null
  prices: CardVariantPrice[]
}

export interface PriceHistoryPoint {
  soldDate: string
  title: string
  itemPriceGbp: number
  postagePriceGbp: number | null
  deliveredPriceGbp: number
  listingFormat: 'Auction' | 'BuyItNow' | 'BestOfferAccepted'
  thumbnailUrl: string | null
  gradedStatus: 'Raw' | 'Graded'
  grader: string | null
  grade: number | null
  rawCondition: 'Unspecified' | 'NM' | 'LP' | 'MP' | 'HP' | 'DMG'
}

const STALENESS_DAYS = 7

/** Prefers the freshest window (30 over 60 over 90) among buckets matching a predicate. */
export function pickBucket(buckets: PriceBucket[], predicate: (b: PriceBucket) => boolean): PriceBucket | null {
  const matches = buckets.filter(predicate)
  if (matches.length === 0) return null
  return matches.slice().sort((a, b) => a.windowDays - b.windowDays)[0]
}

export function isStale(lastScrapedAt: string | null): boolean {
  if (!lastScrapedAt) return true
  const ageMs = Date.now() - new Date(lastScrapedAt).getTime()
  return ageMs > STALENESS_DAYS * 24 * 60 * 60 * 1000
}
