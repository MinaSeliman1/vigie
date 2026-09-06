import type { Role } from '../../api/types'

const labels: Record<Role, string> = {
  Lifeguard: 'Sauveteur',
  PoolChief: 'Chef de piscine',
  SectorManager: 'Chargé de secteur',
  AquaticDirector: 'Régie aquatique',
  Coordinator: 'Chef de piscine',
}

export function roleLabel(role: Role) {
  return labels[role] ?? role
}

export function roleScopeLabel(role: Role, siteName?: string | null, sectorName?: string | null) {
  if (role === 'AquaticDirector') return 'Toutes les piscines'
  if (role === 'SectorManager') return sectorName ?? 'Secteur non défini'
  return siteName ?? 'Piscine non définie'
}
