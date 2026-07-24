using CapTap.Application.DTOs.Users;

namespace CapTap.Application.Interfaces;

public interface IUserProfileService
{
    Task<UserProfileDto> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<UserProfileDto> UpdateTimeZoneAsync(
        Guid userId,
        UpdateTimeZoneRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-delete the account: anonymize PII, deactivate, revoke sessions, unassign NFC tags.
    /// Medication logs are retained under the anonymized user id (DDD data-retention).
    /// Requires the current password (step-up authentication).
    /// </summary>
    Task DeleteAccountAsync(
        Guid userId,
        DeleteAccountRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);
}
