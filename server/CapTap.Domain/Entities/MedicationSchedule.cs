using CapTap.Domain.Common;
using CapTap.Domain.Enums;

namespace CapTap.Domain.Entities;

public class MedicationSchedule : BaseEntity
{
    public Guid MedicationId { get; set; }

    public FrequencyType Frequency { get; set; }

    public int DoseQuantity { get; set; }

    public TimeOnly ScheduledTime { get; set; }

    public Medication Medication { get; set; } = null!;
}
