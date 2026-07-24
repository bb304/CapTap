using CapTap.Application.Common;
using CapTap.Application.DTOs.Users;
using CapTap.Application.Exceptions;
using CapTap.Application.Interfaces;
using CapTap.Domain.Enums;
using CapTap.Domain.Exceptions;
using FluentValidation;
using ValidationException = CapTap.Application.Exceptions.ValidationException;

namespace CapTap.Application.Services;

public sealed class UserProfileService : IUserProfileService
{
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly INfcTagRepository _nfcTags;
    private readonly IMedicationRepository _medications;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordService _passwordService;
    private readonly IAuditService _auditService;
    private readonly IValidator<DeleteAccountRequest> _deleteAccountValidator;

    public UserProfileService(
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        INfcTagRepository nfcTags,
        IMedicationRepository medications,
        IUnitOfWork unitOfWork,
        IPasswordService passwordService,
        IAuditService auditService,
        IValidator<DeleteAccountRequest> deleteAccountValidator)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _nfcTags = nfcTags;
        _medications = medications;
        _unitOfWork = unitOfWork;
        _passwordService = passwordService;
        _auditService = auditService;
        _deleteAccountValidator = deleteAccountValidator;
    }

    public async Task<UserProfileDto> GetProfileAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User was not found.");

        EnsureNotDeleted(user);
        return Map(user);
    }

    public async Task<UserProfileDto> UpdateTimeZoneAsync(
        Guid userId,
        UpdateTimeZoneRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TimeZoneHelper.IsValidIana(request.TimeZoneId))
        {
            throw new InvalidRequestException("Time zone must be a valid IANA identifier (e.g. America/New_York).");
        }

        var user = await _users.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User was not found.");

        EnsureNotDeleted(user);

        user.TimeZoneId = request.TimeZoneId.Trim();
        _users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(user);
    }

    public async Task DeleteAccountAsync(
        Guid userId,
        DeleteAccountRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var validation = await _deleteAccountValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new ValidationException(errors);
        }

        var user = await _users.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User was not found.");

        if (user.Status == UserStatus.Deleted || user.DeletedAt is not null)
        {
            // Idempotent — already anonymized.
            return;
        }

        if (!_passwordService.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedException("Password confirmation failed.");
        }

        // Soft delete + anonymize PII (DDD: Account Deleted → Anonymize → retain logs).
        user.Email = $"deleted-{user.Id:N}@anon.invalid";
        user.PasswordHash = _passwordService.HashPassword(Guid.NewGuid().ToString("N"));
        user.EmailVerified = false;
        user.EmailVerificationSentAt = null;
        user.LastLoginAt = null;
        user.FailedLoginAttempts = 0;
        user.LockedUntil = null;
        user.TimeZoneId = "UTC";
        user.IsActive = false;
        user.Status = UserStatus.Deleted;
        user.DeletedAt = DateTime.UtcNow;
        _users.Update(user);

        await _refreshTokens.RevokeAllForUserAsync(userId, "account_deleted", cancellationToken);

        var tags = await _nfcTags.GetAssignedForUserAsync(userId, cancellationToken);
        foreach (var tag in tags)
        {
            tag.IsAssigned = false;
            _nfcTags.Update(tag);
        }

        var medications = await _medications.GetByUserIdAsync(userId, cancellationToken);
        foreach (var medication in medications)
        {
            await _medications.ArchiveAsync(userId, medication.Id, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync(
            "ACCOUNT_DELETED",
            "User",
            userId,
            userId,
            ipAddress,
            cancellationToken);
    }

    private static void EnsureNotDeleted(Domain.Entities.User user)
    {
        if (user.Status == UserStatus.Deleted || user.DeletedAt is not null || !user.IsActive)
        {
            throw new NotFoundException("User was not found.");
        }
    }

    private static UserProfileDto Map(Domain.Entities.User user) =>
        new()
        {
            Id = user.Id,
            Email = user.Email,
            TimeZoneId = user.TimeZoneId
        };
}
