using CapTap.Application.DTOs.Logging;
using CapTap.Application.Interfaces;
using CapTap.Shared.Responses;
using Microsoft.AspNetCore.Mvc;

namespace CapTap.Api.Controllers;

/// <summary>
/// Single source of truth for recording medication intake (Manual now; Nfc later).
/// </summary>
[Route("api/v1/medication-logs")]
public sealed class MedicationLogsController : AuthorizedApiControllerBase
{
    private readonly IMedicationLogService _logService;

    public MedicationLogsController(ICurrentUserService currentUser, IMedicationLogService logService)
        : base(currentUser)
    {
        _logService = logService;
    }

    /// <summary>Record that a scheduled dose was taken.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<MedicationLogResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<MedicationLogResponseDto>>> CreateLog(
        [FromBody] CreateMedicationLogRequest request,
        CancellationToken cancellationToken)
    {
        var log = await _logService.LogDoseAsync(
            CurrentUserId,
            request,
            GetClientIp(),
            cancellationToken);

        return Success(log);
    }

    /// <summary>Paginated adherence history (newest first).</summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedMedicationLogHistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PaginatedMedicationLogHistoryDto>>> GetHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? medicationId = null,
        CancellationToken cancellationToken = default)
    {
        var history = await _logService.GetHistoryAsync(
            CurrentUserId,
            page,
            pageSize,
            medicationId,
            cancellationToken);

        return Success(history);
    }

    private string? GetClientIp() =>
        HttpContext.Connection.RemoteIpAddress?.ToString();
}
