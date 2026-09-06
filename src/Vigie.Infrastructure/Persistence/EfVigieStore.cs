using Microsoft.EntityFrameworkCore;
using Vigie.Application;
using Vigie.Domain;

namespace Vigie.Infrastructure.Persistence;

/// <summary>
/// Adaptateur EF Core des ports Application. Les requêtes de lecture utilisent des
/// projections simples afin de ne jamais exposer le DbContext au domaine.
/// </summary>
public sealed class EfVigieStore(VigieDbContext db) : IVigieStore
{
    public IReadOnlyCollection<Organization> Organizations => db.Organizations.AsNoTracking().ToArray();
    public IReadOnlyCollection<Employee> Employees => db.Employees.AsNoTracking().ToArray();
    public IReadOnlyCollection<Site> Sites => db.Sites.AsNoTracking().ToArray();
    public IReadOnlyCollection<Shift> Shifts => db.Shifts.AsNoTracking().ToArray();
    public IReadOnlyCollection<CertificationType> CertificationTypes => db.CertificationTypes.AsNoTracking().ToArray();
    public IReadOnlyCollection<Certification> Certifications => db.Certifications.AsNoTracking().ToArray();
    public IReadOnlyCollection<Assignment> Assignments => db.Assignments.AsNoTracking().ToArray();
    public IReadOnlyCollection<SwapRequest> SwapRequests => db.SwapRequests.AsNoTracking().ToArray();
    public IReadOnlyCollection<Availability> Availabilities => db.Availabilities.AsNoTracking().ToArray();

    public Task<Employee?> GetAsync(Guid id, CancellationToken cancellationToken)
        => db.Employees.SingleOrDefaultAsync(employee => employee.Id == id, cancellationToken);

    async Task<Site?> ISiteRepository.GetAsync(Guid id, CancellationToken cancellationToken)
        => await db.Sites.SingleOrDefaultAsync(site => site.Id == id, cancellationToken);

    async Task<Shift?> IShiftRepository.GetAsync(Guid id, CancellationToken cancellationToken)
        => await db.Shifts.SingleOrDefaultAsync(shift => shift.Id == id, cancellationToken);

    async Task<IReadOnlyCollection<Certification>> ICertificationRepository.GetForEmployeeAsync(Guid employeeId, CancellationToken cancellationToken)
        => await db.Certifications.AsNoTracking().Where(certification => certification.EmployeeId == employeeId).ToArrayAsync(cancellationToken);

    async Task<IReadOnlyCollection<CertificationType>> ICertificationTypeRepository.GetRequiredForSiteAsync(Guid siteId, CancellationToken cancellationToken)
    {
        var typeIds = db.SiteCertificationRequirements
            .Where(requirement => requirement.SiteId == siteId)
            .Select(requirement => requirement.CertificationTypeId);
        return await db.CertificationTypes.AsNoTracking().Where(type => typeIds.Contains(type.Id)).ToArrayAsync(cancellationToken);
    }

    async Task<IReadOnlyCollection<ScheduledAssignment>> IAssignmentRepository.GetForEmployeeAsync(Guid employeeId, CancellationToken cancellationToken)
        => await db.Assignments.AsNoTracking()
            .Where(assignment => assignment.EmployeeId == employeeId)
            .Join(db.Shifts.AsNoTracking(), assignment => assignment.ShiftId, shift => shift.Id, (assignment, shift) => new ScheduledAssignment(employeeId, shift))
            .ToArrayAsync(cancellationToken);

    async Task<Assignment?> IAssignmentRepository.GetAsync(Guid id, CancellationToken cancellationToken)
        => await db.Assignments.SingleOrDefaultAsync(assignment => assignment.Id == id, cancellationToken);

    Task<Assignment> IAssignmentRepository.AddAsync(Assignment assignment, CancellationToken cancellationToken)
    {
        db.Assignments.Add(assignment);
        return Task.FromResult(assignment);
    }

    async Task IAssignmentRepository.RemoveAsync(Guid id, CancellationToken cancellationToken)
    {
        var assignment = await db.Assignments.FindAsync([id], cancellationToken);
        if (assignment is not null) db.Assignments.Remove(assignment);
    }

    async Task IAssignmentRepository.ReplaceEmployeeAsync(Guid assignmentId, Guid employeeId, CancellationToken cancellationToken)
    {
        var assignment = await db.Assignments.SingleOrDefaultAsync(item => item.Id == assignmentId, cancellationToken);
        assignment?.ReassignTo(employeeId);
    }

    async Task<SwapRequest?> ISwapRequestRepository.GetAsync(Guid id, CancellationToken cancellationToken)
        => await db.SwapRequests.SingleOrDefaultAsync(request => request.Id == id, cancellationToken);

    async Task<IReadOnlyCollection<SwapRequest>> ISwapRequestRepository.GetPendingAsync(CancellationToken cancellationToken)
        => await db.SwapRequests.AsNoTracking().Where(request => request.Status == SwapStatus.Pending).OrderBy(request => request.RequestedAtUtc).ToArrayAsync(cancellationToken);

    Task<SwapRequest> ISwapRequestRepository.AddAsync(SwapRequest request, CancellationToken cancellationToken)
    {
        db.SwapRequests.Add(request);
        return Task.FromResult(request);
    }

    Task ISwapRequestRepository.SaveAsync(SwapRequest request, CancellationToken cancellationToken)
    {
        db.SwapRequests.Update(request);
        return Task.CompletedTask;
    }

    public void AddOrganization(Organization organization) => db.Organizations.Add(organization);
    public void AddEmployee(Employee employee) => db.Employees.Add(employee);
    public void AddSite(Site site) => db.Sites.Add(site);
    public void AddShift(Shift shift) => db.Shifts.Add(shift);

    public Availability UpsertAvailability(Guid employeeId, DateOnly onDate, bool isAvailable, string? note)
    {
        var existing = db.Availabilities.SingleOrDefault(item => item.EmployeeId == employeeId && item.Date == onDate);
        if (existing is not null)
        {
            existing.Update(isAvailable, note);
            return existing;
        }

        var availability = Availability.Create(employeeId, onDate, isAvailable, note);
        db.Availabilities.Add(availability);
        return availability;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
