import { DndContext } from '@dnd-kit/core'
import { render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import type { BinderSlot } from '../../lib/spread-types'
import { Pocket } from './Pocket'

const baseSlot: BinderSlot = {
  slotId: 'slot-1',
  position: 0,
  card: {
    id: 'base1-4',
    name: 'Charizard',
    imageSmallUrl: 'https://images.pokemontcg.io/base1/4.png',
    imageLargeUrl: null,
    setId: 'base1',
    setName: 'Base',
    number: '4',
    rarity: 'Rare Holo',
  },
  variantTypeName: 'Normal',
  owned: true,
  quantity: null,
  condition: null,
  overlayTag: null,
}

function renderPocket(slot: BinderSlot, greyscaleEnabled: boolean) {
  return render(
    <DndContext onDragEnd={() => {}}>
      <Pocket slot={slot} greyscaleEnabled={greyscaleEnabled} overlaysEnabled={false} onOpen={vi.fn()} />
    </DndContext>,
  )
}

describe('Pocket owned/greyscale rendering', () => {
  it('renders owned cards in full colour even when greyscale is enabled', () => {
    renderPocket({ ...baseSlot, owned: true }, true)
    const img = screen.getByAltText('Charizard')
    expect(img.className).not.toMatch(/grayscale/)
  })

  it('renders not-owned cards in greyscale when the toggle is on', () => {
    renderPocket({ ...baseSlot, owned: false }, true)
    const img = screen.getByAltText('Charizard')
    expect(img.className).toMatch(/grayscale/)
  })

  it('renders not-owned cards in full colour when the greyscale toggle is off', () => {
    renderPocket({ ...baseSlot, owned: false }, false)
    const img = screen.getByAltText('Charizard')
    expect(img.className).not.toMatch(/grayscale/)
  })

  it('renders an empty slot as an add affordance', () => {
    renderPocket({ ...baseSlot, card: null, owned: false }, true)
    expect(screen.getByLabelText(/empty slot/i)).toBeInTheDocument()
  })
})
