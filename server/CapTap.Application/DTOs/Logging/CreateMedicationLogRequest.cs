using CapTap.Domain.Enums;

namespace CapTap.Application.DTOs.Logging;

public sealed class CreateMedicationLogRequest
{
    public Guid MedicationId { get; set; }

    /// <summary>UTC scheduled occurrence (date + schedule clock time).</summary>
    public DateTime ScheduledDoseTime { get; set; }

    public LoggingMethod LoggingMethod { get; set; } = LoggingMethod.Manual;

    /// <summary>Optional; when omitted the server resolves from medication + scheduled time.</summary>
    public Guid? ScheduleId { get; set; }

    public string? Notes { get; set; }
}
