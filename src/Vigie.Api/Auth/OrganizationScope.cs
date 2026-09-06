using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Vigie.Domain;
using Vigie.Infrastructure;

namespace Vigie.Api.Auth;

public sealed record OrganizationScope(Guid EmployeeId, Guid OrganizationId, EmployeeRole Role, Guid? SiteId, Guid? SectorId, Guid? MembershipId)
{
    public bool IsDirector => Role == EmployeeRole.AquaticDirector;
    public bool IsSectorManager => Role == EmployeeRole.SectorManager;
    public bool IsPoolChief => Role is EmployeeRole.PoolChief or EmployeeRole.Coordinator;
}

public static class OrganizationScopeResolver
{
    private static readonly EmployeeRole[] Priority =
    [
        EmployeeRole.AquaticDirector,
        EmployeeRole.SectorManager,
        EmployeeRole.PoolChief,
        EmployeeRole.Coordinator,
        EmployeeRole.Lifeguard
    ];

    public static OrganizationScope? Resolve(ClaimsPrincipal principal, IVigieStore store)
    {
        var employeeId = ParseClaim(principal, JwtRegisteredClaimNames.Sub, ClaimTypes.NameIdentifier);
        var organizationId = ParseClaim(principal, "organization_id");
        if (!employeeId.HasValue || !organizationId.HasValue) return null;

        var employee = store.Employees.SingleOrDefault(item => item.Id == employeeId && item.OrganizationId == organizationId);
        if (employee is null) return null;

        var memberships = store.Memberships
            .Where(item => item.EmployeeId == employeeId && item.OrganizationId == organizationId && item.IsActive)
            .ToArray();
        var membershipId = ParseClaim(principal, "membership_id");
        var membership = membershipId.HasValue ? memberships.SingleOrDefault(item => item.Id == membershipId) : null;
        membership ??= memberships.OrderBy(item => Array.IndexOf(Priority, item.Role)).FirstOrDefault();

        if (membership is not null)
        {
            // Les comptes historiques « Coordinator » gardent leur portée globale pendant la migration.
            var resolvedRole = employee.Role == EmployeeRole.Coordinator && membership.Role == EmployeeRole.PoolChief
                ? EmployeeRole.Coordinator
                : Normalize(membership.Role);
            return new OrganizationScope(employeeId.Value, organizationId.Value, resolvedRole, membership.SiteId, membership.SectorId, membership.Id);
        }

        return new OrganizationScope(employeeId.Value, organizationId.Value, Normalize(employee.Role), null, null, null);
    }

    public static bool CanManageOrganization(OrganizationScope? scope) => scope?.Role == EmployeeRole.AquaticDirector;

    public static bool CanManageSector(OrganizationScope? scope, Guid organizationId)
        => scope is not null && scope.OrganizationId == organizationId && scope.Role is EmployeeRole.AquaticDirector or EmployeeRole.SectorManager;

    public static bool CanManageSite(OrganizationScope? scope, Site site, IVigieStore store)
    {
        if (scope is null || site.OrganizationId != scope.OrganizationId) return false;
        if (scope.Role == EmployeeRole.AquaticDirector) return true;
        if (scope.Role == EmployeeRole.SectorManager) return scope.SectorId.HasValue && site.SectorId == scope.SectorId;
        return scope.Role == EmployeeRole.Coordinator || scope.Role == EmployeeRole.PoolChief && scope.SiteId == site.Id;
    }

    public static bool CanDecideSwap(OrganizationScope? scope, Site site)
        => scope is not null && site.OrganizationId == scope.OrganizationId &&
            (scope.Role == EmployeeRole.AquaticDirector ||
             scope.Role == EmployeeRole.SectorManager && scope.SectorId.HasValue && site.SectorId == scope.SectorId ||
             scope.Role == EmployeeRole.Coordinator || scope.Role == EmployeeRole.PoolChief && scope.SiteId == site.Id);

    public static EmployeeRole Normalize(EmployeeRole role) => role == EmployeeRole.Coordinator ? EmployeeRole.PoolChief : role;

    private static Guid? ParseClaim(ClaimsPrincipal principal, params string[] names)
    {
        foreach (var name in names)
            if (Guid.TryParse(principal.FindFirstValue(name), out var id)) return id;
        return null;
    }
}
