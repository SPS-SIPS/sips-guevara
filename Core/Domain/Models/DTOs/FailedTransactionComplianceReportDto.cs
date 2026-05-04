namespace SPSS.Domain.Models.DTOs;

public sealed class FailedTransactionComplianceReportDto
{
    public long Id { get; set; }
    public DateTimeOffset WindowStart { get; set; }
    public DateTimeOffset WindowEnd { get; set; }
    public DateTimeOffset BaselineStart { get; set; }
    public DateTimeOffset BaselineEnd { get; set; }
    public DateTimeOffset GeneratedAt { get; set; }
    public FailedTransactionComplianceSummaryDto Summary { get; set; } = new();
    public List<FailedTransactionComplianceFindingDto> Findings { get; set; } = [];
}

public sealed class FailedTransactionComplianceSummaryDto
{
    public int TotalFailed { get; set; }
    public int TotalProcessed { get; set; }
    public int BaselineTotalFailed { get; set; }
    public int BaselineTotalProcessed { get; set; }
    public decimal BaselineAverageHourly { get; set; }
    public decimal BaselineProcessedAverageHourly { get; set; }
    public decimal CurrentVsBaselineMultiplier { get; set; }
    public decimal ProcessedVsBaselineMultiplier { get; set; }
    public decimal FailedToProcessedRatio { get; set; }
    public decimal ProcessedValueTotal { get; set; }
    public int UniqueDebtors { get; set; }
    public int UniqueCreditors { get; set; }
    public int HighValueAttempts { get; set; }
    public decimal HighValueTotalAmount { get; set; }
    public int ClassifiedFailedTransactions { get; set; }
    public int UnclassifiedFailedTransactions { get; set; }
    public decimal ClassificationCoveragePercent { get; set; }
    public DateTimeOffset? ClassificationUpdatedAt { get; set; }
    public int HighRiskFindings { get; set; }
    public int MediumRiskFindings { get; set; }
    public FailedTransactionComplianceReconciliationDto NsiReconciliation { get; set; } = new();
    public FailedTransactionComplianceRiskWeightsDto RiskWeights { get; set; } = new();
    public List<FailedTransactionComplianceDimensionDto> TopDebtors { get; set; } = [];
    public List<FailedTransactionComplianceDimensionDto> TopCreditors { get; set; } = [];
    public List<FailedTransactionComplianceDimensionDto> TopRoutes { get; set; } = [];
    public List<FailedTransactionComplianceDimensionDto> TopProcessedDebtors { get; set; } = [];
    public List<FailedTransactionComplianceDimensionDto> TopProcessedRoutes { get; set; } = [];
    public List<FailedTransactionComplianceDimensionDto> TopErrors { get; set; } = [];
    public List<FailedTransactionComplianceDimensionDto> TopRuleIds { get; set; } = [];
}

public sealed class FailedTransactionComplianceDimensionDto
{
    public string Key { get; set; } = string.Empty;
    public string? Name { get; set; }
    public int CurrentCount { get; set; }
    public int BaselineCount { get; set; }
    public decimal BaselineAverageHourly { get; set; }
    public decimal Multiplier { get; set; }
    public decimal TotalAmount { get; set; }
}

public sealed class FailedTransactionComplianceFindingDto
{
    public string Severity { get; set; } = string.Empty;
    public int RiskScore { get; set; }
    public string FindingType { get; set; } = string.Empty;
    public string ActorKey { get; set; } = string.Empty;
    public string? ActorName { get; set; }
    public int CurrentCount { get; set; }
    public int BaselineCount { get; set; }
    public decimal BaselineAverageHourly { get; set; }
    public decimal Multiplier { get; set; }
    public decimal TotalAmount { get; set; }
    public int UniqueCounterparties { get; set; }
    public FailedTransactionComplianceRiskContributionDto RiskContributions { get; set; } = new();
    public List<string> Reasons { get; set; } = [];
    public List<string> RuleIds { get; set; } = [];
    public List<long> SampleTransactionIds { get; set; } = [];
}

public sealed class FailedTransactionComplianceReconciliationDto
{
    public string Status { get; set; } = "PENDING_NSI";
    public string Source { get; set; } = "NSIMonitoring";
    public string? LatestNsiMsgId { get; set; }
    public string? LatestNsiFileName { get; set; }
    public DateTimeOffset? LatestNsiCreatedAt { get; set; }
    public DateTimeOffset? LatestNsiProcessedAt { get; set; }
    public int LatestNsiTransactionCount { get; set; }
    public int ProcessedTxIdsCount { get; set; }
    public int ReconciledProcessedTxIdsCount { get; set; }
    public int UnreconciledProcessedTxIdsCount { get; set; }
    public decimal CoveragePercent { get; set; }
    public List<string> SampleUnreconciledTxIds { get; set; } = [];
}

public sealed class FailedTransactionComplianceRiskWeightsDto
{
    public int VolumeSpike { get; set; } = 25;
    public int AbsoluteFailedCount { get; set; } = 20;
    public int ManyUniqueCounterparties { get; set; } = 15;
    public int HighValueAmount { get; set; } = 15;
    public int SensitiveErrorReason { get; set; } = 15;
    public int NewActorNoBaseline { get; set; } = 10;
    public int MediumRiskScore { get; set; } = 40;
    public int HighRiskScore { get; set; } = 70;
}

public sealed class FailedTransactionComplianceRiskContributionDto
{
    public int VolumeSpike { get; set; }
    public int AbsoluteFailedCount { get; set; }
    public int ManyUniqueCounterparties { get; set; }
    public int HighValueAmount { get; set; }
    public int SensitiveErrorReason { get; set; }
    public int NewActorNoBaseline { get; set; }
}
