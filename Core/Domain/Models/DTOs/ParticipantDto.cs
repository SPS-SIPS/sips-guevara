using SPSS.Domain.Models.BL;

namespace SPSS.Domain.Models.DTOs;

public sealed class ParticipantDto : IMap<Participant>
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = default!;
    public Lookup<int> User { get; set; } = null!;
    public void Mapping(Profile profile)
    {
        profile.CreateMap<Participant, ParticipantDto>()
            .ForMember(d => d.User, o => o.MapFrom(s => s.User));

        profile.CreateMap<Participant, Lookup<string>>()
            .ForMember(d => d.Value, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.Label, o => o.MapFrom(s => s.Name))
            .ForMember(d => d.GroupId, o => o.MapFrom(s => s.UserId));
    }
}