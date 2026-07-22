using CapTap.Shared.Constants;

namespace CapTap.Application.Exceptions;

public sealed class ValidationException : ApplicationException
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(IDictionary<string, string[]> errors)
        : base(ErrorCodes.ValidationError, "One or more validation errors occurred.")
    {
        Errors = errors;
    }

    public ValidationException(string message)
        : base(ErrorCodes.ValidationError, message)
    {
        Errors = new Dictionary<string, string[]>();
    }
}
