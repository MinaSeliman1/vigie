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

    [Fact]
    public void Updates_site_details_without_replacing_its_identity()
    {
        var id = Guid.NewGuid();
        var site = Site.Create(id, "Piscine initiale", "Eastern Standard Time", OpeningSeason.AllYear, SiteType.Indoor, Guid.NewGuid());

        site.UpdateDetails("Piscine rénovée", "America/Toronto", new OpeningSeason(6, 1, 9, 15), SiteType.Outdoor, "1, rue du Parc", "Chomedey", true);

        Assert.Equal(id, site.Id);
        Assert.Equal("Piscine rénovée", site.Name);
        Assert.Equal(SiteType.Outdoor, site.Type);
        Assert.Equal("America/Toronto", site.TimeZoneId);
        Assert.Equal(6, site.OpeningSeason.StartMonth);
        Assert.True(site.IsMunicipal);
        Assert.Equal("Chomedey", site.Neighborhood);
    }
}
