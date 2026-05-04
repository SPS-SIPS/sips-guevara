using SPSS.Domain.Models.BL;

namespace SPSS.Domain.Models.DTOs;

public sealed class NetSettlementSummaryDto : IMap<NetSettlementDetail>
{
    public string Dr { get; set; } = string.Empty;
    public string Cr { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public void Mapping(Profile profile)
    {
        profile.CreateMap<NetSettlementDetail, NetSettlementSummaryDto>()
            .ForMember(dest => dest.Dr, opt => opt.MapFrom(src => src.DebtorBic))
            .ForMember(dest => dest.Cr, opt => opt.MapFrom(src => src.CreditorBic))
            .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.Amount));
    }
}