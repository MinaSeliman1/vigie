using Vigie.Domain;

namespace Vigie.Application;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

public interface IEmployeeRepository
{
    Task<Employee?> GetAsync(Guid id, CancellationToken cancellationToken);
}

public interface ISiteRepository
{
    Task<Site?> GetAsync(Guid id, CancellationToken cancellationToken);
}

public interface IShiftRepository
{
    Task<Shift?> GetAsync(Guid id, CancellationToken cancellationToken);
}

public interface ICertificationRepository
{
    Task<IReadOnlyCollection<Certification>> GetForEmployeeAsync(Guid employeeId, CancellationToken cancellationToken);
}

public interface ICertificationTypeRepository
{
    Task<IReadOnlyCollection<CertificationType>> GetRequiredForSiteAsync(Guid siteId, CancellationToken cancellationToken);
}

public interface IAssignmentRepository
{
    Task<IReadOnlyCollection<ScheduledAssignment>> GetForEmployeeAsync(Guid employeeId, CancellationToken cancellationToken);
    Task<Assignment?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<Assignment> AddAsync(Assignment assignment, CancellationToken cancellationToken);
    Task RemoveAsync(Guid id, CancellationToken cancellationToken);
    Task ReplaceEmployeeAsync(Guid assignmentId, Guid employeeId, CancellationToken cancellationToken);
}

public interface ISwapRequestRepository
{
    Task<SwapRequest?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<SwapRequest>> GetPendingAsync(CancellationToken cancellationToken);
    Task<SwapRequest> AddAsync(SwapRequest request, CancellationToken cancellationToken);
    Task SaveAsync(SwapRequest request, CancellationToken cancellationToken);
}

public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
