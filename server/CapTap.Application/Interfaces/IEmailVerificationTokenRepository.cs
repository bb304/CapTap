using CapTap.Domain.Entities;

namespace CapTap.Application.Interfaces;

public interface IEmailVerificationTokenRepository
{
    Task<EmailVerificationToken?> GetActiveByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task AddAsync(EmailVerificationToken token, CancellationToken cancellationToken = default);

    void Update(EmailVerificationToken token);
}
