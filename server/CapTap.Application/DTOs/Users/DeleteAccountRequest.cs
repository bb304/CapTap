namespace CapTap.Application.DTOs.Users;

/// <summary>
/// Step-up confirmation for account soft-delete.
/// </summary>
public sealed class DeleteAccountRequest
{
    public string Password { get; set; } = string.Empty;
}
