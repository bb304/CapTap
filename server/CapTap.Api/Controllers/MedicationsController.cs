using CapTap.Application.DTOs.Medication;
using CapTap.Application.Interfaces;
using CapTap.Shared.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CapTap.Api.Controllers;

/// <summary>
/// Personal medication list. Every action is scoped to the authenticated user.
/// CapTap stores user-entered medication data; it does not provide medical advice.
/// </summary>
[Route("api/v1/medications")]
public sealed class MedicationsController : AuthorizedApiControllerBase
{
    private readonly IMedicationService _medicationService;

    public MedicationsController(ICurrentUserService currentUser, IMedicationService medicationService)
        : base(currentUser)
    {
        _medicationService = medicationService;
    }

    /// <summary>List the current user's active (non-archived) medications.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<MedicationResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<List<MedicationResponseDto>>>> GetMedications(
        CancellationToken cancellationToken)
    {
        var medications = await _medicationService.GetUserMedicationsAsync(CurrentUserId, cancellationToken);
        return Success(medications);
    }

    /// <summary>Search OpenFDA drug labels by name (lookup aid only; user confirms final details).</summary>
    [HttpGet("search")]
    [EnableRateLimiting("medication-search")]
    [ProducesResponseType(typeof(ApiResponse<List<MedicationSearchResultDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<ApiResponse<List<MedicationSearchResultDto>>>> Search(
        [FromQuery(Name = "q")] string q,
        CancellationToken cancellationToken)
    {
        var results = await _medicationService.SearchMedicationsAsync(
            CurrentUserId,
            q,
            GetClientIp(),
            cancellationToken);

        return Success(results);
    }

    /// <summary>Get a single medication owned by the current user.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<MedicationResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<MedicationResponseDto>>> GetMedication(
        Guid id,
        CancellationToken cancellationToken)
    {
        var medication = await _medicationService.GetMedicationAsync(CurrentUserId, id, cancellationToken);
        return Success(medication);
    }

    /// <summary>Add a medication to the current user's list.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<MedicationResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<MedicationResponseDto>>> CreateMedication(
        [FromBody] CreateMedicationRequest request,
        CancellationToken cancellationToken)
    {
        var medication = await _medicationService.CreateMedicationAsync(
            CurrentUserId,
            request,
            GetClientIp(),
            cancellationToken);

        return Success(medication);
    }

    /// <summary>Update dosage or medication details for an owned medication.</summary>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<MedicationResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<MedicationResponseDto>>> UpdateMedication(
        Guid id,
        [FromBody] UpdateMedicationRequest request,
        CancellationToken cancellationToken)
    {
        var medication = await _medicationService.UpdateMedicationAsync(
            CurrentUserId,
            id,
            request,
            GetClientIp(),
            cancellationToken);

        return Success(medication);
    }

    /// <summary>Soft-archive a medication (does not hard-delete).</summary>
    [HttpPost("{id:guid}/archive")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> ArchiveMedication(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _medicationService.ArchiveMedicationAsync(CurrentUserId, id, GetClientIp(), cancellationToken);
        return Ok(ApiResponse.Ok(message: "Medication archived."));
    }

    private string? GetClientIp() =>
        HttpContext.Connection.RemoteIpAddress?.ToString();
}
