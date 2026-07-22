using CapTap.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CapTap.Infrastructure.Persistence.Configurations;

public sealed class MedicationLogConfiguration : BaseEntityConfiguration<MedicationLog>
{
    public override void Configure(EntityTypeBuilder<MedicationLog> builder)
    {
        base.Configure(builder);

        builder.ToTable("MedicationLogs");

        builder.Property(log => log.TakenAt)
            .IsRequired();

        builder.Property(log => log.LoggingMethod)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(log => log.UserId)
            .HasDatabaseName("IX_MedicationLogs_UserId");

        builder.HasIndex(log => log.TakenAt)
            .HasDatabaseName("IX_MedicationLogs_TakenAt");

        builder.HasIndex(log => log.MedicationId)
            .HasDatabaseName("IX_MedicationLogs_MedicationId");

        builder.HasOne(log => log.Schedule)
            .WithMany()
            .HasForeignKey(log => log.ScheduleId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
