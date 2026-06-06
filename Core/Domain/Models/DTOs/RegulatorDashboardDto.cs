namespace SPSS.Domain.Models.DTOs;

public sealed class RegulatorMonitoringCardDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public sealed class RegulatorVelocityAlertDto
{
    public string Id { get; set; } = string.Empty;
    public string AlertId { get; set; } = string.Empty;
    public string Institution { get; set; } = string.Empty;
    public string Pattern { get; set; } = string.Empty;
    public int TxCount { get; set; }
    public string TimeWindow { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string Severity { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Timestamp { get; set; } = string.Empty;
}

public sealed class RegulatorVelocityAlertHeatmapDto
{
    public int MaxAlerts { get; set; }
    public List<RegulatorVelocityAlertInstitutionDto> Institutions { get; set; } = [];
}

public sealed class RegulatorVelocityAlertInstitutionDto
{
    public string Name { get; set; } = string.Empty;
    public RegulatorVelocityAlertPatternsDto Patterns { get; set; } = new();
    public int TotalAlerts { get; set; }
    public int RiskScore { get; set; }
    public string PeakTime { get; set; } = string.Empty;
}

public sealed class RegulatorVelocityAlertPatternsDto
{
    public int BurstP2P { get; set; }
    public int Smurfing { get; set; }
    public int RoundTrip { get; set; }
    public int Velocity { get; set; }
    public int Structuring { get; set; }
    public int BurstP2M { get; set; }
}

public sealed class RegulatorParticipantAvailabilityDashboardDto
{
    public RegulatorParticipantAvailabilitySummaryDto Summary { get; set; } = new();
    public List<RegulatorParticipantAvailabilityDto> Participants { get; set; } = [];
}

public sealed class RegulatorParticipantAvailabilitySummaryDto
{
    public int TotalParticipants { get; set; }
    public int LiveParticipants { get; set; }
    public int OfflineParticipants { get; set; }
    public int OpenIncidents { get; set; }
    public int TotalChecks { get; set; }
    public int SuccessChecks { get; set; }
    public int FailedChecks { get; set; }
    public double UptimePercent { get; set; }
}

public sealed class RegulatorParticipantAvailabilityDto
{
    public string InstitutionBic { get; set; } = string.Empty;
    public string InstitutionName { get; set; } = string.Empty;
    public bool IsLive { get; set; }
    public DateTimeOffset LastCheckedAt { get; set; }
    public DateTimeOffset LastStatusChangeAt { get; set; }
    public int ConsecutiveFailures { get; set; }
    public int ConsecutiveSuccesses { get; set; }
    public string? LastError { get; set; }
    public int TotalChecks { get; set; }
    public int SuccessChecks { get; set; }
    public int FailedChecks { get; set; }
    public double UptimePercent { get; set; }
    public DateTimeOffset? LastFailureAt { get; set; }
}
