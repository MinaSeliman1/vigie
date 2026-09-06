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
    IReadOnlyCollection<Sector> Sectors { get; }
    IReadOnlyCollection<OrganizationMembership> Memberships { get; }
    IReadOnlyCollection<Shift> Shifts { get; }
    IReadOnlyCollection<CertificationType> CertificationTypes { get; }
    IReadOnlyCollection<Certification> Certifications { get; }
    IReadOnlyCollection<Assignment> Assignments { get; }
    IReadOnlyCollection<SwapRequest> SwapRequests { get; }
    IReadOnlyCollection<Availability> Availabilities { get; }
    IReadOnlyCollection<Notification> Notifications { get; }

    void AddOrganization(Organization organization);
    void AddAuditEntry(AuditEntry entry);
    void AddEmployee(Employee employee);
    void AddInvitation(Invitation invitation);
    void UpdateInvitation(Invitation invitation);
    void AddSite(Site site);
    void AddSector(Sector sector);
    void UpdateSector(Sector sector);
    void AddMembership(OrganizationMembership membership);
    void UpdateMembership(OrganizationMembership membership);
    void AddShift(Shift shift);
    void AddNotification(Notification notification);
    void UpdateNotification(Notification notification);
    Availability UpsertAvailability(Guid employeeId, DateOnly onDate, bool isAvailable, string? note);
}
