using CapTap.Application.Interfaces;

namespace CapTap.Application.Services;

public sealed class SystemTimeProvider : ITimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
