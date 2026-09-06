using Vigie.Domain;

namespace Vigie.Domain.Tests;

public sealed class OrganizationMembershipTests
{
    private static readonly Guid EmployeeId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OrganizationId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid SiteId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid SectorId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    [Fact]
    public void Creates_a_lifeguard_membership_scoped_to_a_site()
    {
        var membership = OrganizationMembership.Create(
            Guid.NewGuid(), EmployeeId, OrganizationId, EmployeeRole.Lifeguard, SiteId, null);

        Assert.Equal(EmployeeRole.Lifeguard, membership.Role);
        Assert.Equal(SiteId, membership.SiteId);
        Assert.Null(membership.SectorId);
        Assert.True(membership.IsActive);
        Assert.Equal(1, membership.Version);
    }

    [Fact]
    public void Creates_a_sector_manager_membership_scoped_to_a_sector()
    {
        var membership = OrganizationMembership.Create(
            Guid.NewGuid(), EmployeeId, OrganizationId, EmployeeRole.SectorManager, null, SectorId);

        Assert.Equal(SectorId, membership.SectorId);
        Assert.Null(membership.SiteId);
    }

    [Fact]
    public void Rejects_a_lifeguard_without_a_site()
    {
        var exception = Assert.Throws<DomainException>(() => OrganizationMembership.Create(
            Guid.NewGuid(), EmployeeId, OrganizationId, EmployeeRole.Lifeguard, null, null));

        Assert.Contains("site", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Rejects_a_pool_chief_with_a_sector_scope()
    {
        var exception = Assert.Throws<DomainException>(() => OrganizationMembership.Create(
            Guid.NewGuid(), EmployeeId, OrganizationId, EmployeeRole.PoolChief, SiteId, SectorId));

        Assert.Contains("secteur", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Rejects_a_sector_manager_without_a_sector()
    {
        var exception = Assert.Throws<DomainException>(() => OrganizationMembership.Create(
            Guid.NewGuid(), EmployeeId, OrganizationId, EmployeeRole.SectorManager, null, null));

        Assert.Contains("secteur", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Aquatic_director_has_organization_scope_only()
    {
        var membership = OrganizationMembership.Create(
            Guid.NewGuid(), EmployeeId, OrganizationId, EmployeeRole.AquaticDirector, null, null);

        Assert.Null(membership.SiteId);
        Assert.Null(membership.SectorId);
    }

    [Fact]
    public void Activation_is_idempotent_and_scope_change_increments_version()
    {
        var membership = OrganizationMembership.Create(
            Guid.NewGuid(), EmployeeId, OrganizationId, EmployeeRole.PoolChief, SiteId, null);

        membership.Deactivate();
        var deactivatedVersion = membership.Version;
        membership.Deactivate();
        Assert.False(membership.IsActive);
        Assert.Equal(deactivatedVersion, membership.Version);

        membership.Activate();
        Assert.True(membership.IsActive);
        Assert.Equal(deactivatedVersion + 1, membership.Version);

        var secondSiteId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        membership.ChangeScope(secondSiteId, null);
        Assert.Equal(secondSiteId, membership.SiteId);
        Assert.Null(membership.SectorId);
        Assert.Equal(deactivatedVersion + 2, membership.Version);
    }

    [Fact]
    public void Rejects_empty_identity_values()
    {
        Assert.Throws<DomainException>(() => OrganizationMembership.Create(
            Guid.Empty, EmployeeId, OrganizationId, EmployeeRole.AquaticDirector, null, null));
        Assert.Throws<DomainException>(() => OrganizationMembership.Create(
            Guid.NewGuid(), Guid.Empty, OrganizationId, EmployeeRole.AquaticDirector, null, null));
        Assert.Throws<DomainException>(() => OrganizationMembership.Create(
            Guid.NewGuid(), EmployeeId, Guid.Empty, EmployeeRole.AquaticDirector, null, null));
    }

    [Fact]
    public void Changes_role_and_scope_atomically()
    {
        var membership = OrganizationMembership.Create(
            Guid.NewGuid(), EmployeeId, OrganizationId, EmployeeRole.PoolChief, SiteId, null);

        membership.ChangeRoleAndScope(EmployeeRole.SectorManager, null, SectorId);

        Assert.Equal(EmployeeRole.SectorManager, membership.Role);
        Assert.Null(membership.SiteId);
        Assert.Equal(SectorId, membership.SectorId);
        Assert.Equal(2, membership.Version);
    }
}

public sealed class SectorTests
{
    [Fact]
    public void Trims_name_and_code_when_created()
    {
        var sector = Sector.Create(Guid.NewGuid(), Guid.NewGuid(), "  Secteur Nord  ", "  NORD  ");

        Assert.Equal("Secteur Nord", sector.Name);
        Assert.Equal("NORD", sector.Code);
        Assert.True(sector.IsActive);
    }

    [Fact]
    public void Rejects_empty_name_or_code()
    {
        Assert.Throws<DomainException>(() => Sector.Create(Guid.NewGuid(), Guid.NewGuid(), "", "NORD"));
        Assert.Throws<DomainException>(() => Sector.Create(Guid.NewGuid(), Guid.NewGuid(), "Nord", ""));
    }
}
