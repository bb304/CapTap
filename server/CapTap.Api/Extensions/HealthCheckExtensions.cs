using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CapTap.Api.Extensions;

public static class HealthCheckExtensions
{
    public static IEndpointRouteBuilder MapCapTapHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = WriteHealthResponseAsync
        });

        return endpoints;
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
