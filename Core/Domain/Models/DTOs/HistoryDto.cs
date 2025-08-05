using SPSS.Domain.Models.BL;

namespace SPSS.Domain.Models.DTOs;

public sealed class HistoryDto : IMap<MonitHistory>
{
    public int Id { get; set; }
    public Lookup<string> Institution { get; set; } = default!;
    public bool Success { get; set; }
    public int HttpStatus { get; set; }
    public string? Response { get; set; }
    public string? Error { get; set; }
    public string? ErrorType { get; set; }
    public DateTimeOffset CheckedAt { get; set; }
    public void Mapping(Profile profile)
    {
        profile.CreateMap<MonitHistory, HistoryDto>()
            .ForMember(dest => dest.Institution, opt => opt.MapFrom(src => new Lookup<string>(src.InstitutionBic, src.InstitutionName, string.Empty, null, null, false)));
    }
}