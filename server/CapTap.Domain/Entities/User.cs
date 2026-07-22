using CapTap.Domain.Common;

namespace CapTap.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public bool EmailVerified { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Medication> Medications { get; set; } = new List<Medication>();

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public ICollection<MedicationLog> MedicationLogs { get; set; } = new List<MedicationLog>();
}
