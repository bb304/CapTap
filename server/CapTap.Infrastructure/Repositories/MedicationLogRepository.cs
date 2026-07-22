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
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var start = DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var end = DateTime.SpecifyKind(date.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

        return _dbContext.MedicationLogs
            .AsNoTracking()
            .Where(log =>
                log.UserId == userId &&
                log.TakenAt >= start &&
                log.TakenAt < end)
            .ToListAsync(cancellationToken);
    }

    public Task<List<MedicationLog>> GetForUserBetweenDatesAsync(
        Guid userId,
        DateOnly fromDate,
        DateOnly toDateInclusive,
        CancellationToken cancellationToken = default)
    {
        var start = DateTime.SpecifyKind(fromDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var end = DateTime.SpecifyKind(toDateInclusive.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

        return _dbContext.MedicationLogs
            .AsNoTracking()
            .Where(log =>
                log.UserId == userId &&
                log.TakenAt >= start &&
                log.TakenAt < end)
            .ToListAsync(cancellationToken);
    }
}
