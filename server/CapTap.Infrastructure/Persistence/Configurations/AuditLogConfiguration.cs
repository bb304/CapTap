using CapTap.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CapTap.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : BaseEntityConfiguration<AuditLog>
{
    public override void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        base.Configure(builder);

        builder.ToTable("AuditLogs");

        builder.Property(log => log.Action)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(log => log.EntityType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(log => log.IpAddress)
            .HasMaxLength(45);

        builder.Property(log => log.Metadata)
            .HasMaxLength(2000);

        builder.HasIndex(log => log.UserId)
            .HasDatabaseName("IX_AuditLogs_UserId");

        builder.HasIndex(log => log.CreatedAt)
            .HasDatabaseName("IX_AuditLogs_CreatedAt");
    }
}
