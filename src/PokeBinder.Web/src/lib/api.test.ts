import { describe, expect, it } from 'vitest'
import { api } from './api'

describe('api params serialization', () => {
  it('serializes array params as repeated plain keys, not bracket notation', () => {
    // ASP.NET Core's default model binding for a string[] query param only recognizes
    // repeated keys (subtypes=A&subtypes=B) — axios's own default uses subtypes[]=A&subtypes[]=B,
    // which silently binds to an empty array server-side and makes every multi-value filter a no-op.
    const url = api.getUri({ url: '/cards/search', params: { subtypes: ['Basic', 'Stage 2'], supertype: 'Pokémon' } })
    expect(url).toContain('subtypes=Basic')
    expect(url).toContain('subtypes=Stage')
    expect(url).not.toContain('subtypes%5B%5D')
    expect(url).not.toContain('subtypes[]')
  })
})
