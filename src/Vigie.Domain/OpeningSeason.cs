namespace Vigie.Domain;

public readonly record struct OpeningSeason
{
    public static OpeningSeason AllYear => new(1, 1, 12, 31);

    public OpeningSeason(int startMonth, int startDay, int endMonth, int endDay)
    {
        if (!IsValidDate(2024, startMonth, startDay) || !IsValidDate(2024, endMonth, endDay))
            throw new DomainException("Les bornes de la saison sont invalides.");
        StartMonth = startMonth;
        StartDay = startDay;
        EndMonth = endMonth;
        EndDay = endDay;
    }

    public int StartMonth { get; }
    public int StartDay { get; }
    public int EndMonth { get; }
    public int EndDay { get; }

    public bool Contains(DateTime localDate)
    {
        var current = localDate.Month * 100 + localDate.Day;
        var start = StartMonth * 100 + StartDay;
        var end = EndMonth * 100 + EndDay;
        return start <= end ? current >= start && current <= end : current >= start || current <= end;
    }

    private static bool IsValidDate(int year, int month, int day)
        => month is >= 1 and <= 12 && day >= 1 && day <= DateTime.DaysInMonth(year, month);
}
