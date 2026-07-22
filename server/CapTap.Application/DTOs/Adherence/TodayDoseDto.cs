using CapTap.Domain.Enums;

namespace CapTap.Application.DTOs.Adherence;

public sealed class TodayDoseDto
{
    public Guid MedicationId { get; set; }

    public string MedicationName { get; set; } = string.Empty;

    public TimeOnly ScheduledTime { get; set; }

    public int DoseQuantity { get; set; }

    public AdherenceStatus Status { get; set; }

    public Guid? LogId { get; set; }

    public Guid ScheduleId { get; set; }
}
