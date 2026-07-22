namespace CapTap.Domain.Exceptions;

public sealed class NotFoundException : DomainException
{
    public NotFoundException(string entityName, Guid id)
        : base("NOT_FOUND", $"{entityName} with id '{id}' was not found.")
    {
    }

    public NotFoundException(string message)
        : base("NOT_FOUND", message)
    {
    }
}
