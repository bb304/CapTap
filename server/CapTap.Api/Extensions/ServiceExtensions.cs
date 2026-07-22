using CapTap.Application.DependencyInjection;
using CapTap.Infrastructure.DependencyInjection;

namespace CapTap.Api.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddCapTapServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddApplicationServices()
            .AddInfrastructureServices(configuration)
            .AddDatabase(configuration);

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
}
