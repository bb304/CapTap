using CapTap.Application.Interfaces;
using CapTap.Shared.Responses;
using Microsoft.AspNetCore.Mvc;

namespace CapTap.Api.Controllers;

/// <summary>
/// Scaffold for NFC APIs. All actions require auth and must scope data to <see cref="CurrentUserId"/>.
/// Full NFC features land in a later phase.
/// </summary>
[Route("api/v1/nfc")]
public sealed class NfcController : AuthorizedApiControllerBase
{
    public NfcController(ICurrentUserService currentUser)
        : base(currentUser)
    {
    }

    [HttpGet("tags")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<ApiResponse<object>> ListTags()
    {
        // Later phase: query NFC tags for medications owned by CurrentUserId only.
        return Success<object>(new { userId = CurrentUserId, items = Array.Empty<object>() });
    }
}
