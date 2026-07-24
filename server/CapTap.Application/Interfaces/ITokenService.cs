using System.Security.Claims;
using CapTap.Domain.Entities;

namespace CapTap.Application.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user, Guid tokenId);

    string GenerateRefreshToken();

    string HashToken(string token);

    ClaimsPrincipal? ValidateToken(string token);
}
