using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vigie.Application.Auth;
using Vigie.Domain;
using Vigie.Infrastructure;

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
            await EnsureLavalStructureAsync(context, cancellationToken);
            await EnsureDemoAuditEntriesAsync(context, cancellationToken);
            if (legacyDemoAccounts.Length > 0) await context.SaveChangesAsync(cancellationToken);
            return;
        }

        var source = new InMemoryVigieStore();
        if (!await context.Organizations.AnyAsync(cancellationToken)) context.Organizations.AddRange(source.Organizations);
        context.Employees.AddRange(source.Employees);
        context.Sites.AddRange(source.Sites);
        context.Sectors.AddRange(source.Sectors);
        context.OrganizationMemberships.AddRange(source.Memberships);
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

    private static async Task EnsureLavalStructureAsync(VigieDbContext context, CancellationToken cancellationToken)
    {
        var organizations = await context.Organizations.AsTracking().ToArrayAsync(cancellationToken);
        foreach (var organization in organizations)
        {
            var sites = await context.Sites.Where(site => site.OrganizationId == organization.Id).AsTracking().ToArrayAsync(cancellationToken);
            var sectors = await context.Sectors.Where(sector => sector.OrganizationId == organization.Id).AsTracking().ToListAsync(cancellationToken);

            // Le catalogue Laval est idempotent : une base existante reçoit les sites
            // manquants sans modifier les quarts ni les affectations déjà en place.
            foreach (var pool in LavalPoolCatalog.All)
            {
                var site = sites.SingleOrDefault(item => item.Id == pool.SiteId);
                if (site is null)
                {
                    site = Site.Create(pool.SiteId, pool.Name, "Eastern Standard Time", pool.OpeningSeason, pool.Type, organization.Id, pool.Address, pool.Neighborhood, isMunicipal: true);
                    context.Sites.Add(site);
                    sites = [.. sites, site];
                }
                else
                {
                    site.SetCatalogMetadata(pool.Address, pool.Neighborhood, isMunicipal: true);
                }

                var catalogSector = sectors.SingleOrDefault(item => item.Id == pool.SectorId);
                if (catalogSector is null)
                {
                    catalogSector = Sector.Create(pool.SectorId, organization.Id, $"Secteur {pool.Code}", pool.Code);
                    context.Sectors.Add(catalogSector);
                    sectors.Add(catalogSector);
                }
                site.SetSector(catalogSector.Id);
            }

            if (organization.Slug == "vigie-demo")
                EnsureDemoStaff(context, organization.Id, sectors, sites);

            foreach (var site in sites)
            {
                var sector = sectors.SingleOrDefault(item => item.Id == site.SectorId);
                if (sector is null)
                {
                    var code = $"SITE-{site.Id:N}"[..Math.Min(32, $"SITE-{site.Id:N}".Length)].ToUpperInvariant();
                    sector = sectors.SingleOrDefault(item => item.Code == code);
                    if (sector is null)
                    {
                        sector = Sector.Create(Guid.NewGuid(), organization.Id, $"Secteur — {site.Name}", code);
                        context.Sectors.Add(sector);
                        sectors.Add(sector);
                    }
                    site.SetSector(sector.Id);
                }
            }

            var employees = await context.Employees.Where(employee => employee.OrganizationId == organization.Id).AsTracking().ToArrayAsync(cancellationToken);
            var existingEmployeeIds = await context.OrganizationMemberships
                .Where(membership => membership.OrganizationId == organization.Id && membership.IsActive)
                .Select(membership => membership.EmployeeId)
                .ToHashSetAsync(cancellationToken);
            var primarySite = sites.FirstOrDefault();
            var primarySector = sectors.FirstOrDefault();
            foreach (var employee in employees.Where(employee => !existingEmployeeIds.Contains(employee.Id)))
            {
                var role = employee.Role == EmployeeRole.Coordinator ? EmployeeRole.PoolChief : employee.Role;
                Guid? siteId = role is EmployeeRole.Lifeguard or EmployeeRole.PoolChief ? primarySite?.Id : null;
                Guid? sectorId = role == EmployeeRole.SectorManager ? primarySector?.Id : null;
                if (role is EmployeeRole.Lifeguard or EmployeeRole.PoolChief && !siteId.HasValue ||
                    role == EmployeeRole.SectorManager && !sectorId.HasValue)
                    continue;

                context.OrganizationMemberships.Add(OrganizationMembership.Create(Guid.NewGuid(), employee.Id, organization.Id, role, siteId, sectorId));
            }
        }
        await context.SaveChangesAsync(cancellationToken);
    }

    private static void EnsureDemoStaff(VigieDbContext context, Guid organizationId, IReadOnlyCollection<Sector> sectors, IReadOnlyCollection<Site> sites)
    {
        var nordSector = sectors.FirstOrDefault(sector => sector.Code == "NORD");
        var nordSite = sites.FirstOrDefault(site => site.Id == Guid.Parse("20000000-0000-0000-0000-000000000001"));
        if (nordSector is null || nordSite is null) return;

        EnsureDemoEmployee(context, organizationId, Guid.Parse("10000000-0000-0000-0000-000000000005"), "Marc-André Bouchard", "charge.nord@vigie.demo", EmployeeRole.SectorManager, nordSector.Id, null);
        EnsureDemoEmployee(context, organizationId, Guid.Parse("10000000-0000-0000-0000-000000000006"), "Élodie Martel", "regie@vigie.demo", EmployeeRole.AquaticDirector, null, null);
    }

    private static void EnsureDemoEmployee(VigieDbContext context, Guid organizationId, Guid employeeId, string name, string email, EmployeeRole role, Guid? sectorId, Guid? siteId)
    {
        var employee = context.Employees.Local.SingleOrDefault(item => item.Id == employeeId) ?? context.Employees.SingleOrDefault(item => item.Id == employeeId);
        if (employee is null)
        {
            employee = Employee.Create(employeeId, name, email, role, 40, organizationId, isDemoAccount: true);
            employee.SetPasswordHash(PasswordHasher.Hash("vigie-demo"));
            context.Employees.Add(employee);
        }

        var hasMembership = context.OrganizationMemberships.Local.Any(item => item.EmployeeId == employeeId && item.OrganizationId == organizationId && item.IsActive) ||
            context.OrganizationMemberships.Any(item => item.EmployeeId == employeeId && item.OrganizationId == organizationId && item.IsActive);
        if (hasMembership) return;

        var membership = OrganizationMembership.Create(Guid.NewGuid(), employeeId, organizationId, role, siteId, sectorId);
        context.OrganizationMemberships.Add(membership);
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
