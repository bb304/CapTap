using CapTap.Application.Interfaces;
using CapTap.Domain.Entities;
using CapTap.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CapTap.Infrastructure.Repositories;

public sealed class ScheduleRepository : IScheduleRepository
{
    private readonly ApplicationDbContext _dbContext;

    public ScheduleRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<MedicationSchedule>> GetByMedicationForUserAsync(
        Guid userId,
        Guid medicationId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.MedicationSchedules
            .AsNoTracking()
            .Where(schedule =>
                schedule.MedicationId == medicationId &&
                schedule.Medication.UserId == userId)
            .OrderBy(schedule => schedule.ScheduledTime)
            .ToListAsync(cancellationToken);
    }

    public Task<MedicationSchedule?> GetByIdForUserAsync(
        Guid userId,
        Guid scheduleId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.MedicationSchedules
            .Include(schedule => schedule.Medication)
            .FirstOrDefaultAsync(
                schedule => schedule.Id == scheduleId && schedule.Medication.UserId == userId,
                cancellationToken);
    }

    public Task<List<MedicationSchedule>> GetActiveForUserOnDateAsync(
        Guid userId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var dayStart = DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var dayEnd = DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MaxValue), DateTimeKind.Utc);

        return _dbContext.MedicationSchedules
            .AsNoTracking()
            .Include(schedule => schedule.Medication)
            .Where(schedule =>
                schedule.Medication.UserId == userId &&
                schedule.IsActive &&
                !schedule.Medication.IsArchived &&
                schedule.EffectiveFrom <= dayEnd &&
                (schedule.EffectiveTo == null || schedule.EffectiveTo >= dayStart))
            .OrderBy(schedule => schedule.ScheduledTime)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> HasActiveDuplicateTimeAsync(
        Guid medicationId,
        TimeOnly scheduledTime,
        Guid? excludeScheduleId = null,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.MedicationSchedules.AnyAsync(
            schedule =>
                schedule.MedicationId == medicationId &&
                schedule.IsActive &&
                schedule.ScheduledTime == scheduledTime &&
                (excludeScheduleId == null || schedule.Id != excludeScheduleId),
            cancellationToken);
    }

    public async Task AddAsync(MedicationSchedule schedule, CancellationToken cancellationToken = default)
    {
        await _dbContext.MedicationSchedules.AddAsync(schedule, cancellationToken);
    }

    public void Update(MedicationSchedule schedule)
    {
        _dbContext.MedicationSchedules.Update(schedule);
    }

    public void Remove(MedicationSchedule schedule)
    {
        _dbContext.MedicationSchedules.Remove(schedule);
    }
}
