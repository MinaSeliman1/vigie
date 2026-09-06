namespace Vigie.Domain;

public sealed class AuditEntry
{
    private AuditEntry() { Action = string.Empty; EntityType = string.Empty; }

    private AuditEntry(Guid id, Guid organizationId, Guid? actorId, string action, string entityType, Guid? entityId, string? details, DateTimeOffset createdAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        ActorId = actorId;
        Action = action;
        EntityType = entityType;
        EntityId = entityId;
        Details = details;
        CreatedAtUtc = createdAtUtc.ToUniversalTime();
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid? ActorId { get; private set; }
    public string Action { get; private set; }
    public string EntityType { get; private set; }
    public Guid? EntityId { get; private set; }
    public string? Details { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static AuditEntry Create(Guid id, Guid organizationId, Guid? actorId, string action, string entityType, Guid? entityId, string? details, DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty || organizationId == Guid.Empty) throw new DomainException("Les identifiants de l'audit sont obligatoires.");
        if (string.IsNullOrWhiteSpace(action) || action.Length > 80) throw new DomainException("L'action d'audit est invalide.");
        if (string.IsNullOrWhiteSpace(entityType) || entityType.Length > 80) throw new DomainException("Le type d'objet d'audit est invalide.");
        if (details?.Length > 2000) throw new DomainException("Le détail d'audit est trop long.");
        return new AuditEntry(id, organizationId, actorId, action.Trim(), entityType.Trim(), entityId, details?.Trim(), createdAtUtc);
    }
}
