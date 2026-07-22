using CapTap.Application.DTOs.Adherence;
using CapTap.Application.Interfaces;
using CapTap.Shared.Responses;
using Microsoft.AspNetCore.Mvc;

namespace CapTap.Api.Controllers;

[Route("api/v1/dashboard")]
public sealed class DashboardController : AuthorizedApiControllerBase
{
    private readonly IAdherenceService _adherenceService;

    public DashboardController(ICurrentUserService currentUser, IAdherenceService adherenceService)
        : base(currentUser)
    {
        _adherenceService = adherenceService;
    }

    /// <summary>Today's expected doses with adherence status (Due → Upcoming → Taken → Missed).</summary>
    [HttpGet("today")]
    [ProducesResponseType(typeof(ApiResponse<List<TodayDoseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<List<TodayDoseDto>>>> GetToday(
        CancellationToken cancellationToken)
    {
        var doses = await _adherenceService.GetTodayDosesAsync(CurrentUserId, cancellationToken);
        return Success(doses);
    }

    /// <summary>Missed doses from completed days (last 7 days, excluding today).</summary>
    [HttpGet("missed")]
    [ProducesResponseType(typeof(ApiResponse<List<TodayDoseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<List<TodayDoseDto>>>> GetMissed(
        CancellationToken cancellationToken)
    {
        var doses = await _adherenceService.GetMissedDosesAsync(CurrentUserId, cancellationToken);
        return Success(doses);
    }
}
