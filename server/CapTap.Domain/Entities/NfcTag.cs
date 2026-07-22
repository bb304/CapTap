using CapTap.Domain.Common;

namespace CapTap.Domain.Entities;

public class NfcTag : BaseEntity
{
    public Guid MedicationId { get; set; }

    public string TagIdentifier { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime AssignedAt { get; set; }

    public DateTime? LastUsedAt { get; set; }

    public Medication Medication { get; set; } = null!;
}
