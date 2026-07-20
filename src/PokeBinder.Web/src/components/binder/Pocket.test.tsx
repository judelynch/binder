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
  cardVariantId: 'variant-1',
  variantTypeName: 'Normal',
  owned: true,
  quantity: null,
  condition: null,
  overlayTag: null,
}

function renderPocket(slot: BinderSlot, greyscaleEnabled: boolean, costToBuyGbp?: number | null) {
  return render(
    <DndContext onDragEnd={() => {}}>
      <Pocket
        slot={slot}
        binderColourHex="#8B5FA6"
        greyscaleEnabled={greyscaleEnabled}
        overlaysEnabled={false}
        onOpen={vi.fn()}
        costToBuyGbp={costToBuyGbp}
      />
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

describe('Pocket cost-to-buy badge', () => {
  it('shows a cost-to-buy badge on a filled, unowned slot with a known price', () => {
    renderPocket({ ...baseSlot, owned: false }, false, 8.5)
    expect(screen.getByText('£8.50')).toBeInTheDocument()
  })

  it('omits the badge when the slot is owned, even with a known price', () => {
    renderPocket({ ...baseSlot, owned: true }, false, 8.5)
    expect(screen.queryByText('£8.50')).not.toBeInTheDocument()
  })

  it('omits the badge when no price is known', () => {
    renderPocket({ ...baseSlot, owned: false }, false, null)
    expect(screen.queryByText(/£/)).not.toBeInTheDocument()
  })
})

describe('Pocket suggestion lightbulb', () => {
  it('shows the lightbulb on an empty slot with suggestions', () => {
    render(
      <DndContext onDragEnd={() => {}}>
        <Pocket
          slot={{ ...baseSlot, card: null, cardVariantId: null, owned: false }}
          binderColourHex="#8B5FA6"
          greyscaleEnabled={false}
          overlaysEnabled={false}
          onOpen={vi.fn()}
          hasSuggestions
          onOpenSuggestions={vi.fn()}
        />
      </DndContext>,
    )
    expect(screen.getByLabelText(/suggested card available/i)).toBeInTheDocument()
  })

  it('never shows the lightbulb on a filled slot, even if hasSuggestions is true', () => {
    render(
      <DndContext onDragEnd={() => {}}>
        <Pocket
          slot={baseSlot}
          binderColourHex="#8B5FA6"
          greyscaleEnabled={false}
          overlaysEnabled={false}
          onOpen={vi.fn()}
          hasSuggestions
          onOpenSuggestions={vi.fn()}
        />
      </DndContext>,
    )
    expect(screen.queryByLabelText(/suggested card available/i)).not.toBeInTheDocument()
  })

  it('omits the lightbulb on an empty slot with no suggestions', () => {
    render(
      <DndContext onDragEnd={() => {}}>
        <Pocket
          slot={{ ...baseSlot, card: null, cardVariantId: null, owned: false }}
          binderColourHex="#8B5FA6"
          greyscaleEnabled={false}
          overlaysEnabled={false}
          onOpen={vi.fn()}
          hasSuggestions={false}
          onOpenSuggestions={vi.fn()}
        />
      </DndContext>,
    )
    expect(screen.queryByLabelText(/suggested card available/i)).not.toBeInTheDocument()
  })
})
