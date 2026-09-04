namespace Vigie.Domain;

public enum SiteType
{
    Indoor,
    Outdoor
}

public sealed class Site
{
    private Site(Guid id, string name, string timeZoneId, OpeningSeason openingSeason, SiteType type)
    {
        Id = id;
        Name = name;
        TimeZoneId = timeZoneId;
        OpeningSeason = openingSeason;
        Type = type;
    }

    public Guid Id { get; }
    public string Name { get; private set; }
    public string TimeZoneId { get; private set; }
    public OpeningSeason OpeningSeason { get; private set; }
    public SiteType Type { get; private set; }

    public static Site Create(Guid id, string name, string timeZoneId, OpeningSeason openingSeason, SiteType type = SiteType.Indoor)
    {
        if (id == Guid.Empty) throw new DomainException("L'identifiant du site est obligatoire.");
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Le nom du site est obligatoire.");
        if (string.IsNullOrWhiteSpace(timeZoneId)) throw new DomainException("Le fuseau horaire du site est obligatoire.");
        return new Site(id, name.Trim(), timeZoneId.Trim(), openingSeason, type);
    }

    public TimeZoneInfo TimeZone
    {
        get
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId); }
            catch (TimeZoneNotFoundException) { return TimeZoneInfo.Utc; }
            catch (InvalidTimeZoneException) { return TimeZoneInfo.Utc; }
        }
    }

    public bool IsOpen(DateTimeOffset startUtc, DateTimeOffset endUtc)
    {
        var localStart = TimeZoneInfo.ConvertTime(startUtc, TimeZone);
        var localEnd = TimeZoneInfo.ConvertTime(endUtc, TimeZone);
        for (var day = localStart.Date; day <= localEnd.Date; day = day.AddDays(1))
            if (!OpeningSeason.Contains(day)) return false;
        return true;
    }
}
