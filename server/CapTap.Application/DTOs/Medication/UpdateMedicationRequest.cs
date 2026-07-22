namespace CapTap.Application.DTOs.Medication;

public sealed class UpdateMedicationRequest
{
    public string? Name { get; set; }

    public string? GenericName { get; set; }

    public string? BrandName { get; set; }

    public string? FdaIdentifier { get; set; }

    public decimal? DosageAmount { get; set; }

    public string? DosageUnit { get; set; }

    public string? Form { get; set; }

    public string? Instructions { get; set; }
}
