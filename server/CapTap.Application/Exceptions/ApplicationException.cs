namespace CapTap.Application.Exceptions;

public abstract class ApplicationException : Exception
{
    public string Code { get; }

    protected ApplicationException(string code, string message)
        : base(message)
    {
        Code = code;
    }
}
