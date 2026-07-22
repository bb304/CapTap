using CapTap.Shared.Constants;

namespace CapTap.Application.Exceptions;

public sealed class InvalidRequestException : ApplicationException
{
    public InvalidRequestException(string message)
        : base(ErrorCodes.InvalidRequest, message)
    {
    }
}
