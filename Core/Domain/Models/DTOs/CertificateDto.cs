using SPSS.Domain.Models.BL;

namespace SPSS.Domain.Models.DTOs;

public sealed class CertificateDto : IMap<Certificate>
{
    public string Id { get; set; } = null!;
    public DateTimeOffset ExpireDate { get; set; }
    public DateTimeOffset CachedOn { get; set; }
    public Lookup<string> Participant { get; set; } = null!;
    public string Issuer { get; set; } = default!;
    public string Subject { get; set; } = default!;
    public DateTimeOffset IssuedAt { get; set; }
    public bool IsRevoked { get; set; } = false;
    public bool Active { get; set; }
    public void Mapping(Profile profile)
    {
        profile.CreateMap<Certificate, CertificateDto>()
            .ForMember(d => d.Participant, o => o.MapFrom(s => s.Participant));
    }
}