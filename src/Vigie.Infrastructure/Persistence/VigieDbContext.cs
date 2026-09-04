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
            entity.Ignore(x => x.OpeningSeason);
            entity.Property<int>("OpeningStartMonth");
            entity.Property<int>("OpeningStartDay");
            entity.Property<int>("OpeningEndMonth");
            entity.Property<int>("OpeningEndDay");
        });
        modelBuilder.Entity<CertificationType>(entity => { entity.HasKey(x => x.Id); entity.Property(x => x.Name).HasMaxLength(120).IsRequired(); });
        modelBuilder.Entity<Certification>(entity => { entity.HasKey(x => x.Id); entity.HasIndex(x => new { x.EmployeeId, x.CertificationTypeId }).IsUnique(); });
        modelBuilder.Entity<Shift>(entity => { entity.HasKey(x => x.Id); entity.Property(x => x.Status).HasConversion<string>(); entity.HasIndex(x => new { x.SiteId, x.StartUtc }); });
        modelBuilder.Entity<Assignment>(entity => { entity.HasKey(x => x.Id); entity.HasIndex(x => new { x.ShiftId, x.EmployeeId }).IsUnique(); });
        modelBuilder.Entity<Availability>(entity => { entity.HasKey(x => x.Id); entity.HasIndex(x => new { x.EmployeeId, x.Date }).IsUnique(); entity.Property(x => x.Note).HasMaxLength(300); });
        modelBuilder.Entity<SwapRequest>(entity => { entity.HasKey(x => x.Id); entity.Property(x => x.Status).HasConversion<string>(); entity.HasIndex(x => new { x.AssignmentId, x.Status }); });
    }
}
