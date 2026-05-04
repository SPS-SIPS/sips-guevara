namespace SPSS.Domain.Models.DTOs;

public sealed class TokenDto : IMap<Token>
{
    public Guid Id { get; set; }
    public Lookup<int> Employee { get; set; } = null!;
    public Lookup<int> Branch { get; set; } = null!;
    public Lookup<int> Type { get; set; } = null!;
    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset ExpiresOn { get; set; }
    public bool Used { get; set; } = false;
    public bool Invalidated { get; set; } = false;
    public string IPAddress { get; set; } = null!;
    public string UserAgent { get; set; } = null!;
    public string OS { get; set; } = null!;

    public void Mapping(Profile profile)
    {
        profile.CreateMap<Token, TokenDto>();
    }
}

