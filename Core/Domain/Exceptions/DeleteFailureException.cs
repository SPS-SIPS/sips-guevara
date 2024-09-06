namespace SPSS.Domain.Exceptions;
public sealed class DeleteFailureException(string name, object key, string message) : Exception($"Deletion of entity \"{name}\" ({key}) failed. {message}")
{
}

