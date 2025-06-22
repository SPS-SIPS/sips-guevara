using SPSS.Domain.Models.BL;

namespace SPSS.Domain.Models.DTOs;

public class BicStatisticsDto : IMap<BicStatistic>
{
    public long NetSettlementHeaderId { get; set; }
    public DateTimeOffset Date { get; set; }
    public string Bic { get; set; } = string.Empty;
    public long SentCount { get; set; }
    public decimal TotalAmountSent { get; set; }
    public long ReceivedCount { get; set; }
    public decimal TotalAmountReceived { get; set; }
    public decimal TotalFeesReceived { get; set; }
    public List<BicInteractionDto> TopReceivers { get; set; } = [];
    public List<BicInteractionDto> TopSenders { get; set; } = [];

    public void Mapping(Profile profile)
    {
        profile.CreateMap<BicStatistic, BicStatisticsDto>()
            .ForMember(d => d.TopReceivers, o => o.MapFrom(s => s.TopReceivers))
            .ForMember(d => d.TopSenders, o => o.MapFrom(s => s.TopSenders));
    }
}
