using CapTap.Domain.Enums;

namespace CapTap.Application.DTOs.Nfc;

public sealed class NfcResolveResponseDto
{
    public Guid MedicationId { get; set; }

    public string MedicationName { get; set; } = string.Empty;

    public decimal DosageAmount { get; set; }

    public string DosageUnit { get; set; } = string.Empty;

    public string? Form { get; set; }

    public Guid? ScheduleId { get; set; }

    /// <summary>Local wall-clock scheduled time (HH:mm:ss) for the recommended dose.</summary>
    public string? ScheduledTime { get; set; }

    /// <summary>UTC ISO scheduled occurrence for POST /medication-logs.</summary>
    public DateTime? ScheduledDoseTime { get; set; }

    public AdherenceStatus? Status { get; set; }

    public bool AlreadyLogged { get; set; }

    public string TagIdentifier { get; set; } = string.Empty;
}
