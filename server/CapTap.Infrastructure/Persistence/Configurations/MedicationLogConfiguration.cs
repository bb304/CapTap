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

        builder.Property(log => log.ScheduledDoseTime)
            .IsRequired();

        builder.Property(log => log.LoggedAt)
            .IsRequired();

        builder.Property(log => log.LoggingMethod)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(log => log.Notes)
            .HasMaxLength(500);

        builder.HasIndex(log => log.UserId)
            .HasDatabaseName("IX_MedicationLogs_UserId");

        builder.HasIndex(log => log.LoggedAt)
            .HasDatabaseName("IX_MedicationLogs_LoggedAt");

        builder.HasIndex(log => log.MedicationId)
            .HasDatabaseName("IX_MedicationLogs_MedicationId");

        builder.HasIndex(log => log.ScheduledDoseTime)
            .HasDatabaseName("IX_MedicationLogs_ScheduledDoseTime");

        // One log per user/schedule/scheduled occurrence — prevents duplicate dose confirmations.
        builder.HasIndex(log => new { log.UserId, log.ScheduleId, log.ScheduledDoseTime })
            .IsUnique()
            .HasDatabaseName("IX_MedicationLogs_User_Schedule_ScheduledDoseTime");

        builder.HasOne(log => log.Schedule)
            .WithMany()
            .HasForeignKey(log => log.ScheduleId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
