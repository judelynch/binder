import { useEffect, useState } from 'react'
import { DEFAULT_SORT_DESCENDING, ENERGY_TYPES, RARITIES, REGULATION_MARKS, SORT_OPTIONS, SUBTYPES, SUPERTYPES } from '../../lib/card-options'
import { useSets, useVariantTypeNames } from '../../lib/queries/cards'
import type { CardSearchFilters } from '../../lib/search-types'
import { useDebouncedValue } from '../../lib/useDebouncedValue'
import { ActiveFilterChips } from './ActiveFilterChips'
import { CheckboxChips } from './CheckboxChips'
import { FilterGroup } from './FilterGroup'
import { RadioChips } from './RadioChips'
import { RangeInput } from './RangeInput'

export function SearchFilterPanel({
  filters,
  onChange,
  resultCount,
}: {
  filters: CardSearchFilters
  onChange: (next: CardSearchFilters) => void
  resultCount: number | null
}) {
  const { data: sets } = useSets()
  const { data: variantTypeNames } = useVariantTypeNames()
  const seriesOptions = Array.from(new Set(sets?.map((s) => s.series) ?? [])).sort()
  const setOptions = (sets ?? []).map((s) => ({ id: s.id, name: s.name }));

  const [nameInput, setNameInput] = useState(filters.name)
  const debouncedName = useDebouncedValue(nameInput, 250)

  useEffect(() => {
    if (debouncedName !== filters.name) {
      onChange({ ...filters, name: debouncedName })
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [debouncedName])

  return (
    <div className="flex h-full flex-col">
      <div className="border-b border-border p-4">
        <input
          type="text"
          value={nameInput}
          onChange={(e) => setNameInput(e.target.value)}
          placeholder="Search card name…"
          aria-label="Card name"
          className="w-full rounded-lg border border-border bg-canvas px-3 py-2 text-sm text-ink placeholder:text-ink-faint focus:border-accent focus:outline-none"
        />
        <div className="mt-2.5 flex items-center justify-between">
          <span className="text-xs text-ink-soft">
            {resultCount === null ? 'Searching…' : `${resultCount.toLocaleString()} results`}
          </span>
          <div className="flex items-center gap-1">
            <select
              aria-label="Sort by"
              value={filters.sort}
              onChange={(e) => {
                const sort = e.target.value as CardSearchFilters['sort']
                onChange({ ...filters, sort, sortDescending: DEFAULT_SORT_DESCENDING[sort] })
              }}
              className="rounded-lg border border-border bg-canvas px-2 py-1 text-xs text-ink"
            >
              {SORT_OPTIONS.map((opt) => (
                <option key={opt.value} value={opt.value}>
                  {opt.label}
                </option>
              ))}
            </select>
            <button
              type="button"
              onClick={() => onChange({ ...filters, sortDescending: !filters.sortDescending })}
              aria-label={filters.sortDescending ? 'Sort descending — click for ascending' : 'Sort ascending — click for descending'}
              title={filters.sortDescending ? 'Descending' : 'Ascending'}
              className="flex h-[26px] w-[26px] shrink-0 items-center justify-center rounded-lg border border-border text-xs text-ink-soft hover:text-ink"
            >
              {filters.sortDescending ? '↓' : '↑'}
            </button>
          </div>
        </div>
        <div className="mt-2.5">
          <ActiveFilterChips filters={filters} onChange={onChange} />
        </div>
      </div>

      <div className="flex-1 overflow-y-auto px-4">
        <FilterGroup title="Supertype" defaultOpen>
          <RadioChips options={SUPERTYPES} value={filters.supertype} onChange={(v) => onChange({ ...filters, supertype: v })} />
        </FilterGroup>

        <FilterGroup title="Card type">
          <CheckboxChips options={SUBTYPES} selected={filters.subtypes} onChange={(v) => onChange({ ...filters, subtypes: v })} />
        </FilterGroup>

        <FilterGroup title="Energy type">
          <CheckboxChips options={ENERGY_TYPES} selected={filters.types} onChange={(v) => onChange({ ...filters, types: v })} />
        </FilterGroup>

        <FilterGroup title="Set">
          <CheckboxChips
            options={setOptions.map((s) => s.name)}
            selected={setOptions.filter((s) => filters.setIds.includes(s.id)).map((s) => s.name)}
            onChange={(names) =>
              onChange({ ...filters, setIds: setOptions.filter((s) => names.includes(s.name)).map((s) => s.id) })
            }
          />
        </FilterGroup>

        <FilterGroup title="Series">
          <CheckboxChips options={seriesOptions} selected={filters.series} onChange={(v) => onChange({ ...filters, series: v })} />
        </FilterGroup>

        <FilterGroup title="Rarity">
          <CheckboxChips options={RARITIES} selected={filters.rarities} onChange={(v) => onChange({ ...filters, rarities: v })} />
        </FilterGroup>

        <FilterGroup title="HP">
          <RangeInput min={filters.hpMin} max={filters.hpMax} onChange={(min, max) => onChange({ ...filters, hpMin: min, hpMax: max })} />
        </FilterGroup>

        <FilterGroup title="Weakness">
          <RadioChips options={ENERGY_TYPES} value={filters.weaknessType} onChange={(v) => onChange({ ...filters, weaknessType: v })} />
        </FilterGroup>

        <FilterGroup title="Resistance">
          <RadioChips options={ENERGY_TYPES} value={filters.resistanceType} onChange={(v) => onChange({ ...filters, resistanceType: v })} />
        </FilterGroup>

        <FilterGroup title="Retreat cost">
          <RangeInput
            min={filters.retreatCostMin}
            max={filters.retreatCostMax}
            onChange={(min, max) => onChange({ ...filters, retreatCostMin: min, retreatCostMax: max })}
          />
        </FilterGroup>

        <FilterGroup title="Artist">
          <input
            type="text"
            value={filters.artist}
            onChange={(e) => onChange({ ...filters, artist: e.target.value })}
            placeholder="Artist name…"
            className="w-full rounded-lg border border-border bg-canvas px-2.5 py-1.5 text-xs text-ink placeholder:text-ink-faint focus:border-accent focus:outline-none"
          />
        </FilterGroup>

        <FilterGroup title="Regulation mark">
          <CheckboxChips
            options={REGULATION_MARKS}
            selected={filters.regulationMarks}
            onChange={(v) => onChange({ ...filters, regulationMarks: v })}
          />
        </FilterGroup>

        <FilterGroup title="Variant">
          <CheckboxChips
            options={variantTypeNames ?? []}
            selected={filters.variantTypes}
            onChange={(v) => onChange({ ...filters, variantTypes: v })}
          />
        </FilterGroup>

        <FilterGroup title="Pokédex #">
          <input
            type="number"
            inputMode="numeric"
            value={filters.nationalPokedexNumber ?? ''}
            onChange={(e) => onChange({ ...filters, nationalPokedexNumber: e.target.value === '' ? null : Number(e.target.value) })}
            placeholder="e.g. 6"
            className="w-24 rounded-lg border border-border bg-canvas px-2.5 py-1.5 text-xs text-ink placeholder:text-ink-faint [font-variant-numeric:tabular-nums] focus:border-accent focus:outline-none"
          />
        </FilterGroup>
      </div>
    </div>
  )
}
