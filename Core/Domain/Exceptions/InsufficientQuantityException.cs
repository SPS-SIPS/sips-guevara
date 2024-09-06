namespace SPSS.Domain.Exceptions;

public sealed class InsufficientQuantityException(decimal avbQTY, decimal OrderQTY) : Exception($"Insufficient Quantity. Available Quantity: {avbQTY}, Order Quantity: {OrderQTY}")
{
}
