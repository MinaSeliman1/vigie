namespace Vigie.Domain;

public static class RuleCodes
{
    public const string CertificationExpired = "CERTIFICATION_EXPIRED";
    public const string ShiftOverlap = "SHIFT_OVERLAP";
    public const string WeeklyQuotaExceeded = "WEEKLY_QUOTA_EXCEEDED";
    public const string SiteClosed = "SITE_CLOSED";
    public const string ShiftCancelled = "SHIFT_CANCELLED";
}

public sealed record RuleViolation(string Code, string Message, IReadOnlyDictionary<string, string>? Details = null);

public sealed record AssignmentCandidate(Employee Employee, Shift Shift);

public sealed record ScheduledAssignment(Guid EmployeeId, Shift Shift);

public sealed record AssignmentContext(
    Site Site,
    IReadOnlyCollection<CertificationType> RequiredCertificationTypes,
    IReadOnlyCollection<Certification> Certifications,
    IReadOnlyCollection<ScheduledAssignment> ExistingAssignments);

public interface ICheckAssignmentRule
{
    RuleViolation? Check(AssignmentCandidate candidate, AssignmentContext context);
}

public sealed class CertificationRule : ICheckAssignmentRule
{
    public RuleViolation? Check(AssignmentCandidate candidate, AssignmentContext context)
    {
        var localEndDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(candidate.Shift.EndUtc, context.Site.TimeZone).DateTime);
        foreach (var required in context.RequiredCertificationTypes.Where(t => t.IsRequired))
        {
            var valid = context.Certifications.Any(c => c.EmployeeId == candidate.Employee.Id && c.CertificationTypeId == required.Id && c.ExpiresOn >= localEndDate);
            if (!valid)
                return new RuleViolation(RuleCodes.CertificationExpired, $"La certification « {required.Name} » est absente ou échue pour ce quart.", new Dictionary<string, string> { ["certificationTypeId"] = required.Id.ToString() });
        }
        return null;
    }
}

public sealed class OverlapRule : ICheckAssignmentRule
{
    public RuleViolation? Check(AssignmentCandidate candidate, AssignmentContext context)
    {
        var overlaps = context.ExistingAssignments.Any(a => a.EmployeeId == candidate.Employee.Id && a.Shift.Id != candidate.Shift.Id && candidate.Shift.StartUtc < a.Shift.EndUtc && a.Shift.StartUtc < candidate.Shift.EndUtc);
        return overlaps ? new RuleViolation(RuleCodes.ShiftOverlap, "Ce sauveteur a déjà un quart qui chevauche cette période.") : null;
    }
}

public sealed class QuotaRule : ICheckAssignmentRule
{
    public RuleViolation? Check(AssignmentCandidate candidate, AssignmentContext context)
    {
        var timeZone = context.Site.TimeZone;
        var candidateLocalStart = TimeZoneInfo.ConvertTime(candidate.Shift.StartUtc, timeZone).DateTime;
        var weekStart = StartOfWeek(candidateLocalStart.Date);
        var weekEnd = weekStart.AddDays(7);
        var existingHours = context.ExistingAssignments
            .Where(a => a.EmployeeId == candidate.Employee.Id)
            .Where(a => TimeZoneInfo.ConvertTime(a.Shift.StartUtc, timeZone).DateTime >= weekStart && TimeZoneInfo.ConvertTime(a.Shift.StartUtc, timeZone).DateTime < weekEnd)
            .Sum(a => a.Shift.Duration.TotalHours);
        var total = existingHours + candidate.Shift.Duration.TotalHours;
        return total > (double)candidate.Employee.WeeklyQuotaHours
            ? new RuleViolation(RuleCodes.WeeklyQuotaExceeded, $"Ce quart porterait le total hebdomadaire à {total:0.#} h, au-dessus du quota de {candidate.Employee.WeeklyQuotaHours:0.#} h.")
            : null;
    }

    private static DateTime StartOfWeek(DateTime date)
    {
        var offset = ((int)date.DayOfWeek + 6) % 7;
        return date.AddDays(-offset).Date;
    }
}

public sealed class SeasonRule : ICheckAssignmentRule
{
    public RuleViolation? Check(AssignmentCandidate candidate, AssignmentContext context)
        => context.Site.IsOpen(candidate.Shift.StartUtc, candidate.Shift.EndUtc)
            ? null
            : new RuleViolation(RuleCodes.SiteClosed, $"Le site « {context.Site.Name} » est fermé pendant la période de ce quart.");
}

public sealed class CancelledShiftRule : ICheckAssignmentRule
{
    public RuleViolation? Check(AssignmentCandidate candidate, AssignmentContext context)
        => candidate.Shift.Status == ShiftStatus.Cancelled
            ? new RuleViolation(RuleCodes.ShiftCancelled, "Ce quart est annulé et ne peut plus recevoir d'assignation.")
            : null;
}

public static class AssignmentPolicy
{
    private static readonly ICheckAssignmentRule[] Rules = [new CancelledShiftRule(), new CertificationRule(), new OverlapRule(), new QuotaRule(), new SeasonRule()];

    public static IReadOnlyList<RuleViolation> Validate(AssignmentCandidate candidate, AssignmentContext context)
        => Rules.Select(rule => rule.Check(candidate, context)).OfType<RuleViolation>().ToArray();
}
