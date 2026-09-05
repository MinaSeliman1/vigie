export type SwapFilter = 'all' | 'pending' | 'processed'
export type SwapSummary = { id: string; status: 'En attente' | 'Approuvé' | 'Refusé' }

export function filterSwaps<T extends SwapSummary>(swaps: T[], filter: SwapFilter) {
  if (filter === 'pending') return swaps.filter((swap) => swap.status === 'En attente')
  if (filter === 'processed') return swaps.filter((swap) => swap.status !== 'En attente')
  return swaps
}
