using CapTap.Domain.Common;
using CapTap.Domain.Enums;

namespace CapTap.Domain.Entities;

/// <summary>
/// A recorded dose intake. <see cref="ScheduledDoseTime"/> is the expected occurrence;
/// <see cref="LoggedAt"/> is when the user actually confirmed taking it.
/// </summary>
public class MedicationLog : BaseEntity
{
    public Guid MedicationId { get; set; }

    public Guid UserId { get; set; }

    public Guid? ScheduleId { get; set; }

    /// <summary>UTC timestamp of the scheduled dose occurrence (calendar day + schedule clock time).</summary>
    public DateTime ScheduledDoseTime { get; set; }

    /// <summary>UTC timestamp when the user logged the dose (actual confirmation time).</summary>
    public DateTime LoggedAt { get; set; }

    public LoggingMethod LoggingMethod { get; set; }

    public string? Notes { get; set; }

    public Medication Medication { get; set; } = null!;

    public User User { get; set; } = null!;

    public MedicationSchedule? Schedule { get; set; }
}
