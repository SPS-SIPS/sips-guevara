namespace SPSS.Domain.Models.DTOs;

public sealed class ExtendedRoleDto : IMap<Role>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Super { get; set; }
    public List<ClaimDto> Claims { get; set; } = null!;
    public void Mapping(Profile profile)
    {
        profile.CreateMap<Role, ExtendedRoleDto>()
            .ForMember(d => d.Claims, o => o.MapFrom(s => s.RoleClaims));
    }
}

