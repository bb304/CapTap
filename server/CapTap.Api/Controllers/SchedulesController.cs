using CapTap.Application.DTOs.Schedule;
using CapTap.Application.Interfaces;
using CapTap.Shared.Responses;
using Microsoft.AspNetCore.Mvc;

namespace CapTap.Api.Controllers;

[Route("api/v1")]
public sealed class SchedulesController : AuthorizedApiControllerBase
{
    private readonly IScheduleService _scheduleService;

    public SchedulesController(ICurrentUserService currentUser, IScheduleService scheduleService)
        : base(currentUser)
    {
        _scheduleService = scheduleService;
    }

    /// <summary>List schedules for a medication owned by the current user.</summary>
    [HttpGet("medications/{medicationId:guid}/schedules")]
    [ProducesResponseType(typeof(ApiResponse<List<ScheduleResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<List<ScheduleResponseDto>>>> GetSchedules(
        Guid medicationId,
        CancellationToken cancellationToken)
    {
        var schedules = await _scheduleService.GetSchedulesAsync(CurrentUserId, medicationId, cancellationToken);
        return Success(schedules);
    }

    /// <summary>Add a scheduled dose time. Multiple times = multiple schedule rows.</summary>
    [HttpPost("medications/{medicationId:guid}/schedules")]
    [ProducesResponseType(typeof(ApiResponse<ScheduleResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ScheduleResponseDto>>> CreateSchedule(
        Guid medicationId,
        [FromBody] CreateScheduleRequest request,
        CancellationToken cancellationToken)
    {
        var schedule = await _scheduleService.CreateScheduleAsync(
            CurrentUserId,
            medicationId,
            request,
            GetClientIp(),
            cancellationToken);

        return Success(schedule);
    }

    /// <summary>Update a schedule owned by the current user.</summary>
    [HttpPatch("schedules/{scheduleId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ScheduleResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ScheduleResponseDto>>> UpdateSchedule(
        Guid scheduleId,
        [FromBody] UpdateScheduleRequest request,
        CancellationToken cancellationToken)
    {
        var schedule = await _scheduleService.UpdateScheduleAsync(
            CurrentUserId,
            scheduleId,
            request,
            GetClientIp(),
            cancellationToken);

        return Success(schedule);
    }

    /// <summary>Deactivate a schedule (soft delete).</summary>
    [HttpDelete("schedules/{scheduleId:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> DeleteSchedule(
        Guid scheduleId,
        CancellationToken cancellationToken)
    {
        await _scheduleService.DeleteScheduleAsync(CurrentUserId, scheduleId, GetClientIp(), cancellationToken);
        return Ok(ApiResponse.Ok(message: "Schedule deleted."));
    }

    private string? GetClientIp() =>
        HttpContext.Connection.RemoteIpAddress?.ToString();
}
