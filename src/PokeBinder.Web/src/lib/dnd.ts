export interface DragEndLike {
  active: { id: string | number }
  over: { id: string | number } | null
}

export interface SlotMove {
  sourceSlotId: string
  targetSlotId: string
}

/** Pure mapping from a dnd-kit drag-end event to a slot move, or null if there's nothing to do (dropped outside a target, or dropped back on itself). */
export function resolveDragMove(event: DragEndLike): SlotMove | null {
  const { active, over } = event
  if (!over || active.id === over.id) return null
  return { sourceSlotId: String(active.id), targetSlotId: String(over.id) }
}
