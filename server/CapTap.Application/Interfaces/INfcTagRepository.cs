using CapTap.Domain.Entities;

namespace CapTap.Application.Interfaces;

public interface INfcTagRepository
{
    Task<NfcTag?> GetByTagIdentifierAsync(
        string tagIdentifier,
        CancellationToken cancellationToken = default);

    Task<NfcTag?> GetAssignedByMedicationForUserAsync(
        Guid userId,
        Guid medicationId,
        CancellationToken cancellationToken = default);

    Task<List<NfcTag>> GetAssignedForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task AddAsync(NfcTag tag, CancellationToken cancellationToken = default);

    void Update(NfcTag tag);
}
