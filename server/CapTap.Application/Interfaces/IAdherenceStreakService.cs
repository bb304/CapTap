using CapTap.Application.DTOs.Adherence;

namespace CapTap.Application.Interfaces;

public interface IAdherenceStreakService
{
    Task<AdherenceStreakDto> GetStreakAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
