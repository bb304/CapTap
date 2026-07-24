namespace CapTap.Application.DTOs.Users;

public sealed class UserProfileDto
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string TimeZoneId { get; set; } = "UTC";
}
