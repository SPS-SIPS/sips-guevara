using SPSS.Domain.Models.Enums;

namespace SPSS.Domain.Models.DTOs;

public sealed class ParticipantThresholdDto
{
    public int Id { get; set; }
    public string Bic { get; set; } = default!;
    public decimal GoldenAmount { get; set; }
    public decimal AmberPercentage { get; set; }
    public decimal RedPercentage { get; set; }
    public ThresholdZone LastKnownZone { get; set; }
    public decimal? LastKnownBalance { get; set; }
    public DateTimeOffset? LastZoneChangeAt { get; set; }
    public DateTimeOffset? LastAlertSentAt { get; set; }
    public string? AlertRecipients { get; set; }
}

public sealed class CreateParticipantThresholdDto
{
    public string Bic { get; set; } = default!;
    public decimal GoldenAmount { get; set; }
    public decimal AmberPercentage { get; set; }
    public decimal RedPercentage { get; set; }
    public string? AlertRecipients { get; set; }
}

public sealed class UpdateParticipantThresholdDto
{
    public int Id { get; set; }
    public decimal GoldenAmount { get; set; }
    public decimal AmberPercentage { get; set; }
    public decimal RedPercentage { get; set; }
    public string? AlertRecipients { get; set; }
}
