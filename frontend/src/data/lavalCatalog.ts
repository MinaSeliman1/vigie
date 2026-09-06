import type { SiteResponse } from '../api/types'

const allYear = { startMonth: 1, startDay: 1, endMonth: 12, endDay: 31 }
const summer = { startMonth: 6, startDay: 13, endMonth: 9, endDay: 1 }
type CatalogRow = [string, string, 'Indoor' | 'Outdoor', string, string, SiteResponse['openingSeason']]

/**
 * Jeu de démonstration hors ligne. Les mêmes installations sont chargées par
 * l'API lorsque le catalogue Laval est initialisé; garder ce miroir permet à
 * un recruteur de parcourir le produit même si le service gratuit est en veille.
 */
export const lavalMunicipalSites: SiteResponse[] = ([
  ['001', 'Piscine Val-des-Arbres', 'Indoor', '1555, boulevard Saint-Martin Est', 'Vimont', allYear],
  ['002', 'Centre du Sablon', 'Indoor', '755, chemin du Sablon', 'Chomedey', allYear],
  ['003', 'Piscine Vanier', 'Indoor', '3995, boulevard Lévesque Est', 'Saint-Vincent-de-Paul', allYear],
  ['004', 'Piscine Poly-Jeunesse', 'Indoor', '3578, boulevard Sainte-Rose', 'Fabreville', allYear],
  ['005', 'Piscine Honoré-Mercier', 'Indoor', '2465, rue Honoré-Mercier', 'Sainte-Rose', allYear],
  ['006', 'Piscine du Centre sportif Josée-Faucher', 'Indoor', '125A, boulevard des Prairies', 'Laval-des-Rapides', allYear],
  ['007', 'Complexe aquatique', 'Indoor', '2205, avenue Terry-Fox', 'Chomedey', allYear],
  ['008', 'Piscine du Moulin', 'Outdoor', '1125, montée du Moulin', 'Saint-François', summer],
  ['009', 'Piscine Jacques-Bourdon', 'Outdoor', '55, croissant De Callières', 'Duvernay', summer],
  ['010', 'Piscine extérieure Saint-Vincent', 'Outdoor', '901, avenue du Parc', 'Saint-Vincent-de-Paul', summer],
  ['011', 'Piscine Bon-Pasteur', 'Outdoor', '70, boulevard du Bon-Pasteur', 'Laval-des-Rapides', summer],
  ['012', 'Piscine Chénier', 'Outdoor', '580, rue des Alouettes', 'Pont-Viau', summer],
  ['013', 'Piscine Émile', 'Outdoor', '55, boulevard Cartier Ouest', 'Laval-des-Rapides', summer],
  ['014', 'Piscine Saint-Claude', 'Outdoor', '99, 7e Rue', 'Laval-des-Rapides', summer],
  ['015', 'Piscine Wilfrid-Pelletier', 'Outdoor', '1865, boulevard Tessier', 'Chomedey', summer],
  ['016', 'Piscine Berthiaume-Du Tremblay', 'Outdoor', '4250, boulevard Lévesque Ouest', 'Chomedey', summer],
  ['017', 'Piscine Montcalm', 'Outdoor', '755, rue Parkway', 'Chomedey', summer],
  ['018', 'Piscine Pie-X', 'Outdoor', '1175, rue du Val-Martin', 'Chomedey', summer],
  ['019', 'Piscine Couvrette', 'Outdoor', '665, rue des Jardins-Sainte-Dorothée', 'Sainte-Dorothée', summer],
  ['020', 'Piscine des Chênes', 'Outdoor', '355, rue les Érables', 'Laval-sur-le-Lac', summer],
  ['021', 'Piscine Jolibourg', 'Outdoor', '1350, rue du Relais', 'Sainte-Dorothée', summer],
  ['022', 'Piscine Raymond', 'Outdoor', '6460, 29e Avenue', 'Laval-Ouest', summer],
  ['023', 'Piscine Roi-du-Nord', 'Outdoor', '222, boulevard du Roi-du-Nord', 'Sainte-Rose', summer],
  ['024', 'Piscine Sacré-Coeur', 'Outdoor', '3165, rue Esther', 'Fabreville', summer],
  ['025', 'Piscine des Saules', 'Outdoor', '100, rue Saint-Saëns Ouest', 'Auteuil', summer],
  ['026', 'Piscine Paradis', 'Outdoor', '2220, rue Marc', 'Vimont', summer],
  ['027', 'Piscine Prévost', 'Outdoor', '110, rue de Toulouse', 'Laval-Ouest', summer],
] as CatalogRow[]).map(([code, name, type, address, neighborhood, openingSeason]) => ({
  id: `90000000-0000-0000-0000-000000000${code}`,
  name,
  type,
  timeZoneId: 'Eastern Standard Time',
  openingSeason,
  address,
  neighborhood,
  isMunicipal: true,
}))

const nordNeighborhoods = new Set(['Vimont', 'Fabreville', 'Sainte-Rose', 'Auteuil'])

export function demoSitesForRole(role: string, localSites: SiteResponse[]) {
  if (role === 'AquaticDirector') return lavalMunicipalSites
  if (role === 'SectorManager') return lavalMunicipalSites.filter((site) => nordNeighborhoods.has(site.neighborhood ?? ''))
  return localSites
}
