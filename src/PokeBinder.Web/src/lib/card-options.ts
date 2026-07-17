export const SUPERTYPES = ['Pokémon', 'Trainer', 'Energy'] as const

export const ENERGY_TYPES = [
  'Grass', 'Fire', 'Water', 'Lightning', 'Psychic', 'Fighting',
  'Darkness', 'Metal', 'Fairy', 'Dragon', 'Colorless',
] as const

export const SUBTYPES = [
  'Basic', 'Stage 1', 'Stage 2', 'VMAX', 'VSTAR', 'V', 'GX', 'EX', 'BREAK',
  'LEGEND', 'Restored', 'Ancient', 'Future', 'Tera',
  'Supporter', 'Item', 'Stadium', 'Pokémon Tool', 'Special', 'Basic Energy',
] as const

export const REGULATION_MARKS = ['D', 'E', 'F', 'G', 'H', 'I'] as const

// Ordered roughly common -> rarest, matching the backend's rarity sort ranking.
export const RARITIES = [
  'Common', 'Uncommon', 'Rare', 'Rare ACE', 'ACE SPEC Rare', 'Promo', 'Rare Holo',
  'Rare BREAK', 'Rare Prime', 'Rare Shining', 'Rare Shiny', 'LEGEND', 'Rare Holo Star',
  'Rare Prism Star', 'Radiant Rare', 'Amazing Rare', 'Rare Holo EX', 'Rare Holo GX',
  'Rare Holo LV.X', 'Rare Holo V', 'Rare Holo VMAX', 'Rare Holo VSTAR', 'Rare Shiny GX',
  'Double Rare', 'Ultra Rare', 'Shiny Ultra Rare', 'Rare Rainbow', 'Rare Secret',
  'Rare Ultra', 'Illustration Rare', 'Trainer Gallery Rare Holo', 'Classic Collection',
  'Hyper Rare', 'Special Illustration Rare', 'Mega Hyper Rare', 'MEGA_ATTACK_RARE',
  'Black White Rare', 'Shiny Rare',
] as const

export const SORT_OPTIONS: { value: string; label: string }[] = [
  { value: 'setNumber', label: 'Set & number' },
  { value: 'name', label: 'Name' },
  { value: 'releaseDate', label: 'Release date' },
  { value: 'rarity', label: 'Rarity' },
]

// Each sort field's natural direction — matches the backend's default when sortDescending is unset.
export const DEFAULT_SORT_DESCENDING: Record<string, boolean> = {
  setNumber: true,
  name: false,
  releaseDate: true,
  rarity: true,
}
