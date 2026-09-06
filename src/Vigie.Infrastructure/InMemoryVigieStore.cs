using Vigie.Application;
using Vigie.Application.Auth;
using Vigie.Domain;

namespace Vigie.Infrastructure;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

public sealed class InMemoryVigieStore :
    IVigieStore
{
    private readonly object sync = new();
    private readonly Dictionary<Guid, Organization> organizations = [];
    private readonly Dictionary<Guid, AuditEntry> auditEntries = [];
    private readonly Dictionary<Guid, Invitation> invitations = [];
    private readonly Dictionary<Guid, Employee> employees = [];
    private readonly Dictionary<Guid, Site> sites = [];
    private readonly Dictionary<Guid, Shift> shifts = [];
    private readonly Dictionary<Guid, CertificationType> certificationTypes = [];
    private readonly Dictionary<Guid, Certification> certifications = [];
    private readonly Dictionary<Guid, Assignment> assignments = [];
    private readonly Dictionary<Guid, SwapRequest> swapRequests = [];
    private readonly Dictionary<Guid, Availability> availabilities = [];
    private readonly Dictionary<Guid, HashSet<Guid>> siteCertificationTypes = [];

    public InMemoryVigieStore()
    {
        Seed();
    }

    public IReadOnlyCollection<Organization> Organizations => organizations.Values.ToArray();
    public IReadOnlyCollection<AuditEntry> AuditEntries => auditEntries.Values.ToArray();
    public IReadOnlyCollection<Invitation> Invitations => invitations.Values.ToArray();
    public IReadOnlyCollection<Employee> Employees => employees.Values.ToArray();
    public IReadOnlyCollection<Site> Sites => sites.Values.ToArray();
    public IReadOnlyCollection<Shift> Shifts => shifts.Values.ToArray();
    public IReadOnlyCollection<CertificationType> CertificationTypes => certificationTypes.Values.ToArray();
    public IReadOnlyCollection<Certification> Certifications => certifications.Values.ToArray();
    public IReadOnlyCollection<Assignment> Assignments => assignments.Values.ToArray();
    public IReadOnlyCollection<SwapRequest> SwapRequests => swapRequests.Values.ToArray();
    public IReadOnlyCollection<Availability> Availabilities => availabilities.Values.ToArray();
    public IReadOnlyCollection<(Guid SiteId, Guid CertificationTypeId)> SiteCertificationLinks
        => siteCertificationTypes.SelectMany(pair => pair.Value.Select(typeId => (pair.Key, typeId))).ToArray();

    public Task<Employee?> GetAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(employees.GetValueOrDefault(id));
    Task<Site?> ISiteRepository.GetAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(sites.GetValueOrDefault(id));
    Task<Shift?> IShiftRepository.GetAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(shifts.GetValueOrDefault(id));

    Task<IReadOnlyCollection<Certification>> ICertificationRepository.GetForEmployeeAsync(Guid employeeId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyCollection<Certification>>(certifications.Values.Where(c => c.EmployeeId == employeeId).ToArray());

    Task<IReadOnlyCollection<CertificationType>> ICertificationTypeRepository.GetRequiredForSiteAsync(Guid siteId, CancellationToken cancellationToken)
    {
        var ids = siteCertificationTypes.GetValueOrDefault(siteId) ?? [];
        return Task.FromResult<IReadOnlyCollection<CertificationType>>(ids.Select(id => certificationTypes[id]).ToArray());
    }

    Task<IReadOnlyCollection<ScheduledAssignment>> IAssignmentRepository.GetForEmployeeAsync(Guid employeeId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyCollection<ScheduledAssignment>>(assignments.Values.Where(a => a.EmployeeId == employeeId && shifts.ContainsKey(a.ShiftId)).Select(a => new ScheduledAssignment(employeeId, shifts[a.ShiftId])).ToArray());

    Task<Assignment?> IAssignmentRepository.GetAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(assignments.GetValueOrDefault(id));

    Task<Assignment> IAssignmentRepository.AddAsync(Assignment assignment, CancellationToken cancellationToken)
    {
        lock (sync) assignments[assignment.Id] = assignment;
        return Task.FromResult(assignment);
    }

    Task IAssignmentRepository.RemoveAsync(Guid id, CancellationToken cancellationToken)
    {
        lock (sync) assignments.Remove(id);
        return Task.CompletedTask;
    }

    Task IAssignmentRepository.ReplaceEmployeeAsync(Guid assignmentId, Guid employeeId, CancellationToken cancellationToken)
    {
        lock (sync)
        {
            if (assignments.TryGetValue(assignmentId, out var assignment)) assignment.ReassignTo(employeeId);
        }
        return Task.CompletedTask;
    }

    Task<SwapRequest?> ISwapRequestRepository.GetAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(swapRequests.GetValueOrDefault(id));
    Task<IReadOnlyCollection<SwapRequest>> ISwapRequestRepository.GetPendingAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyCollection<SwapRequest>>(swapRequests.Values.Where(r => r.Status == SwapStatus.Pending).OrderBy(r => r.RequestedAtUtc).ToArray());
    Task<SwapRequest> ISwapRequestRepository.AddAsync(SwapRequest request, CancellationToken cancellationToken)
    {
        lock (sync) swapRequests[request.Id] = request;
        return Task.FromResult(request);
    }
    Task ISwapRequestRepository.SaveAsync(SwapRequest request, CancellationToken cancellationToken) => Task.CompletedTask;
    Task IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public void AddOrganization(Organization organization) => organizations[organization.Id] = organization;
    public void AddAuditEntry(AuditEntry entry) => auditEntries[entry.Id] = entry;
    public void AddEmployee(Employee employee) => employees[employee.Id] = employee;
    public void AddInvitation(Invitation invitation) => invitations[invitation.Id] = invitation;
    public void UpdateInvitation(Invitation invitation) => invitations[invitation.Id] = invitation;
    public void AddSite(Site site) => sites[site.Id] = site;
    public void AddShift(Shift shift) => shifts[shift.Id] = shift;
    public void AddCertification(Certification certification) => certifications[certification.Id] = certification;
    public void AddCertificationTypeForSite(Guid siteId, Guid certificationTypeId) => siteCertificationTypes.GetOrAdd(siteId).Add(certificationTypeId);

    public Availability UpsertAvailability(Guid employeeId, DateOnly onDate, bool isAvailable, string? note)
    {
        var existing = availabilities.Values.SingleOrDefault(a => a.EmployeeId == employeeId && a.Date == onDate);
        if (existing is not null) { existing.Update(isAvailable, note); return existing; }
        var availability = Availability.Create(employeeId, onDate, isAvailable, note);
        availabilities[availability.Id] = availability;
        return availability;
    }

    private void Seed()
    {
        var organization = Organization.Create(Guid.Parse("00000000-0000-0000-0000-000000000001"), "Vigie — démonstration", "vigie-demo");
        organizations[organization.Id] = organization;
        var coord = Employee.Create(Guid.Parse("10000000-0000-0000-0000-000000000001"), "Camille Gagnon", "coordonnateur@vigie.demo", EmployeeRole.Coordinator, 40, organization.Id, true);
        var amelie = Employee.Create(Guid.Parse("10000000-0000-0000-0000-000000000002"), "Amélie Roy", "amelie@vigie.demo", EmployeeRole.Lifeguard, 24, organization.Id, true);
        var noah = Employee.Create(Guid.Parse("10000000-0000-0000-0000-000000000003"), "Noah Tremblay", "noah@vigie.demo", EmployeeRole.Lifeguard, 32, organization.Id, true);
        var sofia = Employee.Create(Guid.Parse("10000000-0000-0000-0000-000000000004"), "Sofia Nguyen", "sofia@vigie.demo", EmployeeRole.Lifeguard, 20, organization.Id, true);
        foreach (var demoEmployee in new[] { coord, amelie, noah, sofia }) demoEmployee.SetPasswordHash(PasswordHasher.Hash("vigie-demo"));
        foreach (var employee in new[] { coord, amelie, noah, sofia }) employees[employee.Id] = employee;

        var nord = Site.Create(Guid.Parse("20000000-0000-0000-0000-000000000001"), "Piscine du Nord", "Eastern Standard Time", OpeningSeason.AllYear, SiteType.Indoor, organization.Id);
        var parc = Site.Create(Guid.Parse("20000000-0000-0000-0000-000000000002"), "Bassin du parc", "Eastern Standard Time", new OpeningSeason(5, 15, 9, 15), SiteType.Outdoor, organization.Id);
        sites[nord.Id] = nord; sites[parc.Id] = parc;

        var firstAid = CertificationType.Create(Guid.Parse("30000000-0000-0000-0000-000000000001"), "Premiers soins", true);
        var lifeguard = CertificationType.Create(Guid.Parse("30000000-0000-0000-0000-000000000002"), "Sauveteur national", true);
        certificationTypes[firstAid.Id] = firstAid; certificationTypes[lifeguard.Id] = lifeguard;
        AddCertificationTypeForSite(nord.Id, firstAid.Id); AddCertificationTypeForSite(nord.Id, lifeguard.Id);
        AddCertificationTypeForSite(parc.Id, firstAid.Id); AddCertificationTypeForSite(parc.Id, lifeguard.Id);
        AddCertification(Certification.Create(amelie.Id, firstAid.Id, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(75))));
        AddCertification(Certification.Create(amelie.Id, lifeguard.Id, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(220))));
        AddCertification(Certification.Create(noah.Id, firstAid.Id, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(370))));
        AddCertification(Certification.Create(noah.Id, lifeguard.Id, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(370))));
        AddCertification(Certification.Create(sofia.Id, firstAid.Id, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20))));
        AddCertification(Certification.Create(sofia.Id, lifeguard.Id, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20))));

        var today = DateTimeOffset.UtcNow.Date;
        var monday = today.AddDays(-(((int)DateTimeOffset.UtcNow.DayOfWeek + 6) % 7));
        if (monday <= today) monday = monday.AddDays(7);
        var shiftsToAdd = new[]
        {
            Shift.Create(Guid.Parse("40000000-0000-0000-0000-000000000001"), nord.Id, new DateTimeOffset(monday.AddDays(1).AddHours(13), TimeSpan.Zero), new DateTimeOffset(monday.AddDays(1).AddHours(21), TimeSpan.Zero), 2),
            Shift.Create(Guid.Parse("40000000-0000-0000-0000-000000000002"), nord.Id, new DateTimeOffset(monday.AddDays(2).AddHours(13), TimeSpan.Zero), new DateTimeOffset(monday.AddDays(2).AddHours(21), TimeSpan.Zero), 2),
            Shift.Create(Guid.Parse("40000000-0000-0000-0000-000000000003"), parc.Id, new DateTimeOffset(monday.AddDays(4).AddHours(14), TimeSpan.Zero), new DateTimeOffset(monday.AddDays(4).AddHours(22), TimeSpan.Zero), 2),
            Shift.Create(Guid.Parse("40000000-0000-0000-0000-000000000004"), nord.Id, new DateTimeOffset(monday.AddDays(5).AddHours(12), TimeSpan.Zero), new DateTimeOffset(monday.AddDays(5).AddHours(20), TimeSpan.Zero), 2)
        };
        foreach (var shift in shiftsToAdd) shifts[shift.Id] = shift;
        assignments[Guid.Parse("50000000-0000-0000-0000-000000000001")] = Assignment.Create(Guid.Parse("50000000-0000-0000-0000-000000000001"), shiftsToAdd[0].Id, amelie.Id);
        assignments[Guid.Parse("50000000-0000-0000-0000-000000000002")] = Assignment.Create(Guid.Parse("50000000-0000-0000-0000-000000000002"), shiftsToAdd[1].Id, noah.Id);
        var demoSwap = SwapRequest.Create(Guid.Parse("60000000-0000-0000-0000-000000000001"), Guid.Parse("50000000-0000-0000-0000-000000000001"), noah.Id);
        swapRequests[demoSwap.Id] = demoSwap;

        var auditNow = DateTimeOffset.UtcNow;
        AddAuditEntry(AuditEntry.Create(Guid.Parse("70000000-0000-0000-0000-000000000001"), organization.Id, coord.Id, "organization.created", "Organization", organization.Id, null, auditNow.AddDays(-30)));
        AddAuditEntry(AuditEntry.Create(Guid.Parse("70000000-0000-0000-0000-000000000002"), organization.Id, coord.Id, "shift.created", "Shift", shiftsToAdd[0].Id, null, auditNow.AddDays(-4)));
        AddAuditEntry(AuditEntry.Create(Guid.Parse("70000000-0000-0000-0000-000000000003"), organization.Id, coord.Id, "assignment.created", "Assignment", Guid.Parse("50000000-0000-0000-0000-000000000001"), $"employé={amelie.Name}", auditNow.AddDays(-3)));
        AddAuditEntry(AuditEntry.Create(Guid.Parse("70000000-0000-0000-0000-000000000004"), organization.Id, amelie.Id, "swap.created", "SwapRequest", demoSwap.Id, $"receveur={noah.Name}", auditNow.AddHours(-8)));
    }
}

internal static class DictionaryExtensions
{
    public static HashSet<TValue> GetOrAdd<TKey, TValue>(this Dictionary<TKey, HashSet<TValue>> dictionary, TKey key) where TKey : notnull
    {
        if (!dictionary.TryGetValue(key, out var value)) dictionary[key] = value = [];
        return value;
    }
}
