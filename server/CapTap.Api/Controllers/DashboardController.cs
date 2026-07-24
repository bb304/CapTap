using CapTap.Application.DTOs.Adherence;
using CapTap.Application.Interfaces;
using CapTap.Domain.Enums;
using CapTap.Shared.Responses;
using Microsoft.AspNetCore.Mvc;

namespace CapTap.Api.Controllers;

[Route("api/v1/dashboard")]
public sealed class DashboardController : AuthorizedApiControllerBase
{
    private readonly IAdherenceService _adherenceService;
    private readonly IAdherenceStreakService _streakService;

    public DashboardController(
        ICurrentUserService currentUser,
        IAdherenceService adherenceService,
        IAdherenceStreakService streakService)
        : base(currentUser)
    {
        _adherenceService = adherenceService;
        _streakService = streakService;
    }

    /// <summary>
    /// Today's expected doses plus server-computed completion and streak statistics.
    /// </summary>
    [HttpGet("today")]
    [ProducesResponseType(typeof(ApiResponse<DashboardTodayResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<DashboardTodayResponse>>> GetToday(
        CancellationToken cancellationToken)
    {
        var doses = await _adherenceService.GetTodayDosesAsync(CurrentUserId, cancellationToken);
        var streak = await _streakService.GetStreakAsync(CurrentUserId, cancellationToken);

        var takenCount = doses.Count(d => d.Status == AdherenceStatus.Taken);
        var missedCount = doses.Count(d => d.Status == AdherenceStatus.Missed);
        var completionPercent = doses.Count == 0
            ? 0
            : (int)Math.Round(100.0 * takenCount / doses.Count);

        return Success(new DashboardTodayResponse
        {
            Doses = doses,
            CompletionPercent = completionPercent,
            CurrentStreakDays = streak.CurrentStreakDays,
            LongestStreakDays = streak.LongestStreakDays,
            TotalMedicationsToday = doses.Count,
            TakenCount = takenCount,
            MissedCount = missedCount
        });
    }

    /// <summary>Current and longest adherence streaks.</summary>
    [HttpGet("streak")]
    [ProducesResponseType(typeof(ApiResponse<AdherenceStreakDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<AdherenceStreakDto>>> GetStreak(
        CancellationToken cancellationToken)
    {
        var streak = await _streakService.GetStreakAsync(CurrentUserId, cancellationToken);
        return Success(streak);
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
