using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vigie.Application.Auth;
using Vigie.Domain;

namespace Vigie.Infrastructure.Persistence;

/// <summary>
/// Applique les migrations et charge les données fictives une seule fois pour un nouvel
/// environnement PostgreSQL. Les identités utilisées sont les mêmes que dans la démo mémoire.
/// </summary>
public static class VigieDatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var context = services.GetRequiredService<VigieDbContext>();
        await context.Database.MigrateAsync(cancellationToken);

        if (await context.Employees.AnyAsync(cancellationToken))
        {
            var legacyDemoAccounts = await context.Employees
                .Where(employee => employee.PasswordHash == string.Empty && employee.Email.EndsWith("@vigie.demo"))
                .ToArrayAsync(cancellationToken);
            foreach (var employee in legacyDemoAccounts) employee.SetPasswordHash(PasswordHasher.Hash("vigie-demo"));
            await EnsureDemoAuditEntriesAsync(context, cancellationToken);
            if (legacyDemoAccounts.Length > 0) await context.SaveChangesAsync(cancellationToken);
            return;
        }

        var source = new InMemoryVigieStore();
        if (!await context.Organizations.AnyAsync(cancellationToken)) context.Organizations.AddRange(source.Organizations);
        context.Employees.AddRange(source.Employees);
        context.Sites.AddRange(source.Sites);
        context.CertificationTypes.AddRange(source.CertificationTypes);
        context.Certifications.AddRange(source.Certifications);
        context.Shifts.AddRange(source.Shifts);
        context.Assignments.AddRange(source.Assignments);
        context.SwapRequests.AddRange(source.SwapRequests);
        context.AuditEntries.AddRange(source.AuditEntries);
        context.SiteCertificationRequirements.AddRange(source.SiteCertificationLinks.Select(link => new SiteCertificationRequirement
        {
            SiteId = link.SiteId,
            CertificationTypeId = link.CertificationTypeId
        }));

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureDemoAuditEntriesAsync(VigieDbContext context, CancellationToken cancellationToken)
    {
        var demoOrganization = await context.Organizations.SingleOrDefaultAsync(organization => organization.Slug == "vigie-demo", cancellationToken);
        if (demoOrganization is null || await context.AuditEntries.AnyAsync(entry => entry.OrganizationId == demoOrganization.Id, cancellationToken)) return;

        var employees = await context.Employees.Where(employee => employee.OrganizationId == demoOrganization.Id).ToDictionaryAsync(employee => employee.Email, cancellationToken);
        if (!employees.TryGetValue("coordonnateur@vigie.demo", out var coordinator) || !employees.TryGetValue("amelie@vigie.demo", out var lifeguard)) return;
        var shifts = await context.Shifts.Where(shift => context.Sites.Any(site => site.Id == shift.SiteId && site.OrganizationId == demoOrganization.Id)).OrderBy(shift => shift.StartUtc).Take(1).ToArrayAsync(cancellationToken);
        var assignment = await context.Assignments.Where(item => context.Shifts.Any(shift => shift.Id == item.ShiftId && context.Sites.Any(site => site.Id == shift.SiteId && site.OrganizationId == demoOrganization.Id))).OrderBy(item => item.Id).FirstOrDefaultAsync(cancellationToken);
        var auditNow = DateTimeOffset.UtcNow;
        var entries = new List<AuditEntry>
        {
            AuditEntry.Create(Guid.NewGuid(), demoOrganization.Id, coordinator.Id, "organization.created", "Organization", demoOrganization.Id, null, auditNow.AddDays(-30)),
        };
        if (shifts.Length > 0) entries.Add(AuditEntry.Create(Guid.NewGuid(), demoOrganization.Id, coordinator.Id, "shift.created", "Shift", shifts[0].Id, null, auditNow.AddDays(-4)));
        if (assignment is not null)
        {
            var assignedEmployee = employees.Values.SingleOrDefault(employee => employee.Id == assignment.EmployeeId);
            entries.Add(AuditEntry.Create(Guid.NewGuid(), demoOrganization.Id, coordinator.Id, "assignment.created", "Assignment", assignment.Id, assignedEmployee is null ? null : $"employé={assignedEmployee.Name}", auditNow.AddDays(-3)));
        }
        var swap = await context.SwapRequests.Where(request => context.Assignments.Any(item => item.Id == request.AssignmentId && context.Shifts.Any(shift => shift.Id == item.ShiftId && context.Sites.Any(site => site.Id == shift.SiteId && site.OrganizationId == demoOrganization.Id)))).OrderByDescending(request => request.RequestedAtUtc).FirstOrDefaultAsync(cancellationToken);
        if (swap is not null) entries.Add(AuditEntry.Create(Guid.NewGuid(), demoOrganization.Id, lifeguard.Id, "swap.created", "SwapRequest", swap.Id, null, auditNow.AddHours(-8)));
        context.AuditEntries.AddRange(entries);
        await context.SaveChangesAsync(cancellationToken);
    }
}
