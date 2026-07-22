using CapTap.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CapTap.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : BaseEntityConfiguration<RefreshToken>
{
    public override void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        base.Configure(builder);

        builder.ToTable("RefreshTokens");

        builder.Property(token => token.TokenHash)
            .IsRequired();

        builder.Property(token => token.DeviceName)
            .HasMaxLength(100);

        builder.Property(token => token.ExpiresAt)
            .IsRequired();

        builder.HasIndex(token => token.UserId)
            .HasDatabaseName("IX_RefreshTokens_UserId");

        builder.HasIndex(token => token.TokenHash)
            .HasDatabaseName("IX_RefreshTokens_TokenHash");
    }
}
