using CapTap.Application.Interfaces;
using CapTap.Domain.Entities;
using CapTap.Infrastructure.Persistence;

namespace CapTap.Infrastructure.Services;

public sealed class AuditService : IAuditService
{
    private readonly ApplicationDbContext _dbContext;

    public AuditService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task LogAsync(
        string action,
        string entityType,
        Guid? userId = null,
        Guid? entityId = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.AuditLogs.AddAsync(new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            IpAddress = ipAddress
        }, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
