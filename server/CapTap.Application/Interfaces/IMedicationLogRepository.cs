using CapTap.Domain.Entities;

namespace CapTap.Application.Interfaces;

public interface IMedicationLogRepository
{
    Task<List<MedicationLog>> GetForUserOnDateAsync(
        Guid userId,
        DateOnly localDate,
        TimeZoneInfo timeZone,
        CancellationToken cancellationToken = default);

    Task<List<MedicationLog>> GetForUserBetweenDatesAsync(
        Guid userId,
        DateOnly fromLocalDate,
        DateOnly toLocalDateInclusive,
        TimeZoneInfo timeZone,
        CancellationToken cancellationToken = default);

    Task<MedicationLog?> FindDuplicateAsync(
        Guid userId,
        Guid scheduleId,
        DateTime scheduledDoseTime,
        CancellationToken cancellationToken = default);

    Task<(List<MedicationLog> Items, int TotalCount)> GetHistoryAsync(
        Guid userId,
        int page,
        int pageSize,
        Guid? medicationId = null,
        CancellationToken cancellationToken = default);

    Task AddAsync(MedicationLog log, CancellationToken cancellationToken = default);
}
