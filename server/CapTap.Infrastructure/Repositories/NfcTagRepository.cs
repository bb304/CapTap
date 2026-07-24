using CapTap.Application.Interfaces;
using CapTap.Domain.Entities;
using CapTap.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CapTap.Infrastructure.Repositories;

public sealed class NfcTagRepository : INfcTagRepository
{
    private readonly ApplicationDbContext _dbContext;

    public NfcTagRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<NfcTag?> GetByTagIdentifierAsync(
        string tagIdentifier,
        CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(tagIdentifier);
        return _dbContext.NfcTags
            .Include(tag => tag.Medication)
            .FirstOrDefaultAsync(
                tag => tag.TagIdentifier == normalized,
                cancellationToken);
    }

    public Task<NfcTag?> GetAssignedByMedicationForUserAsync(
        Guid userId,
        Guid medicationId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.NfcTags
            .FirstOrDefaultAsync(
                tag =>
                    tag.UserId == userId &&
                    tag.MedicationId == medicationId &&
                    tag.IsAssigned,
                cancellationToken);
    }

    public Task<List<NfcTag>> GetAssignedForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.NfcTags
            .Include(tag => tag.Medication)
            .Where(tag => tag.UserId == userId && tag.IsAssigned)
            .OrderByDescending(tag => tag.AssignedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(NfcTag tag, CancellationToken cancellationToken = default)
    {
        tag.TagIdentifier = Normalize(tag.TagIdentifier);
        await _dbContext.NfcTags.AddAsync(tag, cancellationToken);
    }

    public void Update(NfcTag tag)
    {
        tag.TagIdentifier = Normalize(tag.TagIdentifier);
        _dbContext.NfcTags.Update(tag);
    }

    private static string Normalize(string tagIdentifier) =>
        tagIdentifier.Trim().ToUpperInvariant();
}
