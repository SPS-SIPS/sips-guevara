namespace SPSS.Domain.Models.DTOs;

public sealed class TransactionRecordDto
{
    public string Source { get; set; } = string.Empty;
    public long Id { get; set; }
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
    public DateTimeOffset? ReceivedAt { get; set; }
}
