namespace CapTap.Application.DTOs.Medication;

public sealed class MedicationSearchResultDto
{
    public string Name { get; set; } = string.Empty;

    public string? Brand { get; set; }

    public string? Identifier { get; set; }
}
