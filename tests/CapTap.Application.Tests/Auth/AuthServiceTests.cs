using CapTap.Application.Common;
using CapTap.Application.DTOs.Auth;
using CapTap.Application.Exceptions;
using CapTap.Application.Interfaces;
using CapTap.Application.Services;
using CapTap.Application.Validators;
using CapTap.Domain.Common;
using CapTap.Domain.Entities;
using CapTap.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace CapTap.Application.Tests.Auth;

public sealed class AuthServiceTests
{
    private const string ValidPassword = "SecurePass1!";

    [Fact]
    public async Task Register_WithValidRequest_Succeeds()
    {
        var fixture = AuthTestFixture.Create();

        var result = await fixture.Sut.RegisterAsync(
            new RegisterRequest
            {
                Email = "User@Example.com",
                Password = ValidPassword,
                ConfirmPassword = ValidPassword
            },
            "127.0.0.1");

        result.UserId.Should().NotBeEmpty();
        fixture.Users.Items.Should().ContainSingle(u => u.Email == "user@example.com");
        fixture.Audit.Actions.Should().Contain("REGISTER_SUCCESS");
        fixture.Emails.VerificationEmails.Should().ContainSingle();
    }

    [Fact]
    public async Task Register_DuplicateEmail_FailsSafely()
    {
        var fixture = AuthTestFixture.Create();
        await fixture.Sut.RegisterAsync(
            new RegisterRequest
            {
                Email = "dup@example.com",
                Password = ValidPassword,
                ConfirmPassword = ValidPassword
            },
            null);

        var act = () => fixture.Sut.RegisterAsync(
            new RegisterRequest
            {
                Email = "dup@example.com",
                Password = ValidPassword,
                ConfirmPassword = ValidPassword
            },
            null);

        var exception = await act.Should().ThrowAsync<InvalidRequestException>();
        exception.Which.Message.Should().Be("Account creation failed.");
        exception.Which.Message.Should().NotContain("exists");
    }

