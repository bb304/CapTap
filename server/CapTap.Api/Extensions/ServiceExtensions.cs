using System.Text;
using System.Threading.RateLimiting;
using CapTap.Api.Services;
using CapTap.Application.DependencyInjection;
using CapTap.Application.Interfaces;
using CapTap.Infrastructure.Configuration;
using CapTap.Infrastructure.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace CapTap.Api.Extensions;

public static class ServiceExtensions
{
    private static readonly HashSet<string> WeakJwtSecrets = new(StringComparer.Ordinal)
    {
        "dev-only-change-me-to-a-long-random-secret",
        "your_secret",
        "changeme",
        "secret",
        "jwt_secret"
    };

    public static IServiceCollection AddCapTapServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services
            .AddApplicationServices(configuration)
            .AddInfrastructureServices(configuration, environment)
            .AddDatabase(configuration)
            .AddCapTapAuthentication(configuration, environment)
            .AddCapTapRateLimiting();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        services.AddControllers();
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
                policy.AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowAnyOrigin());
        });

        return services;
    }

    public static IServiceCollection AddCapTapAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();
        ApplyJwtEnvOverrides(jwtSettings);
        EnsureStrongJwtSecret(jwtSettings.Secret, environment);

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        services.AddAuthorization();
        return services;
    }

    public static IServiceCollection AddCapTapRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("auth", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));
        });

        return services;
    }

    internal static void ApplyJwtEnvOverrides(JwtSettings jwtSettings)
    {
        var envSecret = Environment.GetEnvironmentVariable("JWT_SECRET");
        if (!string.IsNullOrWhiteSpace(envSecret))
        {
            jwtSettings.Secret = envSecret;
        }

        var envIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER");
        if (!string.IsNullOrWhiteSpace(envIssuer))
        {
            jwtSettings.Issuer = envIssuer;
        }

        var envAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE");
        if (!string.IsNullOrWhiteSpace(envAudience))
        {
            jwtSettings.Audience = envAudience;
        }
    }

    internal static void EnsureStrongJwtSecret(string? secret, IHostEnvironment environment)
    {
        if (string.IsNullOrWhiteSpace(secret) || secret.Length < 32)
        {
            throw new InvalidOperationException(
                "JWT secret is not configured or is too short. Set JWT_SECRET (min 32 characters). Do not store production secrets in source control.");
        }

        if (environment.IsProduction() && WeakJwtSecrets.Contains(secret.Trim()))
        {
            throw new InvalidOperationException(
                "Production JWT_SECRET must not use a known development placeholder. Generate a strong random secret via environment configuration.");
        }
    }
}
