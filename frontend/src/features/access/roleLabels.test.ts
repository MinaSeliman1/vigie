import { describe, expect, it } from 'vitest'
import { roleLabel, roleScopeLabel } from './roleLabels'

describe('roleLabels', () => {
  it('traduit les quatre rôles opérationnels en français', () => {
    expect(roleLabel('Lifeguard')).toBe('Sauveteur')
    expect(roleLabel('PoolChief')).toBe('Chef de piscine')
    expect(roleLabel('SectorManager')).toBe('Chargé de secteur')
    expect(roleLabel('AquaticDirector')).toBe('Régie aquatique')
  })

  it('décrit la portée d’un membre', () => {
    expect(roleScopeLabel('PoolChief', 'Piscine du Nord', null)).toBe('Piscine du Nord')
    expect(roleScopeLabel('SectorManager', null, 'Secteur Nord')).toBe('Secteur Nord')
    expect(roleScopeLabel('AquaticDirector', null, null)).toBe('Toutes les piscines')
  })
})
