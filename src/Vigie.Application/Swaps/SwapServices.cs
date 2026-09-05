using Vigie.Domain;

namespace Vigie.Application;

public sealed class RequestSwapService(IAssignmentRepository assignments, ISwapRequestRepository swaps, IUnitOfWork unitOfWork)
{
    public async Task<OperationResult<SwapRequest>> ExecuteAsync(Guid requesterId, Guid assignmentId, Guid receiverId, CancellationToken cancellationToken)
    {
        var assignment = await assignments.GetAsync(assignmentId, cancellationToken);
        if (assignment is null) return OperationResult<SwapRequest>.Failure("NOT_FOUND", "L'assignation demandée est introuvable.");
        if (assignment.EmployeeId != requesterId) return OperationResult<SwapRequest>.Failure("FORBIDDEN", "Vous ne pouvez demander un échange que pour votre propre assignation.");
        if (receiverId == requesterId) return OperationResult<SwapRequest>.Failure("INVALID_SWAP", "Le receveur doit être un autre sauveteur.");

        var request = SwapRequest.Create(Guid.NewGuid(), assignmentId, receiverId);
        await swaps.AddAsync(request, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult<SwapRequest>.Success(request);
    }
}

public sealed class ApproveSwapService(
    IEmployeeRepository employees,
    ISiteRepository sites,
    IShiftRepository shifts,
    ICertificationRepository certifications,
    ICertificationTypeRepository certificationTypes,
    IAssignmentRepository assignments,
    ISwapRequestRepository swaps,
    IClock clock,
    IUnitOfWork unitOfWork)
{
    public async Task<OperationResult<SwapRequest>> ExecuteAsync(Guid coordinatorId, Guid requestId, CancellationToken cancellationToken)
    {
        var coordinator = await employees.GetAsync(coordinatorId, cancellationToken);
        if (coordinator?.Role != EmployeeRole.Coordinator) return OperationResult<SwapRequest>.Failure("FORBIDDEN", "Seul un coordonnateur peut approuver un échange.");
        var request = await swaps.GetAsync(requestId, cancellationToken);
        if (request is null) return OperationResult<SwapRequest>.Failure("NOT_FOUND", "La demande d'échange est introuvable.");
        if (request.Status != SwapStatus.Pending) return OperationResult<SwapRequest>.Failure("CONFLICT", "Cette demande d'échange a déjà été traitée.");
        var original = await assignments.GetAsync(request.AssignmentId, cancellationToken);
        if (original is null) return OperationResult<SwapRequest>.Failure("NOT_FOUND", "L'assignation d'origine est introuvable.");
        var receiver = await employees.GetAsync(request.ReceiverId, cancellationToken);
        var shift = await shifts.GetAsync(original.ShiftId, cancellationToken);
        if (receiver is null || shift is null) return OperationResult<SwapRequest>.Failure("NOT_FOUND", "Le receveur ou le quart est introuvable.");
        var site = await sites.GetAsync(shift.SiteId, cancellationToken);
        if (site is null) return OperationResult<SwapRequest>.Failure("NOT_FOUND", "Le site du quart est introuvable.");

        var existing = (await assignments.GetForEmployeeAsync(receiver.Id, cancellationToken)).Where(a => a.Shift.Id != shift.Id).ToArray();
        var context = new AssignmentContext(site, await certificationTypes.GetRequiredForSiteAsync(site.Id, cancellationToken), await certifications.GetForEmployeeAsync(receiver.Id, cancellationToken), existing);
        var violations = AssignmentPolicy.Validate(new AssignmentCandidate(receiver, shift), context);
        if (violations.Count > 0) return OperationResult<SwapRequest>.Failure(violations);

        request.Approve(coordinator.Id, clock.UtcNow);
        await assignments.ReplaceEmployeeAsync(original.Id, receiver.Id, cancellationToken);
        await swaps.SaveAsync(request, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult<SwapRequest>.Success(request);
    }
}

public sealed class RejectSwapService(IEmployeeRepository employees, ISwapRequestRepository swaps, IClock clock, IUnitOfWork unitOfWork)
{
    public async Task<OperationResult<SwapRequest>> ExecuteAsync(Guid coordinatorId, Guid requestId, CancellationToken cancellationToken)
    {
        var coordinator = await employees.GetAsync(coordinatorId, cancellationToken);
        if (coordinator?.Role != EmployeeRole.Coordinator) return OperationResult<SwapRequest>.Failure("FORBIDDEN", "Seul un coordonnateur peut refuser un échange.");
        var request = await swaps.GetAsync(requestId, cancellationToken);
        if (request is null) return OperationResult<SwapRequest>.Failure("NOT_FOUND", "La demande d'échange est introuvable.");
        if (request.Status != SwapStatus.Pending) return OperationResult<SwapRequest>.Failure("CONFLICT", "Cette demande d'échange a déjà été traitée.");
        request.Reject(coordinator.Id, clock.UtcNow);
        await swaps.SaveAsync(request, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OperationResult<SwapRequest>.Success(request);
    }
}
