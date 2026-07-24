using CapTap.Application.Common;
using CapTap.Application.Interfaces;
using CapTap.Application.Services;
using CapTap.Application.Validators;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CapTap.Application.DependencyInjection;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration? configuration = null)
    {
        services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IMedicationService, MedicationService>();
        services.AddScoped<IScheduleService, ScheduleService>();
        services.AddScoped<IAdherenceService, AdherenceService>();
        services.AddScoped<IAdherenceStreakService, AdherenceStreakService>();
        services.AddScoped<IMedicationLogService, MedicationLogService>();
        services.AddScoped<INfcService, NfcService>();
        services.AddScoped<IUserProfileService, UserProfileService>();
        services.AddSingleton<ITimeProvider, SystemTimeProvider>();

        if (configuration is not null)
        {
            services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));
        }

        return services;
    }
}
