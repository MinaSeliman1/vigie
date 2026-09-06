namespace Vigie.Domain;

public enum ShiftStatus
{
    Open,
    Filled,
    Cancelled
}

public sealed class Shift
{
    private Shift() { }

    private Shift(Guid id, Guid siteId, DateTimeOffset startUtc, DateTimeOffset endUtc, int requiredLifeguards)
    {
        Id = id;
        SiteId = siteId;
        StartUtc = startUtc.ToUniversalTime();
        EndUtc = endUtc.ToUniversalTime();
        RequiredLifeguards = requiredLifeguards;
        Status = ShiftStatus.Open;
    }

    public Guid Id { get; private set; }
    public Guid SiteId { get; private set; }
    public DateTimeOffset StartUtc { get; private set; }
    public DateTimeOffset EndUtc { get; private set; }
    public int RequiredLifeguards { get; private set; }
    public ShiftStatus Status { get; private set; }
    public TimeSpan Duration => EndUtc - StartUtc;

    public void Reschedule(DateTimeOffset startUtc, DateTimeOffset endUtc, int requiredLifeguards)
    {
        if (Status == ShiftStatus.Cancelled) throw new DomainException("Un quart annulé ne peut pas être modifié.");
        ValidateSchedule(startUtc, endUtc, requiredLifeguards);
        StartUtc = startUtc.ToUniversalTime();
        EndUtc = endUtc.ToUniversalTime();
        RequiredLifeguards = requiredLifeguards;
    }

    public void Cancel()
    {
        if (Status == ShiftStatus.Cancelled) return;
        Status = ShiftStatus.Cancelled;
    }

    public static Shift Create(Guid id, Guid siteId, DateTimeOffset startUtc, DateTimeOffset endUtc, int requiredLifeguards)
    {
        if (id == Guid.Empty || siteId == Guid.Empty) throw new DomainException("Les identifiants du quart et du site sont obligatoires.");
        ValidateSchedule(startUtc, endUtc, requiredLifeguards);
        return new Shift(id, siteId, startUtc, endUtc, requiredLifeguards);
    }

    private static void ValidateSchedule(DateTimeOffset startUtc, DateTimeOffset endUtc, int requiredLifeguards)
    {
        if (endUtc <= startUtc) throw new DomainException("La fin du quart doit être après son début.");
        if (requiredLifeguards is < 1 or > 50) throw new DomainException("Le nombre de sauveteurs requis est invalide.");
    }
}
