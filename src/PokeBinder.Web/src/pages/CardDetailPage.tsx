import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { CardImage } from '../components/binder/CardImage'
import { CardVariantPriceCard } from '../components/cards/CardVariantPriceCard'
import { OwnershipToggle } from '../components/collection/OwnershipToggle'
import { Skeleton } from '../components/Skeleton'
import { useCard, useCardPrices, useSets } from '../lib/queries/cards'

const TABS = ['Overview', 'Market price'] as const
type Tab = (typeof TABS)[number]

function StatRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex justify-between gap-3 border-b border-border py-1.5 text-sm last:border-b-0">
      <span className="text-ink-faint">{label}</span>
      <span className="text-right text-ink">{value}</span>
    </div>
  )
}

export function CardDetailPage() {
  const { cardId } = useParams<{ cardId: string }>()
  const { data: card, isPending, isError } = useCard(cardId ?? null)
  const { data: prices } = useCardPrices(cardId ?? null)
  const { data: sets } = useSets()
  const set = sets?.find((s) => s.id === card?.setId)
  const [tab, setTab] = useState<Tab>('Overview')

  if (isPending) {
    return (
      <div className="grid grid-cols-1 gap-6 md:grid-cols-[280px_1fr]">
        <Skeleton className="aspect-[5/7] rounded-xl" />
        <Skeleton className="h-96 rounded-xl" />
      </div>
    )
  }

  if (isError || !card) {
    return <p className="text-sm text-bad">Couldn't load this card. Try refreshing.</p>
  }

  return (
    <div>
      {set && (
        <Link to={`/sets/${set.id}`} className="text-xs font-semibold text-accent">
          ← {set.name}
        </Link>
      )}

      <div className="mt-3 grid grid-cols-1 gap-6 md:grid-cols-[280px_1fr]">
        <div className="relative aspect-[5/7] w-full max-w-[280px] overflow-hidden rounded-xl bg-surface-2">
          <CardImage src={card.imageLargeUrl ?? card.imageSmallUrl} alt={card.name} greyscale={false} />
        </div>

        <div>
          <h1 className="font-display text-2xl font-semibold italic text-ink">{card.name}</h1>
          <p className="mt-1 text-sm text-ink-soft">
            {card.supertype}
            {card.subtypes.length > 0 && ` — ${card.subtypes.join(', ')}`}
          </p>

          <div className="mt-4 flex gap-2 border-b border-border">
            {TABS.map((t) => (
              <button
                key={t}
                type="button"
                onClick={() => setTab(t)}
                aria-pressed={tab === t}
                className={`-mb-px border-b-2 px-3 py-2 text-sm font-semibold transition-colors ${
                  tab === t ? 'border-accent text-accent' : 'border-transparent text-ink-soft hover:text-ink'
                }`}
              >
                {t}
              </button>
            ))}
          </div>

          {tab === 'Overview' && (
            <div className="mt-4">
              <div className="grid grid-cols-1 gap-6 sm:grid-cols-2">
                <div className="rounded-xl border border-border bg-surface p-4">
                  <h2 className="font-display text-sm font-semibold text-ink">Stats</h2>
                  <div className="mt-2">
                    {card.hp && <StatRow label="HP" value={card.hp} />}
                    {card.types.length > 0 && <StatRow label="Type" value={card.types.join(', ')} />}
                    {card.evolvesFrom && <StatRow label="Evolves from" value={card.evolvesFrom} />}
                    <StatRow label="Number" value={`#${card.number}`} />
                    {card.rarity && <StatRow label="Rarity" value={card.rarity} />}
                    {card.artist && <StatRow label="Artist" value={card.artist} />}
                    {card.regulationMark && <StatRow label="Regulation mark" value={card.regulationMark} />}
                    {card.nationalPokedexNumbers.length > 0 && (
                      <StatRow label="Pokédex #" value={card.nationalPokedexNumbers.join(', ')} />
                    )}
                    {card.retreatCost.length > 0 && <StatRow label="Retreat cost" value={String(card.retreatCost.length)} />}
                  </div>
                </div>

                <div className="rounded-xl border border-border bg-surface p-4">
                  <h2 className="font-display text-sm font-semibold text-ink">Your copies</h2>
                  <div className="mt-3 flex flex-col gap-3">
                    {card.variants.map((variant) => (
                      <OwnershipToggle key={variant.id} variant={variant} />
                    ))}
                  </div>
                </div>
              </div>

              {card.abilities.length > 0 && (
                <div className="mt-6 rounded-xl border border-border bg-surface p-4">
                  <h2 className="font-display text-sm font-semibold text-ink">Abilities</h2>
                  {card.abilities.map((ability) => (
                    <div key={ability.name} className="mt-2">
                      <div className="text-sm font-semibold text-accent">
                        {ability.type} — {ability.name}
                      </div>
                      <p className="mt-0.5 text-sm text-ink-soft">{ability.text}</p>
                    </div>
                  ))}
                </div>
              )}

              {card.attacks.length > 0 && (
                <div className="mt-6 rounded-xl border border-border bg-surface p-4">
                  <h2 className="font-display text-sm font-semibold text-ink">Attacks</h2>
                  {card.attacks.map((attack) => (
                    <div key={attack.name} className="mt-2">
                      <div className="flex items-baseline justify-between gap-3 text-sm">
                        <span className="font-semibold text-ink">
                          {attack.cost.length > 0 && <span className="text-ink-faint">{attack.cost.join('')} </span>}
                          {attack.name}
                        </span>
                        {attack.damage && <span className="font-semibold text-ink [font-variant-numeric:tabular-nums]">{attack.damage}</span>}
                      </div>
                      {attack.text && <p className="mt-0.5 text-sm text-ink-soft">{attack.text}</p>}
                    </div>
                  ))}
                </div>
              )}

              {(card.weaknesses.length > 0 || card.resistances.length > 0) && (
                <div className="mt-6 flex gap-6">
                  {card.weaknesses.length > 0 && (
                    <div>
                      <h2 className="font-display text-sm font-semibold text-ink">Weakness</h2>
                      {card.weaknesses.map((w) => (
                        <div key={w.type} className="mt-1 text-sm text-ink-soft">
                          {w.type} {w.value}
                        </div>
                      ))}
                    </div>
                  )}
                  {card.resistances.length > 0 && (
                    <div>
                      <h2 className="font-display text-sm font-semibold text-ink">Resistance</h2>
                      {card.resistances.map((r) => (
                        <div key={r.type} className="mt-1 text-sm text-ink-soft">
                          {r.type} {r.value}
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              )}

              {card.flavorText && <p className="mt-6 font-display text-sm italic text-ink-faint">"{card.flavorText}"</p>}
            </div>
          )}

          {tab === 'Market price' && (
            <div className="mt-4">
              {!prices || prices.length === 0 ? (
                <p className="text-sm text-ink-faint">No price data yet for this card.</p>
              ) : (
                <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
                  {card.variants.map((variant) => {
                    const price = prices.find((p) => p.cardVariantId === variant.id)
                    return price ? (
                      <CardVariantPriceCard key={variant.id} cardId={card.id} variantTypeName={variant.variantTypeName} price={price} />
                    ) : null
                  })}
                </div>
              )}
            </div>
          )}
        </div>
      </div>
    </div>
  )
}
