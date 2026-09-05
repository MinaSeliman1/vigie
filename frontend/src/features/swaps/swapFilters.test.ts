import { describe, expect, it } from 'vitest'
import { filterSwaps, type SwapFilter, type SwapSummary } from './swapFilters'

const swaps: SwapSummary[] = [
  { id: 'pending', status: 'En attente' },
  { id: 'approved', status: 'Approuvé' },
  { id: 'rejected', status: 'Refusé' },
]

describe('filterSwaps', () => {
  it.each<[SwapFilter, string[]]>([
    ['all', ['pending', 'approved', 'rejected']],
    ['pending', ['pending']],
    ['processed', ['approved', 'rejected']],
  ])('returns the %s collection', (filter, expectedIds) => {
    expect(filterSwaps(swaps, filter).map((swap) => swap.id)).toEqual(expectedIds)
  })

  it('does not mutate the source collection', () => {
    const source = [...swaps]
    filterSwaps(source, 'pending')
    expect(source).toEqual(swaps)
  })
})
