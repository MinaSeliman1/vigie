namespace Vigie.Domain;

public sealed class Assignment
{
    private Assignment(Guid id, Guid shiftId, Guid employeeId)
    {
        Id = id;
        ShiftId = shiftId;
        EmployeeId = employeeId;
    }

    public Guid Id { get; }
    public Guid ShiftId { get; }
    public Guid EmployeeId { get; private set; }

    public void ReassignTo(Guid employeeId)
    {
        if (employeeId == Guid.Empty) throw new DomainException("L'identifiant du nouvel employé est obligatoire.");
        EmployeeId = employeeId;
    }

    public static Assignment Create(Guid id, Guid shiftId, Guid employeeId)
    {
        if (id == Guid.Empty || shiftId == Guid.Empty || employeeId == Guid.Empty)
            throw new DomainException("Les identifiants d'assignation sont obligatoires.");
        return new Assignment(id, shiftId, employeeId);
    }
}
