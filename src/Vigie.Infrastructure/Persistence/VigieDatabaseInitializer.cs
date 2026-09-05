using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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

        if (await context.Employees.AnyAsync(cancellationToken)) return;

        var source = new InMemoryVigieStore();
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
