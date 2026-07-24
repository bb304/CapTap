using CapTap.Application.Common;
using CapTap.Application.DTOs.Users;
using CapTap.Application.Exceptions;
using CapTap.Application.Interfaces;
using CapTap.Domain.Exceptions;

namespace CapTap.Application.Services;

public sealed class UserProfileService : IUserProfileService
{
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;

    public UserProfileService(IUserRepository users, IUnitOfWork unitOfWork)
    {
        _users = users;
        _unitOfWork = unitOfWork;
    }

    public async Task<UserProfileDto> GetProfileAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User was not found.");

        return Map(user);
    }

    public async Task<UserProfileDto> UpdateTimeZoneAsync(
        Guid userId,
        UpdateTimeZoneRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TimeZoneHelper.IsValidIana(request.TimeZoneId))
        {
            throw new InvalidRequestException("Time zone must be a valid IANA identifier (e.g. America/New_York).");
        }

        var user = await _users.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User was not found.");

        user.TimeZoneId = request.TimeZoneId.Trim();
        _users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(user);
    }

    private static UserProfileDto Map(Domain.Entities.User user) =>
        new()
        {
            Id = user.Id,
            Email = user.Email,
            TimeZoneId = user.TimeZoneId
        };
}
