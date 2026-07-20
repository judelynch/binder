import { useMemo, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { CheckboxChips } from '../components/search/CheckboxChips'
import { FilterGroup } from '../components/search/FilterGroup'
import { RadioChips } from '../components/search/RadioChips'
import { OwnershipSelectionToolbar } from '../components/collection/OwnershipSelectionToolbar'
import { SetCardsGrid } from '../components/collection/SetCardsGrid'
import { Skeleton } from '../components/Skeleton'
import { SetLogo } from '../components/sets/SetLogo'
import { useFullSetCards, useSetPrices, useSets } from '../lib/queries/cards'
import { useBulkSetOwnership } from '../lib/queries/collection'
import { computeSetCompletion, isCardComplete } from '../lib/setCompletion'
import { toggleSelection } from '../lib/selection'

const OWNERSHIP_OPTIONS = ['Complete', 'Incomplete'] as const

export function SetDetailPage() {
  const { setId } = useParams<{ setId: string }>()
  const navigate = useNavigate()
  const { data: sets } = useSets()
  const { data: page, isPending, isError } = useFullSetCards(setId ?? null)
  const { data: prices } = useSetPrices(setId ?? null)
  const bulkSetOwnership = useBulkSetOwnership()

  const priceByVariantId = useMemo(() => {
    const map = new Map<string, number>()
    prices?.forEach((p) => {
      if (p.bestAvailableItemOnlyGbp != null) map.set(p.cardVariantId, p.bestAvailableItemOnlyGbp)
    })
    return map
  }, [prices])

  const [rarities, setRarities] = useState<string[]>([])
  const [variantTypes, setVariantTypes] = useState<string[]>([])
  const [ownershipFilter, setOwnershipFilter] = useState<(typeof OWNERSHIP_OPTIONS)[number] | null>(null)
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set())

  const set = sets?.find((s) => s.id === setId)
  const cards = useMemo(() => page?.items ?? [], [page])
  const completion = useMemo(() => computeSetCompletion(cards), [cards])

  const rarityOptions = useMemo(
    () => [...new Set(cards.map((c) => c.rarity).filter((r): r is string => r !== null))].sort(),
    [cards],
  )
  const variantTypeOptions = useMemo(
    () => [...new Set(cards.flatMap((c) => c.variants.map((v) => v.variantTypeName)))].sort(),
    [cards],
  )

  const visibleCards = useMemo(() => {
    return cards.filter((card) => {
      if (rarities.length > 0 && (card.rarity === null || !rarities.includes(card.rarity))) return false
      if (variantTypes.length > 0 && !card.variants.some((v) => variantTypes.includes(v.variantTypeName))) return false
      if (ownershipFilter) {
        const complete = isCardComplete(card)
        if (ownershipFilter === 'Complete' && !complete) return false
        if (ownershipFilter === 'Incomplete' && complete) return false
      }
      return true
    })
  }, [cards, rarities, variantTypes, ownershipFilter])

  const percent = completion.totalCount === 0 ? 0 : Math.round((completion.ownedCount / completion.totalCount) * 100)
  const visibleVariantIds = useMemo(() => visibleCards.flatMap((c) => c.variants.map((v) => v.id)), [visibleCards])

  function toggleSelect(variantId: string) {
    setSelectedIds((prev) => toggleSelection(prev, variantId))
  }

  function markSelected(owned: boolean) {
    bulkSetOwnership.mutate(
      { cardVariantIds: [...selectedIds], owned },
      { onSuccess: () => setSelectedIds(new Set()) },
    )
  }

  return (
    <div className="flex h-full flex-col">
      <div className="flex items-center gap-4">
        {set && (
          <div className="flex h-14 w-24 shrink-0 items-center justify-center overflow-hidden rounded-lg bg-surface-2 p-2">
            <SetLogo src={set.logoImageUrl} alt={set.name} />
          </div>
        )}
        <div className="min-w-0">
          <h1 className="truncate font-display text-2xl font-semibold italic text-ink">{set?.name ?? 'Set'}</h1>
          <p className="mt-1 text-sm text-ink-soft">{set?.series}</p>
        </div>
      </div>

      <div className="mt-4 flex items-center gap-3">
        <div className="h-2 flex-1 overflow-hidden rounded-full bg-surface-2">
          <div className="h-full rounded-full bg-accent transition-[width]" style={{ width: `${percent}%` }} />
        </div>
        <span className="shrink-0 text-xs text-ink-soft [font-variant-numeric:tabular-nums]">
          {completion.ownedCount} / {completion.totalCount} ({percent}%)
        </span>
      </div>

      <div className="mt-5 flex min-h-0 flex-1 gap-5">
        <aside className="w-56 shrink-0 overflow-y-auto">
          <FilterGroup title="Completion" defaultOpen>
            <RadioChips options={OWNERSHIP_OPTIONS} value={ownershipFilter} onChange={(v) => setOwnershipFilter(v as typeof ownershipFilter)} />
          </FilterGroup>
          <FilterGroup title="Rarity" defaultOpen>
            <CheckboxChips options={rarityOptions} selected={rarities} onChange={setRarities} />
          </FilterGroup>
          <FilterGroup title="Variant" defaultOpen>
            <CheckboxChips options={variantTypeOptions} selected={variantTypes} onChange={setVariantTypes} />
          </FilterGroup>
        </aside>

        <div className="flex min-h-0 min-w-0 flex-1 flex-col">
          <div className="min-h-0 flex-1">
            {isPending ? (
              <div className="grid grid-cols-2 gap-3 sm:grid-cols-4 lg:grid-cols-6">
                {Array.from({ length: 12 }).map((_, i) => (
                  <Skeleton key={i} className="aspect-[5/7] rounded-lg" />
                ))}
              </div>
            ) : isError ? (
              <p className="text-sm text-bad">Couldn't load this set. Try refreshing.</p>
            ) : visibleCards.length === 0 ? (
              <p className="p-4 text-sm text-ink-faint">No cards match these filters.</p>
            ) : (
              <SetCardsGrid
                cards={visibleCards}
                selectedIds={selectedIds}
                onToggleSelect={toggleSelect}
                onOpenCard={(cardId) => navigate(`/cards/${cardId}`)}
                priceByVariantId={priceByVariantId}
              />
            )}
          </div>

          <OwnershipSelectionToolbar
            selectedCount={selectedIds.size}
            totalCount={visibleVariantIds.length}
            onSelectAll={() => setSelectedIds(new Set(visibleVariantIds))}
            onClear={() => setSelectedIds(new Set())}
            onMarkOwned={() => markSelected(true)}
            onMarkUnowned={() => markSelected(false)}
            isMutating={bulkSetOwnership.isPending}
          />
        </div>
      </div>
    </div>
  )
}
