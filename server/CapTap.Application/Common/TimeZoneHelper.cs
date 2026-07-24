namespace CapTap.Application.Common;

/// <summary>
/// Helpers for converting UTC timestamps to/from a user's IANA time zone.
/// Assumptions: schedules' TimeOnly values are wall-clock times in the user's zone;
/// all persisted timestamps remain UTC.
/// </summary>
public static class TimeZoneHelper
{
    public static TimeZoneInfo Resolve(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return TimeZoneInfo.Utc;
        }

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId.Trim());
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.Utc;
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.Utc;
        }
    }

    public static DateOnly LocalDate(DateTime utcNow, TimeZoneInfo zone)
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(EnsureUtc(utcNow), zone);
        return DateOnly.FromDateTime(local);
    }

    public static TimeOnly LocalTime(DateTime utcNow, TimeZoneInfo zone)
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(EnsureUtc(utcNow), zone);
        return TimeOnly.FromDateTime(local);
    }

    /// <summary>UTC instant for a local calendar date + wall-clock schedule time.</summary>
    public static DateTime ToUtc(DateOnly localDate, TimeOnly localTime, TimeZoneInfo zone)
    {
        var localDateTime = localDate.ToDateTime(localTime);
        var unspecified = DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(unspecified, zone);
    }

    /// <summary>Inclusive UTC range covering a local calendar day.</summary>
    public static (DateTime StartUtc, DateTime EndUtcExclusive) LocalDayUtcRange(
        DateOnly localDate,
        TimeZoneInfo zone)
    {
        var start = ToUtc(localDate, TimeOnly.MinValue, zone);
        var end = ToUtc(localDate.AddDays(1), TimeOnly.MinValue, zone);
        return (start, end);
    }

    public static DateOnly LocalDateFromUtc(DateTime utc, TimeZoneInfo zone) =>
        LocalDate(utc, zone);

    public static bool IsValidIana(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return false;
        }

        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId.Trim());
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }
    }

    private static DateTime EnsureUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
}
