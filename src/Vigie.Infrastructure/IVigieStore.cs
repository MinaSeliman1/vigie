using Vigie.Application;
using Vigie.Domain;

namespace Vigie.Infrastructure;

/// <summary>
/// Point d'entrée partagé par le store mémoire de démonstration et le store PostgreSQL.
/// Les cas d'usage dépendent des ports Application; l'API utilise ces vues de lecture pour
/// composer ses réponses HTTP sans connaître le moteur de persistance.
/// </summary>
public interface IVigieStore :
    IEmployeeRepository,
    ISiteRepository,
    IShiftRepository,
    ICertificationRepository,
    ICertificationTypeRepository,
    IAssignmentRepository,
    ISwapRequestRepository,
    IUnitOfWork
{
    IReadOnlyCollection<Organization> Organizations { get; }
    IReadOnlyCollection<AuditEntry> AuditEntries { get; }
    IReadOnlyCollection<Invitation> Invitations { get; }
    IReadOnlyCollection<Employee> Employees { get; }
    IReadOnlyCollection<Site> Sites { get; }
    IReadOnlyCollection<Shift> Shifts { get; }
    IReadOnlyCollection<CertificationType> CertificationTypes { get; }
    IReadOnlyCollection<Certification> Certifications { get; }
    IReadOnlyCollection<Assignment> Assignments { get; }
    IReadOnlyCollection<SwapRequest> SwapRequests { get; }
    IReadOnlyCollection<Availability> Availabilities { get; }

    void AddOrganization(Organization organization);
    void AddAuditEntry(AuditEntry entry);
    void AddEmployee(Employee employee);
    void AddInvitation(Invitation invitation);
    void UpdateInvitation(Invitation invitation);
    void AddSite(Site site);
    void AddShift(Shift shift);
    Availability UpsertAvailability(Guid employeeId, DateOnly onDate, bool isAvailable, string? note);
}
