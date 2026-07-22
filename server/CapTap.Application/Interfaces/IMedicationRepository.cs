using CapTap.Domain.Entities;

namespace CapTap.Application.Interfaces;

public interface IMedicationRepository
{
    Task<List<Medication>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<Medication?> GetByIdForUserAsync(Guid userId, Guid medicationId, CancellationToken cancellationToken = default);

    Task AddAsync(Medication medication, CancellationToken cancellationToken = default);

    void Update(Medication medication);

    Task ArchiveAsync(Guid userId, Guid medicationId, CancellationToken cancellationToken = default);
}
