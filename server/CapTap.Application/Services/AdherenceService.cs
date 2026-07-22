using CapTap.Application.DTOs.Adherence;
using CapTap.Application.Interfaces;
using CapTap.Domain.Entities;
using CapTap.Domain.Enums;

namespace CapTap.Application.Services;

/// <summary>
/// Pure adherence engine: projects expected doses and status without mutating data.
/// </summary>
public sealed class AdherenceService : IAdherenceService
{
    private static readonly AdherenceStatus[] TodaySortOrder =
    [
        AdherenceStatus.Due,
        AdherenceStatus.Upcoming,
        AdherenceStatus.Taken,
        AdherenceStatus.Missed
    ];

    private readonly IScheduleRepository _schedules;
    private readonly IMedicationLogRepository _logs;
    private readonly ITimeProvider _timeProvider;

    public AdherenceService(
        IScheduleRepository schedules,
        IMedicationLogRepository logs,
        ITimeProvider timeProvider)
    {
        _schedules = schedules;
        _logs = logs;
        _timeProvider = timeProvider;
    }

    public async Task<List<TodayDoseDto>> GetTodayDosesAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var utcNow = _timeProvider.UtcNow;
        var today = DateOnly.FromDateTime(utcNow);
        return await BuildDosesForDateAsync(userId, today, utcNow, cancellationToken);
    }

    public async Task<List<TodayDoseDto>> GetMissedDosesAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var utcNow = _timeProvider.UtcNow;
        var today = DateOnly.FromDateTime(utcNow);
        var from = today.AddDays(-7);
        var to = today.AddDays(-1);

        if (to < from)
        {
            return [];
        }

        var missed = new List<TodayDoseDto>();
        for (var date = from; date <= to; date = date.AddDays(1))
        {
            var doses = await BuildDosesForDateAsync(userId, date, utcNow, cancellationToken);
            missed.AddRange(doses.Where(dose => dose.Status == AdherenceStatus.Missed));
        }

        return missed
            .OrderByDescending(dose => dose.ScheduledTime)
            .ThenBy(dose => dose.MedicationName)
            .ToList();
    }

    public AdherenceStatus CalculateStatus(
        TimeOnly scheduledTime,
        DateOnly doseDate,
        DateTime utcNow,
        bool hasLog)
    {
        if (hasLog)
        {
            return AdherenceStatus.Taken;
        }

        var today = DateOnly.FromDateTime(utcNow);
        if (doseDate < today)
        {
            return AdherenceStatus.Missed;
        }

        if (doseDate > today)
        {
            return AdherenceStatus.Upcoming;
        }

        var currentTime = TimeOnly.FromDateTime(utcNow);
        return currentTime < scheduledTime
            ? AdherenceStatus.Upcoming
            : AdherenceStatus.Due;
    }

    private async Task<List<TodayDoseDto>> BuildDosesForDateAsync(
        Guid userId,
        DateOnly date,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var schedules = await _schedules.GetActiveForUserOnDateAsync(userId, date, cancellationToken);
        var logs = await _logs.GetForUserOnDateAsync(userId, date, cancellationToken);

        var doses = new List<TodayDoseDto>();
        foreach (var schedule in schedules)
        {
            var matchingLog = FindMatchingLog(logs, schedule, date);
            var status = CalculateStatus(schedule.ScheduledTime, date, utcNow, matchingLog is not null);

            doses.Add(new TodayDoseDto
            {
                MedicationId = schedule.MedicationId,
                MedicationName = schedule.Medication.Name,
                ScheduledTime = schedule.ScheduledTime,
                DoseQuantity = schedule.DoseQuantity,
                Status = status,
                LogId = matchingLog?.Id,
                ScheduleId = schedule.Id
            });
        }

        return doses
            .OrderBy(dose => Array.IndexOf(TodaySortOrder, dose.Status))
            .ThenBy(dose => dose.ScheduledTime)
            .ThenBy(dose => dose.MedicationName)
            .ToList();
    }

    private static MedicationLog? FindMatchingLog(
        IReadOnlyList<MedicationLog> logs,
        MedicationSchedule schedule,
        DateOnly date)
    {
        return logs.FirstOrDefault(log =>
            log.ScheduleId == schedule.Id &&
            DateOnly.FromDateTime(log.TakenAt) == date);
    }
}
