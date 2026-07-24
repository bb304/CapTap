namespace CapTap.Application.DTOs.Adherence;

/// <summary>
/// Today's doses plus server-computed adherence statistics.
/// </summary>
public sealed class DashboardTodayResponse
{
    public List<TodayDoseDto> Doses { get; set; } = [];

    public int CompletionPercent { get; set; }

    public int CurrentStreakDays { get; set; }

    public int LongestStreakDays { get; set; }

    public int TotalMedicationsToday { get; set; }

    public int TakenCount { get; set; }

    public int MissedCount { get; set; }
}
