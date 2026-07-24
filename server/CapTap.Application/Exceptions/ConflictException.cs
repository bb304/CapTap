using CapTap.Shared.Constants;

namespace CapTap.Application.Exceptions;

public sealed class ConflictException : ApplicationException
{
    public ConflictException(string message)
        : base(ErrorCodes.Conflict, message)
    {
    }
}
