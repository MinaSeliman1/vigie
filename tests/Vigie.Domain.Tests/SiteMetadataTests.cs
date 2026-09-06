using Vigie.Domain;

namespace Vigie.Domain.Tests;

public sealed class SiteMetadataTests
{
    [Fact]
    public void Creates_a_municipal_site_with_public_catalog_metadata()
    {
        var site = Site.Create(
            Guid.NewGuid(),
            "Piscine Val-des-Arbres",
            "Eastern Standard Time",
            OpeningSeason.AllYear,
            SiteType.Indoor,
            Guid.NewGuid(),
            "1555, boulevard Saint-Martin Est",
            "Vimont",
            isMunicipal: true);

        Assert.True(site.IsMunicipal);
        Assert.Equal("1555, boulevard Saint-Martin Est", site.Address);
        Assert.Equal("Vimont", site.Neighborhood);
    }
}
