namespace SPSS.Domain.Models.DTOs;

public sealed class BalanceWebhookDto
{
    public string Bic { get; set; } = default!;
    public decimal RemainingNetDebitCap { get; set; }
    public string Currency { get; set; } = default!;
    public DateTimeOffset Timestamp { get; set; }
}
