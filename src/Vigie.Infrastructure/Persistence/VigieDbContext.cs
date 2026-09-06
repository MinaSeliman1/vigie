using Microsoft.EntityFrameworkCore;
using Vigie.Domain;

namespace Vigie.Infrastructure.Persistence;

public sealed class VigieDbContext(DbContextOptions<VigieDbContext> options) : DbContext(options)
{
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Site> Sites => Set<Site>();
    public DbSet<Sector> Sectors => Set<Sector>();
    public DbSet<OrganizationMembership> OrganizationMemberships => Set<OrganizationMembership>();
    public DbSet<CertificationType> CertificationTypes => Set<CertificationType>();
    public DbSet<Certification> Certifications => Set<Certification>();
    public DbSet<Shift> Shifts => Set<Shift>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<Availability> Availabilities => Set<Availability>();
    public DbSet<SwapRequest> SwapRequests => Set<SwapRequest>();
    public DbSet<SiteCertificationRequirement> SiteCertificationRequirements => Set<SiteCertificationRequirement>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Organization>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Slug).HasMaxLength(80).IsRequired();
            entity.HasIndex(x => x.Slug).IsUnique();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
        });
        modelBuilder.Entity<AuditEntry>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Action).HasMaxLength(80).IsRequired();
            entity.Property(x => x.EntityType).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Details).HasMaxLength(2000);
            entity.HasIndex(x => new { x.OrganizationId, x.CreatedAtUtc });
            entity.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Employee>().WithMany().HasForeignKey(x => x.ActorId).OnDelete(DeleteBehavior.SetNull);
        });
        modelBuilder.Entity<Invitation>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Email).HasMaxLength(180).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Role).HasConversion<string>().HasMaxLength(24).IsRequired();
            entity.Property(x => x.SiteId);
            entity.Property(x => x.SectorId);
            entity.HasIndex(x => new { x.OrganizationId, x.SiteId, x.SectorId });
            entity.HasOne<Site>().WithMany().HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Sector>().WithMany().HasForeignKey(x => x.SectorId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(24).IsRequired();
            entity.HasIndex(x => x.TokenHash).IsUnique();
            entity.HasIndex(x => new { x.OrganizationId, x.Email, x.Status });
            entity.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.OrganizationId).IsRequired();
            entity.HasIndex(x => x.OrganizationId);
            entity.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(180).IsRequired();
            entity.HasIndex(x => x.Email).IsUnique();
            entity.Property(x => x.Role).HasConversion<string>().HasMaxLength(24);
            entity.Property(x => x.WeeklyQuotaHours).HasPrecision(5, 2);
            entity.Property(x => x.PasswordHash).HasMaxLength(256).IsRequired();
            entity.Property(x => x.IsDemoAccount).IsRequired();
        });
        modelBuilder.Entity<Site>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.OrganizationId).IsRequired();
            entity.HasIndex(x => x.OrganizationId);
            entity.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(x => x.SectorId);
            entity.HasIndex(x => new { x.OrganizationId, x.SectorId });
            entity.HasOne<Sector>().WithMany().HasForeignKey(x => x.SectorId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Address).HasMaxLength(240).IsRequired();
            entity.Property(x => x.Neighborhood).HasMaxLength(120).IsRequired();
            entity.Property(x => x.IsMunicipal).IsRequired();
            entity.Property(x => x.TimeZoneId).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Type).HasConversion<string>().HasMaxLength(24);
            entity.Property(x => x.OpeningSeason)
                .HasConversion(
                    season => $"{season.StartMonth:00}-{season.StartDay:00}:{season.EndMonth:00}-{season.EndDay:00}",
                    value => ParseOpeningSeason(value))
                .HasMaxLength(12)
                .IsRequired();
        });
        modelBuilder.Entity<Sector>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.OrganizationId).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Code).HasMaxLength(32).IsRequired();
            entity.Property(x => x.IsActive).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
            entity.HasIndex(x => new { x.OrganizationId, x.Code }).IsUnique();
            entity.HasIndex(x => new { x.OrganizationId, x.Name }).IsUnique();
            entity.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<OrganizationMembership>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.OrganizationId).IsRequired();
            entity.Property(x => x.EmployeeId).IsRequired();
            entity.Property(x => x.Role).HasConversion<string>().HasMaxLength(24).IsRequired();
            entity.Property(x => x.IsActive).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken().IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
            entity.HasIndex(x => new { x.OrganizationId, x.EmployeeId });
            entity.HasIndex(x => new { x.EmployeeId, x.OrganizationId, x.SiteId })
                .HasFilter("\"IsActive\" = TRUE AND \"SiteId\" IS NOT NULL")
                .IsUnique();
            entity.HasIndex(x => new { x.EmployeeId, x.OrganizationId, x.SectorId })
                .HasFilter("\"IsActive\" = TRUE AND \"SectorId\" IS NOT NULL")
                .IsUnique();
            entity.HasIndex(x => new { x.EmployeeId, x.OrganizationId })
                .HasFilter("\"IsActive\" = TRUE AND \"SiteId\" IS NULL AND \"SectorId\" IS NULL")
                .IsUnique();
            entity.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Employee>().WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Site>().WithMany().HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Sector>().WithMany().HasForeignKey(x => x.SectorId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<CertificationType>(entity => { entity.HasKey(x => x.Id); entity.Property(x => x.Name).HasMaxLength(120).IsRequired(); });
        modelBuilder.Entity<Certification>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.EmployeeId, x.CertificationTypeId }).IsUnique();
            entity.HasOne<Employee>().WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<CertificationType>().WithMany().HasForeignKey(x => x.CertificationTypeId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Shift>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Status).HasConversion<string>();
            entity.HasIndex(x => new { x.SiteId, x.StartUtc });
            entity.HasOne<Site>().WithMany().HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Assignment>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.ShiftId, x.EmployeeId }).IsUnique();
            entity.HasOne<Shift>().WithMany().HasForeignKey(x => x.ShiftId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Employee>().WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Availability>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.EmployeeId, x.Date }).IsUnique();
            entity.Property(x => x.Note).HasMaxLength(300);
            entity.HasOne<Employee>().WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<SwapRequest>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Status).HasConversion<string>();
            entity.HasIndex(x => new { x.AssignmentId, x.Status });
            entity.HasOne<Assignment>().WithMany().HasForeignKey(x => x.AssignmentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Employee>().WithMany().HasForeignKey(x => x.ReceiverId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Employee>().WithMany().HasForeignKey(x => x.DecidedBy).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<SiteCertificationRequirement>(entity =>
        {
            entity.HasKey(x => new { x.SiteId, x.CertificationTypeId });
            entity.HasIndex(x => x.CertificationTypeId);
            entity.HasOne<Site>().WithMany().HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<CertificationType>().WithMany().HasForeignKey(x => x.CertificationTypeId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Type).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Title).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Body).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.ActionUrl).HasMaxLength(240);
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.HasIndex(x => new { x.RecipientEmployeeId, x.CreatedAtUtc });
            entity.HasIndex(x => new { x.RecipientEmployeeId, x.ReadAtUtc });
            entity.HasOne<Organization>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Employee>().WithMany().HasForeignKey(x => x.RecipientEmployeeId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static OpeningSeason ParseOpeningSeason(string value)
    {
        var parts = value.Split(':', StringSplitOptions.TrimEntries);
        if (parts.Length != 2) return OpeningSeason.AllYear;
        var start = parts[0].Split('-');
        var end = parts[1].Split('-');
        return start.Length == 2 && end.Length == 2 &&
            int.TryParse(start[0], out var startMonth) && int.TryParse(start[1], out var startDay) &&
            int.TryParse(end[0], out var endMonth) && int.TryParse(end[1], out var endDay)
            ? new OpeningSeason(startMonth, startDay, endMonth, endDay)
            : OpeningSeason.AllYear;
    }
}
