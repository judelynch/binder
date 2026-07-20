export interface CardSlotSummary {
  id: string
  name: string
  imageSmallUrl: string | null
  imageLargeUrl: string | null
  setId: string
  setName: string
  number: string
  rarity: string | null
}

export interface OverlayTag {
  id: string
  name: string
  colourHex: string
}

export type SlotCondition = 'NM' | 'LP' | 'MP' | 'HP' | 'DMG'

export interface BinderSlot {
  slotId: string
  position: number
  card: CardSlotSummary | null
  cardVariantId: string | null
  variantTypeName: string | null
  owned: boolean
  quantity: number | null
  condition: SlotCondition | null
  overlayTag: OverlayTag | null
}

export type PanelType = 'cover' | 'page'

export interface SpreadPanel {
  type: PanelType
  pageNumber: number | null
  slots: BinderSlot[] | null
}

export interface Spread {
  leftPanel: SpreadPanel
  rightPanel: SpreadPanel
  totalSpreads: number
}

export type SuggestionReason = 'NextInSet' | 'PrevInSet' | 'NextRelease' | 'SameThemeRarity'

export interface SuggestedCard {
  cardId: string
  name: string
  setId: string
  setName: string
  number: string
  imageSmallUrl: string | null
  rarity: string | null
  cardVariantId: string
  reason: SuggestionReason
}

export interface SlotSuggestions {
  slotId: string
  suggestions: SuggestedCard[]
}

/** SlotSuggestions remapped from its source (filled) slot onto the nearest following empty slot -
 * see BinderDetailPage's emptySlotSuggestions - plus the filled card's name for the "Based on X"
 * framing in the modal, since an empty slot itself has no card to read that from. */
export interface EmptySlotSuggestions extends SlotSuggestions {
  basedOnCardName: string
}
