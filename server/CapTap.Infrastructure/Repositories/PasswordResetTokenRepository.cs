using CapTap.Application.Interfaces;
using CapTap.Domain.Entities;
using CapTap.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CapTap.Infrastructure.Repositories;

public sealed class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly ApplicationDbContext _dbContext;

    public PasswordResetTokenRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<PasswordResetToken?> GetActiveByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;
        return _dbContext.PasswordResetTokens.FirstOrDefaultAsync(
            token => token.TokenHash == tokenHash
                     && token.UsedAt == null
                     && token.ExpiresAt > utcNow,
            cancellationToken);
    }

    public async Task InvalidateUnusedForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;
        var active = await _dbContext.PasswordResetTokens
            .Where(token => token.UserId == userId && token.UsedAt == null && token.ExpiresAt > utcNow)
            .ToListAsync(cancellationToken);

        foreach (var token in active)
        {
            token.UsedAt = utcNow;
        }
    }

    public async Task AddAsync(PasswordResetToken token, CancellationToken cancellationToken = default)
    {
        await _dbContext.PasswordResetTokens.AddAsync(token, cancellationToken);
    }

    public void Update(PasswordResetToken token)
    {
        _dbContext.PasswordResetTokens.Update(token);
    }
}
