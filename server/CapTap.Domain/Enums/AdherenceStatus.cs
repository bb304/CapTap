namespace CapTap.Domain.Enums;

/// <summary>
/// Computed adherence state for a scheduled dose. Not persisted — derived at query time.
/// </summary>
public enum AdherenceStatus
{
    Upcoming,
    Due,
    Taken,
    Missed
}
