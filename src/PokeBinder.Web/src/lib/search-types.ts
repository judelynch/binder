import type { VariantSummary } from './queries/cards'

export type SortOption = 'setNumber' | 'name' | 'releaseDate' | 'rarity'

export interface CardSearchFilters {
  name: string
  supertype: string | null
  subtypes: string[]
  types: string[]
  setIds: string[]
  series: string[]
  rarities: string[]
  hpMin: number | null
  hpMax: number | null
  weaknessType: string | null
  resistanceType: string | null
  retreatCostMin: number | null
  retreatCostMax: number | null
  artist: string
  regulationMarks: string[]
  nationalPokedexNumber: number | null
  variantTypes: string[]
  sort: SortOption
  sortDescending: boolean
}

export const EMPTY_FILTERS: CardSearchFilters = {
  name: '',
  supertype: null,
  subtypes: [],
  types: [],
  setIds: [],
  series: [],
  rarities: [],
  hpMin: null,
  hpMax: null,
  weaknessType: null,
  resistanceType: null,
  retreatCostMin: null,
  retreatCostMax: null,
  artist: '',
  regulationMarks: [],
  nationalPokedexNumber: null,
  variantTypes: [],
  sort: 'setNumber',
  sortDescending: true,
}

export interface CardSearchResult {
  id: string
  setId: string
  setName: string
  name: string
  number: string
  rarity: string | null
  supertype: string
  imageSmallUrl: string | null
  imageLargeUrl: string | null
  variants: VariantSummary[]
}

/** Active-filter chips: one entry per filter currently set, with a function to clear just that one. */
export interface ActiveFilterChip {
  key: string
  label: string
  clear: (filters: CardSearchFilters) => CardSearchFilters
}

export function describeActiveFilters(filters: CardSearchFilters): ActiveFilterChip[] {
  const chips: ActiveFilterChip[] = []

  if (filters.name) chips.push({ key: 'name', label: `Name: "${filters.name}"`, clear: (f) => ({ ...f, name: '' }) })
  if (filters.supertype) chips.push({ key: 'supertype', label: filters.supertype, clear: (f) => ({ ...f, supertype: null }) })

  for (const value of filters.subtypes) {
    chips.push({ key: `subtype-${value}`, label: value, clear: (f) => ({ ...f, subtypes: f.subtypes.filter((v) => v !== value) }) })
  }
  for (const value of filters.types) {
    chips.push({ key: `type-${value}`, label: value, clear: (f) => ({ ...f, types: f.types.filter((v) => v !== value) }) })
  }
  for (const value of filters.setIds) {
    chips.push({ key: `set-${value}`, label: value, clear: (f) => ({ ...f, setIds: f.setIds.filter((v) => v !== value) }) })
  }
  for (const value of filters.series) {
    chips.push({ key: `series-${value}`, label: value, clear: (f) => ({ ...f, series: f.series.filter((v) => v !== value) }) })
  }
  for (const value of filters.rarities) {
    chips.push({ key: `rarity-${value}`, label: value, clear: (f) => ({ ...f, rarities: f.rarities.filter((v) => v !== value) }) })
  }
  for (const value of filters.regulationMarks) {
    chips.push({ key: `reg-${value}`, label: `Reg. ${value}`, clear: (f) => ({ ...f, regulationMarks: f.regulationMarks.filter((v) => v !== value) }) })
  }
  for (const value of filters.variantTypes) {
    chips.push({ key: `variant-${value}`, label: value, clear: (f) => ({ ...f, variantTypes: f.variantTypes.filter((v) => v !== value) }) })
  }

  if (filters.hpMin !== null || filters.hpMax !== null) {
    chips.push({
      key: 'hp',
      label: `HP ${filters.hpMin ?? 0}–${filters.hpMax ?? '∞'}`,
      clear: (f) => ({ ...f, hpMin: null, hpMax: null }),
    })
  }
  if (filters.retreatCostMin !== null || filters.retreatCostMax !== null) {
    chips.push({
      key: 'retreat',
      label: `Retreat ${filters.retreatCostMin ?? 0}–${filters.retreatCostMax ?? '∞'}`,
      clear: (f) => ({ ...f, retreatCostMin: null, retreatCostMax: null }),
    })
  }
  if (filters.weaknessType) {
    chips.push({ key: 'weakness', label: `Weak to ${filters.weaknessType}`, clear: (f) => ({ ...f, weaknessType: null }) })
  }
  if (filters.resistanceType) {
    chips.push({ key: 'resistance', label: `Resists ${filters.resistanceType}`, clear: (f) => ({ ...f, resistanceType: null }) })
  }
  if (filters.artist) {
    chips.push({ key: 'artist', label: `Artist: "${filters.artist}"`, clear: (f) => ({ ...f, artist: '' }) })
  }
  if (filters.nationalPokedexNumber !== null) {
    chips.push({ key: 'pokedex', label: `Pokédex #${filters.nationalPokedexNumber}`, clear: (f) => ({ ...f, nationalPokedexNumber: null }) })
  }

  return chips
}
