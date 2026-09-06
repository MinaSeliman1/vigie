using Vigie.Domain;

namespace Vigie.Domain.Tests;

public sealed class InvitationTests
{
    [Fact]
    public void Invitation_expires_after_its_lifetime()
    {
        var created = new DateTimeOffset(2026, 9, 5, 12, 0, 0, TimeSpan.Zero);
        var invitation = Invitation.Create(Guid.NewGuid(), Guid.NewGuid(), "personne@exemple.test", "Personne invitée", EmployeeRole.Lifeguard, "hash", created, TimeSpan.FromDays(7));

        Assert.True(invitation.IsPending(created.AddDays(6)));
        Assert.False(invitation.IsPending(created.AddDays(7)));
        Assert.Equal(InvitationStatus.Expired, invitation.Status);
    }

    [Fact]
    public void Invitation_can_only_be_accepted_once()
    {
        var invitation = Invitation.Create(Guid.NewGuid(), Guid.NewGuid(), "personne@exemple.test", "Personne invitée", EmployeeRole.Lifeguard, "hash", DateTimeOffset.UtcNow, TimeSpan.FromDays(7));
        invitation.Accept(DateTimeOffset.UtcNow);

        Assert.Equal(InvitationStatus.Accepted, invitation.Status);
        Assert.Throws<DomainException>(() => invitation.Accept(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Invitation_keeps_the_requested_role_and_scope()
    {
        var siteId = Guid.NewGuid();
        var invitation = Invitation.Create(Guid.NewGuid(), Guid.NewGuid(), "chef@exemple.test", "Chef", EmployeeRole.PoolChief, "hash", DateTimeOffset.UtcNow, TimeSpan.FromDays(7), siteId, null);

        Assert.Equal(EmployeeRole.PoolChief, invitation.Role);
        Assert.Equal(siteId, invitation.SiteId);
        Assert.Null(invitation.SectorId);
    }
}
