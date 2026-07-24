namespace CapTap.Application.DTOs.Nfc;

public sealed class NfcTagResponseDto
{
    public Guid Id { get; set; }

    public Guid MedicationId { get; set; }

    public string MedicationName { get; set; } = string.Empty;

    public string TagIdentifier { get; set; } = string.Empty;

    public bool IsAssigned { get; set; }

    public DateTime AssignedAt { get; set; }

    public DateTime? LastScannedAt { get; set; }
}
