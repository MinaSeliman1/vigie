namespace Vigie.Domain;

public enum EmployeeRole
{
    Lifeguard,
    PoolChief,
    SectorManager,
    AquaticDirector,

    /// <summary>
    /// Rôle conservé pour les tokens et données créés avant la hiérarchie Laval.
    /// Il est normalisé en <see cref="PoolChief"/> par l'autorisation.
    /// </summary>
    Coordinator
}