    [Fact]
    public async Task Register_HashesPassword()
    {
        var fixture = AuthTestFixture.Create();

        await fixture.Sut.RegisterAsync(
            new RegisterRequest
            {
                Email = "hash@example.com",
                Password = ValidPassword,
                ConfirmPassword = ValidPassword
            },
            null);

        var user = fixture.Users.Items.Single();
        user.PasswordHash.Should().NotBe(ValidPassword);
        user.PasswordHash.Should().StartWith("hashed:");
        fixture.Passwords.VerifyPassword(ValidPassword, user.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task Login_WithCorrectPassword_Succeeds()
    {
        var fixture = AuthTestFixture.Create();
        await fixture.Sut.RegisterAsync(
            new RegisterRequest
            {
                Email = "login@example.com",
                Password = ValidPassword,
                ConfirmPassword = ValidPassword
            },
            null);

        var response = await fixture.Sut.LoginAsync(
            new LoginRequest { Email = "login@example.com", Password = ValidPassword },
            "127.0.0.1");

        response.AccessToken.Should().NotBeNullOrWhiteSpace();
        response.RefreshToken.Should().NotBeNullOrWhiteSpace();
        response.ExpiresIn.Should().Be(900);
        fixture.Audit.Actions.Should().Contain("LOGIN_SUCCESS");
    }

    [Fact]
    public async Task Login_WithIncorrectPassword_Fails()
    {
        var fixture = AuthTestFixture.Create();
        await fixture.Sut.RegisterAsync(
            new RegisterRequest
            {
                Email = "badlogin@example.com",
                Password = ValidPassword,
                ConfirmPassword = ValidPassword
            },
            null);

        var act = () => fixture.Sut.LoginAsync(
            new LoginRequest { Email = "badlogin@example.com", Password = "WrongPass1!" },
            null);

        await act.Should().ThrowAsync<UnauthorizedException>();
        fixture.Audit.Actions.Should().Contain("LOGIN_FAILED");
        fixture.Users.Items.Single().FailedLoginAttempts.Should().Be(1);
    }

    [Fact]
    public async Task RefreshToken_ValidToken_Works()
    {
        var fixture = AuthTestFixture.Create();
        await fixture.Sut.RegisterAsync(
            new RegisterRequest
            {
                Email = "refresh@example.com",
                Password = ValidPassword,
                ConfirmPassword = ValidPassword
            },
            null);

        var login = await fixture.Sut.LoginAsync(
            new LoginRequest { Email = "refresh@example.com", Password = ValidPassword },
            null);

        var refreshed = await fixture.Sut.RefreshTokenAsync(
            new RefreshTokenRequest { RefreshToken = login.RefreshToken },
            null);

        refreshed.AccessToken.Should().NotBeNullOrWhiteSpace();
        refreshed.RefreshToken.Should().NotBe(login.RefreshToken);
        fixture.RefreshTokens.Items.Count(t => t.RevokedAt is not null).Should().Be(1);
        fixture.RefreshTokens.Items.Select(t => t.FamilyId).Distinct().Should().ContainSingle();
    }

    [Fact]
    public async Task RefreshToken_ReuseOfRevokedToken_RevokesFamily()
    {
        var fixture = AuthTestFixture.Create();
        await fixture.Sut.RegisterAsync(
            new RegisterRequest
            {
                Email = "reuse@example.com",
                Password = ValidPassword,
                ConfirmPassword = ValidPassword
            },
            null);

        var login = await fixture.Sut.LoginAsync(
            new LoginRequest { Email = "reuse@example.com", Password = ValidPassword },
            null);

        var rotated = await fixture.Sut.RefreshTokenAsync(
            new RefreshTokenRequest { RefreshToken = login.RefreshToken },
            null);

        // Attacker replays the old (already rotated) refresh token.
        var reuse = () => fixture.Sut.RefreshTokenAsync(
            new RefreshTokenRequest { RefreshToken = login.RefreshToken },
            null);

        await reuse.Should().ThrowAsync<UnauthorizedException>();
        fixture.Audit.Actions.Should().Contain("REFRESH_REUSE_DETECTED");
        fixture.RefreshTokens.Items.Should().OnlyContain(t => t.RevokedAt != null);

        // Current rotated token is also dead after family revocation.
        var current = () => fixture.Sut.RefreshTokenAsync(
            new RefreshTokenRequest { RefreshToken = rotated.RefreshToken },
            null);

        await current.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task RefreshToken_ExpiredToken_Fails()
    {
        var fixture = AuthTestFixture.Create();
        await fixture.Sut.RegisterAsync(
            new RegisterRequest
            {
                Email = "expired@example.com",
                Password = ValidPassword,
                ConfirmPassword = ValidPassword
            },
            null);

        var login = await fixture.Sut.LoginAsync(
            new LoginRequest { Email = "expired@example.com", Password = ValidPassword },
            null);

        var stored = fixture.RefreshTokens.Items.Single(t => t.RevokedAt is null);
        stored.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);

        var act = () => fixture.Sut.RefreshTokenAsync(
            new RefreshTokenRequest { RefreshToken = login.RefreshToken },
            null);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Login_RepeatedFailures_LocksAccount()
    {
        var fixture = AuthTestFixture.Create();
        await fixture.Sut.RegisterAsync(
            new RegisterRequest
            {
                Email = "lock@example.com",
                Password = ValidPassword,
                ConfirmPassword = ValidPassword
            },
            null);

        for (var i = 0; i < 5; i++)
        {
            try
            {
                await fixture.Sut.LoginAsync(
                    new LoginRequest { Email = "lock@example.com", Password = "WrongPass1!" },
                    null);
            }
            catch (UnauthorizedException)
            {
                // expected
            }
        }

        var user = fixture.Users.Items.Single();
        user.Status.Should().Be(UserStatus.Locked);
        user.LockedUntil.Should().NotBeNull();
        user.LockedUntil.Should().BeAfter(DateTime.UtcNow);

        var lockedLogin = () => fixture.Sut.LoginAsync(
            new LoginRequest { Email = "lock@example.com", Password = ValidPassword },
            null);

        var exception = await lockedLogin.Should().ThrowAsync<UnauthorizedException>();
        exception.Which.Message.Should().Contain("locked");
    }

    private sealed class AuthTestFixture
    {
        public required AuthService Sut { get; init; }
        public required InMemoryUserRepository Users { get; init; }
        public required InMemoryRefreshTokenRepository RefreshTokens { get; init; }
        public required FakePasswordService Passwords { get; init; }
        public required FakeEmailService Emails { get; init; }
        public required FakeAuditService Audit { get; init; }

        public static AuthTestFixture Create()
        {
            var users = new InMemoryUserRepository();
            var refreshTokens = new InMemoryRefreshTokenRepository();
            var emailTokens = new InMemoryGenericRepository<EmailVerificationToken>();
            var resetTokens = new InMemoryPasswordResetTokenRepository();
            var unitOfWork = new FakeUnitOfWork();
            var passwords = new FakePasswordService();
            var tokens = new FakeTokenService();
            var emails = new FakeEmailService();
            var audit = new FakeAuditService();

            var sut = new AuthService(
                users,
                refreshTokens,
                emailTokens,
                resetTokens,
                unitOfWork,
                passwords,
                tokens,
                emails,
                audit,
                new RegisterRequestValidator(),
                new LoginRequestValidator(),
                new RefreshTokenRequestValidator(),
                new ForgotPasswordRequestValidator(),
                new ResetPasswordRequestValidator(),
                NullLogger<AuthService>.Instance,
                Options.Create(new AuthOptions
                {
                    AccessTokenExpirationMinutes = 15,
                    RefreshTokenExpirationDays = 30
                }));

            return new AuthTestFixture
            {
                Sut = sut,
                Users = users,
                RefreshTokens = refreshTokens,
                Passwords = passwords,
                Emails = emails,
                Audit = audit
            };
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
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
            if (user.Id == Guid.Empty)
            {
                user.Id = Guid.NewGuid();
            }

            Items.Add(user);
            return Task.CompletedTask;
        }

        public void Update(User user)
        {
            // tracked in-memory
        }
    }

    private sealed class InMemoryRefreshTokenRepository : IRefreshTokenRepository
    {
        public List<RefreshToken> Items { get; } = [];

        public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(t => t.TokenHash == tokenHash));

        public Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
        {
            if (refreshToken.Id == Guid.Empty)
            {
                refreshToken.Id = Guid.NewGuid();
            }

            Items.Add(refreshToken);
            return Task.CompletedTask;
        }

        public void Update(RefreshToken refreshToken)
        {
            // tracked in-memory
        }

        public Task RevokeFamilyAsync(Guid familyId, string reason, CancellationToken cancellationToken = default)
        {
            var utcNow = DateTime.UtcNow;
            foreach (var token in Items.Where(t => t.FamilyId == familyId && t.RevokedAt is null))
            {
                token.RevokedAt = utcNow;
                token.RevokedReason = reason;
            }

            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryPasswordResetTokenRepository : IPasswordResetTokenRepository
    {
        private readonly List<PasswordResetToken> _items = [];

        public Task<PasswordResetToken?> GetActiveByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(t =>
                t.TokenHash == tokenHash && t.UsedAt is null && t.ExpiresAt > DateTime.UtcNow));

        public Task AddAsync(PasswordResetToken token, CancellationToken cancellationToken = default)
        {
            if (token.Id == Guid.Empty)
            {
                token.Id = Guid.NewGuid();
            }

            _items.Add(token);
            return Task.CompletedTask;
        }

        public void Update(PasswordResetToken token)
        {
            // tracked in-memory
        }
    }

    private sealed class InMemoryGenericRepository<T> : IGenericRepository<T>
        where T : BaseEntity
    {
        private readonly List<T> _items = [];

        public Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(i => i.Id == id));

        public Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.ToList());

        public Task AddAsync(T entity, CancellationToken cancellationToken = default)
        {
            if (entity.Id == Guid.Empty)
            {
                entity.Id = Guid.NewGuid();
            }

            _items.Add(entity);
            return Task.CompletedTask;
        }

        public void Update(T entity)
        {
            // tracked in-memory
        }

        public void Remove(T entity) => _items.Remove(entity);
    }

    private sealed class FakePasswordService : IPasswordService
    {
        public string HashPassword(string password) => $"hashed:{password}";

        public bool VerifyPassword(string password, string passwordHash) =>
            passwordHash == $"hashed:{password}";
    }

    private sealed class FakeTokenService : ITokenService
    {
        public string GenerateAccessToken(User user, Guid tokenId) =>
            $"access:{user.Id}:{tokenId}";

        public string GenerateRefreshToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes);
        }

        public string HashToken(string token)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(bytes);
        }

        public ClaimsPrincipal? ValidateToken(string token) =>
            new(new ClaimsIdentity([new Claim("token", token)], "test"));
    }

    private sealed class FakeEmailService : IEmailService
    {
        public List<string> VerificationEmails { get; } = [];

        public Task SendEmailVerificationAsync(string email, string verificationToken, CancellationToken cancellationToken = default)
        {
            VerificationEmails.Add(email);
            return Task.CompletedTask;
        }

        public Task SendPasswordResetAsync(string email, string resetToken, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class FakeAuditService : IAuditService
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
