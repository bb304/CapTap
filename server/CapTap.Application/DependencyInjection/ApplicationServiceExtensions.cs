using Microsoft.Extensions.DependencyInjection;

namespace CapTap.Application.DependencyInjection;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Application services (auth, medications, NFC) will be registered in later phases.
        return services;
    }
}
