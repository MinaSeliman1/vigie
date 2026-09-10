using Vigie.Domain;

namespace Vigie.Domain.Tests;

public sealed class CertificationTests
{
    [Fact]
    public void Updates_the_expiration_date_without_replacing_the_certification()
    {
        var id = Guid.NewGuid();
        var certification = Certification.Create(id, Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 9, 10));

        certification.UpdateExpiration(new DateOnly(2027, 9, 10));

        Assert.Equal(id, certification.Id);
        Assert.Equal(new DateOnly(2027, 9, 10), certification.ExpiresOn);
    }
}
