import { describe, expect, it } from 'vitest'
import { filterSites, type SiteFilters } from './siteFilters'
import type { SiteResponse } from '../../api/types'

const sites: SiteResponse[] = [
  { id: 'indoor', name: 'Centre du Sablon', type: 'Indoor', timeZoneId: 'Eastern Standard Time', openingSeason: { startMonth: 1, startDay: 1, endMonth: 12, endDay: 31 }, address: '755, chemin du Sablon', neighborhood: 'Chomedey', isMunicipal: true },
  { id: 'outdoor', name: 'Piscine Paradis', type: 'Outdoor', timeZoneId: 'Eastern Standard Time', openingSeason: { startMonth: 6, startDay: 13, endMonth: 9, endDay: 1 }, address: '2220, rue Marc', neighborhood: 'Vimont', isMunicipal: true },
]

describe('filterSites', () => {
  it.each<[SiteFilters, string[]]>([
    [{ query: '', type: 'all' }, ['indoor', 'outdoor']],
    [{ query: 'vimont', type: 'all' }, ['outdoor']],
    [{ query: 'piscine', type: 'Indoor' }, []],
    [{ query: 'sablon', type: 'Indoor' }, ['indoor']],
  ])('filters by query and type', (filters, expectedIds) => {
    expect(filterSites(sites, filters).map((site) => site.id)).toEqual(expectedIds)
  })

  it('keeps the source collection unchanged', () => {
    const source = [...sites]
    filterSites(source, { query: 'chomedey', type: 'all' })
    expect(source).toEqual(sites)
  })
})
