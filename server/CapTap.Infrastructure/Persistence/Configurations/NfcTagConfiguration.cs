using CapTap.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CapTap.Infrastructure.Persistence.Configurations;

public sealed class NfcTagConfiguration : BaseEntityConfiguration<NfcTag>
{
    public override void Configure(EntityTypeBuilder<NfcTag> builder)
    {
        base.Configure(builder);

        builder.ToTable("NfcTags");

        builder.Property(tag => tag.TagIdentifier)
            .IsRequired()
            .HasMaxLength(255);

        // One physical tag identity forever (soft-unassign keeps the row).
        builder.HasIndex(tag => tag.TagIdentifier)
            .IsUnique()
            .HasDatabaseName("IX_NfcTags_TagIdentifier");

        // At most one currently-assigned sticker per medication.
        builder.HasIndex(tag => tag.MedicationId)
            .IsUnique()
            .HasFilter("\"IsAssigned\" = TRUE")
            .HasDatabaseName("IX_NfcTags_MedicationId_Assigned");

        builder.HasIndex(tag => tag.UserId)
            .HasDatabaseName("IX_NfcTags_UserId");

        builder.Property(tag => tag.IsAssigned)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(tag => tag.AssignedAt)
            .IsRequired();

        builder.HasOne(tag => tag.User)
            .WithMany(user => user.NfcTags)
            .HasForeignKey(tag => tag.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(tag => tag.Medication)
            .WithMany(medication => medication.NfcTags)
            .HasForeignKey(tag => tag.MedicationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
