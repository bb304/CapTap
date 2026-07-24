using CapTap.Domain.Common;

namespace CapTap.Domain.Entities;

public class AuditLog : BaseEntity
{
    public Guid? UserId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty;

    public Guid? EntityId { get; set; }

    public string? IpAddress { get; set; }

    /// <summary>Optional JSON payload (e.g. NFC tag id, device id, medication id).</summary>
    public string? Metadata { get; set; }
}
