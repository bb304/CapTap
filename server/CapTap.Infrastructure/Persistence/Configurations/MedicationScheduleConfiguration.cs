using CapTap.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CapTap.Infrastructure.Persistence.Configurations;

public sealed class MedicationScheduleConfiguration : BaseEntityConfiguration<MedicationSchedule>
{
    public override void Configure(EntityTypeBuilder<MedicationSchedule> builder)
    {
        base.Configure(builder);

        builder.ToTable("MedicationSchedules");

        builder.Property(schedule => schedule.Frequency)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(schedule => schedule.DoseQuantity)
            .IsRequired();

        builder.Property(schedule => schedule.ScheduledTime)
            .IsRequired()
            .HasColumnType("time");

        builder.Property(schedule => schedule.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(schedule => schedule.EffectiveFrom)
            .IsRequired();

        builder.Property(schedule => schedule.EffectiveTo);

        builder.HasIndex(schedule => schedule.MedicationId)
            .HasDatabaseName("IX_MedicationSchedules_MedicationId");

        builder.HasIndex(schedule => new { schedule.MedicationId, schedule.ScheduledTime })
            .HasDatabaseName("IX_MedicationSchedules_MedicationId_ScheduledTime");
    }
}
