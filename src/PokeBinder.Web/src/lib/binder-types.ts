export interface BinderSummary {
  id: string
  name: string
  colourHex: string
  rows: number
  columns: number
  pageCount: number
  totalSlots: number
  filledSlots: number
  ownedCount: number
  missingCount: number
  createdAt: string
  lastAccessedAt: string | null
}

export interface DashboardBinder {
  id: string
  name: string
  colourHex: string
  completenessPercent: number
  lastAccessedAt: string | null
}

export interface DashboardValuableCard {
  cardVariantId: string
  cardId: string
  cardName: string
  imageSmallUrl: string | null
  setName: string
  number: string
  variantTypeName: string | null
  priceGbp: number
}

export interface Dashboard {
  cardsOwned: number
  cardsMissing: number
  binderCount: number
  recentBinders: DashboardBinder[]
  portfolioValueGbp: number | null
  topValuableCards: DashboardValuableCard[]
}

export interface CreateBinderInput {
  name: string
  colourHex: string
  rows: number
  columns: number
  initialPageCount: number
}

export interface UpdateBinderInput {
  name?: string
  colourHex?: string
}

export interface BulkAssignResult {
  placed: number
  skipped: number
  pagesAdded: number
}
