using CapTap.Api.Middleware;

namespace CapTap.Api.Extensions;

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseCapTapMiddleware(this IApplicationBuilder app)
    {
        app.UseMiddleware<ExceptionMiddleware>();
        app.UseMiddleware<RequestTimingMiddleware>();
        app.UseMiddleware<SecurityHeadersMiddleware>();
        return app;
    }
}
