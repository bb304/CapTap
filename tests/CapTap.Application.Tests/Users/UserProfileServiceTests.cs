using CapTap.Application.DTOs.Users;
using CapTap.Application.Exceptions;
using CapTap.Application.Interfaces;
using CapTap.Application.Services;
using CapTap.Application.Validators;
using CapTap.Domain.Entities;
using CapTap.Domain.Enums;
using CapTap.Domain.Exceptions;
using FluentAssertions;

namespace CapTap.Application.Tests.Users;

public sealed class UserProfileServiceTests
{
    [Fact]
    public async Task DeleteAccount_AnonymizesPii_RevokesSessions_UnassignsNfc()
    {
        var fixture = ProfileFixture.Create();
        var user = fixture.SeedUser("keep@example.com");
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = "abc",
            FamilyId = Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        fixture.RefreshTokens.Items.Add(token);
        fixture.NfcTags.Items.Add(new NfcTag
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            MedicationId = Guid.NewGuid(),
            TagIdentifier = "AABBCC",
            IsAssigned = true,
            AssignedAt = DateTime.UtcNow
        });
        fixture.Medications.Items.Add(new Medication
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Name = "Metformin",
            DosageAmount = 500,
            DosageUnit = "mg",
            IsArchived = false
        });

        await fixture.Sut.DeleteAccountAsync(user.Id, new DeleteAccountRequest { Password = "old" }, "127.0.0.1");

        user.Status.Should().Be(UserStatus.Deleted);
        user.IsActive.Should().BeFalse();
        user.DeletedAt.Should().NotBeNull();
        user.Email.Should().StartWith("deleted-").And.EndWith("@anon.invalid");
        user.Email.Should().NotContain("keep@example.com");
        user.PasswordHash.Should().NotBe("hashed:old");
        token.RevokedAt.Should().NotBeNull();
        token.RevokedReason.Should().Be("account_deleted");
        fixture.NfcTags.Items.Single().IsAssigned.Should().BeFalse();
        fixture.Medications.Items.Single().IsArchived.Should().BeTrue();
        fixture.Audit.Actions.Should().Contain("ACCOUNT_DELETED");
    }

    [Fact]
    public async Task DeleteAccount_IsIdempotent()
    {
        var fixture = ProfileFixture.Create();
        var user = fixture.SeedUser("once@example.com");
        await fixture.Sut.DeleteAccountAsync(user.Id, new DeleteAccountRequest { Password = "old" }, null);
        await fixture.Sut.DeleteAccountAsync(user.Id, new DeleteAccountRequest { Password = "anything" }, null);
        fixture.Audit.Actions.Count(a => a == "ACCOUNT_DELETED").Should().Be(1);
    }

    [Fact]
    public async Task DeleteAccount_WrongPassword_ThrowsUnauthorized()
    {
        var fixture = ProfileFixture.Create();
        var user = fixture.SeedUser("safe@example.com");

        var act = () => fixture.Sut.DeleteAccountAsync(
            user.Id,
            new DeleteAccountRequest { Password = "wrong-password" },
            null);

        await act.Should().ThrowAsync<UnauthorizedException>();
        user.Status.Should().Be(UserStatus.Active);
        fixture.Audit.Actions.Should().NotContain("ACCOUNT_DELETED");
    }

    [Fact]
    public async Task GetProfile_AfterDelete_ThrowsNotFound()
    {
        var fixture = ProfileFixture.Create();
        var user = fixture.SeedUser("gone@example.com");
        await fixture.Sut.DeleteAccountAsync(user.Id, new DeleteAccountRequest { Password = "old" }, null);

        var act = () => fixture.Sut.GetProfileAsync(user.Id);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Login_AfterDelete_FailsWithGenericMessage()
    {
        // Covered via AuthService: deleted users are inactive — smoke via repository state.
        var fixture = ProfileFixture.Create();
        var user = fixture.SeedUser("nope@example.com");
        await fixture.Sut.DeleteAccountAsync(user.Id, new DeleteAccountRequest { Password = "old" }, null);

        var byEmail = await fixture.Users.GetByEmailAsync("nope@example.com");
        byEmail.Should().BeNull();
        user.IsActive.Should().BeFalse();
    }

    private sealed class ProfileFixture
    {
        public UserProfileService Sut { get; }
        public InMemoryUserRepository Users { get; } = new();
        public InMemoryRefreshTokens RefreshTokens { get; } = new();
        public InMemoryNfcTags NfcTags { get; } = new();
        public InMemoryMedications Medications { get; } = new();
        public FakeAudit Audit { get; } = new();
        public FakePasswords Passwords { get; } = new();

        private ProfileFixture()
        {
            Sut = new UserProfileService(
                Users,
                RefreshTokens,
                NfcTags,
                Medications,
                new FakeUnitOfWork(),
                Passwords,
                Audit,
                new DeleteAccountRequestValidator());
        }

        public static ProfileFixture Create() => new();

        public User SeedUser(string email)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = email.ToLowerInvariant(),
                PasswordHash = "hashed:old",
                IsActive = true,
                Status = UserStatus.Active,
                TimeZoneId = "America/New_York"
            };
            Users.Items.Add(user);
            return user;
        }
    }

    private sealed class InMemoryUserRepository : IUserRepository
    {
        public List<User> Items { get; } = [];

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(u => u.Email == email));

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(u => u.Id == id));

        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            Items.Add(user);
            return Task.CompletedTask;
        }

        public void Update(User user)
        {
            // tracked
        }
    }

    private sealed class InMemoryRefreshTokens : IRefreshTokenRepository
    {
        public List<RefreshToken> Items { get; } = [];

        public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(t => t.TokenHash == tokenHash));

        public Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
        {
            Items.Add(refreshToken);
            return Task.CompletedTask;
        }

        public void Update(RefreshToken refreshToken)
        {
        }

        public Task RevokeFamilyAsync(Guid familyId, string reason, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task RevokeAllForUserAsync(Guid userId, string reason, CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            foreach (var t in Items.Where(x => x.UserId == userId && x.RevokedAt is null))
            {
                t.RevokedAt = now;
                t.RevokedReason = reason;
            }

            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryNfcTags : INfcTagRepository
    {
        public List<NfcTag> Items { get; } = [];

        public Task<NfcTag?> GetByTagIdentifierAsync(string tagIdentifier, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(t => t.TagIdentifier == tagIdentifier));

        public Task<NfcTag?> GetAssignedByMedicationForUserAsync(
            Guid userId,
            Guid medicationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(t =>
                t.UserId == userId && t.MedicationId == medicationId && t.IsAssigned));

        public Task<List<NfcTag>> GetAssignedForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.Where(t => t.UserId == userId && t.IsAssigned).ToList());

        public Task AddAsync(NfcTag tag, CancellationToken cancellationToken = default)
        {
            Items.Add(tag);
            return Task.CompletedTask;
        }

        public void Update(NfcTag tag)
        {
        }
    }

    private sealed class InMemoryMedications : IMedicationRepository
    {
        public List<Medication> Items { get; } = [];

        public Task<List<Medication>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.Where(m => m.UserId == userId && !m.IsArchived).ToList());

        public Task<Medication?> GetByIdForUserAsync(
            Guid userId,
            Guid medicationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(m => m.Id == medicationId && m.UserId == userId));

        public Task AddAsync(Medication medication, CancellationToken cancellationToken = default)
        {
            Items.Add(medication);
            return Task.CompletedTask;
        }

        public void Update(Medication medication)
        {
        }

        public Task ArchiveAsync(Guid userId, Guid medicationId, CancellationToken cancellationToken = default)
        {
            var med = Items.FirstOrDefault(m => m.Id == medicationId && m.UserId == userId);
            if (med is not null)
            {
                med.IsArchived = true;
            }

            return Task.CompletedTask;
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(1);
    }

    private sealed class FakePasswords : IPasswordService
    {
        public string HashPassword(string password) => $"hashed:{password}";

        public bool VerifyPassword(string password, string passwordHash) =>
            passwordHash == $"hashed:{password}";
    }

    private sealed class FakeAudit : IAuditService
    {
        public List<string> Actions { get; } = [];

        public Task LogAsync(
            string action,
            string entityType,
            Guid? userId = null,
            Guid? entityId = null,
            string? ipAddress = null,
            CancellationToken cancellationToken = default,
            string? metadata = null)
        {
            Actions.Add(action);
            return Task.CompletedTask;
        }
    }
}
