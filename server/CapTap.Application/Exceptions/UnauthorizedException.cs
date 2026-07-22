using CapTap.Shared.Constants;

namespace CapTap.Application.Exceptions;

public sealed class UnauthorizedException : ApplicationException
{
    public UnauthorizedException(string message)
        : base(ErrorCodes.Unauthorized, message)
    {
    }
}
