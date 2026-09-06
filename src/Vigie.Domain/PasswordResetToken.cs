namespace Vigie.Domain;

public sealed class PasswordResetToken
{
    private PasswordResetToken() { }

    private PasswordResetToken(Guid id, Guid organizationId, Guid employeeId, string tokenHash, DateTimeOffset createdAtUtc, DateTimeOffset expiresAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        EmployeeId = employeeId;
        TokenHash = tokenHash;
        CreatedAtUtc = createdAtUtc.ToUniversalTime();
        ExpiresAtUtc = expiresAtUtc.ToUniversalTime();
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? UsedAtUtc { get; private set; }

    public bool IsUsable(DateTimeOffset nowUtc)
        => !UsedAtUtc.HasValue && ExpiresAtUtc > nowUtc.ToUniversalTime();

    public static PasswordResetToken Create(Guid id, Guid organizationId, Guid employeeId, string tokenHash, DateTimeOffset createdAtUtc, DateTimeOffset expiresAtUtc)
    {
        if (id == Guid.Empty || organizationId == Guid.Empty || employeeId == Guid.Empty)
            throw new DomainException("Les identifiants de récupération sont obligatoires.");
        if (string.IsNullOrWhiteSpace(tokenHash) || tokenHash.Trim().Length != 64)
            throw new DomainException("Le jeton de récupération est invalide.");
        if (expiresAtUtc <= createdAtUtc)
            throw new DomainException("L'expiration du jeton de récupération est invalide.");
        return new PasswordResetToken(id, organizationId, employeeId, tokenHash.Trim().ToLowerInvariant(), createdAtUtc, expiresAtUtc);
    }

    public void MarkUsed(DateTimeOffset nowUtc)
    {
        if (!IsUsable(nowUtc)) throw new DomainException("Le lien de récupération est expiré ou a déjà été utilisé.");
        UsedAtUtc = nowUtc.ToUniversalTime();
    }
}
