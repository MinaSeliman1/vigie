namespace Vigie.Domain;

public sealed class CertificationType
{
    private CertificationType(Guid id, string name, bool isRequired)
    {
        Id = id;
        Name = name;
        IsRequired = isRequired;
    }

    public Guid Id { get; }
    public string Name { get; private set; }
    public bool IsRequired { get; private set; }

    public static CertificationType Create(Guid id, string name, bool isRequired)
    {
        if (id == Guid.Empty) throw new DomainException("L'identifiant du type de certification est obligatoire.");
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Le nom de la certification est obligatoire.");
        return new CertificationType(id, name.Trim(), isRequired);
    }
}
