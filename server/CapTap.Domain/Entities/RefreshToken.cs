using CapTap.Domain.Common;

namespace CapTap.Domain.Entities;

public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }

    /// <summary>
    /// Groups rotated refresh tokens for a login session. Reuse of a revoked
    /// token in the family revokes the entire family.
    /// </summary>
    public Guid FamilyId { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public string? DeviceName { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public string? RevokedReason { get; set; }

    public User User { get; set; } = null!;
}
