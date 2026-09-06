using Vigie.Domain;

namespace Vigie.Domain.Tests;

public sealed class NotificationTests
{
    [Fact]
    public void Create_requires_content_and_identity()
    {
        Assert.Throws<DomainException>(() => Notification.Create(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), "swap", "Titre", "Corps", DateTimeOffset.UtcNow));
        Assert.Throws<DomainException>(() => Notification.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "", "Titre", "Corps", DateTimeOffset.UtcNow));
        Assert.Throws<DomainException>(() => Notification.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "swap", "", "Corps", DateTimeOffset.UtcNow));
        Assert.Throws<DomainException>(() => Notification.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "swap", "Titre", "", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void MarkRead_is_idempotent_and_uses_utc()
    {
        var notification = Notification.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "swap", "Échange à traiter", "Une demande attend votre approbation.", DateTimeOffset.UtcNow);
        var firstRead = new DateTimeOffset(2026, 9, 6, 15, 30, 0, TimeSpan.FromHours(-4));

        notification.MarkRead(firstRead);
        notification.MarkRead(firstRead.AddHours(1));

        Assert.True(notification.IsRead);
        Assert.Equal(firstRead.ToUniversalTime(), notification.ReadAtUtc);
    }
}
