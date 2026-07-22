using CapTap.Domain.Common;
using CapTap.Domain.Enums;

namespace CapTap.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public bool EmailVerified { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public bool IsActive { get; set; } = true;

    public UserStatus Status { get; set; } = UserStatus.Active;

    public int FailedLoginAttempts { get; set; }

    public DateTime? LockedUntil { get; set; }

    public DateTime? EmailVerificationSentAt { get; set; }

    public ICollection<Medication> Medications { get; set; } = new List<Medication>();

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public ICollection<MedicationLog> MedicationLogs { get; set; } = new List<MedicationLog>();

    public ICollection<EmailVerificationToken> EmailVerificationTokens { get; set; } = new List<EmailVerificationToken>();

    public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();
}
