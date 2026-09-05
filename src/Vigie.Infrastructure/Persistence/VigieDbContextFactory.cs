using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Vigie.Infrastructure.Persistence;

/// <summary>
/// Permet à dotnet-ef de générer les migrations sans démarrer l’API ni exiger une base active.
/// </summary>
public sealed class VigieDbContextFactory : IDesignTimeDbContextFactory<VigieDbContext>
{
    public VigieDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<VigieDbContext>()
            .UseNpgsql("Host=localhost;Port=54329;Database=vigie;Username=vigie")
            .Options;

        return new VigieDbContext(options);
    }
}
