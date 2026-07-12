import { describe, expect, it } from 'vitest'
import {
  clampPanelIndex,
  clampSpreadIndex,
  formatPanelLabel,
  formatSpreadLabel,
  locationToPanelIndex,
  panelIndexToLocation,
  totalPanels,
} from './panel-nav'
import type { SpreadPanel } from './spread-types'

const cover = (): SpreadPanel => ({ type: 'cover', pageNumber: null, slots: null })
const page = (n: number): SpreadPanel => ({ type: 'page', pageNumber: n, slots: [] })

describe('panelIndexToLocation / locationToPanelIndex', () => {
  it('round-trips for the first few panels', () => {
    expect(panelIndexToLocation(0)).toEqual({ spreadIndex: 0, side: 'left' })
    expect(panelIndexToLocation(1)).toEqual({ spreadIndex: 0, side: 'right' })
    expect(panelIndexToLocation(2)).toEqual({ spreadIndex: 1, side: 'left' })
    expect(panelIndexToLocation(3)).toEqual({ spreadIndex: 1, side: 'right' })
  })

  it('is a true inverse of locationToPanelIndex', () => {
    for (let i = 0; i < 20; i++) {
      expect(locationToPanelIndex(panelIndexToLocation(i))).toBe(i)
    }
  })
})

describe('totalPanels / clamping', () => {
  it('doubles the spread count', () => {
    expect(totalPanels(3)).toBe(6)
    expect(totalPanels(0)).toBe(0)
  })

  it('clampSpreadIndex stays within [0, totalSpreads-1]', () => {
    expect(clampSpreadIndex(-5, 3)).toBe(0)
    expect(clampSpreadIndex(10, 3)).toBe(2)
    expect(clampSpreadIndex(1, 3)).toBe(1)
  })

  it('clampPanelIndex stays within [0, totalPanels-1]', () => {
    expect(clampPanelIndex(-5, 3)).toBe(0)
    expect(clampPanelIndex(99, 3)).toBe(5)
    expect(clampPanelIndex(2, 3)).toBe(2)
  })
})

describe('formatSpreadLabel — 4-page worked example', () => {
  it('first spread: front cover + page 1', () => {
    expect(formatSpreadLabel(cover(), page(1), 4)).toBe('Front Cover · Page 1 of 4')
  })

  it('middle spread: pages 2-3', () => {
    expect(formatSpreadLabel(page(2), page(3), 4)).toBe('Pages 2–3 of 4')
  })

  it('last spread: page 4 + back cover', () => {
    expect(formatSpreadLabel(page(4), cover(), 4)).toBe('Page 4 · Back Cover of 4')
  })
})

describe('formatPanelLabel — mobile single-panel', () => {
  it('labels a real page by number', () => {
    expect(formatPanelLabel(page(2), 4, 'left')).toBe('Page 2 of 4')
  })

  it('labels the left cover as Front Cover', () => {
    expect(formatPanelLabel(cover(), 4, 'left')).toBe('Front Cover')
  })

  it('labels the right cover as Back Cover', () => {
    expect(formatPanelLabel(cover(), 4, 'right')).toBe('Back Cover')
  })
})
