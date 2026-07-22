namespace CapTap.Infrastructure.Configuration;

public sealed class EmailSettings
{
    public const string SectionName = "EmailSettings";

    /// <summary>
    /// "Smtp" for real delivery, "Mock" for local/dev logging only.
    /// Production requires Smtp.
    /// </summary>
    public string Provider { get; set; } = "Mock";

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 587;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string FromEmail { get; set; } = string.Empty;

    public string FromName { get; set; } = "CapTap";

    public bool UseSsl { get; set; } = true;

    /// <summary>
    /// Base URL used in verification/reset links (e.g. https://app.captap.example).
    /// </summary>
    public string AppBaseUrl { get; set; } = "http://localhost:8081";
}
