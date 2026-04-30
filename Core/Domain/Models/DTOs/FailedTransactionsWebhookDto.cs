using System.Text.Json;
using System.Text.Json.Serialization;

namespace SPSS.Domain.Models.DTOs;

public sealed class FailedTransactionsWebhookDto
{
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; }

    [JsonPropertyName("from_date")]
    public DateTimeOffset FromDate { get; set; }

    [JsonPropertyName("to_date")]
    public DateTimeOffset ToDate { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = default!;

    [JsonPropertyName("status")]
    public string Status { get; set; } = default!;

    [JsonPropertyName("total_count")]
    public int TotalCount { get; set; }

    [JsonPropertyName("transactions")]
    public List<FailedTransactionDto> Transactions { get; set; } = new();
}

public sealed class FailedTransactionDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("rule_ids")]
    public List<string> RuleIds { get; set; } = new();

    [JsonPropertyName("bankFrom")]
    public string? BankFrom { get; set; }

    [JsonPropertyName("bankFromName")]
    public string? BankFromName { get; set; }

    [JsonPropertyName("bankToName")]
    public string? BankToName { get; set; }

    [JsonPropertyName("bankTo")]
    public string? BankTo { get; set; }

    [JsonPropertyName("transaction")]
    public JsonElement? Transaction { get; set; }

    [JsonPropertyName("createdDate")]
    public DateTimeOffset? CreatedDate { get; set; }

    [JsonPropertyName("modifiedDate")]
    public DateTimeOffset? ModifiedDate { get; set; }

    [JsonPropertyName("lastModifiedDate")]
    public DateTimeOffset? LastModifiedDate { get; set; }

    [JsonPropertyName("acceptanceDate")]
    public DateTimeOffset? AcceptanceDate { get; set; }

    [JsonPropertyName("txId")]
    public string? TxId { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("amount")]
    public string? Amount { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("bankFromSaPosition")]
    public string? BankFromSaPosition { get; set; }

    [JsonPropertyName("bankToSaPosition")]
    public string? BankToSaPosition { get; set; }

    [JsonPropertyName("bankFromWaPosition")]
    public string? BankFromWaPosition { get; set; }

    [JsonPropertyName("bankToWaPosition")]
    public string? BankToWaPosition { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("sttlId")]
    public long? SttlId { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("intrBkSttlmCurr")]
    public string? IntrBkSttlmCurr { get; set; }

    [JsonPropertyName("intrBkSttlmAtm")]
    public string? IntrBkSttlmAtm { get; set; }

    [JsonPropertyName("dbtrAcctId")]
    public string? DbtrAcctId { get; set; }

    [JsonPropertyName("dbtrAcctType")]
    public string? DbtrAcctType { get; set; }

    [JsonPropertyName("dbtrName")]
    public string? DbtrName { get; set; }

    [JsonPropertyName("dbtrAddress")]
    public string? DbtrAddress { get; set; }

    [JsonPropertyName("cdtrName")]
    public string? CdtrName { get; set; }

    [JsonPropertyName("cdtrAddress")]
    public string? CdtrAddress { get; set; }

    [JsonPropertyName("cdtrAcctId")]
    public string? CdtrAcctId { get; set; }

    [JsonPropertyName("requestId")]
    public string? RequestId { get; set; }

    [JsonPropertyName("transCode")]
    public string? TransCode { get; set; }

    [JsonPropertyName("oldBalance")]
    public string? OldBalance { get; set; }

    [JsonPropertyName("newBalance")]
    public string? NewBalance { get; set; }

    [JsonPropertyName("aliasFrom")]
    public string? AliasFrom { get; set; }

    [JsonPropertyName("aliasTo")]
    public string? AliasTo { get; set; }

    [JsonPropertyName("aliasTypeFrom")]
    public string? AliasTypeFrom { get; set; }

    [JsonPropertyName("aliasTypeTo")]
    public string? AliasTypeTo { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("errorReason")]
    public string? ErrorReason { get; set; }

    [JsonPropertyName("transactions")]
    public List<JsonElement> Transactions { get; set; } = new();

    [JsonPropertyName("messages")]
    public List<JsonElement> Messages { get; set; } = new();
}
