using CapTap.Application.Common;
using CapTap.Application.Interfaces;
using CapTap.Domain.Entities;
using CapTap.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CapTap.Infrastructure.Repositories;

public sealed class MedicationLogRepository : IMedicationLogRepository
{
    private readonly ApplicationDbContext _dbContext;

    public MedicationLogRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<MedicationLog>> GetForUserOnDateAsync(
        Guid userId,
        DateOnly localDate,
        TimeZoneInfo timeZone,
        CancellationToken cancellationToken = default)
    {
        var (start, end) = TimeZoneHelper.LocalDayUtcRange(localDate, timeZone);

        // Match by scheduled occurrence day in the user's zone (not LoggedAt).
        return _dbContext.MedicationLogs
            .AsNoTracking()
            .Where(log =>
                log.UserId == userId &&
                log.ScheduledDoseTime >= start &&
                log.ScheduledDoseTime < end)
            .ToListAsync(cancellationToken);
    }

    public Task<List<MedicationLog>> GetForUserBetweenDatesAsync(
        Guid userId,
        DateOnly fromLocalDate,
        DateOnly toLocalDateInclusive,
        TimeZoneInfo timeZone,
        CancellationToken cancellationToken = default)
    {
        var (start, _) = TimeZoneHelper.LocalDayUtcRange(fromLocalDate, timeZone);
        var (_, end) = TimeZoneHelper.LocalDayUtcRange(toLocalDateInclusive, timeZone);

        return _dbContext.MedicationLogs
            .AsNoTracking()
            .Where(log =>
                log.UserId == userId &&
                log.ScheduledDoseTime >= start &&
                log.ScheduledDoseTime < end)
            .ToListAsync(cancellationToken);
    }

    public Task<MedicationLog?> FindDuplicateAsync(
        Guid userId,
        Guid scheduleId,
        DateTime scheduledDoseTime,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.MedicationLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(
                log =>
                    log.UserId == userId &&
                    log.ScheduleId == scheduleId &&
                    log.ScheduledDoseTime == scheduledDoseTime,
                cancellationToken);
    }

    public async Task<(List<MedicationLog> Items, int TotalCount)> GetHistoryAsync(
        Guid userId,
        int page,
        int pageSize,
        Guid? medicationId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.MedicationLogs
            .AsNoTracking()
            .Include(log => log.Medication)
            .Where(log => log.UserId == userId);

        if (medicationId.HasValue)
        {
            query = query.Where(log => log.MedicationId == medicationId.Value);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(log => log.LoggedAt)
            .ThenByDescending(log => log.ScheduledDoseTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task AddAsync(MedicationLog log, CancellationToken cancellationToken = default)
    {
        await _dbContext.MedicationLogs.AddAsync(log, cancellationToken);
    }
}
