using SPSS.Domain.Models.BL;

namespace SPSS.Domain.Models.DTOs;

public sealed class EndpointDto : IMap<Endpoint>
{
    public int Id { get; set; }
    public DateTimeOffset Date { get; set; }
    public string Name { get; set; } = default!;
    public string Address { get; set; } = default!;
    public Lookup<string> Participant { get; set; } = null!;
    public bool Active { get; set; }
    public void Mapping(Profile profile)
    {
        profile.CreateMap<Endpoint, EndpointDto>()
            .ForMember(d => d.Participant, o => o.MapFrom(s => s.Participant));

        profile.CreateMap<Endpoint, Lookup<int>>()
            .ForMember(d => d.Value, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.Label, o => o.MapFrom(s => s.Name))
            .ForMember(d => d.GroupId, o => o.MapFrom(s => s.ParticipantId));
    }
}
