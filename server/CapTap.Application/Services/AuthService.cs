using CapTap.Application.Common;
using CapTap.Application.DTOs.Auth;
using CapTap.Application.Exceptions;
using CapTap.Application.Interfaces;
using CapTap.Domain.Entities;
using CapTap.Domain.Enums;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ValidationException = CapTap.Application.Exceptions.ValidationException;

namespace CapTap.Application.Services;

public sealed class AuthService : IAuthService
{
    private const int MaxFailedLoginAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan EmailVerificationLifetime = TimeSpan.FromHours(24);
    private static readonly TimeSpan PasswordResetLifetime = TimeSpan.FromHours(1);

    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IGenericRepository<EmailVerificationToken> _emailVerificationTokens;
    private readonly IPasswordResetTokenRepository _passwordResetTokens;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordService _passwordService;
    private readonly ITokenService _tokenService;
    private readonly IEmailService _emailService;
    private readonly IAuditService _auditService;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<RefreshTokenRequest> _refreshValidator;
    private readonly IValidator<ForgotPasswordRequest> _forgotPasswordValidator;
    private readonly IValidator<ResetPasswordRequest> _resetPasswordValidator;
    private readonly ILogger<AuthService> _logger;
    private readonly AuthOptions _authOptions;

    public AuthService(
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        IGenericRepository<EmailVerificationToken> emailVerificationTokens,
        IPasswordResetTokenRepository passwordResetTokens,
        IUnitOfWork unitOfWork,
        IPasswordService passwordService,
        ITokenService tokenService,
        IEmailService emailService,
        IAuditService auditService,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator,
        IValidator<RefreshTokenRequest> refreshValidator,
        IValidator<ForgotPasswordRequest> forgotPasswordValidator,
        IValidator<ResetPasswordRequest> resetPasswordValidator,
        ILogger<AuthService> logger,
        IOptions<AuthOptions> authOptions)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _emailVerificationTokens = emailVerificationTokens;
        _passwordResetTokens = passwordResetTokens;
        _unitOfWork = unitOfWork;
        _passwordService = passwordService;
        _tokenService = tokenService;
        _emailService = emailService;
        _auditService = auditService;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _refreshValidator = refreshValidator;
        _forgotPasswordValidator = forgotPasswordValidator;
        _resetPasswordValidator = resetPasswordValidator;
        _logger = logger;
        _authOptions = authOptions.Value;
    }

    public async Task<RegisterResponse> RegisterAsync(
        RegisterRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_registerValidator, request, cancellationToken);

        var email = NormalizeEmail(request.Email);
        var existing = await _users.GetByEmailAsync(email, cancellationToken);
        if (existing is not null)
        {
            throw new InvalidRequestException("Account creation failed.");
        }

        var user = new User
        {
            Email = email,
            PasswordHash = _passwordService.HashPassword(request.Password),
            EmailVerified = false,
            IsActive = true,
            Status = UserStatus.Active,
            FailedLoginAttempts = 0
        };

        await _users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var verificationToken = _tokenService.GenerateRefreshToken();
        await _emailVerificationTokens.AddAsync(new EmailVerificationToken
        {
            UserId = user.Id,
            TokenHash = _tokenService.HashToken(verificationToken),
            ExpiresAt = DateTime.UtcNow.Add(EmailVerificationLifetime)
        }, cancellationToken);

        user.EmailVerificationSentAt = DateTime.UtcNow;
        _users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _emailService.SendEmailVerificationAsync(user.Email, verificationToken, cancellationToken);
        await _auditService.LogAsync("REGISTER_SUCCESS", "User", user.Id, user.Id, ipAddress, cancellationToken);

        return new RegisterResponse
        {
            UserId = user.Id,
            Message = "Verification email sent"
        };
    }

    public async Task<AuthResponse> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_loginValidator, request, cancellationToken);

        var email = NormalizeEmail(request.Email);
        var user = await _users.GetByEmailAsync(email, cancellationToken);

        if (user is null || !user.IsActive)
        {
            await _auditService.LogAsync("LOGIN_FAILED", "User", null, null, ipAddress, cancellationToken);
            throw new UnauthorizedException("Invalid email or password.");
        }

        if (IsLocked(user))
        {
            await _auditService.LogAsync("LOGIN_FAILED", "User", user.Id, user.Id, ipAddress, cancellationToken);
            throw new UnauthorizedException("Account is temporarily locked. Try again later.");
        }

        if (!_passwordService.VerifyPassword(request.Password, user.PasswordHash))
        {
            await HandleFailedLoginAsync(user, ipAddress, cancellationToken);
            throw new UnauthorizedException("Invalid email or password.");
        }

        user.FailedLoginAttempts = 0;
        user.LockedUntil = null;
        if (user.Status == UserStatus.Locked)
        {
            user.Status = UserStatus.Active;
        }

        user.LastLoginAt = DateTime.UtcNow;
        _users.Update(user);

        var response = await IssueTokensAsync(user, familyId: Guid.NewGuid(), cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync("LOGIN_SUCCESS", "User", user.Id, user.Id, ipAddress, cancellationToken);

        return response;
    }

    public async Task<AuthResponse> RefreshTokenAsync(
        RefreshTokenRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_refreshValidator, request, cancellationToken);

        var tokenHash = _tokenService.HashToken(request.RefreshToken);
        var stored = await _refreshTokens.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (stored is null)
        {
            throw new UnauthorizedException("Invalid or expired refresh token.");
        }

        // Reuse of a revoked token → compromise signal → revoke entire family.
        if (stored.RevokedAt is not null)
        {
            await _refreshTokens.RevokeFamilyAsync(stored.FamilyId, "reuse_detected", cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _auditService.LogAsync(
                "REFRESH_REUSE_DETECTED",
                "RefreshToken",
                stored.UserId,
                stored.Id,
                ipAddress,
                cancellationToken);

            throw new UnauthorizedException("Invalid or expired refresh token.");
        }

        if (stored.ExpiresAt <= DateTime.UtcNow)
        {
            throw new UnauthorizedException("Invalid or expired refresh token.");
        }

        var user = await _users.GetByIdAsync(stored.UserId, cancellationToken);
        if (user is null || !user.IsActive || IsLocked(user))
        {
            throw new UnauthorizedException("Invalid or expired refresh token.");
        }

        // Rotation: revoke presented token, then issue a new pair in the same family.
        stored.RevokedAt = DateTime.UtcNow;
        stored.RevokedReason = "rotated";
        _refreshTokens.Update(stored);

        var response = await IssueTokensAsync(user, stored.FamilyId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync("TOKEN_REFRESHED", "RefreshToken", user.Id, stored.Id, ipAddress, cancellationToken);

        return response;
    }

    public async Task LogoutAsync(
        RefreshTokenRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_refreshValidator, request, cancellationToken);

        var tokenHash = _tokenService.HashToken(request.RefreshToken);
        var stored = await _refreshTokens.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (stored is null)
        {
            return;
        }

        await _refreshTokens.RevokeFamilyAsync(stored.FamilyId, "logout", cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync("LOGOUT", "RefreshToken", stored.UserId, stored.Id, ipAddress, cancellationToken);
    }

    public async Task ForgotPasswordAsync(
        ForgotPasswordRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_forgotPasswordValidator, request, cancellationToken);

        var email = NormalizeEmail(request.Email);
        var user = await _users.GetByEmailAsync(email, cancellationToken);

        if (user is not null && user.IsActive)
        {
            var resetToken = _tokenService.GenerateRefreshToken();
            await _passwordResetTokens.AddAsync(new PasswordResetToken
            {
                UserId = user.Id,
                TokenHash = _tokenService.HashToken(resetToken),
                ExpiresAt = DateTime.UtcNow.Add(PasswordResetLifetime)
            }, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _emailService.SendPasswordResetAsync(user.Email, resetToken, cancellationToken);
            await _auditService.LogAsync("PASSWORD_RESET_REQUEST", "User", user.Id, user.Id, ipAddress, cancellationToken);
        }
        else
        {
            // Never reveal whether the account exists.
            _logger.LogInformation("Password reset requested for unknown or inactive account.");
            await _auditService.LogAsync("PASSWORD_RESET_REQUEST", "User", null, null, ipAddress, cancellationToken);
        }
    }

    public async Task ResetPasswordAsync(
        ResetPasswordRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_resetPasswordValidator, request, cancellationToken);

        var tokenHash = _tokenService.HashToken(request.Token);
        var stored = await _passwordResetTokens.GetActiveByTokenHashAsync(tokenHash, cancellationToken);

        if (stored is null)
        {
            throw new InvalidRequestException("Unable to reset password.");
        }

        var user = await _users.GetByIdAsync(stored.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new InvalidRequestException("Unable to reset password.");
        }

        user.PasswordHash = _passwordService.HashPassword(request.NewPassword);
        user.FailedLoginAttempts = 0;
        user.LockedUntil = null;
        if (user.Status == UserStatus.Locked)
        {
            user.Status = UserStatus.Active;
        }

        stored.UsedAt = DateTime.UtcNow;
        _users.Update(user);
        _passwordResetTokens.Update(stored);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync("PASSWORD_RESET_SUCCESS", "User", user.Id, user.Id, ipAddress, cancellationToken);
    }

    private async Task HandleFailedLoginAsync(User user, string? ipAddress, CancellationToken cancellationToken)
    {
        user.FailedLoginAttempts += 1;
        if (user.FailedLoginAttempts >= MaxFailedLoginAttempts)
        {
            user.Status = UserStatus.Locked;
            user.LockedUntil = DateTime.UtcNow.Add(LockoutDuration);
            user.FailedLoginAttempts = 0;
        }

        _users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync("LOGIN_FAILED", "User", user.Id, user.Id, ipAddress, cancellationToken);
    }

    private async Task<AuthResponse> IssueTokensAsync(User user, Guid familyId, CancellationToken cancellationToken)
    {
        var refreshTokenId = Guid.NewGuid();
        var accessToken = _tokenService.GenerateAccessToken(user, refreshTokenId);
        var refreshToken = _tokenService.GenerateRefreshToken();

        await _refreshTokens.AddAsync(new RefreshToken
        {
            Id = refreshTokenId,
            UserId = user.Id,
            FamilyId = familyId,
            TokenHash = _tokenService.HashToken(refreshToken),
            ExpiresAt = DateTime.UtcNow.AddDays(_authOptions.RefreshTokenExpirationDays)
        }, cancellationToken);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = _authOptions.AccessTokenExpirationMinutes * 60
        };
    }

    private static bool IsLocked(User user)
    {
        if (user.Status == UserStatus.Suspended)
        {
            return true;
        }

        return user.LockedUntil is not null && user.LockedUntil > DateTime.UtcNow;
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static async Task ValidateAsync<T>(IValidator<T> validator, T request, CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(request, cancellationToken);
        if (!result.IsValid)
        {
            var errors = result.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            throw new ValidationException(errors);
        }
    }
}
