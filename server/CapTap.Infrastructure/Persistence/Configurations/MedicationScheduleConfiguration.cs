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

        builder.HasIndex(schedule => schedule.MedicationId)
            .HasDatabaseName("IX_MedicationSchedules_MedicationId");
    }
}
