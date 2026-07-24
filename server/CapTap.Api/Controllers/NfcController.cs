using CapTap.Application.DTOs.Nfc;
using CapTap.Application.Interfaces;
using CapTap.Shared.Responses;
using Microsoft.AspNetCore.Mvc;

namespace CapTap.Api.Controllers;

/// <summary>
/// NFC tag assignment and resolution. Logging still goes through POST /medication-logs.
/// </summary>
[Route("api/v1/nfc")]
public sealed class NfcController : AuthorizedApiControllerBase
{
    private readonly INfcService _nfcService;

    public NfcController(ICurrentUserService currentUser, INfcService nfcService)
        : base(currentUser)
    {
        _nfcService = nfcService;
    }

    [HttpGet("tags")]
    [ProducesResponseType(typeof(ApiResponse<List<NfcTagResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<List<NfcTagResponseDto>>>> ListTags(
        CancellationToken cancellationToken)
    {
        var tags = await _nfcService.ListAssignedAsync(CurrentUserId, cancellationToken);
        return Success(tags);
    }

    [HttpPost("assign")]
    [ProducesResponseType(typeof(ApiResponse<NfcTagResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<NfcTagResponseDto>>> Assign(
        [FromBody] AssignNfcTagRequest request,
        CancellationToken cancellationToken)
    {
        var tag = await _nfcService.AssignAsync(
            CurrentUserId,
            request,
            GetClientIp(),
            GetDeviceId(),
            cancellationToken);

        return Success(tag);
    }

    [HttpPost("unassign")]
    [ProducesResponseType(typeof(ApiResponse<NfcTagResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<NfcTagResponseDto>>> Unassign(
        [FromBody] UnassignNfcTagRequest request,
        CancellationToken cancellationToken)
    {
        var tag = await _nfcService.UnassignAsync(
            CurrentUserId,
            request,
            GetClientIp(),
            GetDeviceId(),
            cancellationToken);

        return Success(tag);
    }

    /// <summary>Resolve a scanned tag to today's medication dose summary (user-scoped).</summary>
    [HttpGet("{tagIdentifier}")]
    [ProducesResponseType(typeof(ApiResponse<NfcResolveResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<NfcResolveResponseDto>>> Resolve(
        string tagIdentifier,
        CancellationToken cancellationToken)
    {
        var resolved = await _nfcService.ResolveAsync(
            CurrentUserId,
            Uri.UnescapeDataString(tagIdentifier),
            GetClientIp(),
            GetDeviceId(),
            cancellationToken);

        return Success(resolved);
    }

    private string? GetClientIp() =>
        HttpContext.Connection.RemoteIpAddress?.ToString();

    private string? GetDeviceId() =>
        Request.Headers.TryGetValue("X-Device-Id", out var value)
            ? value.ToString()
            : null;
}
