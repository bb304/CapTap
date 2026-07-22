using CapTap.Domain.Common;
using CapTap.Domain.Enums;

namespace CapTap.Domain.Entities;

public class MedicationLog : BaseEntity
{
    public Guid MedicationId { get; set; }

    public Guid UserId { get; set; }

    public Guid? ScheduleId { get; set; }

    public DateTime TakenAt { get; set; }

    public LoggingMethod LoggingMethod { get; set; }

    public Medication Medication { get; set; } = null!;

    public User User { get; set; } = null!;

    public MedicationSchedule? Schedule { get; set; }
}
