namespace Vigie.Domain;

public enum InvitationStatus
{
    Pending,
    Accepted,
    Expired
}

public sealed class Invitation
{
    private Invitation() { Email = string.Empty; Name = string.Empty; TokenHash = string.Empty; }

    private Invitation(Guid id, Guid organizationId, string email, string name, EmployeeRole role, string tokenHash, DateTimeOffset createdAtUtc, DateTimeOffset expiresAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        Email = email;
        Name = name;
        Role = role;
        TokenHash = tokenHash;
        CreatedAtUtc = createdAtUtc.ToUniversalTime();
        ExpiresAtUtc = expiresAtUtc.ToUniversalTime();
        Status = InvitationStatus.Pending;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string Email { get; private set; }
    public string Name { get; private set; }
    public EmployeeRole Role { get; private set; }
    public string TokenHash { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? AcceptedAtUtc { get; private set; }
    public InvitationStatus Status { get; private set; }

    public static Invitation Create(Guid id, Guid organizationId, string email, string name, EmployeeRole role, string tokenHash, DateTimeOffset createdAtUtc, TimeSpan lifetime)
    {
        if (id == Guid.Empty || organizationId == Guid.Empty) throw new DomainException("Les identifiants de l'invitation sont obligatoires.");
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@', StringComparison.Ordinal)) throw new DomainException("Le courriel de l'invitation est invalide.");
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Le nom de l'invité est obligatoire.");
        if (role is not (EmployeeRole.Lifeguard or EmployeeRole.Coordinator)) throw new DomainException("Le rôle de l'invitation est invalide.");
        if (string.IsNullOrWhiteSpace(tokenHash)) throw new DomainException("Le jeton de l'invitation est obligatoire.");
        if (lifetime <= TimeSpan.Zero || lifetime > TimeSpan.FromDays(30)) throw new DomainException("La durée de l'invitation est invalide.");
        var created = createdAtUtc.ToUniversalTime();
        return new Invitation(id, organizationId, email.Trim().ToLowerInvariant(), name.Trim(), role, tokenHash.Trim(), created, created.Add(lifetime));
    }

    public bool IsPending(DateTimeOffset now)
    {
        if (Status == InvitationStatus.Pending && now >= ExpiresAtUtc) Status = InvitationStatus.Expired;
        return Status == InvitationStatus.Pending;
    }

    public void Accept(DateTimeOffset now)
    {
        if (!IsPending(now)) throw new DomainException("Cette invitation n'est plus valide.");
        Status = InvitationStatus.Accepted;
        AcceptedAtUtc = now.ToUniversalTime();
    }
}
