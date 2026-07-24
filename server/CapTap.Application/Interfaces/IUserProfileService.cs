using CapTap.Application.DTOs.Users;

namespace CapTap.Application.Interfaces;

public interface IUserProfileService
{
    Task<UserProfileDto> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<UserProfileDto> UpdateTimeZoneAsync(
        Guid userId,
        UpdateTimeZoneRequest request,
        CancellationToken cancellationToken = default);
}
