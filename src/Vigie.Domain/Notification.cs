namespace Vigie.Domain;

public sealed class Notification
{
    private Notification()
    {
        Type = string.Empty;
        Title = string.Empty;
        Body = string.Empty;
    }

    private Notification(Guid id, Guid organizationId, Guid recipientEmployeeId, string type, string title, string body, string? actionUrl, DateTimeOffset createdAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        RecipientEmployeeId = recipientEmployeeId;
        Type = type;
        Title = title;
        Body = body;
        ActionUrl = actionUrl;
        CreatedAtUtc = createdAtUtc.ToUniversalTime();
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid RecipientEmployeeId { get; private set; }
    public string Type { get; private set; }
    public string Title { get; private set; }
    public string Body { get; private set; }
    public string? ActionUrl { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? ReadAtUtc { get; private set; }
    public bool IsRead => ReadAtUtc.HasValue;

    public static Notification Create(Guid id, Guid organizationId, Guid recipientEmployeeId, string type, string title, string body, DateTimeOffset createdAtUtc, string? actionUrl = null)
    {
        if (id == Guid.Empty || organizationId == Guid.Empty || recipientEmployeeId == Guid.Empty)
            throw new DomainException("Les identifiants de la notification sont obligatoires.");
        if (string.IsNullOrWhiteSpace(type) || type.Trim().Length > 40)
            throw new DomainException("Le type de notification est invalide.");
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > 160)
            throw new DomainException("Le titre de notification est invalide.");
        if (string.IsNullOrWhiteSpace(body) || body.Trim().Length > 1000)
            throw new DomainException("Le contenu de notification est invalide.");
        if (actionUrl is not null && actionUrl.Length > 240)
            throw new DomainException("Le lien de notification est trop long.");
        return new Notification(id, organizationId, recipientEmployeeId, type.Trim(), title.Trim(), body.Trim(), actionUrl?.Trim(), createdAtUtc);
    }

    public void MarkRead(DateTimeOffset now)
    {
        ReadAtUtc ??= now.ToUniversalTime();
    }
}
