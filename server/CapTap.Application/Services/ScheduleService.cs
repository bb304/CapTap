using CapTap.Application.DTOs.Schedule;
using CapTap.Application.Exceptions;
using CapTap.Application.Interfaces;
using CapTap.Domain.Entities;
using CapTap.Domain.Exceptions;
using FluentValidation;
using ValidationException = CapTap.Application.Exceptions.ValidationException;

namespace CapTap.Application.Services;

public sealed class ScheduleService : IScheduleService
{
    private readonly IScheduleRepository _schedules;
    private readonly IMedicationRepository _medications;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly IValidator<CreateScheduleRequest> _createValidator;
    private readonly IValidator<UpdateScheduleRequest> _updateValidator;

    public ScheduleService(
        IScheduleRepository schedules,
        IMedicationRepository medications,
        IUnitOfWork unitOfWork,
        IAuditService auditService,
        IValidator<CreateScheduleRequest> createValidator,
        IValidator<UpdateScheduleRequest> updateValidator)
    {
        _schedules = schedules;
        _medications = medications;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<List<ScheduleResponseDto>> GetSchedulesAsync(
        Guid userId,
        Guid medicationId,
        CancellationToken cancellationToken = default)
    {
        await EnsureMedicationOwnedAsync(userId, medicationId, cancellationToken);
        var schedules = await _schedules.GetByMedicationForUserAsync(userId, medicationId, cancellationToken);
        return schedules.Select(MapToDto).ToList();
    }

    public async Task<ScheduleResponseDto> CreateScheduleAsync(
        Guid userId,
        Guid medicationId,
        CreateScheduleRequest request,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_createValidator, request, cancellationToken);
        await EnsureMedicationOwnedAsync(userId, medicationId, cancellationToken);

        if (await _schedules.HasActiveDuplicateTimeAsync(medicationId, request.ScheduledTime, null, cancellationToken))
        {
            throw new InvalidRequestException("A schedule already exists for this medication at that time.");
        }

        var effectiveFrom = (request.EffectiveFrom ?? DateTime.UtcNow).Date;
        var schedule = new MedicationSchedule
        {
            MedicationId = medicationId,
            Frequency = request.Frequency,
            ScheduledTime = request.ScheduledTime,
            DoseQuantity = request.DoseQuantity,
            IsActive = true,
            EffectiveFrom = DateTime.SpecifyKind(effectiveFrom, DateTimeKind.Utc),
            EffectiveTo = request.EffectiveTo.HasValue
                ? DateTime.SpecifyKind(request.EffectiveTo.Value.Date, DateTimeKind.Utc)
                : null
        };

        await _schedules.AddAsync(schedule, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync(
            "SCHEDULE_CREATED",
            "MedicationSchedule",
            userId,
            schedule.Id,
            ipAddress,
            cancellationToken);

        return MapToDto(schedule);
    }

    public async Task<ScheduleResponseDto> UpdateScheduleAsync(
        Guid userId,
        Guid scheduleId,
        UpdateScheduleRequest request,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_updateValidator, request, cancellationToken);

        var schedule = await _schedules.GetByIdForUserAsync(userId, scheduleId, cancellationToken);
        if (schedule is null)
        {
            throw new NotFoundException("Schedule was not found.");
        }

        var nextTime = request.ScheduledTime ?? schedule.ScheduledTime;
        var willBeActive = request.IsActive ?? schedule.IsActive;
        if (willBeActive &&
            await _schedules.HasActiveDuplicateTimeAsync(
                schedule.MedicationId,
                nextTime,
                schedule.Id,
                cancellationToken))
        {
            throw new InvalidRequestException("A schedule already exists for this medication at that time.");
        }

        if (request.Frequency.HasValue)
        {
            schedule.Frequency = request.Frequency.Value;
        }

        if (request.ScheduledTime.HasValue)
        {
            schedule.ScheduledTime = request.ScheduledTime.Value;
        }

        if (request.DoseQuantity.HasValue)
        {
            schedule.DoseQuantity = request.DoseQuantity.Value;
        }

        if (request.IsActive.HasValue)
        {
            schedule.IsActive = request.IsActive.Value;
        }

        if (request.EffectiveFrom.HasValue)
        {
            schedule.EffectiveFrom = DateTime.SpecifyKind(request.EffectiveFrom.Value.Date, DateTimeKind.Utc);
        }

        if (request.EffectiveTo.HasValue)
        {
            schedule.EffectiveTo = DateTime.SpecifyKind(request.EffectiveTo.Value.Date, DateTimeKind.Utc);
        }

        if (schedule.EffectiveTo.HasValue && schedule.EffectiveTo.Value < schedule.EffectiveFrom)
        {
            throw new InvalidRequestException("EffectiveTo must be on or after EffectiveFrom.");
        }

        _schedules.Update(schedule);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync(
            "SCHEDULE_UPDATED",
            "MedicationSchedule",
            userId,
            schedule.Id,
            ipAddress,
            cancellationToken);

        return MapToDto(schedule);
    }

    public async Task DeleteScheduleAsync(
        Guid userId,
        Guid scheduleId,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        var schedule = await _schedules.GetByIdForUserAsync(userId, scheduleId, cancellationToken);
        if (schedule is null)
        {
            throw new NotFoundException("Schedule was not found.");
        }

        // Soft-delete: deactivate so historical logs retain schedule linkage.
        schedule.IsActive = false;
        schedule.EffectiveTo ??= DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
        _schedules.Update(schedule);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync(
            "SCHEDULE_DELETED",
            "MedicationSchedule",
            userId,
            schedule.Id,
            ipAddress,
            cancellationToken);
    }

    private async Task EnsureMedicationOwnedAsync(
        Guid userId,
        Guid medicationId,
        CancellationToken cancellationToken)
    {
        var medication = await _medications.GetByIdForUserAsync(userId, medicationId, cancellationToken);
        if (medication is null)
        {
            throw new NotFoundException("Medication was not found.");
        }
    }

    private static ScheduleResponseDto MapToDto(MedicationSchedule schedule) =>
        new()
        {
            Id = schedule.Id,
            MedicationId = schedule.MedicationId,
            Frequency = schedule.Frequency,
            ScheduledTime = schedule.ScheduledTime,
            DoseQuantity = schedule.DoseQuantity,
            IsActive = schedule.IsActive,
            EffectiveFrom = schedule.EffectiveFrom,
            EffectiveTo = schedule.EffectiveTo
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
