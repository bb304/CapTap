namespace CapTap.Infrastructure.Configuration;

public sealed class ApplicationSettings
{
    public const string SectionName = "ApplicationSettings";

    public string AppName { get; set; } = "CapTap";

    public string ApiVersion { get; set; } = "v1";
}
