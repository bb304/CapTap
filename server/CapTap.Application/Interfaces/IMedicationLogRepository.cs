using CapTap.Domain.Entities;

namespace CapTap.Application.Interfaces;

public interface IMedicationLogRepository
{
    Task<List<MedicationLog>> GetForUserOnDateAsync(
        Guid userId,
        DateOnly date,
        CancellationToken cancellationToken = default);

    Task<List<MedicationLog>> GetForUserBetweenDatesAsync(
        Guid userId,
        DateOnly fromDate,
        DateOnly toDateInclusive,
        CancellationToken cancellationToken = default);
}
