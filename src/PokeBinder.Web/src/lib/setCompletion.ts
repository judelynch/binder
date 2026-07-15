import type { CardSummary, OwnedVariantSummary } from './queries/cards'

/**
 * A variant is "required" for completion unless its type name contains "Stamp"
 * (case-insensitive) - e.g. "Promo Stamp" doesn't block a card from counting as complete.
 * This must stay in sync with the identical rule in SetsController.GetSets on the backend -
 * it's implemented twice (SQL there, TypeScript here) so both need the same fixtures asserted.
 */
function isRequiredVariant(variant: OwnedVariantSummary): boolean {
  return !variant.variantTypeName.toUpperCase().includes('STAMP')
}

/** A card with no required variants (e.g. its only printing is a Promo Stamp) is vacuously complete. */
export function isCardComplete(card: CardSummary): boolean {
  return card.variants.filter(isRequiredVariant).every((v) => v.owned)
}

export interface SetCompletion {
  ownedCount: number
  totalCount: number
}

export function computeSetCompletion(cards: CardSummary[]): SetCompletion {
  return {
    ownedCount: cards.filter(isCardComplete).length,
    totalCount: cards.length,
  }
}
