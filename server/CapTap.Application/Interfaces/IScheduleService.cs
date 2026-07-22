using CapTap.Application.DTOs.Schedule;

namespace CapTap.Application.Interfaces;

public interface IScheduleService
{
    Task<List<ScheduleResponseDto>> GetSchedulesAsync(
        Guid userId,
        Guid medicationId,
        CancellationToken cancellationToken = default);

    Task<ScheduleResponseDto> CreateScheduleAsync(
        Guid userId,
        Guid medicationId,
        CreateScheduleRequest request,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    Task<ScheduleResponseDto> UpdateScheduleAsync(
        Guid userId,
        Guid scheduleId,
        UpdateScheduleRequest request,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    Task DeleteScheduleAsync(
        Guid userId,
        Guid scheduleId,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);
}
