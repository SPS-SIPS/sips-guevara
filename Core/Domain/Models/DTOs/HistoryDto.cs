using SPSS.Domain.Models.BL;

namespace SPSS.Domain.Models.DTOs;

public sealed class HistoryDto : IMap<MonitHistory>
{
    public int Id { get; set; }
    public DateTimeOffset Date { get; set; }
    public Lookup<string> Participant { get; set; } = null!;
    public Lookup<int> Type { get; set; } = null!;
    public Lookup<int> Status { get; set; } = null!;
    public string Address { get; set; } = null!;
    public long RoundtripTime { get; set; }
    public int PacketLoss { get; set; }
    public bool Sent { get; set; }
    public void Mapping(Profile profile)
    {
        profile.CreateMap<MonitHistory, HistoryDto>()
            .ForMember(d => d.Participant, o => o.MapFrom(s => s.Participant))
            .ForMember(d => d.Type, o => o.MapFrom(s => s.Type.ToLookup))
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToLookup));

    }
}