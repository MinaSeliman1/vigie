namespace Vigie.Domain;

public sealed class Certification
{
    private Certification() { }

    private Certification(Guid id, Guid employeeId, Guid certificationTypeId, DateOnly expiresOn)
    {
        Id = id;
        EmployeeId = employeeId;
        CertificationTypeId = certificationTypeId;
        ExpiresOn = expiresOn;
    }

    public Guid Id { get; private set; }
    public Guid EmployeeId { get; private set; }
    public Guid CertificationTypeId { get; private set; }
    public DateOnly ExpiresOn { get; private set; }

    public static Certification Create(Guid employeeId, Guid certificationTypeId, DateOnly expiresOn)
        => Create(Guid.NewGuid(), employeeId, certificationTypeId, expiresOn);

    public static Certification Create(Guid id, Guid employeeId, Guid certificationTypeId, DateOnly expiresOn)
    {
        if (id == Guid.Empty || employeeId == Guid.Empty || certificationTypeId == Guid.Empty)
            throw new DomainException("Les identifiants de certification sont obligatoires.");
        return new Certification(id, employeeId, certificationTypeId, expiresOn);
    }
}
