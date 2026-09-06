namespace Vigie.Domain;

public enum SiteType
{
    Indoor,
    Outdoor
}

public sealed class Site
{
    private Site() { Name = string.Empty; TimeZoneId = TimeZoneInfo.Utc.Id; OpeningSeason = OpeningSeason.AllYear; }

    private Site(Guid id, string name, string timeZoneId, OpeningSeason openingSeason, SiteType type)
    {
        Id = id;
        Name = name;
        TimeZoneId = timeZoneId;
        OpeningSeason = openingSeason;
        Type = type;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string Name { get; private set; }
    public string TimeZoneId { get; private set; }
    public OpeningSeason OpeningSeason { get; private set; }
    public SiteType Type { get; private set; }

    public static Site Create(Guid id, string name, string timeZoneId, OpeningSeason openingSeason, SiteType type = SiteType.Indoor, Guid? organizationId = null)
    {
        if (id == Guid.Empty) throw new DomainException("L'identifiant du site est obligatoire.");
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Le nom du site est obligatoire.");
        if (string.IsNullOrWhiteSpace(timeZoneId)) throw new DomainException("Le fuseau horaire du site est obligatoire.");
        var site = new Site(id, name.Trim(), timeZoneId.Trim(), openingSeason, type);
        if (organizationId.HasValue) site.SetOrganization(organizationId.Value);
        return site;
    }

    public void SetOrganization(Guid organizationId)
    {
        if (organizationId == Guid.Empty) throw new DomainException("L'organisation du site est obligatoire.");
        OrganizationId = organizationId;
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
