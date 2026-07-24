using CapTap.Domain.Enums;

namespace CapTap.Application.DTOs.Logging;

public sealed class MedicationLogResponseDto
{
    public Guid Id { get; set; }

    public Guid MedicationId { get; set; }

    public string MedicationName { get; set; } = string.Empty;

    public Guid? ScheduleId { get; set; }

    public DateTime ScheduledDoseTime { get; set; }

    public DateTime LoggedAt { get; set; }

    public LoggingMethod LoggingMethod { get; set; }

    public string? Notes { get; set; }
}
