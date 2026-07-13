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
