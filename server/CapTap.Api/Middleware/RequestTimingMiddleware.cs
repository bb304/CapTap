using System.Diagnostics;

namespace CapTap.Api.Middleware;

/// <summary>
/// Lightweight request timing. Never logs Authorization headers, tokens, or bodies
/// (medication / health data must not appear in operational logs).
/// </summary>
public sealed class RequestTimingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestTimingMiddleware> _logger;

    public RequestTimingMiddleware(RequestDelegate next, ILogger<RequestTimingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var started = Stopwatch.GetTimestamp();

        try
        {
            await _next(context);
        }
        finally
        {
            var elapsedMs = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            var path = context.Request.Path.HasValue ? context.Request.Path.Value! : "/";
            var method = context.Request.Method;
            var status = context.Response.StatusCode;

            // Skip noisy health probes at Information level.
            if (path.StartsWith("/health", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug(
                    "{Method} {Path} => {StatusCode} in {ElapsedMs:0}ms",
                    method,
                    path,
                    status,
                    elapsedMs);
            }
            else
            {
                _logger.LogInformation(
                    "{Method} {Path} => {StatusCode} in {ElapsedMs:0}ms",
                    method,
                    path,
                    status,
                    elapsedMs);
            }
        }
    }
}
