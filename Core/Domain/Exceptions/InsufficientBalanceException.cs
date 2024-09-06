namespace SPSS.Domain.Exceptions;

public sealed class InsufficientBalanceException(decimal avb, decimal bill) : Exception($"Insufficient Balance. Available Balance: {avb}, Bill Balance: {bill}")
{
}
