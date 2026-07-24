using CapTap.Domain.Enums;

namespace CapTap.Application.DTOs.Medication;

public sealed class MedicationScheduleSummaryDto
{
    public Guid Id { get; set; }

    public FrequencyType Frequency { get; set; }

    public TimeOnly ScheduledTime { get; set; }

    public int DoseQuantity { get; set; }

    public bool IsActive { get; set; }
}
