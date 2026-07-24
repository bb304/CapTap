using CapTap.Application.DTOs.Logging;
using CapTap.Domain.Enums;

namespace CapTap.Application.Interfaces;

public interface IMedicationLogService
{
    Task<MedicationLogResponseDto> LogDoseAsync(
        Guid userId,
        CreateMedicationLogRequest request,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    Task<PaginatedMedicationLogHistoryDto> GetHistoryAsync(
        Guid userId,
        int page,
        int pageSize,
        Guid? medicationId = null,
        CancellationToken cancellationToken = default);
}
