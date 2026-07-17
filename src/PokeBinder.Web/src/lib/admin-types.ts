export interface SyncSetSummary {
  setId: string
  name: string
}

export interface SyncFieldChange {
  field: string
  count: number
}

export interface SyncManualConflict {
  entityType: 'Card' | 'Set'
  entityId: string
  name: string
  changedFields: string[]
}

export interface SyncDiffSummary {
  setsAdded: number
  setsUpdated: number
  cardsAdded: number
  cardsUpdated: number
  newSets: SyncSetSummary[]
  changedFieldCounts: SyncFieldChange[]
  manualConflicts: SyncManualConflict[]
  elapsed: string
}

export type SyncRunStatus = 'Running' | 'Completed' | 'Failed'

export interface SyncRun {
  id: string
  startedAt: string
  completedAt: string | null
  runByEmail: string
  status: SyncRunStatus
  setsProcessed: number
  totalSets: number
  cardsProcessed: number
  setsAdded: number
  setsUpdated: number
  cardsAdded: number
  cardsUpdated: number
  changedFieldCounts: SyncFieldChange[]
  remainingManualConflicts: SyncManualConflict[]
  errorMessage: string | null
}

export interface VariantType {
  id: string
  name: string
}

export interface CardEditAudit {
  id: string
  editedByEmail: string
  editedAt: string
  note: string
  changedFields: string[]
}

export interface BulkVariantAssignResult {
  matchedCards: number
  created: number
  skipped: number
}

// ---- Pricing pipeline ----

export interface QueuedListing {
  classificationId: string
  rawListingId: string
  title: string
  itemPriceGbp: number
  postagePriceGbp: number | null
  soldDate: string
  listingFormat: 'Auction' | 'BuyItNow' | 'BestOfferAccepted'
  thumbnailUrl: string | null
  resolvedCardVariantId: string
  cardName: string
  setNumber: string
  variantTypeName: string
  identityMatchStrong: boolean
  gradedStatus: 'Raw' | 'Graded'
  grader: string | null
  grade: number | null
  rawCondition: 'Unspecified' | 'NM' | 'LP' | 'MP' | 'HP' | 'DMG'
  variantMatch: 'Confirmed' | 'Ambiguous' | 'Mismatch'
  language: string
  bestOfferAccepted: boolean
  killReason: string | null
  confidenceScore: number
  status: 'AutoAccepted' | 'Quarantined' | 'Rejected'
  classifiedAt: string
}

export interface ReclassifyPayload {
  gradedStatus: 'Raw' | 'Graded'
  grader: string | null
  grade: number | null
  rawCondition: 'Unspecified' | 'NM' | 'LP' | 'MP' | 'HP' | 'DMG'
  reason?: string
}

export interface BulkClassificationActionResult {
  succeeded: number
  failed: number
}

export type ScrapeRunStatus = 'Running' | 'Completed' | 'Failed'
export type ScrapeTrigger = 'Nightly' | 'LoginCatchUp' | 'Manual'

export interface ScrapeRun {
  id: string
  startedAt: string
  completedAt: string | null
  status: ScrapeRunStatus
  triggeredBy: ScrapeTrigger
  triggeredByUserId: string | null
  cardsProcessed: number
  listingsFound: number
  listingsAccepted: number
  listingsQuarantined: number
  listingsRejected: number
  errorMessage: string | null
}
