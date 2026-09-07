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
    public string? OrganizationType { get; private set; }
    public string? PoolCount { get; private set; }
    public string? TeamSize { get; private set; }
    public string? PrimaryGoal { get; private set; }

    public static Organization Create(Guid id, string name, string slug, DateTimeOffset? createdAtUtc = null)
    {
        if (id == Guid.Empty) throw new DomainException("L'identifiant de l'organisation est obligatoire.");
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Le nom de l'organisation est obligatoire.");
        if (string.IsNullOrWhiteSpace(slug) || slug.Length > 80) throw new DomainException("Le slug de l'organisation est invalide.");
        if (slug.Any(character => !(char.IsLetterOrDigit(character) || character is '-' or '_')))
            throw new DomainException("Le slug de l'organisation contient des caractères invalides.");
        return new Organization(id, name.Trim(), slug.Trim().ToLowerInvariant(), createdAtUtc ?? DateTimeOffset.UtcNow);
    }

    public void SetOnboardingProfile(string? organizationType, string? poolCount, string? teamSize, string? primaryGoal)
    {
        OrganizationType = NormalizeChoice(organizationType, OrganizationOnboardingOptions.OrganizationTypes);
        PoolCount = NormalizeChoice(poolCount, OrganizationOnboardingOptions.PoolCounts);
        TeamSize = NormalizeChoice(teamSize, OrganizationOnboardingOptions.TeamSizes);
        PrimaryGoal = NormalizeChoice(primaryGoal, OrganizationOnboardingOptions.PrimaryGoals);
    }

    private static string? NormalizeChoice(string? value, IReadOnlySet<string> allowedValues)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim().ToLowerInvariant();
        if (!allowedValues.Contains(normalized)) throw new DomainException("Une option de profil est invalide.");
        return normalized;
    }
}

public static class OrganizationOnboardingOptions
{
    public static readonly IReadOnlySet<string> OrganizationTypes = new HashSet<string>(StringComparer.Ordinal)
    {
        "municipal", "private", "school", "hotel", "other"
    };

    public static readonly IReadOnlySet<string> PoolCounts = new HashSet<string>(StringComparer.Ordinal)
    {
        "one", "two-to-five", "six-to-ten", "more-than-ten", "unknown"
    };

    public static readonly IReadOnlySet<string> TeamSizes = new HashSet<string>(StringComparer.Ordinal)
    {
        "one-to-ten", "eleven-to-thirty", "thirty-one-to-one-hundred", "more-than-one-hundred", "unknown"
    };

    public static readonly IReadOnlySet<string> PrimaryGoals = new HashSet<string>(StringComparer.Ordinal)
    {
        "planning", "swaps", "certifications", "all"
    };
}
