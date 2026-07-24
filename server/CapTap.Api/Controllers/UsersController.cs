using CapTap.Application.DTOs.Users;
using CapTap.Application.Interfaces;
using CapTap.Shared.Responses;
using Microsoft.AspNetCore.Mvc;

namespace CapTap.Api.Controllers;

[Route("api/v1/users")]
public sealed class UsersController : AuthorizedApiControllerBase
{
    private readonly IUserProfileService _profiles;

    public UsersController(ICurrentUserService currentUser, IUserProfileService profiles)
        : base(currentUser)
    {
        _profiles = profiles;
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> GetMe(CancellationToken cancellationToken)
    {
        var profile = await _profiles.GetProfileAsync(CurrentUserId, cancellationToken);
        return Success(profile);
    }

    [HttpPut("me/timezone")]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> UpdateTimeZone(
        [FromBody] UpdateTimeZoneRequest request,
        CancellationToken cancellationToken)
    {
        var profile = await _profiles.UpdateTimeZoneAsync(CurrentUserId, request, cancellationToken);
        return Success(profile);
    }
}
