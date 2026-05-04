namespace SPSS.Domain.Exceptions;
public sealed class DuplicateReferenceException(string message) : Exception(message)
{
}
