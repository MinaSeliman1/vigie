namespace Vigie.Domain;

public sealed class Sector
{
    private Sector()
    {
        Name = string.Empty;
        Code = string.Empty;
    }

    private Sector(Guid id, Guid organizationId, string name, string code, bool isActive)
    {
        Id = id;
        OrganizationId = organizationId;
        Name = name;
        Code = code;
        IsActive = isActive;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string Name { get; private set; }
    public string Code { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static Sector Create(Guid id, Guid organizationId, string name, string code, bool isActive = true)
    {
        if (id == Guid.Empty) throw new DomainException("L'identifiant du secteur est obligatoire.");
        if (organizationId == Guid.Empty) throw new DomainException("L'organisation du secteur est obligatoire.");
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Le nom du secteur est obligatoire.");
        if (string.IsNullOrWhiteSpace(code)) throw new DomainException("Le code du secteur est obligatoire.");
        return new Sector(id, organizationId, name.Trim(), code.Trim().ToUpperInvariant(), isActive);
    }

    public void Rename(string name, string code)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Le nom du secteur est obligatoire.");
        if (string.IsNullOrWhiteSpace(code)) throw new DomainException("Le code du secteur est obligatoire.");
        Name = name.Trim();
        Code = code.Trim().ToUpperInvariant();
        Touch();
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

    private void Touch() => UpdatedAtUtc = DateTimeOffset.UtcNow;
}
