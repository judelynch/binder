import { describe, expect, it } from 'vitest'
import { resolveDragMove } from './dnd'

describe('resolveDragMove', () => {
  it('resolves a move between two different slots', () => {
    expect(resolveDragMove({ active: { id: 'slot-a' }, over: { id: 'slot-b' } })).toEqual({
      sourceSlotId: 'slot-a',
      targetSlotId: 'slot-b',
    })
  })

  it('returns null when dropped outside any droppable', () => {
    expect(resolveDragMove({ active: { id: 'slot-a' }, over: null })).toBeNull()
  })

  it('returns null when dropped back on the same slot', () => {
    expect(resolveDragMove({ active: { id: 'slot-a' }, over: { id: 'slot-a' } })).toBeNull()
  })
})
