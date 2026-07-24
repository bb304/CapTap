using CapTap.Application.Common;
using CapTap.Application.DTOs.Logging;
using CapTap.Application.Exceptions;
using CapTap.Application.Interfaces;
using CapTap.Domain.Entities;
using CapTap.Domain.Enums;
using CapTap.Domain.Exceptions;
using FluentValidation;
using ValidationException = CapTap.Application.Exceptions.ValidationException;

namespace CapTap.Application.Services;

/// <summary>
/// Single source of truth for recording medication intake (Manual and Nfc).
/// </summary>
public sealed class MedicationLogService : IMedicationLogService
{
    private readonly IMedicationRepository _medications;
    private readonly IScheduleRepository _schedules;
    private readonly IMedicationLogRepository _logs;
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly ITimeProvider _timeProvider;
    private readonly IValidator<CreateMedicationLogRequest> _createValidator;

    public MedicationLogService(
        IMedicationRepository medications,
        IScheduleRepository schedules,
        IMedicationLogRepository logs,
        IUserRepository users,
        IUnitOfWork unitOfWork,
        IAuditService auditService,
        ITimeProvider timeProvider,
        IValidator<CreateMedicationLogRequest> createValidator)
    {
        _medications = medications;
        _schedules = schedules;
        _logs = logs;
        _users = users;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
        _timeProvider = timeProvider;
        _createValidator = createValidator;
    }

    public async Task<MedicationLogResponseDto> LogDoseAsync(
        Guid userId,
        CreateMedicationLogRequest request,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_createValidator, request, cancellationToken);

        if (request.LoggingMethod is not LoggingMethod.Manual and not LoggingMethod.Nfc)
        {
            throw new InvalidRequestException("Unsupported logging method.");
        }

        var medication = await _medications.GetByIdForUserAsync(userId, request.MedicationId, cancellationToken);
        if (medication is null || medication.IsArchived)
        {
            throw new NotFoundException("Medication was not found.");
        }

        var user = await _users.GetByIdAsync(userId, cancellationToken);
        var zone = TimeZoneHelper.Resolve(user?.TimeZoneId);

        var utcNow = _timeProvider.UtcNow;
        var scheduledUtc = NormalizeUtc(request.ScheduledDoseTime);
        var doseDate = TimeZoneHelper.LocalDateFromUtc(scheduledUtc, zone);
        var today = TimeZoneHelper.LocalDate(utcNow, zone);

        // Dose must belong to today's active schedule (user's local calendar day).
        if (doseDate != today)
        {
            throw new InvalidRequestException("Only today's scheduled doses can be logged.");
        }

        var schedule = await ResolveScheduleAsync(userId, request, scheduledUtc, doseDate, zone, cancellationToken);
        if (schedule is null)
        {
            throw new InvalidRequestException("No active schedule matches that dose time for today.");
        }

        // Canonical scheduled occurrence = local wall clock on today's local date → UTC.
        var canonicalScheduled = TimeZoneHelper.ToUtc(doseDate, schedule.ScheduledTime, zone);

        var duplicate = await _logs.FindDuplicateAsync(
            userId,
            schedule.Id,
            canonicalScheduled,
            cancellationToken);
        if (duplicate is not null)
        {
            throw new ConflictException("This dose has already been logged.");
        }

        var log = new MedicationLog
        {
            MedicationId = medication.Id,
            UserId = userId,
            ScheduleId = schedule.Id,
            ScheduledDoseTime = canonicalScheduled,
            LoggedAt = utcNow,
            LoggingMethod = request.LoggingMethod,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim()
        };

        await _logs.AddAsync(log, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var auditAction = request.LoggingMethod == LoggingMethod.Nfc
            ? "MEDICATION_LOGGED_NFC"
            : "MEDICATION_LOGGED";

        await _auditService.LogAsync(
            auditAction,
            "MedicationLog",
            userId,
            log.Id,
            ipAddress,
            cancellationToken);

        return new MedicationLogResponseDto
        {
            Id = log.Id,
            MedicationId = medication.Id,
            MedicationName = medication.Name,
            ScheduleId = schedule.Id,
            ScheduledDoseTime = log.ScheduledDoseTime,
            LoggedAt = log.LoggedAt,
            LoggingMethod = log.LoggingMethod,
            Notes = log.Notes
        };
    }

    public async Task<PaginatedMedicationLogHistoryDto> GetHistoryAsync(
        Guid userId,
        int page,
        int pageSize,
        Guid? medicationId = null,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        if (medicationId.HasValue)
        {
            var medication = await _medications.GetByIdForUserAsync(userId, medicationId.Value, cancellationToken);
            if (medication is null)
            {
                throw new NotFoundException("Medication was not found.");
            }
        }

        var (items, total) = await _logs.GetHistoryAsync(
            userId,
            page,
            pageSize,
            medicationId,
            cancellationToken);

        return new PaginatedMedicationLogHistoryDto
        {
            Items = items.Select(log => new MedicationLogHistoryItemDto
            {
                Id = log.Id,
                MedicationId = log.MedicationId,
                MedicationName = log.Medication.Name,
                ScheduledDoseTime = log.ScheduledDoseTime,
                LoggedAt = log.LoggedAt,
                LoggingMethod = log.LoggingMethod,
                Notes = log.Notes
            }).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
            TotalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize)
        };
    }

    private async Task<MedicationSchedule?> ResolveScheduleAsync(
        Guid userId,
        CreateMedicationLogRequest request,
        DateTime scheduledUtc,
        DateOnly localDoseDate,
        TimeZoneInfo zone,
        CancellationToken cancellationToken)
    {
        var localClock = TimeZoneHelper.LocalTime(scheduledUtc, zone);
        var utcClock = TimeOnly.FromDateTime(scheduledUtc);
        var active = await _schedules.GetActiveForUserOnDateAsync(userId, localDoseDate, cancellationToken);

        if (request.ScheduleId.HasValue)
        {
            return active.FirstOrDefault(s =>
                s.Id == request.ScheduleId.Value &&
                s.MedicationId == request.MedicationId);
        }

        return active.FirstOrDefault(s =>
            s.MedicationId == request.MedicationId &&
            (s.ScheduledTime == localClock || s.ScheduledTime == utcClock));
    }

    private static DateTime NormalizeUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

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
