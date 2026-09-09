using Vigie.Domain;

namespace Vigie.Infrastructure;

/// <summary>
/// Prépare immédiatement l'espace d'une organisation municipale de Laval.
/// L'opération est idempotente afin de pouvoir être rejouée après une interruption
/// ou lors d'une migration sans créer de doublons.
/// </summary>
public static class LavalOrganizationProvisioner
{
    private static readonly Guid FirstAidCertificationTypeId = Guid.Parse("30000000-0000-0000-0000-000000000001");
    private static readonly Guid LifeguardCertificationTypeId = Guid.Parse("30000000-0000-0000-0000-000000000002");

    public static void ProvisionMunicipalCatalog(IVigieStore store, Organization organization)
    {
        if (!string.Equals(organization.OrganizationType, "municipal", StringComparison.OrdinalIgnoreCase)) return;

        var pools = LavalPoolCatalog.ForOrganization(organization.Id);
        var sites = store.Sites.Where(site => site.OrganizationId == organization.Id).ToList();
        var sectors = store.Sectors.Where(sector => sector.OrganizationId == organization.Id).ToList();
        var sectorsByCode = sectors.ToDictionary(sector => sector.Code, StringComparer.OrdinalIgnoreCase);

        var firstAid = GetOrCreateCertificationType(store, FirstAidCertificationTypeId, "Premiers soins");
        var lifeguard = GetOrCreateCertificationType(store, LifeguardCertificationTypeId, "Sauveteur national");
        var certificationTypeIds = new[] { firstAid.Id, lifeguard.Id };
        var links = store.SiteCertificationLinks.ToHashSet();

        foreach (var pool in pools)
        {
            if (!sectorsByCode.TryGetValue(pool.SectorCode, out var sector))
            {
                sector = Sector.Create(pool.SectorId, organization.Id, pool.SectorName, pool.SectorCode);
                store.AddSector(sector);
                sectors.Add(sector);
                sectorsByCode[sector.Code] = sector;
            }
            sector.Activate();

            var site = sites.SingleOrDefault(item => item.Id == pool.SiteId) ??
                sites.SingleOrDefault(item => string.Equals(item.Name, pool.Name, StringComparison.OrdinalIgnoreCase));
            if (site is null)
            {
                site = Site.Create(pool.SiteId, pool.Name, "Eastern Standard Time", pool.OpeningSeason, pool.Type, organization.Id, pool.Address, pool.Neighborhood, isMunicipal: true);
                store.AddSite(site);
                sites.Add(site);
            }
            else
            {
                site.SetCatalogMetadata(pool.Address, pool.Neighborhood, isMunicipal: true);
            }
            site.SetSector(sector.Id);

            foreach (var certificationTypeId in certificationTypeIds)
            {
                if (links.Add((site.Id, certificationTypeId))) store.AddCertificationTypeForSite(site.Id, certificationTypeId);
            }
        }
    }

    private static CertificationType GetOrCreateCertificationType(IVigieStore store, Guid id, string name)
    {
        var existing = store.CertificationTypes.SingleOrDefault(type =>
            type.Id == id || string.Equals(type.Name, name, StringComparison.OrdinalIgnoreCase));
        if (existing is not null) return existing;

        var created = CertificationType.Create(id, name, isRequired: true);
        store.AddCertificationType(created);
        return created;
    }
}
