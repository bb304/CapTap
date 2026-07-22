using CapTap.Application.Interfaces;
using CapTap.Infrastructure.Configuration;
using CapTap.Infrastructure.Persistence;
using CapTap.Infrastructure.Repositories;
using CapTap.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace CapTap.Infrastructure.DependencyInjection;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.Configure<DatabaseSettings>(configuration.GetSection(DatabaseSettings.SectionName));
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<ApplicationSettings>(configuration.GetSection(ApplicationSettings.SectionName));
        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));

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

            var envIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER");
            if (!string.IsNullOrWhiteSpace(envIssuer))
            {
                settings.Issuer = envIssuer;
            }

            var envAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE");
            if (!string.IsNullOrWhiteSpace(envAudience))
            {
                settings.Audience = envAudience;
            }
        });

        services.PostConfigure<EmailSettings>(settings =>
        {
            ApplyEmailEnvOverrides(settings);

            if (environment.IsProduction() &&
                !string.Equals(settings.Provider, "Smtp", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Production requires EmailSettings:Provider=Smtp with real SMTP credentials via environment variables.");
            }
        });

        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuditService, AuditService>();

        services.AddScoped<IEmailService>(sp =>
        {
            var emailSettings = sp.GetRequiredService<IOptions<EmailSettings>>().Value;
            if (string.Equals(emailSettings.Provider, "Smtp", StringComparison.OrdinalIgnoreCase))
            {
                return sp.GetRequiredService<SmtpEmailService>();
            }

            return sp.GetRequiredService<MockEmailService>();
        });
        services.AddScoped<SmtpEmailService>();
        services.AddScoped<MockEmailService>();

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

    private static void ApplyEmailEnvOverrides(EmailSettings settings)
    {
        var provider = Environment.GetEnvironmentVariable("EMAIL_PROVIDER");
        if (!string.IsNullOrWhiteSpace(provider))
        {
            settings.Provider = provider;
        }

        var host = Environment.GetEnvironmentVariable("EMAIL_HOST");
        if (!string.IsNullOrWhiteSpace(host))
        {
            settings.Host = host;
        }

        var port = Environment.GetEnvironmentVariable("EMAIL_PORT");
        if (int.TryParse(port, out var parsedPort))
        {
            settings.Port = parsedPort;
        }

        var username = Environment.GetEnvironmentVariable("EMAIL_USERNAME");
        if (!string.IsNullOrWhiteSpace(username))
        {
            settings.Username = username;
        }

        var password = Environment.GetEnvironmentVariable("EMAIL_PASSWORD");
        if (!string.IsNullOrWhiteSpace(password))
        {
            settings.Password = password;
        }

        var fromEmail = Environment.GetEnvironmentVariable("EMAIL_FROM");
        if (!string.IsNullOrWhiteSpace(fromEmail))
        {
            settings.FromEmail = fromEmail;
        }

        var fromName = Environment.GetEnvironmentVariable("EMAIL_FROM_NAME");
        if (!string.IsNullOrWhiteSpace(fromName))
        {
            settings.FromName = fromName;
        }

        var appBaseUrl = Environment.GetEnvironmentVariable("EMAIL_APP_BASE_URL");
        if (!string.IsNullOrWhiteSpace(appBaseUrl))
        {
            settings.AppBaseUrl = appBaseUrl;
        }

        var useSsl = Environment.GetEnvironmentVariable("EMAIL_USE_SSL");
        if (bool.TryParse(useSsl, out var parsedSsl))
        {
            settings.UseSsl = parsedSsl;
        }
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
