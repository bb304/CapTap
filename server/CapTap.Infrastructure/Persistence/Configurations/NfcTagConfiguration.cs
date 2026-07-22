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

        builder.HasIndex(tag => tag.TagIdentifier)
            .IsUnique()
            .HasDatabaseName("IX_NfcTags_TagIdentifier");

        builder.HasIndex(tag => tag.MedicationId)
            .IsUnique()
            .HasDatabaseName("IX_NfcTags_MedicationId");

        builder.Property(tag => tag.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(tag => tag.AssignedAt)
            .IsRequired();
    }
}
