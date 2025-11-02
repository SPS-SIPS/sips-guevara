namespace SPSS.Domain.Interfaces;

public interface INSIEmailNotificationService
{
    Task NotifyAsync(long netSettlementHeaderId, CancellationToken cancellationToken = default);
}
