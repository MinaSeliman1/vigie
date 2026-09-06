namespace Vigie.Domain;

public sealed class OrganizationMembership
{
    private OrganizationMembership()
    {
    }

    private OrganizationMembership(Guid id, Guid employeeId, Guid organizationId, EmployeeRole role, Guid? siteId, Guid? sectorId)
    {
        Id = id;
        EmployeeId = employeeId;
        OrganizationId = organizationId;
        Role = role;
        SiteId = siteId;
        SectorId = sectorId;
        IsActive = true;
        Version = 1;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid EmployeeId { get; private set; }
    public Guid OrganizationId { get; private set; }
    public EmployeeRole Role { get; private set; }
    public Guid? SiteId { get; private set; }
    public Guid? SectorId { get; private set; }
    public bool IsActive { get; private set; }
    public int Version { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static OrganizationMembership Create(Guid id, Guid employeeId, Guid organizationId, EmployeeRole role, Guid? siteId, Guid? sectorId)
    {
        ValidateIdentity(id, employeeId, organizationId);
        ValidateScope(role, siteId, sectorId);
        return new OrganizationMembership(id, employeeId, organizationId, role, siteId, sectorId);
    }

    public void Activate()
    {
        if (IsActive) return;
        IsActive = true;
        Touch();
    }

    public void Deactivate()
    {
        if (!IsActive) return;
        IsActive = false;
        Touch();
    }

    public void ChangeScope(Guid? siteId, Guid? sectorId)
    {
        ValidateScope(Role, siteId, sectorId);
        if (SiteId == siteId && SectorId == sectorId) return;
        SiteId = siteId;
        SectorId = sectorId;
        Touch();
    }

    public void ChangeRole(EmployeeRole role)
    {
        ValidateScope(role, SiteId, SectorId);
        if (Role == role) return;
        Role = role;
        Touch();
    }

    public void ChangeRoleAndScope(EmployeeRole role, Guid? siteId, Guid? sectorId)
    {
        ValidateScope(role, siteId, sectorId);
        if (Role == role && SiteId == siteId && SectorId == sectorId) return;

        Role = role;
        SiteId = siteId;
        SectorId = sectorId;
        Touch();
    }

    private static void ValidateIdentity(Guid id, Guid employeeId, Guid organizationId)
    {
        if (id == Guid.Empty) throw new DomainException("L'identifiant du membership est obligatoire.");
        if (employeeId == Guid.Empty) throw new DomainException("L'employé du membership est obligatoire.");
        if (organizationId == Guid.Empty) throw new DomainException("L'organisation du membership est obligatoire.");
    }

    private static void ValidateScope(EmployeeRole role, Guid? siteId, Guid? sectorId)
    {
        if (siteId == Guid.Empty) throw new DomainException("L'identifiant du site est invalide.");
        if (sectorId == Guid.Empty) throw new DomainException("L'identifiant du secteur est invalide.");

        switch (role)
        {
            case EmployeeRole.Lifeguard:
                if (!siteId.HasValue) throw new DomainException("Un sauveteur doit être rattaché à un site (piscine).");
                if (sectorId.HasValue) throw new DomainException("Un sauveteur ne peut pas être limité à un secteur.");
                break;
            case EmployeeRole.PoolChief:
                if (!siteId.HasValue) throw new DomainException("Un chef de piscine doit être rattaché à un site (piscine).");
                if (sectorId.HasValue) throw new DomainException("Un chef de piscine ne peut pas être limité à un secteur.");
                break;
            case EmployeeRole.SectorManager:
                if (!sectorId.HasValue) throw new DomainException("Un chargé de secteur doit être rattaché à un secteur.");
                if (siteId.HasValue) throw new DomainException("Un chargé de secteur est porté par son secteur.");
                break;
            case EmployeeRole.AquaticDirector:
                if (siteId.HasValue || sectorId.HasValue) throw new DomainException("La Régie aquatique doit avoir une portée organisationnelle.");
                break;
            case EmployeeRole.Coordinator:
                // Compatibilité : les anciennes données ne portent pas toujours un site.
                break;
            default:
                throw new DomainException("Le rôle du membership est invalide.");
        }
    }

    private void Touch()
    {
        if (Version == int.MaxValue) Version = 1;
        else Version++;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
