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

    /// <summary>IANA time zone id (e.g. America/New_York). Timestamps stay UTC; local day uses this.</summary>
    public string TimeZoneId { get; set; } = "UTC";

    /// <summary>Set when the account is soft-deleted and PII is anonymized.</summary>
    public DateTime? DeletedAt { get; set; }

    public ICollection<Medication> Medications { get; set; } = new List<Medication>();

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public ICollection<MedicationLog> MedicationLogs { get; set; } = new List<MedicationLog>();

    public ICollection<NfcTag> NfcTags { get; set; } = new List<NfcTag>();

    public ICollection<EmailVerificationToken> EmailVerificationTokens { get; set; } = new List<EmailVerificationToken>();

    public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();
}
