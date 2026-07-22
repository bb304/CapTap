using CapTap.Application.Interfaces;
using CapTap.Shared.Responses;
using Microsoft.AspNetCore.Mvc;

namespace CapTap.Api.Controllers;

/// <summary>
/// Scaffold for medication APIs. All actions require auth and must scope data to <see cref="CurrentUserId"/>.
/// Full medication features land in a later phase.
/// </summary>
[Route("api/v1/medications")]
public sealed class MedicationsController : AuthorizedApiControllerBase
{
    public MedicationsController(ICurrentUserService currentUser)
        : base(currentUser)
    {
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<ApiResponse<object>> List()
    {
        // Phase 4+: query medications WHERE UserId == CurrentUserId only.
        return Success<object>(new { userId = CurrentUserId, items = Array.Empty<object>() });
    }
}
