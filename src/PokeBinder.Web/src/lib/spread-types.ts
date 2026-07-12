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
