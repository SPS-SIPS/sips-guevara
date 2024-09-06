namespace SPSS.Domain.Exceptions;
public sealed class OutOfQuitoException(string name) : Exception($"Entity \"{name}\" is exceeded the allowed Quota.")
{
}
