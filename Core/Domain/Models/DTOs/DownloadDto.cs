using SPSS.Domain.Models.BL;

namespace SPSS.Domain.Models.DTOs;

public sealed class DownloadDto : IMap<Download>
{
    public string Id { get; set; } = null!;
    public Lookup<string> Participant { get; set; } = null!;
    public string CertificateId { get; set; } = default!;
    public string Username { get; set; } = default!;
    public DateTimeOffset Date { get; set; }
    public void Mapping(Profile profile)
    {
        profile.CreateMap<Download, DownloadDto>()
            .ForMember(d => d.Participant, o => o.MapFrom(s => s.Participant));
    }
}