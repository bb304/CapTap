using CapTap.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CapTap.Infrastructure.Persistence.Configurations;

public sealed class PasswordResetTokenConfiguration : BaseEntityConfiguration<PasswordResetToken>
{
    public override void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        base.Configure(builder);

        builder.ToTable("PasswordResetTokens");

        builder.Property(token => token.TokenHash)
            .IsRequired();

        builder.Property(token => token.ExpiresAt)
            .IsRequired();

        builder.HasIndex(token => token.UserId)
            .HasDatabaseName("IX_PasswordResetTokens_UserId");

        builder.HasIndex(token => token.TokenHash)
            .HasDatabaseName("IX_PasswordResetTokens_TokenHash");

        builder.HasOne(token => token.User)
            .WithMany(user => user.PasswordResetTokens)
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
