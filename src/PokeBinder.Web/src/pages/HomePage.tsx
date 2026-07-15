import { useMemo } from 'react'
import { useNavigate } from 'react-router-dom'
import { RecentBinderTile } from '../components/RecentBinderTile'
import { SetTile } from '../components/sets/SetTile'
import { StatCard } from '../components/StatCard'
import { StatCardSkeleton } from '../components/Skeleton'
import { EmptyState } from '../components/EmptyState'
import { useAuth } from '../lib/auth-context'
import { useDashboard } from '../lib/queries/binders'
import { useSets } from '../lib/queries/cards'
import { topInProgressSets } from '../lib/filterSets'

export function HomePage() {
  const { user } = useAuth()
  const { data, isPending, isError } = useDashboard()
  const { data: sets, isPending: setsPending } = useSets()
  const navigate = useNavigate()

  const inProgressSets = useMemo(() => topInProgressSets(sets ?? [], 5), [sets])

  // The API reports cardsOwned/cardsMissing as account-wide totals but doesn't
  // return a single "overall completeness" figure, so it's derived here the
  // same way each binder's own completeness is: owned / (owned + missing).
  const overallCompleteness =
    data && data.cardsOwned + data.cardsMissing > 0
      ? Math.round((data.cardsOwned / (data.cardsOwned + data.cardsMissing)) * 100)
      : 0

  return (
    <div>
      <h1 className="font-display text-2xl font-semibold italic text-ink">
        Welcome back{user ? `, ${user.email.split('@')[0]}` : ''}
      </h1>
      <p className="mt-1 text-sm text-ink-soft">Here's where your collection stands today.</p>

      <div className="mt-6 grid grid-cols-2 gap-3 lg:grid-cols-4">
        {isPending ? (
          <>
            <StatCardSkeleton />
            <StatCardSkeleton />
            <StatCardSkeleton />
            <StatCardSkeleton />
          </>
        ) : isError || !data ? (
          <div className="col-span-full text-sm text-bad">Couldn't load your dashboard. Try refreshing.</div>
        ) : (
          <>
            <StatCard label="Cards owned" value={data.cardsOwned.toLocaleString()} sub={`across ${data.binderCount} binders`} />
            <StatCard label="Cards missing" value={data.cardsMissing.toLocaleString()} sub="assigned, not owned" />
            <StatCard label="Binders" value={data.binderCount.toLocaleString()} />
            <StatCard label="Completeness" value={`${overallCompleteness}%`} sub="overall" accent />
          </>
        )}
      </div>

      <div className="mt-8 flex items-baseline justify-between">
        <h2 className="font-display text-base font-semibold text-ink">Recently accessed</h2>
        {data && data.recentBinders.length > 0 && (
          <button onClick={() => navigate('/binders')} className="text-xs font-semibold text-accent">
            View all →
          </button>
        )}
      </div>

      <div className="mt-3">
        {isPending ? null : !data || data.recentBinders.length === 0 ? (
          <EmptyState
            title="No binders yet"
            message="Create your first binder to start tracking your collection."
            actionLabel="Create a binder"
            onAction={() => navigate('/binders')}
          />
        ) : (
          <div className="flex gap-3 overflow-x-auto pb-2">
            {data.recentBinders.map((binder) => (
              <RecentBinderTile key={binder.id} binder={binder} />
            ))}
          </div>
        )}
      </div>

      <div className="mt-8 flex items-baseline justify-between">
        <h2 className="font-display text-base font-semibold text-ink">Sets in progress</h2>
        <button onClick={() => navigate('/sets')} className="text-xs font-semibold text-accent">
          View all sets →
        </button>
      </div>

      <div className="mt-3">
        {setsPending ? null : inProgressSets.length === 0 ? (
          <EmptyState
            title="No sets in progress"
            message="Mark a few cards owned from any set to start tracking its completion here."
            actionLabel="Browse sets"
            onAction={() => navigate('/sets')}
          />
        ) : (
          <div className="flex gap-3 overflow-x-auto pb-2">
            {inProgressSets.map((set) => (
              <div key={set.id} className="w-56 shrink-0">
                <SetTile set={set} />
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  )
}
