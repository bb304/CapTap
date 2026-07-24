using CapTap.Application.Interfaces;
using CapTap.Domain.Entities;
using CapTap.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CapTap.Infrastructure.Repositories;

public sealed class MedicationRepository : IMedicationRepository
{
    private readonly ApplicationDbContext _dbContext;

    public MedicationRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<Medication>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Medications
            .AsNoTracking()
            .Include(medication => medication.Schedules.Where(s => s.IsActive))
            .Where(medication => medication.UserId == userId && !medication.IsArchived)
            .OrderBy(medication => medication.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<Medication?> GetByIdForUserAsync(
        Guid userId,
        Guid medicationId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Medications
            .Include(medication => medication.Schedules.Where(s => s.IsActive))
            .FirstOrDefaultAsync(
                medication => medication.Id == medicationId && medication.UserId == userId,
                cancellationToken);
    }

    public async Task AddAsync(Medication medication, CancellationToken cancellationToken = default)
    {
        await _dbContext.Medications.AddAsync(medication, cancellationToken);
    }

    public void Update(Medication medication)
    {
        _dbContext.Medications.Update(medication);
    }

    public async Task ArchiveAsync(Guid userId, Guid medicationId, CancellationToken cancellationToken = default)
    {
        var medication = await _dbContext.Medications
            .FirstOrDefaultAsync(
                item => item.Id == medicationId && item.UserId == userId,
                cancellationToken);

        if (medication is null)
        {
            return;
        }

        medication.IsArchived = true;
    }
}
