using SPSS.Domain.Models.BL;

namespace SPSS.Domain.Models.DTOs;

public class BicInteractionDto : IMap<BicInteraction>
{
    public string Bic { get; set; } = string.Empty;
    public long TransactionCount { get; set; }
    public decimal TotalAmount { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<BicInteraction, BicInteractionDto>()
            .ForMember(d => d.Bic, o => o.MapFrom(s => s.Bic))
            .ForMember(d => d.TransactionCount, o => o.MapFrom(s => s.TransactionCount))
            .ForMember(d => d.TotalAmount, o => o.MapFrom(s => s.TotalAmount));
    }
}
