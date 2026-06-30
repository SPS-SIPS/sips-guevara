using System.Text.Json;
using System.Text.Json.Serialization;

namespace SPSS.Domain.Models.DTOs;

public sealed class OracleTransactionsWebhookDto
{
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; }

    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    [JsonPropertyName("from_id")]
    public long FromId { get; set; }

    [JsonPropertyName("to_id")]
    public long ToId { get; set; }

    [JsonPropertyName("high_watermark_id")]
    public long HighWatermarkId { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("statuses")]
    public List<string>? Statuses { get; set; }

    [JsonPropertyName("total_count")]
    public int TotalCount { get; set; }

    [JsonPropertyName("transactions")]
    public List<OracleTransactionWebhookRowDto> Transactions { get; set; } = [];
}

public sealed class OracleTransactionWebhookRowDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("bankFrom")]
    public string? BankFrom { get; set; }

    [JsonPropertyName("bankTo")]
    public string? BankTo { get; set; }

    [JsonPropertyName("createdDate")]
    public string? CreatedDate { get; set; }

    [JsonPropertyName("modifiedDate")]
    public string? ModifiedDate { get; set; }

    [JsonPropertyName("parentId")]
    public long? ParentId { get; set; }

    [JsonPropertyName("lastModifiedDate")]
    public string? LastModifiedDate { get; set; }

    [JsonPropertyName("acceptanceDate")]
    public string? AcceptanceDate { get; set; }

    [JsonPropertyName("txId")]
    public string? TxId { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("typeDescription")]
    public string? TypeDescription { get; set; }

    [JsonPropertyName("amount")]
    public string? Amount { get; set; }

    [JsonPropertyName("amountValue")]
    public decimal? AmountValue { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("txSts")]
    public string? TxSts { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("intrBkSttlmAtm")]
    public string? IntrBkSttlmAtm { get; set; }

    [JsonPropertyName("intrBkSttlmAtmValue")]
    public decimal? IntrBkSttlmAtmValue { get; set; }

    [JsonPropertyName("intrBkSttlmCurr")]
    public string? IntrBkSttlmCurr { get; set; }

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

    [JsonPropertyName("cdtrAcctType")]
    public string? CdtrAcctType { get; set; }

    [JsonPropertyName("sttlId")]
    public long? SttlId { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("errDesc")]
    public string? ErrDesc { get; set; }

    [JsonPropertyName("errorReason")]
    public string? ErrorReason { get; set; }

    [JsonPropertyName("requestId")]
    public string? RequestId { get; set; }

    [JsonPropertyName("transCode")]
    public string? TransCode { get; set; }

    [JsonPropertyName("lclInstrm")]
    public string? LclInstrm { get; set; }

    [JsonPropertyName("ctgyPurp")]
    public string? CtgyPurp { get; set; }

    [JsonPropertyName("rtrId")]
    public string? RtrId { get; set; }

    [JsonPropertyName("oldBalance")]
    public string? OldBalance { get; set; }

    [JsonPropertyName("newBalance")]
    public string? NewBalance { get; set; }

    [JsonPropertyName("bankFromSaPosition")]
    public string? BankFromSaPosition { get; set; }

    [JsonPropertyName("bankToSaPosition")]
    public string? BankToSaPosition { get; set; }

    [JsonPropertyName("bankFromWaPosition")]
    public string? BankFromWaPosition { get; set; }

    [JsonPropertyName("bankToWaPosition")]
    public string? BankToWaPosition { get; set; }

    [JsonPropertyName("endToEndId")]
    public string? EndToEndId { get; set; }

    [JsonPropertyName("iban")]
    public string? Iban { get; set; }

    [JsonPropertyName("rtpId")]
    public string? RtpId { get; set; }

    [JsonPropertyName("attributes")]
    public string? Attributes { get; set; }

    [JsonPropertyName("orgTrxId")]
    public string? OrgTrxId { get; set; }

    [JsonPropertyName("stsRsnInfRsn")]
    public string? StsRsnInfRsn { get; set; }

    [JsonPropertyName("stsRsnInfAddtlInf")]
    public string? StsRsnInfAddtlInf { get; set; }

    [JsonPropertyName("mandateReqId")]
    public string? MandateReqId { get; set; }

    [JsonPropertyName("transaction")]
    public JsonElement? Transaction { get; set; }

    [JsonPropertyName("transactions")]
    public JsonElement? Transactions { get; set; }

    [JsonPropertyName("messages")]
    public JsonElement? Messages { get; set; }
}

public sealed class OracleTransactionBatchResponseDto
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
