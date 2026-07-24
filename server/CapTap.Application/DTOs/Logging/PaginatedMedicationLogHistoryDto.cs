namespace CapTap.Application.DTOs.Logging;

public sealed class PaginatedMedicationLogHistoryDto
{
    public List<MedicationLogHistoryItemDto> Items { get; set; } = [];

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }

    public int TotalPages { get; set; }
}
