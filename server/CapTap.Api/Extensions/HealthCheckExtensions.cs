using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CapTap.Api.Extensions;

public static class HealthCheckExtensions
{
    public static IEndpointRouteBuilder MapCapTapHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        // Liveness: process is up (ALB / k8s / Docker HEALTHCHECK).
        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = WriteSimpleStatusAsync
        });

        // Readiness: dependencies (PostgreSQL via EF).
        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Name.Equals("database", StringComparison.OrdinalIgnoreCase),
            ResponseWriter = WriteHealthResponseAsync
        });

        // Backward-compatible aggregate (same payload as ready).
        endpoints.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = WriteHealthResponseAsync
        });

        return endpoints;
    }

    private static Task WriteSimpleStatusAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";
        var status = report.Status == HealthStatus.Healthy ? "healthy" : "unhealthy";
        return context.Response.WriteAsync(
            JsonSerializer.Serialize(new { status, check = "live" }));
    }

    private static async Task WriteHealthResponseAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var status = report.Status == HealthStatus.Healthy ? "healthy" : "unhealthy";

        var payload = new
        {
            status,
            database = report.Entries.TryGetValue("database", out var databaseEntry)
                ? databaseEntry.Status == HealthStatus.Healthy ? "connected" : "unavailable"
                : "unknown",
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString().ToLowerInvariant()
            })
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
