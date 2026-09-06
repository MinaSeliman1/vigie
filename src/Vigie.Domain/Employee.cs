namespace Vigie.Domain;

public sealed class Employee
{
    private Employee() { Name = string.Empty; Email = string.Empty; PasswordHash = string.Empty; }

    private Employee(Guid id, string name, string email, EmployeeRole role, decimal weeklyQuotaHours)
    {
        Id = id;
        Name = name;
        Email = email;
        Role = role;
        WeeklyQuotaHours = weeklyQuotaHours;
        PasswordHash = string.Empty;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string Name { get; private set; }
    public string Email { get; private set; }
    public EmployeeRole Role { get; private set; }
    public decimal WeeklyQuotaHours { get; private set; }
    public string PasswordHash { get; private set; }
    public bool IsDemoAccount { get; private set; }

    public static Employee Create(Guid id, string name, string email, EmployeeRole role, decimal weeklyQuotaHours, Guid? organizationId = null, bool isDemoAccount = false)
    {
        if (id == Guid.Empty) throw new DomainException("L'identifiant de l'employé est obligatoire.");
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Le nom de l'employé est obligatoire.");
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@')) throw new DomainException("Le courriel de l'employé est invalide.");
        if (weeklyQuotaHours <= 0 || weeklyQuotaHours > 168) throw new DomainException("Le quota hebdomadaire doit être compris entre 0 et 168 heures.");
        var employee = new Employee(id, name.Trim(), email.Trim().ToLowerInvariant(), role, weeklyQuotaHours)
        {
            IsDemoAccount = isDemoAccount
        };
        if (organizationId.HasValue) employee.SetOrganization(organizationId.Value);
        return employee;
    }

    public void SetOrganization(Guid organizationId)
    {
        if (organizationId == Guid.Empty) throw new DomainException("L'organisation de l'employé est obligatoire.");
        OrganizationId = organizationId;
    }

    public void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash)) throw new DomainException("Le mot de passe haché est obligatoire.");
        PasswordHash = passwordHash.Trim();
    }
}
