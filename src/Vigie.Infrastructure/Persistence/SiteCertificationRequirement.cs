namespace Vigie.Infrastructure.Persistence;

/// <summary>Table de liaison de l’exigence de certification par site.</summary>
public sealed class SiteCertificationRequirement
{
    public Guid SiteId { get; set; }
    public Guid CertificationTypeId { get; set; }
}
