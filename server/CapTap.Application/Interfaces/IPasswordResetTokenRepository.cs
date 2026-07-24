using CapTap.Domain.Entities;

namespace CapTap.Application.Interfaces;

public interface IPasswordResetTokenRepository
{
    Task<PasswordResetToken?> GetActiveByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task InvalidateUnusedForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task AddAsync(PasswordResetToken token, CancellationToken cancellationToken = default);

    void Update(PasswordResetToken token);
}
