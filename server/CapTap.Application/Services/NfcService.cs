using System.Text.Json;
using CapTap.Application.Common;
using CapTap.Application.DTOs.Nfc;
using CapTap.Application.Exceptions;
using CapTap.Application.Interfaces;
using CapTap.Domain.Entities;
using CapTap.Domain.Enums;
using CapTap.Domain.Exceptions;

namespace CapTap.Application.Services;

/// <summary>
/// NFC convenience layer: assign/resolve tags, then hand off to the shared medication logging pipeline.
/// </summary>
public sealed class NfcService : INfcService
{
    private readonly INfcTagRepository _tags;
    private readonly IMedicationRepository _medications;
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _audit;
    private readonly ITimeProvider _time;
    private readonly IAdherenceService _adherence;

    public NfcService(
        INfcTagRepository tags,
        IMedicationRepository medications,
        IUserRepository users,
        IUnitOfWork unitOfWork,
        IAuditService audit,
        ITimeProvider time,
        IAdherenceService adherence)
    {
        _tags = tags;
        _medications = medications;
        _users = users;
        _unitOfWork = unitOfWork;
        _audit = audit;
        _time = time;
        _adherence = adherence;
    }

    public async Task<NfcTagResponseDto> AssignAsync(
        Guid userId,
        AssignNfcTagRequest request,
        string? ipAddress = null,
        string? deviceId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.TagIdentifier))
        {
            throw new InvalidRequestException("Tag identifier is required.");
        }

        var medication = await _medications.GetByIdForUserAsync(userId, request.MedicationId, cancellationToken);
        if (medication is null || medication.IsArchived)
        {
            throw new NotFoundException("Medication was not found.");
        }

        var utcNow = _time.UtcNow;
        var normalized = request.TagIdentifier.Trim().ToUpperInvariant();

        // Soft-unassign any current sticker on this medication.
        var existingOnMed = await _tags.GetAssignedByMedicationForUserAsync(
            userId,
            request.MedicationId,
            cancellationToken);
        if (existingOnMed is not null &&
            !string.Equals(existingOnMed.TagIdentifier, normalized, StringComparison.Ordinal))
        {
            existingOnMed.IsAssigned = false;
            _tags.Update(existingOnMed);
        }

        var byTag = await _tags.GetByTagIdentifierAsync(normalized, cancellationToken);
        if (byTag is not null)
        {
            if (byTag.UserId != userId)
            {
                throw new ConflictException("This NFC tag is already assigned.");
            }

            byTag.MedicationId = medication.Id;
            byTag.IsAssigned = true;
            byTag.AssignedAt = utcNow;
            _tags.Update(byTag);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await AuditNfcAsync(
                "NFC_ASSIGNED",
                userId,
                byTag.Id,
                medication.Id,
                byTag.TagIdentifier,
                ipAddress,
                deviceId,
                cancellationToken);

            return MapTag(byTag, medication.Name);
        }

        var tag = new NfcTag
        {
            UserId = userId,
            MedicationId = medication.Id,
            TagIdentifier = normalized,
            IsAssigned = true,
            AssignedAt = utcNow
        };

        await _tags.AddAsync(tag, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await AuditNfcAsync(
            "NFC_ASSIGNED",
            userId,
            tag.Id,
            medication.Id,
            tag.TagIdentifier,
            ipAddress,
            deviceId,
            cancellationToken);

        return MapTag(tag, medication.Name);
    }

    public async Task<NfcTagResponseDto> UnassignAsync(
        Guid userId,
        UnassignNfcTagRequest request,
        string? ipAddress = null,
        string? deviceId = null,
        CancellationToken cancellationToken = default)
    {
        var medication = await _medications.GetByIdForUserAsync(userId, request.MedicationId, cancellationToken);
        if (medication is null)
        {
            throw new NotFoundException("Medication was not found.");
        }

        var tag = await _tags.GetAssignedByMedicationForUserAsync(userId, request.MedicationId, cancellationToken);
        if (tag is null)
        {
            throw new NotFoundException("No NFC tag is assigned to this medication.");
        }

        tag.IsAssigned = false;
        _tags.Update(tag);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await AuditNfcAsync(
            "NFC_UNASSIGNED",
            userId,
            tag.Id,
            medication.Id,
            tag.TagIdentifier,
            ipAddress,
            deviceId,
            cancellationToken);

        return MapTag(tag, medication.Name);
    }

    public async Task<NfcResolveResponseDto> ResolveAsync(
        Guid userId,
        string tagIdentifier,
        string? ipAddress = null,
        string? deviceId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tagIdentifier))
        {
            throw new InvalidRequestException("Tag identifier is required.");
        }

        var tag = await _tags.GetByTagIdentifierAsync(tagIdentifier, cancellationToken);
        if (tag is null || !tag.IsAssigned || tag.UserId != userId)
        {
            // Identical response for unknown / other-user tags — do not leak ownership.
            throw new NotFoundException("NFC tag was not found.");
        }

        var medication = tag.Medication;
        if (medication is null || medication.UserId != userId || medication.IsArchived)
        {
            throw new NotFoundException("NFC tag was not found.");
        }

        var utcNow = _time.UtcNow;
        tag.LastScannedAt = utcNow;
        _tags.Update(tag);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await AuditNfcAsync(
            "NFC_SCANNED",
            userId,
            tag.Id,
            medication.Id,
            tag.TagIdentifier,
            ipAddress,
            deviceId,
            cancellationToken);

        var user = await _users.GetByIdAsync(userId, cancellationToken);
        var zone = TimeZoneHelper.Resolve(user?.TimeZoneId);
        var localToday = TimeZoneHelper.LocalDate(utcNow, zone);

        var doses = await _adherence.GetTodayDosesAsync(userId, cancellationToken);
        var forMed = doses
            .Where(d => d.MedicationId == medication.Id)
            .OrderBy(d => Array.IndexOf(
                [AdherenceStatus.Due, AdherenceStatus.Upcoming, AdherenceStatus.Taken, AdherenceStatus.Missed],
                d.Status))
            .ThenBy(d => d.ScheduledTime)
            .ToList();

        var recommended = forMed.FirstOrDefault(d => d.Status is AdherenceStatus.Due or AdherenceStatus.Upcoming)
            ?? forMed.FirstOrDefault();

        DateTime? scheduledDoseUtc = null;
        if (recommended is not null)
        {
            scheduledDoseUtc = TimeZoneHelper.ToUtc(localToday, recommended.ScheduledTime, zone);
        }

        return new NfcResolveResponseDto
        {
            MedicationId = medication.Id,
            MedicationName = medication.Name,
            DosageAmount = medication.DosageAmount,
            DosageUnit = medication.DosageUnit,
            Form = medication.Form,
            ScheduleId = recommended?.ScheduleId,
            ScheduledTime = recommended?.ScheduledTime.ToString("HH\\:mm\\:ss"),
            ScheduledDoseTime = scheduledDoseUtc,
            Status = recommended?.Status,
            AlreadyLogged = recommended?.Status == AdherenceStatus.Taken,
            TagIdentifier = tag.TagIdentifier
        };
    }

    public async Task<List<NfcTagResponseDto>> ListAssignedAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var tags = await _tags.GetAssignedForUserAsync(userId, cancellationToken);
        return tags
            .Select(tag => MapTag(tag, tag.Medication.Name))
            .ToList();
    }

    private async Task AuditNfcAsync(
        string action,
        Guid userId,
        Guid tagId,
        Guid medicationId,
        string tagIdentifier,
        string? ipAddress,
        string? deviceId,
        CancellationToken cancellationToken)
    {
        var metadata = JsonSerializer.Serialize(new
        {
            medicationId,
            tagIdentifier,
            deviceId
        });

        await _audit.LogAsync(
            action,
            "NfcTag",
            userId,
            tagId,
            ipAddress,
            cancellationToken,
            metadata);
    }

    private static NfcTagResponseDto MapTag(NfcTag tag, string medicationName) =>
        new()
        {
            Id = tag.Id,
            MedicationId = tag.MedicationId,
            MedicationName = medicationName,
            TagIdentifier = tag.TagIdentifier,
            IsAssigned = tag.IsAssigned,
            AssignedAt = tag.AssignedAt,
            LastScannedAt = tag.LastScannedAt
        };
}
