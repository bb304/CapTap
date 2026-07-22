using CapTap.Application.DTOs.Medication;

namespace CapTap.Application.Interfaces;

public interface IFdaMedicationService
{
    Task<List<MedicationSearchResultDto>> SearchMedicationAsync(
        string query,
        CancellationToken cancellationToken = default);
}
