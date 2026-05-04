namespace SPSS.Domain.Models.DTOs;

public sealed partial class ClaimDto : IMap<Claim>
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public void Mapping(Profile profile)
    {
        profile.CreateMap<Claim, ClaimDto>();

        profile.CreateMap<Claim, Lookup<string>>()
            .ForMember(d => d.Value, o => o.MapFrom(s => s.Type))
            .ForMember(d => d.Label, o => o.MapFrom(s => s.Description))
            .ForMember(d => d.GroupId, o => o.Ignore());

        profile.CreateMap<RoleClaim, ClaimDto>()
             .ForMember(d => d.Description, o => o.MapFrom(s => s.Claim.Description))
            .ForMember(d => d.Type, o => o.MapFrom(s => s.Type));
    }
}

