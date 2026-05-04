namespace SPSS.Domain.Models.DTOs;

public sealed class RoleDto : IMap<Role>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string[] Claims { get; set; } = [];
    public string[] ClaimNames { get; set; } = [];

    public void Mapping(Profile profile)
    {
        profile.CreateMap<Role, RoleDto>()
            .ForMember(d => d.Claims, o => o.MapFrom(s => s.RoleClaims.Select(x => x.Type).ToArray()))
            .ForMember(d => d.ClaimNames, o => o.MapFrom(s => s.RoleClaims.Select(x => x.Claim.Description).ToArray()));

        profile.CreateMap<Role, Lookup<Guid>>()
            .ForMember(d => d.Value, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.Label, o => o.MapFrom(s => s.Name))
            .ForMember(d => d.GroupId, o => o.Ignore());

        profile.CreateMap<UserRole, RoleDto>()
            .ForMember(d => d.Id, o => o.MapFrom(s => s.RoleId))
            .ForMember(d => d.Name, o => o.MapFrom(s => s.Role.Name));
    }
}