using CapTap.Domain.Entities;

namespace CapTap.Application.Interfaces;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);

    void Update(RefreshToken refreshToken);

    Task RevokeFamilyAsync(Guid familyId, string reason, CancellationToken cancellationToken = default);
}
