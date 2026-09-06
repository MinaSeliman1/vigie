using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vigie.Application.Auth;

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
        context.SiteCertificationRequirements.AddRange(source.SiteCertificationLinks.Select(link => new SiteCertificationRequirement
        {
            SiteId = link.SiteId,
            CertificationTypeId = link.CertificationTypeId
        }));

        await context.SaveChangesAsync(cancellationToken);
    }
}
