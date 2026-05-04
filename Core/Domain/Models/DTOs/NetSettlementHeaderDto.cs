using SPSS.Domain.Models.BL;

namespace SPSS.Domain.Models.DTOs;

public sealed class NetSettlementHeaderDto : IMap<NetSettlementHeader>
{
    public long Id { get; set; }
    public string MsgId { get; set; } = string.Empty;
    public string OriginalName { get; set; } = string.Empty;
    public DateTimeOffset CredDtTm { get; set; }
    public int NumberOfTrans { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalFees { get; set; }
    public decimal TotalTransactions { get; set; }
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string OriginalUserReference { get; set; } = string.Empty;
    public string BizMsgId { get; set; } = string.Empty;
    public bool Processed { get; set; } = false;
    public DateTimeOffset? ProcessedOn { get; set; }
    public int Count { get; set; } = 0;
    public string? Error { get; set; }
    public void Mapping(Profile profile)
    {
        profile.CreateMap<NetSettlementHeader, NetSettlementHeaderDto>();
    }
}