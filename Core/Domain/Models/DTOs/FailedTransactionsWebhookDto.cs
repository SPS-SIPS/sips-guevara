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

    [JsonPropertyName("bankFrom")]
    public string? BankFrom { get; set; }

    [JsonPropertyName("bankFromName")]
    public string? BankFromName { get; set; }

    [JsonPropertyName("bankToName")]
    public string? BankToName { get; set; }

    [JsonPropertyName("bankTo")]
    public string? BankTo { get; set; }

    [JsonPropertyName("createdDate")]
    public DateTimeOffset? CreatedDate { get; set; }

    [JsonPropertyName("modifiedDate")]
    public DateTimeOffset? ModifiedDate { get; set; }

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

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("sttlId")]
    public long? SttlId { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

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

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("errorReason")]
    public string? ErrorReason { get; set; }
}

public sealed class FailedTransactionRecordDto
{
    public long Id { get; set; }
    public long IpsTransactionId { get; set; }
    public DateTimeOffset SnapshotTimestamp { get; set; }
    public DateTimeOffset SnapshotFromDate { get; set; }
    public DateTimeOffset SnapshotToDate { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }
    public List<string> RuleIds { get; set; } = [];
    public string? BankFrom { get; set; }
    public string? BankFromName { get; set; }
    public string? BankTo { get; set; }
    public string? BankToName { get; set; }
    public DateTimeOffset? CreatedDate { get; set; }
    public DateTimeOffset? ModifiedDate { get; set; }
    public DateTimeOffset? AcceptanceDate { get; set; }
    public string? TxId { get; set; }
    public string? Type { get; set; }
    public string? Amount { get; set; }
    public string? Status { get; set; }
    public string? Description { get; set; }
    public long? SttlId { get; set; }
    public string? Currency { get; set; }
    public string? IntrBkSttlmAtm { get; set; }
    public string? DbtrAcctId { get; set; }
    public string? DbtrAcctType { get; set; }
    public string? DbtrName { get; set; }
    public string? DbtrAddress { get; set; }
    public string? CdtrAcctId { get; set; }
    public string? CdtrName { get; set; }
    public string? CdtrAddress { get; set; }
    public string? RequestId { get; set; }
    public string? TransCode { get; set; }
    public string? Error { get; set; }
    public string? ErrorReason { get; set; }
}

public sealed class FailedTransactionRuleIdsCsvUploadResultDto
{
    public int TotalRows { get; set; }
    public int UpdatedCount { get; set; }
    public int NotFoundCount { get; set; }
    public int InvalidRowCount { get; set; }
    public List<long> UpdatedIpsTransactionIds { get; set; } = [];
    public List<long> MissingIpsTransactionIds { get; set; } = [];
    public List<string> Errors { get; set; } = [];
}
