using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vigie.Application;

namespace Vigie.Infrastructure.Persistence;

public static class VigiePersistenceExtensions
{
    public static IServiceCollection AddVigiePersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Vigie");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddSingleton<InMemoryVigieStore>();
            services.AddSingleton<IVigieStore>(sp => sp.GetRequiredService<InMemoryVigieStore>());
        }
        else
        {
            services.AddDbContext<VigieDbContext>(options => options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly(typeof(VigieDbContext).Assembly.FullName)));
            services.AddScoped<EfVigieStore>();
            services.AddScoped<IVigieStore>(sp => sp.GetRequiredService<EfVigieStore>());
        }

        services.AddScoped<IEmployeeRepository>(sp => sp.GetRequiredService<IVigieStore>());
        services.AddScoped<ISiteRepository>(sp => sp.GetRequiredService<IVigieStore>());
        services.AddScoped<IShiftRepository>(sp => sp.GetRequiredService<IVigieStore>());
        services.AddScoped<ICertificationRepository>(sp => sp.GetRequiredService<IVigieStore>());
        services.AddScoped<ICertificationTypeRepository>(sp => sp.GetRequiredService<IVigieStore>());
        services.AddScoped<IAssignmentRepository>(sp => sp.GetRequiredService<IVigieStore>());
        services.AddScoped<ISwapRequestRepository>(sp => sp.GetRequiredService<IVigieStore>());
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<IVigieStore>());
        return services;
    }
}
