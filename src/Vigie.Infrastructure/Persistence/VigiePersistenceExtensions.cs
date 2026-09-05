using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Vigie.Application;

namespace Vigie.Infrastructure.Persistence;

public static class VigiePersistenceExtensions
{
    public static IServiceCollection AddVigiePersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = NormalizeConnectionString(configuration.GetConnectionString("Vigie"));
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

    private static string? NormalizeConnectionString(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString)) return connectionString;

        var value = connectionString.Trim();
        if (!value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
            !value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase)) return value;

        var uri = new Uri(value);
        var credentials = uri.UserInfo.Split(':', 2, StringSplitOptions.None);
        if (credentials.Length != 2)
            throw new InvalidOperationException("La chaîne PostgreSQL doit inclure un nom d'utilisateur et un mot de passe.");

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Database = uri.AbsolutePath.Trim('/'),
            Username = Uri.UnescapeDataString(credentials[0]),
            Password = Uri.UnescapeDataString(credentials[1]),
            SslMode = SslMode.Require
        };

        return builder.ConnectionString;
    }
}
