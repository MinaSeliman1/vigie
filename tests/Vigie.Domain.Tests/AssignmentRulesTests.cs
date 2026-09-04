using Vigie.Domain;

namespace Vigie.Domain.Tests;

public sealed class AssignmentRulesTests
{
    private static readonly DateTimeOffset ShiftStart = new(2026, 9, 7, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Refuses_assignment_when_required_certification_is_expired_before_shift_end()
    {
        var employee = Employee.Create(Guid.NewGuid(), "Amélie Roy", "amelie@example.test", EmployeeRole.Lifeguard, 40);
        var firstAid = CertificationType.Create(Guid.NewGuid(), "Premiers soins", true);
        var site = Site.Create(Guid.NewGuid(), "Piscine Nord", "UTC", OpeningSeason.AllYear);
        var shift = Shift.Create(Guid.NewGuid(), site.Id, ShiftStart, ShiftStart.AddHours(8), 1);
        var context = Context(employee, site, shift, firstAid, Certification.Create(employee.Id, firstAid.Id, new DateOnly(2026, 9, 6)));

        var violations = AssignmentPolicy.Validate(new AssignmentCandidate(employee, shift), context);

        var violation = Assert.Single(violations, v => v.Code == RuleCodes.CertificationExpired);
        Assert.Contains("Premiers soins", violation.Message);
    }

    [Fact]
    public void Accepts_certification_that_covers_the_whole_shift()
    {
        var employee = Employee.Create(Guid.NewGuid(), "Amélie Roy", "amelie@example.test", EmployeeRole.Lifeguard, 40);
        var firstAid = CertificationType.Create(Guid.NewGuid(), "Premiers soins", true);
        var site = Site.Create(Guid.NewGuid(), "Piscine Nord", "UTC", OpeningSeason.AllYear);
        var shift = Shift.Create(Guid.NewGuid(), site.Id, ShiftStart, ShiftStart.AddHours(8), 1);
        var context = Context(employee, site, shift, firstAid, Certification.Create(employee.Id, firstAid.Id, new DateOnly(2026, 9, 7)));

        Assert.DoesNotContain(AssignmentPolicy.Validate(new AssignmentCandidate(employee, shift), context), v => v.Code == RuleCodes.CertificationExpired);
    }

    [Fact]
    public void Refuses_overlapping_shift_for_same_employee()
    {
        var employee = Employee.Create(Guid.NewGuid(), "Noah Tremblay", "noah@example.test", EmployeeRole.Lifeguard, 40);
        var site = Site.Create(Guid.NewGuid(), "Piscine Nord", "UTC", OpeningSeason.AllYear);
        var shift = Shift.Create(Guid.NewGuid(), site.Id, ShiftStart, ShiftStart.AddHours(4), 1);
        var existing = Shift.Create(Guid.NewGuid(), site.Id, ShiftStart.AddHours(3), ShiftStart.AddHours(7), 1);
        var context = Context(employee, site, shift, null, existing: new ScheduledAssignment(employee.Id, existing));

        var violations = AssignmentPolicy.Validate(new AssignmentCandidate(employee, shift), context);

        Assert.Contains(violations, v => v.Code == RuleCodes.ShiftOverlap);
    }

    [Fact]
    public void Allows_adjacent_shifts_without_overlap()
    {
        var employee = Employee.Create(Guid.NewGuid(), "Noah Tremblay", "noah@example.test", EmployeeRole.Lifeguard, 40);
        var site = Site.Create(Guid.NewGuid(), "Piscine Nord", "UTC", OpeningSeason.AllYear);
        var shift = Shift.Create(Guid.NewGuid(), site.Id, ShiftStart, ShiftStart.AddHours(4), 1);
        var existing = Shift.Create(Guid.NewGuid(), site.Id, ShiftStart.AddHours(4), ShiftStart.AddHours(8), 1);
        var context = Context(employee, site, shift, null, existing: new ScheduledAssignment(employee.Id, existing));

        Assert.DoesNotContain(AssignmentPolicy.Validate(new AssignmentCandidate(employee, shift), context), v => v.Code == RuleCodes.ShiftOverlap);
    }

    [Fact]
    public void Refuses_quota_when_weekly_hours_would_be_exceeded()
    {
        var employee = Employee.Create(Guid.NewGuid(), "Noah Tremblay", "noah@example.test", EmployeeRole.Lifeguard, 7);
        var site = Site.Create(Guid.NewGuid(), "Piscine Nord", "UTC", OpeningSeason.AllYear);
        var shift = Shift.Create(Guid.NewGuid(), site.Id, ShiftStart, ShiftStart.AddHours(4), 1);
        var existing = Shift.Create(Guid.NewGuid(), site.Id, ShiftStart.AddHours(5), ShiftStart.AddHours(9), 1);
        var context = Context(employee, site, shift, null, existing: new ScheduledAssignment(employee.Id, existing));

        Assert.Contains(AssignmentPolicy.Validate(new AssignmentCandidate(employee, shift), context), v => v.Code == RuleCodes.WeeklyQuotaExceeded);
    }

    [Fact]
    public void Refuses_shift_outside_site_opening_season()
    {
        var employee = Employee.Create(Guid.NewGuid(), "Sofia Nguyen", "sofia@example.test", EmployeeRole.Lifeguard, 40);
        var site = Site.Create(Guid.NewGuid(), "Bassin extérieur", "UTC", new OpeningSeason(6, 1, 8, 31));
        var shift = Shift.Create(Guid.NewGuid(), site.Id, new DateTimeOffset(2026, 2, 14, 10, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 2, 14, 14, 0, 0, TimeSpan.Zero), 1);
        var context = Context(employee, site, shift);

        Assert.Contains(AssignmentPolicy.Validate(new AssignmentCandidate(employee, shift), context), v => v.Code == RuleCodes.SiteClosed);
    }

    [Fact]
    public void Supports_opening_season_that_crosses_new_year()
    {
        var employee = Employee.Create(Guid.NewGuid(), "Sofia Nguyen", "sofia@example.test", EmployeeRole.Lifeguard, 40);
        var site = Site.Create(Guid.NewGuid(), "Centre hivernal", "UTC", new OpeningSeason(11, 1, 3, 31));
        var shift = Shift.Create(Guid.NewGuid(), site.Id, new DateTimeOffset(2026, 1, 10, 10, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 1, 10, 14, 0, 0, TimeSpan.Zero), 1);
        var context = Context(employee, site, shift);

        Assert.DoesNotContain(AssignmentPolicy.Validate(new AssignmentCandidate(employee, shift), context), v => v.Code == RuleCodes.SiteClosed);
    }

    private static AssignmentContext Context(Employee employee, Site site, Shift shift, CertificationType? type = null, Certification? certification = null, params ScheduledAssignment[] existing)
        => new(site, type is null ? [] : [type], certification is null ? [] : [certification], existing);
}
