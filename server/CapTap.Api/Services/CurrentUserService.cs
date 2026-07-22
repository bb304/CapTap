using CapTap.Application.Interfaces;
using System.Security.Claims;

namespace CapTap.Api.Services;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated =>
        Principal?.Identity?.IsAuthenticated == true;

    public Guid UserId
    {
        get
        {
            if (!TryGetUserId(out var userId))
            {
                throw new UnauthorizedAccessException("Authenticated user id claim is missing.");
            }

            return userId;
        }
    }

    public string? Email =>
        Principal?.FindFirstValue(ClaimTypes.Email)
        ?? Principal?.FindFirstValue("email");

    public bool TryGetUserId(out Guid userId)
    {
        userId = Guid.Empty;
        var raw =
            Principal?.FindFirstValue("userId")
            ?? Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? Principal?.FindFirstValue("sub");

        return Guid.TryParse(raw, out userId);
    }
}
