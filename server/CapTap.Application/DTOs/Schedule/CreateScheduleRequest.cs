using CapTap.Domain.Enums;

namespace CapTap.Application.DTOs.Schedule;

public sealed class CreateScheduleRequest
{
    public FrequencyType Frequency { get; set; } = FrequencyType.OnceDaily;

    public TimeOnly ScheduledTime { get; set; }

    public int DoseQuantity { get; set; } = 1;

    public DateTime? EffectiveFrom { get; set; }

    public DateTime? EffectiveTo { get; set; }
}
