using CapTap.Domain.Entities;

namespace CapTap.Application.Interfaces;

public interface IScheduleRepository
{
    Task<List<MedicationSchedule>> GetByMedicationForUserAsync(
        Guid userId,
        Guid medicationId,
        CancellationToken cancellationToken = default);

    Task<MedicationSchedule?> GetByIdForUserAsync(
        Guid userId,
        Guid scheduleId,
        CancellationToken cancellationToken = default);

    Task<List<MedicationSchedule>> GetActiveForUserOnDateAsync(
        Guid userId,
        DateOnly date,
        CancellationToken cancellationToken = default);

    Task<bool> HasActiveDuplicateTimeAsync(
        Guid medicationId,
        TimeOnly scheduledTime,
        Guid? excludeScheduleId = null,
        CancellationToken cancellationToken = default);

    Task AddAsync(MedicationSchedule schedule, CancellationToken cancellationToken = default);

    void Update(MedicationSchedule schedule);

    void Remove(MedicationSchedule schedule);
}
