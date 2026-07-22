using CapTap.Application.DTOs.Adherence;
using CapTap.Domain.Enums;

namespace CapTap.Application.Interfaces;

public interface IAdherenceService
{
    Task<List<TodayDoseDto>> GetTodayDosesAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<List<TodayDoseDto>> GetMissedDosesAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    AdherenceStatus CalculateStatus(
        TimeOnly scheduledTime,
        DateOnly doseDate,
        DateTime utcNow,
        bool hasLog);
}
