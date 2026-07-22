using CapTap.Domain.Entities;
using System.Security.Claims;

namespace CapTap.Application.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user, Guid tokenId);

    string GenerateRefreshToken();

    string HashToken(string token);

    ClaimsPrincipal? ValidateToken(string token);
}
