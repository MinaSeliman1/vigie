namespace Vigie.Domain;

public enum SwapStatus
{
    Pending,
    Approved,
    Rejected,
    Cancelled
}

public sealed class SwapRequest
{
    private SwapRequest(Guid id, Guid assignmentId, Guid receiverId)
    {
        Id = id;
        AssignmentId = assignmentId;
        ReceiverId = receiverId;
        Status = SwapStatus.Pending;
        RequestedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; }
    public Guid AssignmentId { get; }
    public Guid ReceiverId { get; }
    public SwapStatus Status { get; private set; }
    public DateTimeOffset RequestedAtUtc { get; }
    public Guid? DecidedBy { get; private set; }
    public DateTimeOffset? DecidedAtUtc { get; private set; }

    public static SwapRequest Create(Guid id, Guid assignmentId, Guid receiverId)
    {
        if (id == Guid.Empty || assignmentId == Guid.Empty || receiverId == Guid.Empty)
            throw new DomainException("Les identifiants de l'échange sont obligatoires.");
        return new SwapRequest(id, assignmentId, receiverId);
    }

    public void Approve(Guid coordinatorId, DateTimeOffset nowUtc)
    {
        EnsurePending();
        Status = SwapStatus.Approved;
        DecidedBy = coordinatorId;
        DecidedAtUtc = nowUtc.ToUniversalTime();
    }

    public void Reject(Guid coordinatorId, DateTimeOffset nowUtc)
    {
        EnsurePending();
        Status = SwapStatus.Rejected;
        DecidedBy = coordinatorId;
        DecidedAtUtc = nowUtc.ToUniversalTime();
    }

    private void EnsurePending()
    {
        if (Status != SwapStatus.Pending) throw new DomainException("Cette demande d'échange a déjà été traitée.");
    }
}
