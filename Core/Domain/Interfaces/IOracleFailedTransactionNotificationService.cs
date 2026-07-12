namespace SPSS.Domain.Interfaces;

public interface IOracleFailedTransactionNotificationService
{
    Task NotifyNextWindowAsync(DateTimeOffset now, CancellationToken cancellationToken = default);
    Task NotifyAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);
}
