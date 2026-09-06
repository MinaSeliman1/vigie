namespace Vigie.Domain;

public sealed class Organization
{
    private Organization() { Name = string.Empty; Slug = string.Empty; }

    private Organization(Guid id, string name, string slug, DateTimeOffset createdAtUtc)
    {
        Id = id;
        Name = name;
        Slug = slug;
        CreatedAtUtc = createdAtUtc.ToUniversalTime();
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Slug { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static Organization Create(Guid id, string name, string slug, DateTimeOffset? createdAtUtc = null)
    {
        if (id == Guid.Empty) throw new DomainException("L'identifiant de l'organisation est obligatoire.");
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Le nom de l'organisation est obligatoire.");
        if (string.IsNullOrWhiteSpace(slug) || slug.Length > 80) throw new DomainException("Le slug de l'organisation est invalide.");
        if (slug.Any(character => !(char.IsLetterOrDigit(character) || character is '-' or '_')))
            throw new DomainException("Le slug de l'organisation contient des caractères invalides.");
        return new Organization(id, name.Trim(), slug.Trim().ToLowerInvariant(), createdAtUtc ?? DateTimeOffset.UtcNow);
    }
}
