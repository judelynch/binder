import type { SpreadPanel } from './spread-types'

export type PanelSide = 'left' | 'right'

export interface PanelLocation {
  spreadIndex: number
  side: PanelSide
}

/** Total individually-addressable panels across a binder (covers included) — used for mobile's one-panel-at-a-time navigation. */
export function totalPanels(totalSpreads: number): number {
  return totalSpreads * 2
}

export function panelIndexToLocation(panelIndex: number): PanelLocation {
  return {
    spreadIndex: Math.floor(panelIndex / 2),
    side: panelIndex % 2 === 0 ? 'left' : 'right',
  }
}

export function locationToPanelIndex(location: PanelLocation): number {
  return location.spreadIndex * 2 + (location.side === 'left' ? 0 : 1)
}

export function clampSpreadIndex(spreadIndex: number, totalSpreads: number): number {
  if (totalSpreads <= 0) return 0
  return Math.max(0, Math.min(totalSpreads - 1, spreadIndex))
}

export function clampPanelIndex(panelIndex: number, totalSpreads: number): number {
  const max = totalPanels(totalSpreads) - 1
  return Math.max(0, Math.min(max, panelIndex))
}

/** Human label for the desktop two-panel toolbar, e.g. "Pages 2–3 of 4", "Front Cover · Page 1 of 4". */
export function formatSpreadLabel(left: SpreadPanel, right: SpreadPanel, totalPages: number): string {
  if (left.type === 'page' && right.type === 'page') {
    return `Pages ${left.pageNumber}–${right.pageNumber} of ${totalPages}`
  }
  if (left.type === 'cover' && right.type === 'page') {
    return `Front Cover · Page ${right.pageNumber} of ${totalPages}`
  }
  if (left.type === 'page' && right.type === 'cover') {
    return `Page ${left.pageNumber} · Back Cover of ${totalPages}`
  }
  return totalPages === 0 ? 'Empty binder' : `Cover of ${totalPages}`
}

/** Human label for the mobile single-panel toolbar, e.g. "Page 2 of 4", "Front Cover". */
export function formatPanelLabel(panel: SpreadPanel, totalPages: number, side: PanelSide): string {
  if (panel.type === 'page') {
    return `Page ${panel.pageNumber} of ${totalPages}`
  }
  return side === 'left' ? 'Front Cover' : 'Back Cover'
}
