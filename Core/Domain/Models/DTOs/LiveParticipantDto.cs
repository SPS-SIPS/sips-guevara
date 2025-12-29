namespace SPSS.Domain.Models.DTOs;

public sealed class LiveParticipantDto
{
    public string InstitutionBic { get; set; } = default!;
    public string InstitutionName { get; set; } = default!;
    public bool IsLive { get; set; }
    public DateTimeOffset LastCheckedAt { get; set; }
    public DateTimeOffset LastStatusChangeAt { get; set; }
    public int ConsecutiveFailures { get; set; }
    public int ConsecutiveSuccesses { get; set; }
    public string? LastError { get; set; }
}
