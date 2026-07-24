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

    /// <summary>
    /// Soft-delete the signed-in account: anonymize PII, revoke sessions, unassign NFC.
    /// Medication logs are retained under the anonymized user id.
    /// Requires current password confirmation.
    /// </summary>
    [HttpDelete("me")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMe(
        [FromBody] DeleteAccountRequest request,
        CancellationToken cancellationToken)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        await _profiles.DeleteAccountAsync(CurrentUserId, request, ip, cancellationToken);
        return NoContent();
    }
}
