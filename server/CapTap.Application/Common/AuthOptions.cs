namespace CapTap.Application.Common;

/// <summary>
/// Auth-related options mirrored from JwtSettings for the Application layer.
/// </summary>
public sealed class AuthOptions
{
    public const string SectionName = "JwtSettings";

    public int AccessTokenExpirationMinutes { get; set; } = 15;

    public int RefreshTokenExpirationDays { get; set; } = 30;
}
