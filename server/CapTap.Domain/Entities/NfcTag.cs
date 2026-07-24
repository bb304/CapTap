using CapTap.Domain.Common;

namespace CapTap.Domain.Entities;

/// <summary>
/// Physical NFC sticker linked to a medication. Soft-unassign only — never hard-delete.
/// </summary>
public class NfcTag : BaseEntity
{
    public Guid UserId { get; set; }

    public Guid MedicationId { get; set; }

    /// <summary>Opaque hardware identifier (e.g. UID). CapTap never stores medication names on the tag.</summary>
    public string TagIdentifier { get; set; } = string.Empty;

    public bool IsAssigned { get; set; } = true;

    public DateTime AssignedAt { get; set; }

    public DateTime? LastScannedAt { get; set; }

    public User User { get; set; } = null!;

    public Medication Medication { get; set; } = null!;
}
