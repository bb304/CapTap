using CapTap.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CapTap.Infrastructure.Persistence.Configurations;

public sealed class EmailVerificationTokenConfiguration : BaseEntityConfiguration<EmailVerificationToken>
{
    public override void Configure(EntityTypeBuilder<EmailVerificationToken> builder)
    {
        base.Configure(builder);

        builder.ToTable("EmailVerificationTokens");

        builder.Property(token => token.TokenHash)
            .IsRequired();

        builder.Property(token => token.ExpiresAt)
            .IsRequired();

        builder.HasIndex(token => token.UserId)
            .HasDatabaseName("IX_EmailVerificationTokens_UserId");

        builder.HasIndex(token => token.TokenHash)
            .HasDatabaseName("IX_EmailVerificationTokens_TokenHash");

        builder.HasOne(token => token.User)
            .WithMany(user => user.EmailVerificationTokens)
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
