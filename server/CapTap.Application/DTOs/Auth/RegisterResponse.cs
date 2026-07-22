namespace CapTap.Application.DTOs.Auth;

public sealed class RegisterResponse
{
    public Guid UserId { get; set; }

    public string Message { get; set; } = "Verification email sent";
}
