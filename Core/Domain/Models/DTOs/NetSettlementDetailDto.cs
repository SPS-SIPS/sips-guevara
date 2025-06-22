using SPSS.Domain.Models.BL;

namespace SPSS.Domain.Models.DTOs;

public sealed class NetSettlementDetailDto : IMap<NetSettlementDetail>
{
    public string EndToEndId { get; set; } = string.Empty;
    public string TxId { get; set; } = string.Empty;
    public DateTimeOffset Date { get; set; }
    public decimal Amount { get; set; }
    public string DebtorBic { get; set; } = string.Empty;
    public string CreditorBic { get; set; } = string.Empty;
    public bool IsFee { get; set; }
    public void Mapping(Profile profile)
    {
        profile.CreateMap<NetSettlementDetail, NetSettlementDetailDto>();
    }
}
