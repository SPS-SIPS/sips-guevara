namespace SPSS.Domain.Models.DTOs;

public sealed class OracleFailedTransactionNotificationResult
{
    public string Bic { get; set; } = string.Empty;
    public DateTimeOffset From { get; set; }
    public DateTimeOffset To { get; set; }
    public int SourceRows { get; set; }
    public int SentCount { get; set; }
    public int SkippedCount { get; set; }
    public bool Sent => SentCount > 0;
    public string Message { get; set; } = string.Empty;
}
