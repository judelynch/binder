import { useState } from 'react'
import { BinderCard } from '../components/BinderCard'
import { CreateBinderForm } from '../components/CreateBinderForm'
import { DeleteBinderDialog } from '../components/DeleteBinderDialog'
import { EditBinderForm } from '../components/EditBinderForm'
import { EmptyState } from '../components/EmptyState'
import { Modal } from '../components/Modal'
import { BinderCardSkeleton } from '../components/Skeleton'
import type { BinderSummary } from '../lib/binder-types'
import { useBinders, useCreateBinder, useDeleteBinder, useUpdateBinder } from '../lib/queries/binders'

function EditBinderModal({ binder, onClose }: { binder: BinderSummary; onClose: () => void }) {
  const updateBinder = useUpdateBinder(binder.id)

  return (
    <Modal title="Edit binder" onClose={onClose}>
      <EditBinderForm
        binder={binder}
        onCancel={onClose}
        isSubmitting={updateBinder.isPending}
        onSubmit={(input) => {
          updateBinder.mutate(input, { onSuccess: onClose })
        }}
      />
    </Modal>
  )
}

export function BindersPage() {
  const { data: binders, isPending, isError } = useBinders()
  const createBinder = useCreateBinder()
  const deleteBinder = useDeleteBinder()

  const [isCreateOpen, setCreateOpen] = useState(false)
  const [editingBinder, setEditingBinder] = useState<BinderSummary | null>(null)
  const [deletingBinder, setDeletingBinder] = useState<BinderSummary | null>(null)

  return (
    <div>
      <div className="flex items-center justify-between">
        <div>
          <h1 className="font-display text-2xl font-semibold italic text-ink">Binders</h1>
          <p className="mt-1 text-sm text-ink-soft">Your collection, organised however you like.</p>
        </div>
        {binders && binders.length > 0 && (
          <button
            type="button"
            onClick={() => setCreateOpen(true)}
            className="rounded-lg bg-accent px-4 py-2 text-sm font-semibold text-accent-ink hover:opacity-90"
          >
            + New binder
          </button>
        )}
      </div>

      <div className="mt-6">
        {isPending ? (
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
            <BinderCardSkeleton />
            <BinderCardSkeleton />
            <BinderCardSkeleton />
          </div>
        ) : isError ? (
          <p className="text-sm text-bad">Couldn't load your binders. Try refreshing.</p>
        ) : binders && binders.length === 0 ? (
          <EmptyState
            title="No binders yet"
            message="Create your first binder to start organising your cards."
            actionLabel="Create your first binder"
            onAction={() => setCreateOpen(true)}
          />
        ) : (
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
            {binders?.map((binder) => (
              <BinderCard
                key={binder.id}
                binder={binder}
                onEdit={() => setEditingBinder(binder)}
                onDelete={() => setDeletingBinder(binder)}
              />
            ))}
          </div>
        )}
      </div>

      {isCreateOpen && (
        <Modal title="Create a binder" onClose={() => setCreateOpen(false)}>
          <CreateBinderForm
            isSubmitting={createBinder.isPending}
            onCancel={() => setCreateOpen(false)}
            onSubmit={(input) => {
              createBinder.mutate(input, { onSuccess: () => setCreateOpen(false) })
            }}
          />
        </Modal>
      )}

      {editingBinder && <EditBinderModal binder={editingBinder} onClose={() => setEditingBinder(null)} />}

      {deletingBinder && (
        <DeleteBinderDialog
          binder={deletingBinder}
          isDeleting={deleteBinder.isPending}
          onCancel={() => setDeletingBinder(null)}
          onConfirm={() => {
            deleteBinder.mutate(deletingBinder.id, { onSuccess: () => setDeletingBinder(null) })
          }}
        />
      )}
    </div>
  )
}
