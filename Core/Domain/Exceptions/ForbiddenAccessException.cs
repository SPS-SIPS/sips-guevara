namespace SPSS.Domain.Exceptions;

public sealed class ForbiddenAccessException(string msg = "You are not authorized to access resource") : Exception(msg)
{
}
