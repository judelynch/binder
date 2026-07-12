export interface CapResult {
  ids: string[]
  wasCapped: boolean
  totalAvailable: number
}

/** Caps a result-id list at `cap`, reporting whether truncation happened, for the select-all-results flow. */
export function capSelection(ids: string[], cap: number): CapResult {
  return {
    ids: ids.slice(0, cap),
    wasCapped: ids.length > cap,
    totalAvailable: ids.length,
  }
}

export function toggleSelection(selected: ReadonlySet<string>, id: string): Set<string> {
  const next = new Set(selected)
  if (next.has(id)) {
    next.delete(id)
  } else {
    next.add(id)
  }
  return next
}
