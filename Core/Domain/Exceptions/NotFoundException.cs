namespace SPSS.Domain.Exceptions;
public sealed class NotFoundException : Exception
{
    public NotFoundException(string name, object key)
        : base($"Entity \"{name}\" ({key}) was not found.")
    {

    }
    public NotFoundException(string name, object key, object key2)
        : base($"Entity \"{name}\" ({key}-{key2}) was not found.")
    {

    }
    public NotFoundException(string name, object key, object key2, object key3)
        : base($"Entity \"{name}\" ({key}-{key2}-{key3}) was not found.")
    {

    }

    public NotFoundException(string message)
        : base(message)
    {
    }
}
