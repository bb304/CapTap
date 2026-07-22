using CapTap.Application.DTOs.Medication;
using CapTap.Application.Exceptions;
using CapTap.Application.Interfaces;
using CapTap.Domain.Entities;
using CapTap.Domain.Exceptions;
using FluentValidation;
using ValidationException = CapTap.Application.Exceptions.ValidationException;

namespace CapTap.Application.Services;

public sealed class MedicationService : IMedicationService
{
    private readonly IMedicationRepository _medications;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFdaMedicationService _fdaMedicationService;
    private readonly IAuditService _auditService;
    private readonly IValidator<CreateMedicationRequest> _createValidator;
    private readonly IValidator<UpdateMedicationRequest> _updateValidator;

    public MedicationService(
        IMedicationRepository medications,
        IUnitOfWork unitOfWork,
        IFdaMedicationService fdaMedicationService,
        IAuditService auditService,
        IValidator<CreateMedicationRequest> createValidator,
        IValidator<UpdateMedicationRequest> updateValidator)
    {
        _medications = medications;
        _unitOfWork = unitOfWork;
        _fdaMedicationService = fdaMedicationService;
        _auditService = auditService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<List<MedicationResponseDto>> GetUserMedicationsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var medications = await _medications.GetByUserIdAsync(userId, cancellationToken);
        return medications.Select(MapToDto).ToList();
    }

    public async Task<MedicationResponseDto> GetMedicationAsync(
        Guid userId,
        Guid medicationId,
        CancellationToken cancellationToken = default)
    {
        var medication = await _medications.GetByIdForUserAsync(userId, medicationId, cancellationToken);
        if (medication is null)
        {
            // Same response whether missing or owned by another user.
            throw new NotFoundException("Medication was not found.");
        }

        return MapToDto(medication);
    }

    public async Task<MedicationResponseDto> CreateMedicationAsync(
        Guid userId,
        CreateMedicationRequest request,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_createValidator, request, cancellationToken);

        var medication = new Medication
        {
            UserId = userId,
            Name = request.Name.Trim(),
            GenericName = NormalizeOptional(request.GenericName),
            BrandName = NormalizeOptional(request.BrandName),
            FdaIdentifier = NormalizeOptional(request.FdaIdentifier),
            DosageAmount = request.DosageAmount,
            DosageUnit = request.DosageUnit.Trim(),
            Form = NormalizeOptional(request.Form),
            Instructions = NormalizeOptional(request.Instructions),
            IsArchived = false
        };

        await _medications.AddAsync(medication, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync(
            "MEDICATION_CREATED",
            "Medication",
            userId,
            medication.Id,
            ipAddress,
            cancellationToken);

        return MapToDto(medication);
    }

    public async Task<MedicationResponseDto> UpdateMedicationAsync(
        Guid userId,
        Guid medicationId,
        UpdateMedicationRequest request,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_updateValidator, request, cancellationToken);

        var medication = await _medications.GetByIdForUserAsync(userId, medicationId, cancellationToken);
        if (medication is null)
        {
            throw new NotFoundException("Medication was not found.");
        }

        if (request.Name is not null)
        {
            medication.Name = request.Name.Trim();
        }

        if (request.GenericName is not null)
        {
            medication.GenericName = NormalizeOptional(request.GenericName);
        }

        if (request.BrandName is not null)
        {
            medication.BrandName = NormalizeOptional(request.BrandName);
        }

        if (request.FdaIdentifier is not null)
        {
            medication.FdaIdentifier = NormalizeOptional(request.FdaIdentifier);
        }

        if (request.DosageAmount.HasValue)
        {
            medication.DosageAmount = request.DosageAmount.Value;
        }

        if (request.DosageUnit is not null)
        {
            medication.DosageUnit = request.DosageUnit.Trim();
        }

        if (request.Form is not null)
        {
            medication.Form = NormalizeOptional(request.Form);
        }

        if (request.Instructions is not null)
        {
            medication.Instructions = NormalizeOptional(request.Instructions);
        }

        _medications.Update(medication);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync(
            "MEDICATION_UPDATED",
            "Medication",
            userId,
            medication.Id,
            ipAddress,
            cancellationToken);

        return MapToDto(medication);
    }

    public async Task ArchiveMedicationAsync(
        Guid userId,
        Guid medicationId,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        var medication = await _medications.GetByIdForUserAsync(userId, medicationId, cancellationToken);
        if (medication is null)
        {
            throw new NotFoundException("Medication was not found.");
        }

        await _medications.ArchiveAsync(userId, medicationId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync(
            "MEDICATION_ARCHIVED",
            "Medication",
            userId,
            medicationId,
            ipAddress,
            cancellationToken);
    }

    public async Task<List<MedicationSearchResultDto>> SearchMedicationsAsync(
        Guid userId,
        string query,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new InvalidRequestException("Search query is required.");
        }

        if (query.Trim().Length > 100)
        {
            throw new InvalidRequestException("Search query is too long.");
        }

        var results = await _fdaMedicationService.SearchMedicationAsync(query.Trim(), cancellationToken);
        await _auditService.LogAsync(
            "MEDICATION_SEARCHED",
            "Medication",
            userId,
            null,
            ipAddress,
            cancellationToken);

        return results;
    }

    private static MedicationResponseDto MapToDto(Medication medication) =>
        new()
        {
            Id = medication.Id,
            Name = medication.Name,
            GenericName = medication.GenericName,
            BrandName = medication.BrandName,
            DosageAmount = medication.DosageAmount,
            DosageUnit = medication.DosageUnit,
            Form = medication.Form,
            Instructions = medication.Instructions,
            IsArchived = medication.IsArchived
        };

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

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
