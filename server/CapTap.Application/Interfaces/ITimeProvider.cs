namespace CapTap.Application.Interfaces;

public interface ITimeProvider
{
    DateTime UtcNow { get; }
}
