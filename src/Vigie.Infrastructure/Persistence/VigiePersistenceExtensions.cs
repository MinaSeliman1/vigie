using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Vigie.Infrastructure.Persistence;

public static class VigiePersistenceExtensions
{
    public static IServiceCollection AddVigiePostgres(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Vigie");
        if (!string.IsNullOrWhiteSpace(connectionString))
            services.AddDbContext<VigieDbContext>(options => options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly(typeof(VigieDbContext).Assembly.FullName)));
        return services;
    }
}
