using CapTap.Domain.Enums;

namespace CapTap.Application.DTOs.Adherence;

public sealed class TodayDoseDto
{
    public Guid MedicationId { get; set; }

    public string MedicationName { get; set; } = string.Empty;

    public TimeOnly ScheduledTime { get; set; }

    /// <summary>UTC instant for this local scheduled occurrence (for POST /medication-logs).</summary>
    public DateTime ScheduledDoseTime { get; set; }

    public int DoseQuantity { get; set; }

    public AdherenceStatus Status { get; set; }

    public Guid? LogId { get; set; }

    public Guid ScheduleId { get; set; }
}
