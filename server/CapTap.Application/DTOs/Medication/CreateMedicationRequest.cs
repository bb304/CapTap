namespace CapTap.Application.DTOs.Medication;

public sealed class CreateMedicationRequest
{
    public string Name { get; set; } = string.Empty;

    public string? GenericName { get; set; }

    public string? BrandName { get; set; }

    public string? FdaIdentifier { get; set; }

    public decimal DosageAmount { get; set; }

    public string DosageUnit { get; set; } = string.Empty;

    public string? Form { get; set; }

    public string? Instructions { get; set; }
}
