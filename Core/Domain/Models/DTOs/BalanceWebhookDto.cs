using System.Text.Json.Serialization;

namespace SPSS.Domain.Models.DTOs;

public sealed class BalanceWebhookDto
{
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; }
    
    [JsonPropertyName("total_count")]
    public int TotalCount { get; set; }
    
    [JsonPropertyName("participants")]
    public List<ParticipantBalanceDto> Participants { get; set; } = new();
}

public sealed class ParticipantBalanceDto
{
    [JsonPropertyName("account_number")]
    public string AccountNumber { get; set; } = default!;
    
    [JsonPropertyName("bic")]
    public string Bic { get; set; } = default!;
    
    [JsonPropertyName("participant_name")]
    public string ParticipantName { get; set; } = default!;
    
    [JsonPropertyName("remaining_net_debit_cap")]
    public decimal RemainingNetDebitCap { get; set; }
    
    [JsonPropertyName("raw_value")]
    public string RawValue { get; set; } = default!;
    
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = default!;
    
    [JsonPropertyName("state")]
    public string State { get; set; } = default!;
    
    [JsonPropertyName("status")]
    public string Status { get; set; } = default!;
    
    [JsonPropertyName("inst_id")]
    public int InstId { get; set; }
    
    [JsonPropertyName("checked_at")]
    public DateTimeOffset CheckedAt { get; set; }
}
