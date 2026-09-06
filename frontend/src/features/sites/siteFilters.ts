import type { SiteResponse } from '../../api/types'

export type SiteTypeFilter = 'all' | 'Indoor' | 'Outdoor'
export type SiteFilters = { query: string; type: SiteTypeFilter }

export function filterSites(sites: SiteResponse[], filters: SiteFilters) {
  const query = filters.query.trim().toLocaleLowerCase('fr-CA')
  return sites.filter((site) => {
    const matchesType = filters.type === 'all' || site.type === filters.type
    const haystack = `${site.name} ${site.address ?? ''} ${site.neighborhood ?? ''}`.toLocaleLowerCase('fr-CA')
    return matchesType && (!query || haystack.includes(query))
  })
}
