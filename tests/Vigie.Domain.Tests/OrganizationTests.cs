using Vigie.Domain;

namespace Vigie.Domain.Tests;

public sealed class OrganizationTests
{
    [Fact]
    public void Onboarding_profile_normalizes_supported_choices()
    {
        var organization = Organization.Create(Guid.NewGuid(), "Centre aquatique Laval", "centre-aquatique-laval");

        organization.SetOnboardingProfile(" MUNICIPAL ", "two-to-five", "eleven-to-thirty", "all");

        Assert.Equal("municipal", organization.OrganizationType);
        Assert.Equal("two-to-five", organization.PoolCount);
        Assert.Equal("eleven-to-thirty", organization.TeamSize);
        Assert.Equal("all", organization.PrimaryGoal);
    }

    [Fact]
    public void Onboarding_profile_rejects_unknown_choices()
    {
        var organization = Organization.Create(Guid.NewGuid(), "Centre aquatique Laval", "centre-aquatique-laval");

        Assert.Throws<DomainException>(() => organization.SetOnboardingProfile("unknown-type", "one", "one-to-ten", "planning"));
    }
}
