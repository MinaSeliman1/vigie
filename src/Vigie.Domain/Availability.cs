namespace Vigie.Domain;

public sealed class Availability
{
    private Availability(Guid id, Guid employeeId, DateOnly date, bool isAvailable, string? note)
    {
        Id = id;
        EmployeeId = employeeId;
        Date = date;
        IsAvailable = isAvailable;
        Note = note?.Trim();
    }

    public Guid Id { get; }
    public Guid EmployeeId { get; }
    public DateOnly Date { get; }
    public bool IsAvailable { get; private set; }
    public string? Note { get; private set; }

    public static Availability Create(Guid employeeId, DateOnly date, bool isAvailable, string? note = null)
        => new(Guid.NewGuid(), employeeId, date, isAvailable, note);

    public void Update(bool isAvailable, string? note)
    {
        IsAvailable = isAvailable;
        Note = note?.Trim();
    }
}
