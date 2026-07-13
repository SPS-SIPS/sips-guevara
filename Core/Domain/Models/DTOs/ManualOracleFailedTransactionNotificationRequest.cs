namespace SPSS.Domain.Models.DTOs;

public sealed class ManualOracleFailedTransactionNotificationRequest
{
    public string Bic { get; set; } = string.Empty;
    public DateTimeOffset From { get; set; }
    public DateTimeOffset To { get; set; }
}
