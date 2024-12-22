using SPSS.Domain.Models.BL;

namespace SPSS.Domain.Models.DTOs;

public sealed class TunnelDto : IMap<Tunnel>
{
    public int Id { get; set; }
    public DateTimeOffset Date { get; set; }
    public string Endpoint { get; set; } = default!;
    public string Name { get; set; } = default!;
    public Lookup<int> VPN { get; set; } = null!;
    public string ParticipantId { get; set; } = default!;
    public bool Active { get; set; }
    public void Mapping(Profile profile)
    {
        profile.CreateMap<Tunnel, TunnelDto>()
            .ForMember(d => d.ParticipantId, o => o.MapFrom(s => s.VPN.ParticipantId))
            .ForMember(d => d.VPN, o => o.MapFrom(s => s.VPN));

        profile.CreateMap<Tunnel, Lookup<int>>()
            .ForMember(d => d.Value, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.Label, o => o.MapFrom(s => s.Name))
            .ForMember(d => d.GroupId, o => o.MapFrom(s => s.VPNId));
    }
}
