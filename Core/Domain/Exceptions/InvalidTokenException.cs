namespace SPSS.Domain.Exceptions;

public sealed class InvalidTokenException(string message) : Exception(message)
{
}
