using System.Text.Json.Serialization;

namespace SPSS.Domain.Models.DTOs;

public sealed class OracleFraudTransactionsWebhookDto
{
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; }

    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    [JsonPropertyName("from_utrnno")]
    public long FromUtrnno { get; set; }

    [JsonPropertyName("to_utrnno")]
    public long ToUtrnno { get; set; }

    [JsonPropertyName("high_watermark_utrnno")]
    public long HighWatermarkUtrnno { get; set; }

    [JsonPropertyName("total_count")]
    public int TotalCount { get; set; }

    [JsonPropertyName("transactions")]
    public List<OracleFraudTransactionWebhookRowDto> Transactions { get; set; } = [];
}

public sealed class OracleFraudTransactionWebhookRowDto
{
    [JsonPropertyName("utrnno")]
    public long Utrnno { get; set; }

    [JsonPropertyName("respCode")]
    public string? RespCode { get; set; }

    [JsonPropertyName("fraudZone")]
    public string? FraudZone { get; set; }

    [JsonPropertyName("rulesList")]
    public string? RulesList { get; set; }

    [JsonPropertyName("refnum")]
    public string? Refnum { get; set; }

    [JsonPropertyName("riskValue")]
    public string? RiskValue { get; set; }
}

public sealed class OracleFraudTransactionBatchResponseDto
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("batch_key")]
    public string BatchKey { get; set; } = string.Empty;

    [JsonPropertyName("received")]
    public int Received { get; set; }

    [JsonPropertyName("duplicates")]
    public int Duplicates { get; set; }

    [JsonPropertyName("inserted")]
    public int Inserted { get; set; }

    [JsonPropertyName("updated")]
    public int Updated { get; set; }
}
