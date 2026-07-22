using CapTap.Domain.Enums;

namespace CapTap.Application.DTOs.Schedule;

public sealed class ScheduleResponseDto
{
    public Guid Id { get; set; }

    public Guid MedicationId { get; set; }

    public FrequencyType Frequency { get; set; }

    public TimeOnly ScheduledTime { get; set; }

    public int DoseQuantity { get; set; }

    public bool IsActive { get; set; }

    public DateTime EffectiveFrom { get; set; }

    public DateTime? EffectiveTo { get; set; }
}
