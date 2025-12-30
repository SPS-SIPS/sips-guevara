using SPSS.Domain.Models.Enums;

namespace SPSS.Domain.Models.DTOs;

public sealed class ParticipantBalanceStatusDto
{
    public string Bic { get; set; } = default!;
    public string ParticipantName { get; set; } = default!;
    public decimal? LastKnownBalance { get; set; }
    public ThresholdZone CurrentZone { get; set; }
    public DateTimeOffset? LastZoneChangeAt { get; set; }
    public DateTimeOffset? LastAlertSentAt { get; set; }
    public DateTimeOffset? LastUpdatedAt { get; set; }
    public string Currency { get; set; } = "USD";
}
