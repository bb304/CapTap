namespace CapTap.Application.Interfaces;

public interface IAuditService
{
    Task LogAsync(
        string action,
        string entityType,
        Guid? userId = null,
        Guid? entityId = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);
}
