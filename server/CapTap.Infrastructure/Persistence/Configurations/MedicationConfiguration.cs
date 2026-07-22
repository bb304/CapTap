using CapTap.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CapTap.Infrastructure.Persistence.Configurations;

public sealed class MedicationConfiguration : BaseEntityConfiguration<Medication>
{
    public override void Configure(EntityTypeBuilder<Medication> builder)
    {
        base.Configure(builder);

        builder.ToTable("Medications");

        builder.Property(medication => medication.Name)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(medication => medication.GenericName)
            .HasMaxLength(255);

        builder.Property(medication => medication.BrandName)
            .HasMaxLength(255);

        builder.Property(medication => medication.FdaIdentifier)
            .HasMaxLength(100);

        builder.Property(medication => medication.DosageAmount)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(medication => medication.DosageUnit)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(medication => medication.Form)
            .HasMaxLength(50);

        builder.Property(medication => medication.IsArchived)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasIndex(medication => medication.UserId)
            .HasDatabaseName("IX_Medications_UserId");

        builder.HasIndex(medication => medication.FdaIdentifier)
            .HasDatabaseName("IX_Medications_FdaIdentifier");

        builder.HasMany(medication => medication.Schedules)
            .WithOne(schedule => schedule.Medication)
            .HasForeignKey(schedule => schedule.MedicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(medication => medication.Logs)
            .WithOne(log => log.Medication)
            .HasForeignKey(log => log.MedicationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(medication => medication.NfcTag)
            .WithOne(tag => tag.Medication)
            .HasForeignKey<NfcTag>(tag => tag.MedicationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
