using Vigie.Domain;

namespace Vigie.Application;

public sealed class AssignShiftService(
    IEmployeeRepository employees,
    ISiteRepository sites,
    IShiftRepository shifts,
    ICertificationRepository certifications,
    ICertificationTypeRepository certificationTypes,
    IAssignmentRepository assignments,
    IUnitOfWork unitOfWork)
{
    public async Task<OperationResult<Assignment>> ExecuteAsync(Guid employeeId, Guid shiftId, CancellationToken cancellationToken)
    {
        var employee = await employees.GetAsync(employeeId, cancellationToken);
        var shift = await shifts.GetAsync(shiftId, cancellationToken);
        if (employee is null || shift is null) return OperationResult<Assignment>.Failure("NOT_FOUND", "Le sauveteur ou le quart demandé est introuvable.");

        var site = await sites.GetAsync(shift.SiteId, cancellationToken);
        if (site is null) return OperationResult<Assignment>.Failure("NOT_FOUND", "Le site du quart est introuvable.");

        var context = new AssignmentContext(
            site,
            await certificationTypes.GetRequiredForSiteAsync(site.Id, cancellationToken),
            await certifications.GetForEmployeeAsync(employee.Id, cancellationToken),
            await assignments.GetForEmployeeAsync(employee.Id, cancellationToken));
        var violations = AssignmentPolicy.Validate(new AssignmentCandidate(employee, shift), context);
        if (violations.Count > 0) return OperationResult<Assignment>.Failure(violations);

        var assignment = Assignment.Create(Guid.NewGuid(), shift.Id, employee.Id);
        await assignments.AddAsync(assignment, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult<Assignment>.Success(assignment);
    }
}
