namespace CapTap.Application.DTOs.Nfc;

public sealed class AssignNfcTagRequest
{
    public Guid MedicationId { get; set; }

    public string TagIdentifier { get; set; } = string.Empty;
}
