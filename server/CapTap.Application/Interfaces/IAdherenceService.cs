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

    /// <summary>Project expected doses and status for an arbitrary local calendar day.</summary>
    Task<List<TodayDoseDto>> GetDosesForDateAsync(
        Guid userId,
        DateOnly date,
        CancellationToken cancellationToken = default);

    AdherenceStatus CalculateStatus(
        TimeOnly scheduledTime,
        DateOnly doseDate,
        DateTime utcNow,
        bool hasLog);
}
