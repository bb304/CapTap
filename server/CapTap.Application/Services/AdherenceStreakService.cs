using CapTap.Application.Common;
using CapTap.Application.DTOs.Adherence;
using CapTap.Application.Interfaces;
using CapTap.Domain.Enums;

namespace CapTap.Application.Services;

/// <summary>
/// Computes current and longest adherence streaks from schedule projections + logs.
/// A day is complete when every expected dose that day has a log.
/// Days are evaluated in the user's local calendar (via AdherenceService).
/// Future (Upcoming) doses on today are ignored so an incomplete morning does not break the streak.
///
/// Performance note: day-by-day evaluation is O(lookback). If profiling shows bottlenecks
/// (e.g. 365-day streaks under load), prefer bulk-loading schedules + logs for the window
/// and computing day completions in memory. See docs/nfc-integration.md.
/// </summary>
public sealed class AdherenceStreakService : IAdherenceStreakService
{
    private const int LookbackDays = 365;

    private readonly IAdherenceService _adherence;
    private readonly IUserRepository _users;
    private readonly ITimeProvider _timeProvider;

    public AdherenceStreakService(
        IAdherenceService adherence,
        IUserRepository users,
        ITimeProvider timeProvider)
    {
        _adherence = adherence;
        _users = users;
        _timeProvider = timeProvider;
    }

    public async Task<AdherenceStreakDto> GetStreakAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var utcNow = _timeProvider.UtcNow;
        var user = await _users.GetByIdAsync(userId, cancellationToken);
        var zone = TimeZoneHelper.Resolve(user?.TimeZoneId);
        var today = TimeZoneHelper.LocalDate(utcNow, zone);
        var from = today.AddDays(-LookbackDays);

        var dayCompletions = new Dictionary<DateOnly, bool?>();
        for (var date = from; date <= today; date = date.AddDays(1))
        {
            dayCompletions[date] = await EvaluateDayAsync(userId, date, utcNow, zone, cancellationToken);
        }

        var current = CalculateCurrentStreak(dayCompletions, today);
        var longest = CalculateLongestStreak(dayCompletions);

        return new AdherenceStreakDto
        {
            CurrentStreakDays = current,
            LongestStreakDays = Math.Max(longest, current)
        };
    }

    private async Task<bool?> EvaluateDayAsync(
        Guid userId,
        DateOnly date,
        DateTime utcNow,
        TimeZoneInfo zone,
        CancellationToken cancellationToken)
    {
        var doses = await _adherence.GetDosesForDateAsync(userId, date, cancellationToken);
        if (doses.Count == 0)
        {
            return null; // No schedules — skip day (neither completes nor breaks a streak).
        }

        var today = TimeZoneHelper.LocalDate(utcNow, zone);
        IEnumerable<TodayDoseDto> relevant = doses;
        if (date == today)
        {
            // Ignore future scheduled doses for today's completion.
            relevant = doses.Where(d => d.Status != AdherenceStatus.Upcoming);
            if (!relevant.Any())
            {
                // Entire day is still in the future — treat as in-progress (null), not broken.
                return null;
            }
        }

        return relevant.All(d => d.Status == AdherenceStatus.Taken);
    }

    private static int CalculateCurrentStreak(IReadOnlyDictionary<DateOnly, bool?> days, DateOnly today)
    {
        var cursor = today;
        days.TryGetValue(today, out var todayComplete);

        // If today is incomplete the streak is broken.
        if (todayComplete == false)
        {
            return 0;
        }

        // If today is still in progress (null), start counting from yesterday.
        if (todayComplete is null)
        {
            cursor = today.AddDays(-1);
        }

        var streak = 0;
        while (days.TryGetValue(cursor, out var complete))
        {
            if (complete is null)
            {
                cursor = cursor.AddDays(-1);
                continue;
            }

            if (complete == false)
            {
                break;
            }

            streak++;
            cursor = cursor.AddDays(-1);
        }

        return streak;
    }

    private static int CalculateLongestStreak(IReadOnlyDictionary<DateOnly, bool?> days)
    {
        var longest = 0;
        var current = 0;
        foreach (var date in days.Keys.OrderBy(d => d))
        {
            var complete = days[date];
            if (complete is null)
            {
                continue;
            }

            if (complete == true)
            {
                current++;
                longest = Math.Max(longest, current);
            }
            else
            {
                current = 0;
            }
        }

        return longest;
    }
}
