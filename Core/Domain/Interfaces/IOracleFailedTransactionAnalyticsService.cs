namespace SPSS.Domain.Interfaces;

public interface IOracleFailedTransactionAnalyticsService
{
    Task<long> GenerateAsync(CancellationToken cancellationToken = default);
}
