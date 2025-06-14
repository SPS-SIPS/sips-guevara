using SPSS.Domain.Models.BL;

namespace SPSS.Domain.Models.DTOs;

public sealed class NetSettlementHeaderSimpleDto : IMap<NetSettlementHeader>
{
    public long Id { get; set; }
    public DateTimeOffset CredDtTm { get; set; }
    public void Mapping(Profile profile)
    {
        profile.CreateMap<NetSettlementHeader, NetSettlementHeaderSimpleDto>();
    }
}