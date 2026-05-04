namespace SPSS.Domain.Models.DTOs;

public sealed class UserDto : IMap<User>, IAuditableEntity
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Photo { get; set; } = string.Empty;
    public bool Active { get; set; }
    public string PIN { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset CreatedOn { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTimeOffset? LastModifiedOn { get; set; }
    public DateTimeOffset? DeletedOn { get; set; }
    public string? DeletedBy { get; set; }
    public Guid[] Roles { get; set; } = [];
    public string[] RoleNames { get; set; } = [];

    public void Mapping(Profile profile)
    {
        profile.CreateMap<User, UserDto>()
            .ForMember(d => d.Roles, o => o.MapFrom(s => s.Roles.Select(x => x.Role.Id).ToArray()))
            .ForMember(d => d.RoleNames, o => o.MapFrom(s => s.Roles.Select(x => x.Role.Name).ToArray()));

        profile.CreateMap<User, Lookup<int>>()
            .ForMember(d => d.Value, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.Label, o => o.MapFrom(s => s.Username))
            .ForMember(d => d.GroupId, o => o.Ignore());

        profile.CreateMap<UserRole, UserDto>()
        .ForMember(d => d.Roles, o => o.Ignore())
        .ForMember(d => d.Id, o => o.MapFrom(s => s.User.Id))
        .ForMember(d => d.Username, o => o.MapFrom(s => s.User.Username))
        .ForMember(d => d.Phone, o => o.MapFrom(s => s.User.Phone))
        .ForMember(d => d.Email, o => o.MapFrom(s => s.User.Email))
        .ForMember(d => d.CreatedBy, o => o.MapFrom(s => s.User.CreatedBy))
        .ForMember(d => d.CreatedOn, o => o.MapFrom(s => s.User.CreatedOn))
        .ForMember(d => d.LastModifiedBy, o => o.MapFrom(s => s.User.LastModifiedBy))
        .ForMember(d => d.LastModifiedOn, o => o.MapFrom(s => s.User.LastModifiedOn));
    }
}

