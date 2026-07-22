using CapTap.Application.DTOs.Medication;

namespace CapTap.Application.Interfaces;

public interface IMedicationService
{
    Task<List<MedicationResponseDto>> GetUserMedicationsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<MedicationResponseDto> GetMedicationAsync(
        Guid userId,
        Guid medicationId,
        CancellationToken cancellationToken = default);

    Task<MedicationResponseDto> CreateMedicationAsync(
        Guid userId,
        CreateMedicationRequest request,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    Task<MedicationResponseDto> UpdateMedicationAsync(
        Guid userId,
        Guid medicationId,
        UpdateMedicationRequest request,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    Task ArchiveMedicationAsync(
        Guid userId,
        Guid medicationId,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    Task<List<MedicationSearchResultDto>> SearchMedicationsAsync(
        Guid userId,
        string query,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);
}
