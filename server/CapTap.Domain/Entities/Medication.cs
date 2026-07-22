using CapTap.Domain.Common;

namespace CapTap.Domain.Entities;

public class Medication : BaseEntity
{
    public Guid UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? GenericName { get; set; }

    public string? BrandName { get; set; }

    public string? FdaIdentifier { get; set; }

    public decimal DosageAmount { get; set; }

    public string DosageUnit { get; set; } = string.Empty;

    public string? Form { get; set; }

    public string? Instructions { get; set; }

    public bool IsArchived { get; set; }

    public User User { get; set; } = null!;

    public ICollection<MedicationSchedule> Schedules { get; set; } = new List<MedicationSchedule>();

    public ICollection<MedicationLog> Logs { get; set; } = new List<MedicationLog>();

    public NfcTag? NfcTag { get; set; }
}
