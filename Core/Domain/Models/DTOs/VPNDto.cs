using SPSS.Domain.Models.BL;

namespace SPSS.Domain.Models.DTOs;

public sealed class VPNDto : IMap<VPN>
{
    public int Id { get; set; }
    public DateTimeOffset Date { get; set; }
    public string Gateway { get; set; } = default!;
    public Lookup<string> Participant { get; set; } = null!;
    public bool Active { get; set; }
    public List<TunnelDto> Tunnels { get; set; } = [];
    public void Mapping(Profile profile)
    {
        profile.CreateMap<VPN, VPNDto>()
            .ForMember(d => d.Participant, o => o.MapFrom(s => s.Participant))
            .ForMember(d => d.Tunnels, o => o.MapFrom(s => s.Tunnels));

        profile.CreateMap<VPN, Lookup<int>>()
            .ForMember(d => d.Value, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.Label, o => o.MapFrom(s => s.Gateway))
            .ForMember(d => d.GroupId, o => o.MapFrom(s => s.ParticipantId));
    }
}