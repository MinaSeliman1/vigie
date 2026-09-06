using Vigie.Domain;

namespace Vigie.Infrastructure;

public sealed record LavalPoolDefinition(
    Guid SiteId,
    Guid SectorId,
    string Code,
    string Name,
    SiteType Type,
    string Address,
    string Neighborhood,
    OpeningSeason OpeningSeason);

/// <summary>
/// Catalogue de référence des installations aquatiques municipales de Laval.
/// Les horaires opérationnels restent gérés dans Vigie; ce catalogue ne remplace
/// pas les avis d'ouverture publiés par la Ville.
/// </summary>
public static class LavalPoolCatalog
{
    public const string SourceUrl = "https://www.laval.ca/sports-loisirs/sports/piscines/piscines-exterieures-jeux-eau/";
    public const string IndoorSourceUrl = "https://www.laval.ca/sports-loisirs/sports/piscines/piscines-interieures/";

    public static IReadOnlyList<LavalPoolDefinition> All { get; } =
    [
        Indoor("VAL-DES-ARBRES", "Piscine Val-des-Arbres", "1555, boulevard Saint-Martin Est", "Vimont"),
        Indoor("SABLON", "Centre du Sablon", "755, chemin du Sablon", "Chomedey"),
        Indoor("VANIER", "Piscine Vanier", "3995, boulevard Lévesque Est", "Saint-Vincent-de-Paul"),
        Indoor("POLY-JEUNESSE", "Piscine Poly-Jeunesse", "3578, boulevard Sainte-Rose", "Fabreville"),
        Indoor("HONORE-MERCIER", "Piscine Honoré-Mercier", "2465, rue Honoré-Mercier", "Sainte-Rose"),
        Indoor("JOSEE-FAUCHER", "Piscine du Centre sportif Josée-Faucher", "125A, boulevard des Prairies", "Laval-des-Rapides"),
        Indoor("COMPLEXE-AQUATIQUE", "Complexe aquatique", "2205, avenue Terry-Fox", "Chomedey"),

        Outdoor("DU-MOULIN", "Piscine du Moulin", "1125, montée du Moulin", "Saint-François"),
        Outdoor("JACQUES-BOURDON", "Piscine Jacques-Bourdon", "55, croissant De Callières", "Duvernay"),
        Outdoor("SAINT-VINCENT", "Piscine extérieure Saint-Vincent", "901, avenue du Parc", "Saint-Vincent-de-Paul"),
        Outdoor("BON-PASTEUR", "Piscine Bon-Pasteur", "70, boulevard du Bon-Pasteur", "Laval-des-Rapides"),
        Outdoor("CHENIER", "Piscine Chénier", "580, rue des Alouettes", "Pont-Viau"),
        Outdoor("EMILE", "Piscine Émile", "55, boulevard Cartier Ouest", "Laval-des-Rapides"),
        Outdoor("SAINT-CLAUDE", "Piscine Saint-Claude", "99, 7e Rue", "Laval-des-Rapides"),
        Outdoor("WILFRID-PELLETIER", "Piscine Wilfrid-Pelletier", "1865, boulevard Tessier", "Chomedey"),
        Outdoor("BERTHIAUME-DU-TREMBLAY", "Piscine Berthiaume-Du Tremblay", "4250, boulevard Lévesque Ouest", "Chomedey"),
        Outdoor("MONTCALM", "Piscine Montcalm", "755, rue Parkway", "Chomedey"),
        Outdoor("PIE-X", "Piscine Pie-X", "1175, rue du Val-Martin", "Chomedey"),
        Outdoor("COUVRETTE", "Piscine Couvrette", "665, rue des Jardins-Sainte-Dorothée", "Sainte-Dorothée"),
        Outdoor("DES-CHENES", "Piscine des Chênes", "355, rue les Érables", "Laval-sur-le-Lac"),
        Outdoor("JOLIBOURG", "Piscine Jolibourg", "1350, rue du Relais", "Sainte-Dorothée"),
        Outdoor("RAYMOND", "Piscine Raymond", "6460, 29e Avenue", "Laval-Ouest"),
        Outdoor("ROI-DU-NORD", "Piscine Roi-du-Nord", "222, boulevard du Roi-du-Nord", "Sainte-Rose"),
        Outdoor("SACRE-COEUR", "Piscine Sacré-Coeur", "3165, rue Esther", "Fabreville"),
        Outdoor("DES-SAULES", "Piscine des Saules", "100, rue Saint-Saëns Ouest", "Auteuil"),
        Outdoor("PARADIS", "Piscine Paradis", "2220, rue Marc", "Vimont"),
        Outdoor("PREVOST", "Piscine Prévost", "110, rue de Toulouse", "Laval-Ouest")
    ];

    private static LavalPoolDefinition Indoor(string code, string name, string address, string neighborhood)
        => Create(code, name, SiteType.Indoor, address, neighborhood, OpeningSeason.AllYear);

    private static LavalPoolDefinition Outdoor(string code, string name, string address, string neighborhood)
        => Create(code, name, SiteType.Outdoor, address, neighborhood, new OpeningSeason(6, 13, 9, 1));

    private static LavalPoolDefinition Create(string code, string name, SiteType type, string address, string neighborhood, OpeningSeason openingSeason)
    {
        var normalized = code.ToLowerInvariant().Replace("-", string.Empty, StringComparison.Ordinal);
        var siteId = DeterministicGuid($"site:{normalized}");
        var sectorId = DeterministicGuid($"sector:{normalized}");
        return new LavalPoolDefinition(siteId, sectorId, code, name, type, address, neighborhood, openingSeason);
    }

    private static Guid DeterministicGuid(string value)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value));
        return new Guid(bytes[..16]);
    }
}
