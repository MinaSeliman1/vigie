using Vigie.Domain;

namespace Vigie.Domain.Tests;

public sealed class ShiftLifecycleTests
{
    [Fact]
    public void Reschedule_updates_the_schedule_and_keeps_the_shift_open()
    {
        var shift = Shift.Create(Guid.NewGuid(), Guid.NewGuid(),
            new DateTimeOffset(2026, 9, 10, 9, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 9, 10, 17, 0, 0, TimeSpan.Zero), 2);

        shift.Reschedule(
            new DateTimeOffset(2026, 9, 11, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 9, 11, 18, 0, 0, TimeSpan.Zero), 3);

        Assert.Equal(new DateTimeOffset(2026, 9, 11, 10, 0, 0, TimeSpan.Zero), shift.StartUtc);
        Assert.Equal(new DateTimeOffset(2026, 9, 11, 18, 0, 0, TimeSpan.Zero), shift.EndUtc);
        Assert.Equal(3, shift.RequiredLifeguards);
        Assert.Equal(ShiftStatus.Open, shift.Status);
    }

    [Fact]
    public void Cancel_is_idempotent_and_a_cancelled_shift_cannot_be_rescheduled()
    {
        var shift = Shift.Create(Guid.NewGuid(), Guid.NewGuid(),
            new DateTimeOffset(2026, 9, 10, 9, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 9, 10, 17, 0, 0, TimeSpan.Zero), 2);

        shift.Cancel();
        shift.Cancel();

        Assert.Equal(ShiftStatus.Cancelled, shift.Status);
        Assert.Throws<DomainException>(() => shift.Reschedule(shift.StartUtc, shift.EndUtc.AddHours(1), 2));
    }

    [Fact]
    public void Cancelled_shift_is_rejected_by_assignment_policy()
    {
        var site = Site.Create(Guid.NewGuid(), "Piscine", "UTC", OpeningSeason.AllYear);
        var employee = Employee.Create(Guid.NewGuid(), "Sauveteur", "sauveteur@example.test", EmployeeRole.Lifeguard, 40);
        var shift = Shift.Create(Guid.NewGuid(), site.Id,
            new DateTimeOffset(2026, 9, 10, 9, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 9, 10, 17, 0, 0, TimeSpan.Zero), 1);
        shift.Cancel();

        var violations = AssignmentPolicy.Validate(new AssignmentCandidate(employee, shift),
            new AssignmentContext(site, [], [], []));

        Assert.Contains(violations, violation => violation.Code == RuleCodes.ShiftCancelled);
    }
}
