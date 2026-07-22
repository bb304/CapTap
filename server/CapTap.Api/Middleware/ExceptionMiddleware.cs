using CapTap.Application.Exceptions;
using CapTap.Domain.Exceptions;
using CapTap.Shared.Constants;
using CapTap.Shared.Responses;
using System.Net;
using System.Text.Json;
using ApplicationException = CapTap.Application.Exceptions.ApplicationException;

namespace CapTap.Api.Middleware;

public sealed class ExceptionMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(
        RequestDelegate next,
        ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, code, message) = MapException(exception);

        if (statusCode >= (int)HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Unhandled server error");
        }
        else
        {
            _logger.LogWarning(exception, "Handled application error {ErrorCode}", code);
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var response = ApiResponse.Fail(code, message);

        // Never expose stack traces or database details to clients.
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }

    private static (int StatusCode, string Code, string Message) MapException(Exception exception)
    {
        return exception switch
        {
            ValidationException validationException =>
                ((int)HttpStatusCode.BadRequest, validationException.Code, validationException.Message),

            UnauthorizedException unauthorizedException =>
                ((int)HttpStatusCode.Unauthorized, unauthorizedException.Code, unauthorizedException.Message),

            UnauthorizedAccessException =>
                ((int)HttpStatusCode.Unauthorized, ErrorCodes.Unauthorized, "Authentication required."),

            InvalidRequestException invalidRequestException =>
                ((int)HttpStatusCode.BadRequest, invalidRequestException.Code, invalidRequestException.Message),

            ApplicationException applicationException =>
                ((int)HttpStatusCode.BadRequest, applicationException.Code, applicationException.Message),

            NotFoundException notFoundException =>
                ((int)HttpStatusCode.NotFound, notFoundException.Code, notFoundException.Message),

            DomainException domainException =>
                ((int)HttpStatusCode.BadRequest, domainException.Code, domainException.Message),

            _ => (
                (int)HttpStatusCode.InternalServerError,
                ErrorCodes.ServerError,
                "An unexpected error occurred.")
        };
    }
}
