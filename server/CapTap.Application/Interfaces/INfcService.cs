using CapTap.Application.DTOs.Nfc;

namespace CapTap.Application.Interfaces;

public interface INfcService
{
    Task<NfcTagResponseDto> AssignAsync(
        Guid userId,
        AssignNfcTagRequest request,
        string? ipAddress = null,
        string? deviceId = null,
        CancellationToken cancellationToken = default);

    Task<NfcTagResponseDto> UnassignAsync(
        Guid userId,
        UnassignNfcTagRequest request,
        string? ipAddress = null,
        string? deviceId = null,
        CancellationToken cancellationToken = default);

    Task<NfcResolveResponseDto> ResolveAsync(
        Guid userId,
        string tagIdentifier,
        string? ipAddress = null,
        string? deviceId = null,
        CancellationToken cancellationToken = default);

    Task<List<NfcTagResponseDto>> ListAssignedAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
