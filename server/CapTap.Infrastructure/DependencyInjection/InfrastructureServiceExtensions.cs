using CapTap.Application.Interfaces;
using CapTap.Infrastructure.Configuration;
using CapTap.Infrastructure.Persistence;
using CapTap.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CapTap.Infrastructure.DependencyInjection;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<DatabaseSettings>(configuration.GetSection(DatabaseSettings.SectionName));
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<ApplicationSettings>(configuration.GetSection(ApplicationSettings.SectionName));

        // Allow env-var overrides used by local .env / deployment secrets.
        services.PostConfigure<DatabaseSettings>(settings =>
        {
            var envConnection = Environment.GetEnvironmentVariable("DATABASE_CONNECTION");
            if (!string.IsNullOrWhiteSpace(envConnection))
            {
                settings.ConnectionString = envConnection;
            }
        });

        services.PostConfigure<JwtSettings>(settings =>
        {
            var envSecret = Environment.GetEnvironmentVariable("JWT_SECRET");
            if (!string.IsNullOrWhiteSpace(envSecret))
            {
                settings.Secret = envSecret;
            }
        });

        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    public static IServiceCollection AddDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
        {
            var databaseSettings = serviceProvider.GetRequiredService<IOptions<DatabaseSettings>>().Value;
            var connectionString = ResolveConnectionString(configuration, databaseSettings);

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Database connection string is not configured. Set DatabaseSettings:ConnectionString or DATABASE_CONNECTION.");
            }

            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
            });
        });

        services.AddHealthChecks()
            .AddDbContextCheck<ApplicationDbContext>("database");

        return services;
    }

    private static string ResolveConnectionString(
        IConfiguration configuration,
        DatabaseSettings databaseSettings)
    {
        if (!string.IsNullOrWhiteSpace(databaseSettings.ConnectionString))
        {
            return databaseSettings.ConnectionString;
        }

        var envConnection = Environment.GetEnvironmentVariable("DATABASE_CONNECTION");
        if (!string.IsNullOrWhiteSpace(envConnection))
        {
            return envConnection;
        }

        return configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
    }
}
