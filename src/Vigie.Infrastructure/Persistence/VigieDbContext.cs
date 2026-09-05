using Microsoft.EntityFrameworkCore;
using Vigie.Domain;

namespace Vigie.Infrastructure.Persistence;

public sealed class VigieDbContext(DbContextOptions<VigieDbContext> options) : DbContext(options)
{
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Site> Sites => Set<Site>();
    public DbSet<CertificationType> CertificationTypes => Set<CertificationType>();
    public DbSet<Certification> Certifications => Set<Certification>();
    public DbSet<Shift> Shifts => Set<Shift>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<Availability> Availabilities => Set<Availability>();
    public DbSet<SwapRequest> SwapRequests => Set<SwapRequest>();
    public DbSet<SiteCertificationRequirement> SiteCertificationRequirements => Set<SiteCertificationRequirement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(180).IsRequired();
            entity.HasIndex(x => x.Email).IsUnique();
            entity.Property(x => x.Role).HasConversion<string>().HasMaxLength(24);
            entity.Property(x => x.WeeklyQuotaHours).HasPrecision(5, 2);
        });
        modelBuilder.Entity<Site>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.TimeZoneId).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Type).HasConversion<string>().HasMaxLength(24);
            entity.Property(x => x.OpeningSeason)
                .HasConversion(
                    season => $"{season.StartMonth:00}-{season.StartDay:00}:{season.EndMonth:00}-{season.EndDay:00}",
                    value => ParseOpeningSeason(value))
                .HasMaxLength(12)
                .IsRequired();
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
