using CapTap.Application.Interfaces;
using CapTap.Domain.Entities;
using CapTap.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CapTap.Infrastructure.Repositories;

public sealed class EmailVerificationTokenRepository : IEmailVerificationTokenRepository
{
    private readonly ApplicationDbContext _dbContext;

    public EmailVerificationTokenRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<EmailVerificationToken?> GetActiveByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;
        return _dbContext.EmailVerificationTokens.FirstOrDefaultAsync(
            token => token.TokenHash == tokenHash
                     && token.UsedAt == null
                     && token.ExpiresAt > utcNow,
            cancellationToken);
    }

    public async Task AddAsync(EmailVerificationToken token, CancellationToken cancellationToken = default)
    {
        await _dbContext.EmailVerificationTokens.AddAsync(token, cancellationToken);
    }

    public void Update(EmailVerificationToken token)
    {
        _dbContext.EmailVerificationTokens.Update(token);
    }
}
