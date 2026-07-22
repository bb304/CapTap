using CapTap.Domain.Entities;
using CapTap.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CapTap.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : BaseEntityConfiguration<User>
{
    public override void Configure(EntityTypeBuilder<User> builder)
    {
        base.Configure(builder);

        builder.ToTable("Users");

        builder.Property(user => user.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.HasIndex(user => user.Email)
            .IsUnique()
            .HasDatabaseName("IX_Users_Email");

        builder.Property(user => user.PasswordHash)
            .IsRequired();

        builder.Property(user => user.EmailVerified)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(user => user.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(user => user.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired()
            .HasDefaultValue(UserStatus.Active);

        builder.Property(user => user.FailedLoginAttempts)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(user => user.LockedUntil);

        builder.Property(user => user.EmailVerificationSentAt);

        builder.HasMany(user => user.Medications)
            .WithOne(medication => medication.User)
            .HasForeignKey(medication => medication.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(user => user.RefreshTokens)
            .WithOne(token => token.User)
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(user => user.MedicationLogs)
            .WithOne(log => log.User)
            .HasForeignKey(log => log.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
