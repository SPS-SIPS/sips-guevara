using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SPSS.Domain.Models.BL;

namespace SPSS.Infrastructure.Workers;

public sealed class FailedTransactionComplianceReportConfiguration
{
    public bool Enabled { get; set; } = true;
    public int RunIntervalMinutes { get; set; } = 60;
    public int InitialDelayMinutes { get; set; } = 2;
    public int BaselineHours { get; set; } = 24;
    public int ReportWindowMinutes { get; set; } = 60;
    public int MinCurrentCount { get; set; } = 5;
    public decimal StormMultiplier { get; set; } = 3;
    public decimal HighValueThreshold { get; set; } = 10000;
    public int ManyCounterpartiesThreshold { get; set; } = 5;
    public int MaxFindings { get; set; } = 50;
    public int MediumRiskScore { get; set; } = 40;
    public int HighRiskScore { get; set; } = 70;
}

public sealed class FailedTransactionComplianceReportWorker(
    ILogger<FailedTransactionComplianceReportWorker> logger,
    IServiceScopeFactory serviceScopeFactory,
    IOptions<FailedTransactionComplianceReportConfiguration> options) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ILogger<FailedTransactionComplianceReportWorker> _logger = logger;
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;
    private readonly FailedTransactionComplianceReportConfiguration _config = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_config.Enabled)
        {
            _logger.LogInformation("Failed transaction compliance report worker is disabled.");
            return;
        }

        _logger.LogInformation(
            "Failed transaction compliance report worker started. Interval={IntervalMinutes} minutes, Baseline={BaselineHours} hours",
            _config.RunIntervalMinutes,
            _config.BaselineHours);

        await Task.Delay(TimeSpan.FromMinutes(Math.Max(0, _config.InitialDelayMinutes)), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await GenerateLatestReportAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromMinutes(Math.Max(1, _config.RunIntervalMinutes)), stoppingToken);
        }
    }

    private async Task GenerateLatestReportAsync(CancellationToken cancellationToken)
    {
        try
        {
            var now = DateTimeOffset.UtcNow;
            var windowEnd = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, 0, 0, TimeSpan.Zero);
            var windowStart = windowEnd.AddMinutes(-Math.Max(1, _config.ReportWindowMinutes));
            var baselineEnd = windowStart;
            var baselineStart = baselineEnd.AddHours(-Math.Max(1, _config.BaselineHours));

            using var scope = _serviceScopeFactory.CreateScope();
            var storage = scope.ServiceProvider.GetRequiredService<IStorageBroker>();

            var failedTransactions = await storage.FailedTransactions
                .AsNoTracking()
                .Where(x => x.CreatedDate >= baselineStart && x.CreatedDate < windowEnd)
                .Select(x => new ComplianceTransaction
                {
                    Id = x.Id,
                    CreatedDate = x.CreatedDate,
                    BankFrom = x.BankFrom,
                    BankFromName = x.BankFromName,
                    BankTo = x.BankTo,
                    BankToName = x.BankToName,
                    TxId = x.TxId,
                    Amount = x.Amount,
                    Currency = x.Currency,
                    DbtrAcctId = x.DbtrAcctId,
                    DbtrName = x.DbtrName,
                    CdtrAcctId = x.CdtrAcctId,
                    CdtrName = x.CdtrName,
                    Error = x.Error,
                    ErrorReason = x.ErrorReason,
                    IsFailed = true
                })
                .ToListAsync(cancellationToken);

            var processedTransactions = await storage.ProcessedTransactions
                .AsNoTracking()
                .Where(x => x.CreatedDate >= baselineStart && x.CreatedDate < windowEnd)
                .Select(x => new ComplianceTransaction
                {
                    Id = x.Id,
                    CreatedDate = x.CreatedDate,
                    BankFrom = x.BankFrom,
                    BankFromName = x.BankFromName,
                    BankTo = x.BankTo,
                    BankToName = x.BankToName,
                    TxId = x.TxId,
                    Amount = x.Amount,
                    Currency = x.Currency,
                    DbtrAcctId = x.DbtrAcctId,
                    DbtrName = x.DbtrName,
                    CdtrAcctId = x.CdtrAcctId,
                    CdtrName = x.CdtrName,
                    IsFailed = false
                })
                .ToListAsync(cancellationToken);

            var currentFailed = failedTransactions
                .Where(x => x.CreatedDate >= windowStart && x.CreatedDate < windowEnd)
                .ToList();
            var baselineFailed = failedTransactions
                .Where(x => x.CreatedDate >= baselineStart && x.CreatedDate < baselineEnd)
                .ToList();
            var currentProcessed = processedTransactions
                .Where(x => x.CreatedDate >= windowStart && x.CreatedDate < windowEnd)
                .ToList();
            var baselineProcessed = processedTransactions
                .Where(x => x.CreatedDate >= baselineStart && x.CreatedDate < baselineEnd)
                .ToList();

            var report = BuildReport(
                windowStart,
                windowEnd,
                baselineStart,
                baselineEnd,
                currentFailed,
                baselineFailed,
                currentProcessed,
                baselineProcessed);
            var reportJson = JsonSerializer.Serialize(report, JsonOptions);

            var entity = await storage.FailedTransactionComplianceReports
                .FirstOrDefaultAsync(x => x.WindowStart == windowStart && x.WindowEnd == windowEnd, cancellationToken);

            if (entity is null)
            {
                await storage.FailedTransactionComplianceReports.AddAsync(
                    MapEntity(new FailedTransactionComplianceReport(), report, reportJson),
                    cancellationToken);
            }
            else
            {
                MapEntity(entity, report, reportJson);
                storage.FailedTransactionComplianceReports.Update(entity);
            }

            await storage.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Failed transaction compliance report generated. Window={WindowStart} to {WindowEnd}, Total={Total}, HighRisk={HighRisk}, MediumRisk={MediumRisk}",
                windowStart,
                windowEnd,
                report.Summary.TotalFailed,
                report.Summary.HighRiskFindings,
                report.Summary.MediumRiskFindings);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Failed transaction compliance report worker is stopping.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate failed transaction compliance report.");
        }
    }

    private FailedTransactionComplianceReportDto BuildReport(
        DateTimeOffset windowStart,
        DateTimeOffset windowEnd,
        DateTimeOffset baselineStart,
        DateTimeOffset baselineEnd,
        List<ComplianceTransaction> current,
        List<ComplianceTransaction> baseline,
        List<ComplianceTransaction> currentProcessed,
        List<ComplianceTransaction> baselineProcessed)
    {
        var baselineAverageHourly = Divide(baseline.Count, Math.Max(1, _config.BaselineHours));
        var baselineProcessedAverageHourly = Divide(baselineProcessed.Count, Math.Max(1, _config.BaselineHours));
        var findings = new List<FailedTransactionComplianceFindingDto>();

        findings.AddRange(BuildVolumeFindings("DEBTOR_STORM", current, baseline, x => x.DbtrAcctId, x => x.DbtrName, x => x.CdtrAcctId));
        findings.AddRange(BuildVolumeFindings("CREDITOR_TARGETING", current, baseline, x => x.CdtrAcctId, x => x.CdtrName, x => x.DbtrAcctId));
        findings.AddRange(BuildVolumeFindings("PARTICIPANT_LEVEL_STORM", current, baseline, x => x.BankFrom, x => x.BankFromName, x => x.CdtrAcctId));
        findings.AddRange(BuildVolumeFindings("RECEIVER_BANK_STORM", current, baseline, x => x.BankTo, x => x.BankToName, x => x.DbtrAcctId));
        findings.AddRange(BuildVolumeFindings("BANK_ROUTE_SPIKE", current, baseline, x => JoinKey(x.BankFrom, x.BankTo), x => JoinKey(x.BankFromName, x.BankToName), x => x.DbtrAcctId));
        findings.AddRange(BuildVolumeFindings("DEBTOR_CREDITOR_REPEAT", current, baseline, x => JoinKey(x.DbtrAcctId, x.CdtrAcctId), x => JoinKey(x.DbtrName, x.CdtrName), x => x.CdtrAcctId));
        findings.AddRange(BuildVolumeFindings("SENSITIVE_ERROR_REASON", current, baseline, x => FirstNonEmpty(x.ErrorReason, x.Error), x => x.Error, x => x.DbtrAcctId));
        findings.AddRange(BuildHighValueFindings(current));
        findings.AddRange(BuildManyCounterpartyFindings(current));
        findings.AddRange(BuildFailedToProcessedFindings(current, currentProcessed, baseline, baselineProcessed));
        findings.AddRange(BuildProcessedVolumeFindings("PROCESSED_DEBTOR_SPIKE", currentProcessed, baselineProcessed, x => x.DbtrAcctId, x => x.DbtrName, x => x.CdtrAcctId));
        findings.AddRange(BuildProcessedVolumeFindings("PROCESSED_ROUTE_SPIKE", currentProcessed, baselineProcessed, x => JoinKey(x.BankFrom, x.BankTo), x => JoinKey(x.BankFromName, x.BankToName), x => x.DbtrAcctId));

        findings = findings
            .Where(x => x.RiskScore >= _config.MediumRiskScore)
            .GroupBy(x => new { x.FindingType, x.ActorKey })
            .Select(x => x.OrderByDescending(f => f.RiskScore).First())
            .OrderByDescending(x => x.RiskScore)
            .ThenByDescending(x => x.CurrentCount)
            .Take(Math.Max(1, _config.MaxFindings))
            .ToList();

        var highRisk = findings.Count(x => x.Severity == "HIGH");
        var mediumRisk = findings.Count(x => x.Severity == "MEDIUM");
        var highValueTransactions = current.Where(x => ParseAmount(x.Amount) >= _config.HighValueThreshold).ToList();

        return new FailedTransactionComplianceReportDto
        {
            WindowStart = windowStart,
            WindowEnd = windowEnd,
            BaselineStart = baselineStart,
            BaselineEnd = baselineEnd,
            GeneratedAt = DateTimeOffset.UtcNow,
            Summary = new FailedTransactionComplianceSummaryDto
            {
                TotalFailed = current.Count,
                TotalProcessed = currentProcessed.Count,
                BaselineTotalFailed = baseline.Count,
                BaselineTotalProcessed = baselineProcessed.Count,
                BaselineAverageHourly = baselineAverageHourly,
                BaselineProcessedAverageHourly = baselineProcessedAverageHourly,
                CurrentVsBaselineMultiplier = CalculateMultiplier(current.Count, baselineAverageHourly),
                ProcessedVsBaselineMultiplier = CalculateMultiplier(currentProcessed.Count, baselineProcessedAverageHourly),
                FailedToProcessedRatio = Divide(current.Count, Math.Max(1, currentProcessed.Count)),
                ProcessedValueTotal = currentProcessed.Sum(x => ParseAmount(x.Amount)),
                UniqueDebtors = current.Select(x => x.DbtrAcctId).Where(HasValue).Distinct().Count(),
                UniqueCreditors = current.Select(x => x.CdtrAcctId).Where(HasValue).Distinct().Count(),
                HighValueAttempts = highValueTransactions.Count,
                HighValueTotalAmount = highValueTransactions.Sum(x => ParseAmount(x.Amount)),
                HighRiskFindings = highRisk,
                MediumRiskFindings = mediumRisk,
                RiskWeights = BuildRiskWeights(),
                TopDebtors = BuildTopDimensions(current, baseline, x => x.DbtrAcctId, x => x.DbtrName),
                TopCreditors = BuildTopDimensions(current, baseline, x => x.CdtrAcctId, x => x.CdtrName),
                TopRoutes = BuildTopDimensions(current, baseline, x => JoinKey(x.BankFrom, x.BankTo), x => JoinKey(x.BankFromName, x.BankToName)),
                TopProcessedDebtors = BuildTopDimensions(currentProcessed, baselineProcessed, x => x.DbtrAcctId, x => x.DbtrName),
                TopProcessedRoutes = BuildTopDimensions(currentProcessed, baselineProcessed, x => JoinKey(x.BankFrom, x.BankTo), x => JoinKey(x.BankFromName, x.BankToName)),
                TopErrors = BuildTopDimensions(current, baseline, x => FirstNonEmpty(x.ErrorReason, x.Error), x => x.Error)
            },
            Findings = findings
        };
    }

    private IEnumerable<FailedTransactionComplianceFindingDto> BuildVolumeFindings(
        string findingType,
        List<ComplianceTransaction> current,
        List<ComplianceTransaction> baseline,
        Func<ComplianceTransaction, string?> keySelector,
        Func<ComplianceTransaction, string?> nameSelector,
        Func<ComplianceTransaction, string?> counterpartySelector)
    {
        var baselineCounts = baseline
            .Where(x => HasValue(keySelector(x)))
            .GroupBy(x => keySelector(x)!)
            .ToDictionary(x => x.Key, x => x.Count());

        return current
            .Where(x => HasValue(keySelector(x)))
            .GroupBy(x => keySelector(x)!)
            .Select(group =>
            {
                baselineCounts.TryGetValue(group.Key, out var baselineCount);
                var baselineAverageHourly = Divide(baselineCount, Math.Max(1, _config.BaselineHours));
                var multiplier = CalculateMultiplier(group.Count(), baselineAverageHourly);
                var uniqueCounterparties = group.Select(counterpartySelector).Where(HasValue).Distinct().Count();

                if (group.Count() < _config.MinCurrentCount)
                {
                    return null;
                }

                if (baselineCount > 0 && multiplier < _config.StormMultiplier && group.Count() < _config.MinCurrentCount * 2)
                {
                    return null;
                }

                var reasons = new List<string>();
                if (group.Count() >= _config.MinCurrentCount)
                {
                    reasons.Add($"Current hour has {group.Count()} failed attempts.");
                }
                if (baselineCount == 0)
                {
                    reasons.Add("No matching activity in the previous 24-hour baseline.");
                }
                else if (multiplier >= _config.StormMultiplier)
                {
                    reasons.Add($"Current hour is {multiplier:0.##}x above the hourly baseline.");
                }
                if (uniqueCounterparties >= _config.ManyCounterpartiesThreshold)
                {
                    reasons.Add($"Activity touches {uniqueCounterparties} unique counterparties.");
                }

                var totalAmount = group.Sum(x => ParseAmount(x.Amount));
                var risk = Score(
                    group.Count(),
                    multiplier,
                    baselineCount,
                    uniqueCounterparties,
                    totalAmount,
                    group.Any(HasSensitiveError));

                return new FailedTransactionComplianceFindingDto
                {
                    Severity = Severity(risk.Score),
                    RiskScore = risk.Score,
                    FindingType = findingType,
                    ActorKey = group.Key,
                    ActorName = group.Select(nameSelector).FirstOrDefault(HasValue),
                    CurrentCount = group.Count(),
                    BaselineCount = baselineCount,
                    BaselineAverageHourly = baselineAverageHourly,
                    Multiplier = multiplier,
                    TotalAmount = totalAmount,
                    UniqueCounterparties = uniqueCounterparties,
                    RiskContributions = risk.Contributions,
                    Reasons = reasons,
                    SampleTransactionIds = group.OrderByDescending(x => x.CreatedDate).Take(10).Select(x => x.Id).ToList()
                };
            })
            .Where(x => x is not null)!;
    }

    private IEnumerable<FailedTransactionComplianceFindingDto> BuildHighValueFindings(List<ComplianceTransaction> current)
    {
        return current
            .Select(x => new { Transaction = x, Amount = ParseAmount(x.Amount) })
            .Where(x => x.Amount >= _config.HighValueThreshold)
            .OrderByDescending(x => x.Amount)
            .Take(20)
            .Select(x =>
            {
                var risk = Score(
                    currentCount: 1,
                    multiplier: 0,
                    baselineCount: 1,
                    uniqueCounterparties: 1,
                    totalAmount: x.Amount,
                    hasSensitiveError: HasSensitiveError(x.Transaction));
                return new FailedTransactionComplianceFindingDto
                {
                    Severity = Severity(risk.Score),
                    RiskScore = risk.Score,
                    FindingType = "HIGH_VALUE_FAILED_ATTEMPT",
                    ActorKey = x.Transaction.TxId ?? x.Transaction.Id.ToString(CultureInfo.InvariantCulture),
                    ActorName = JoinKey(x.Transaction.DbtrName, x.Transaction.CdtrName),
                    CurrentCount = 1,
                    TotalAmount = x.Amount,
                    RiskContributions = risk.Contributions,
                    Reasons = [$"Failed attempt amount {x.Amount:0.##} is above configured threshold {_config.HighValueThreshold:0.##}."],
                    SampleTransactionIds = [x.Transaction.Id]
                };
            });
    }

    private IEnumerable<FailedTransactionComplianceFindingDto> BuildManyCounterpartyFindings(List<ComplianceTransaction> current)
    {
        return current
            .Where(x => HasValue(x.DbtrAcctId))
            .GroupBy(x => x.DbtrAcctId!)
            .Select(group =>
            {
                var uniqueCreditors = group.Select(x => x.CdtrAcctId).Where(HasValue).Distinct().Count();
                if (uniqueCreditors < _config.ManyCounterpartiesThreshold)
                {
                    return null;
                }

                var totalAmount = group.Sum(x => ParseAmount(x.Amount));
                var risk = Score(
                    group.Count(),
                    multiplier: 0,
                    baselineCount: 1,
                    uniqueCounterparties: uniqueCreditors,
                    totalAmount: totalAmount,
                    hasSensitiveError: group.Any(HasSensitiveError));

                return new FailedTransactionComplianceFindingDto
                {
                    Severity = Severity(risk.Score),
                    RiskScore = risk.Score,
                    FindingType = "DEBTOR_MANY_COUNTERPARTIES",
                    ActorKey = group.Key,
                    ActorName = group.Select(x => x.DbtrName).FirstOrDefault(HasValue),
                    CurrentCount = group.Count(),
                    TotalAmount = totalAmount,
                    UniqueCounterparties = uniqueCreditors,
                    RiskContributions = risk.Contributions,
                    Reasons = [$"Debtor attempted failed payments to {uniqueCreditors} unique creditors in one hour."],
                    SampleTransactionIds = group.OrderByDescending(x => x.CreatedDate).Take(10).Select(x => x.Id).ToList()
                };
            })
            .Where(x => x is not null)!;
    }

    private IEnumerable<FailedTransactionComplianceFindingDto> BuildFailedToProcessedFindings(
        List<ComplianceTransaction> currentFailed,
        List<ComplianceTransaction> currentProcessed,
        List<ComplianceTransaction> baselineFailed,
        List<ComplianceTransaction> baselineProcessed)
    {
        var processedByDebtor = currentProcessed
            .Where(x => HasValue(x.DbtrAcctId))
            .GroupBy(x => x.DbtrAcctId!)
            .ToDictionary(x => x.Key, x => x.ToList());

        var baselineByDebtor = baselineFailed
            .Concat(baselineProcessed)
            .Where(x => HasValue(x.DbtrAcctId))
            .GroupBy(x => x.DbtrAcctId!)
            .ToDictionary(x => x.Key, x => x.Count());

        return currentFailed
            .Where(x => HasValue(x.DbtrAcctId))
            .GroupBy(x => x.DbtrAcctId!)
            .Select(group =>
            {
                if (group.Count() < _config.MinCurrentCount ||
                    !processedByDebtor.TryGetValue(group.Key, out var processed) ||
                    processed.Count == 0)
                {
                    return null;
                }

                baselineByDebtor.TryGetValue(group.Key, out var baselineCount);
                var currentCount = group.Count() + processed.Count;
                var baselineAverageHourly = Divide(baselineCount, Math.Max(1, _config.BaselineHours));
                var multiplier = CalculateMultiplier(currentCount, baselineAverageHourly);
                var uniqueCounterparties = group.Concat(processed)
                    .Select(x => x.CdtrAcctId)
                    .Where(HasValue)
                    .Distinct()
                    .Count();
                var totalAmount = group.Concat(processed).Sum(x => ParseAmount(x.Amount));
                var risk = Score(
                    currentCount,
                    multiplier,
                    baselineCount,
                    uniqueCounterparties,
                    totalAmount,
                    group.Any(HasSensitiveError));

                return new FailedTransactionComplianceFindingDto
                {
                    Severity = Severity(risk.Score),
                    RiskScore = risk.Score,
                    FindingType = "FAILED_AND_PROCESSED_SAME_DEBTOR",
                    ActorKey = group.Key,
                    ActorName = group.Select(x => x.DbtrName).Concat(processed.Select(x => x.DbtrName)).FirstOrDefault(HasValue),
                    CurrentCount = currentCount,
                    BaselineCount = baselineCount,
                    BaselineAverageHourly = baselineAverageHourly,
                    Multiplier = multiplier,
                    TotalAmount = totalAmount,
                    UniqueCounterparties = uniqueCounterparties,
                    RiskContributions = risk.Contributions,
                    Reasons =
                    [
                        $"Debtor has {group.Count()} failed attempts and {processed.Count} processed transactions in the same hour.",
                        $"Failed-to-processed ratio is {Divide(group.Count(), Math.Max(1, processed.Count)):0.##}."
                    ],
                    SampleTransactionIds = group.Concat(processed)
                        .OrderByDescending(x => x.CreatedDate)
                        .Take(10)
                        .Select(x => x.Id)
                        .ToList()
                };
            })
            .Where(x => x is not null)!;
    }

    private IEnumerable<FailedTransactionComplianceFindingDto> BuildProcessedVolumeFindings(
        string findingType,
        List<ComplianceTransaction> current,
        List<ComplianceTransaction> baseline,
        Func<ComplianceTransaction, string?> keySelector,
        Func<ComplianceTransaction, string?> nameSelector,
        Func<ComplianceTransaction, string?> counterpartySelector)
    {
        var baselineCounts = baseline
            .Where(x => HasValue(keySelector(x)))
            .GroupBy(x => keySelector(x)!)
            .ToDictionary(x => x.Key, x => x.Count());

        return current
            .Where(x => HasValue(keySelector(x)))
            .GroupBy(x => keySelector(x)!)
            .Select(group =>
            {
                baselineCounts.TryGetValue(group.Key, out var baselineCount);
                var baselineAverageHourly = Divide(baselineCount, Math.Max(1, _config.BaselineHours));
                var multiplier = CalculateMultiplier(group.Count(), baselineAverageHourly);
                var uniqueCounterparties = group.Select(counterpartySelector).Where(HasValue).Distinct().Count();
                var totalAmount = group.Sum(x => ParseAmount(x.Amount));

                if (group.Count() < _config.MinCurrentCount &&
                    totalAmount < _config.HighValueThreshold * 5)
                {
                    return null;
                }

                if (baselineCount > 0 &&
                    multiplier < _config.StormMultiplier &&
                    totalAmount < _config.HighValueThreshold * 5)
                {
                    return null;
                }

                var risk = Score(
                    group.Count(),
                    multiplier,
                    baselineCount,
                    uniqueCounterparties,
                    totalAmount,
                    hasSensitiveError: false);

                return new FailedTransactionComplianceFindingDto
                {
                    Severity = Severity(risk.Score),
                    RiskScore = risk.Score,
                    FindingType = findingType,
                    ActorKey = group.Key,
                    ActorName = group.Select(nameSelector).FirstOrDefault(HasValue),
                    CurrentCount = group.Count(),
                    BaselineCount = baselineCount,
                    BaselineAverageHourly = baselineAverageHourly,
                    Multiplier = multiplier,
                    TotalAmount = totalAmount,
                    UniqueCounterparties = uniqueCounterparties,
                    RiskContributions = risk.Contributions,
                    Reasons =
                    [
                        $"Processed flow has {group.Count()} transactions in the current hour.",
                        baselineCount == 0
                            ? "No matching processed activity in the previous 24-hour baseline."
                            : $"Processed flow is {multiplier:0.##}x above the hourly baseline."
                    ],
                    SampleTransactionIds = group.OrderByDescending(x => x.CreatedDate).Take(10).Select(x => x.Id).ToList()
                };
            })
            .Where(x => x is not null)!;
    }

    private List<FailedTransactionComplianceDimensionDto> BuildTopDimensions(
        List<ComplianceTransaction> current,
        List<ComplianceTransaction> baseline,
        Func<ComplianceTransaction, string?> keySelector,
        Func<ComplianceTransaction, string?> nameSelector)
    {
        var baselineCounts = baseline
            .Where(x => HasValue(keySelector(x)))
            .GroupBy(x => keySelector(x)!)
            .ToDictionary(x => x.Key, x => x.Count());

        return current
            .Where(x => HasValue(keySelector(x)))
            .GroupBy(x => keySelector(x)!)
            .OrderByDescending(x => x.Count())
            .ThenByDescending(x => x.Sum(t => ParseAmount(t.Amount)))
            .Take(10)
            .Select(group =>
            {
                baselineCounts.TryGetValue(group.Key, out var baselineCount);
                var baselineAverageHourly = Divide(baselineCount, Math.Max(1, _config.BaselineHours));
                return new FailedTransactionComplianceDimensionDto
                {
                    Key = group.Key,
                    Name = group.Select(nameSelector).FirstOrDefault(HasValue),
                    CurrentCount = group.Count(),
                    BaselineCount = baselineCount,
                    BaselineAverageHourly = baselineAverageHourly,
                    Multiplier = CalculateMultiplier(group.Count(), baselineAverageHourly),
                    TotalAmount = group.Sum(x => ParseAmount(x.Amount))
                };
            })
            .ToList();
    }

    private static FailedTransactionComplianceReport MapEntity(
        FailedTransactionComplianceReport entity,
        FailedTransactionComplianceReportDto report,
        string reportJson)
    {
        entity.WindowStart = report.WindowStart;
        entity.WindowEnd = report.WindowEnd;
        entity.BaselineStart = report.BaselineStart;
        entity.BaselineEnd = report.BaselineEnd;
        entity.GeneratedAt = report.GeneratedAt;
        entity.TotalFailed = report.Summary.TotalFailed;
        entity.BaselineTotalFailed = report.Summary.BaselineTotalFailed;
        entity.BaselineAverageHourly = report.Summary.BaselineAverageHourly;
        entity.UniqueDebtors = report.Summary.UniqueDebtors;
        entity.UniqueCreditors = report.Summary.UniqueCreditors;
        entity.HighRiskFindings = report.Summary.HighRiskFindings;
        entity.MediumRiskFindings = report.Summary.MediumRiskFindings;
        entity.ReportJson = reportJson;

        return entity;
    }

    private RiskScoreResult Score(
        int currentCount,
        decimal multiplier,
        int baselineCount,
        int uniqueCounterparties,
        decimal totalAmount,
        bool hasSensitiveError)
    {
        var contributions = new FailedTransactionComplianceRiskContributionDto();

        contributions.VolumeSpike = multiplier switch
        {
            >= 10 => 25,
            >= 5 => 20,
            >= 3 => 15,
            >= 2 => 8,
            _ => 0
        };

        contributions.AbsoluteFailedCount = currentCount switch
        {
            >= 50 => 20,
            >= 25 => 15,
            >= 10 => 10,
            >= 5 => 5,
            _ => 0
        };

        contributions.ManyUniqueCounterparties = uniqueCounterparties switch
        {
            >= 20 => 15,
            >= 10 => 10,
            >= 5 => 6,
            _ => 0
        };

        contributions.HighValueAmount = totalAmount switch
        {
            >= 100000 => 15,
            _ when totalAmount >= _config.HighValueThreshold * 5 => 15,
            _ when totalAmount >= _config.HighValueThreshold => 10,
            _ => 0
        };

        contributions.SensitiveErrorReason = hasSensitiveError ? 15 : 0;

        if (baselineCount == 0)
        {
            contributions.NewActorNoBaseline = 10;
        }

        var score = contributions.VolumeSpike +
                    contributions.AbsoluteFailedCount +
                    contributions.ManyUniqueCounterparties +
                    contributions.HighValueAmount +
                    contributions.SensitiveErrorReason +
                    contributions.NewActorNoBaseline;

        return new RiskScoreResult(Math.Min(100, score), contributions);
    }

    private static FailedTransactionComplianceRiskWeightsDto BuildRiskWeights()
    {
        return new FailedTransactionComplianceRiskWeightsDto();
    }

    private string Severity(int score)
    {
        if (score >= _config.HighRiskScore)
        {
            return "HIGH";
        }

        return score >= _config.MediumRiskScore ? "MEDIUM" : "LOW";
    }

    private static bool HasSensitiveError(ComplianceTransaction transaction)
    {
        var value = $"{transaction.ErrorReason} {transaction.Error}".ToUpperInvariant();
        return value.Contains("SANCTION", StringComparison.Ordinal) ||
               value.Contains("BLOCK", StringComparison.Ordinal) ||
               value.Contains("RESTRICT", StringComparison.Ordinal) ||
               value.Contains("FRAUD", StringComparison.Ordinal) ||
               value.Contains("AML", StringComparison.Ordinal) ||
               value.Contains("BLACKLIST", StringComparison.Ordinal) ||
               value.Contains("WATCHLIST", StringComparison.Ordinal);
    }

    private static decimal ParseAmount(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }

        var clean = value.Replace(",", string.Empty, StringComparison.Ordinal).Trim();
        return decimal.TryParse(clean, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var amount)
            ? Math.Abs(amount)
            : 0;
    }

    private static decimal Divide(decimal value, decimal divisor)
    {
        return divisor == 0 ? 0 : Math.Round(value / divisor, 2, MidpointRounding.AwayFromZero);
    }

    private static decimal CalculateMultiplier(int currentCount, decimal baselineAverageHourly)
    {
        return baselineAverageHourly <= 0
            ? currentCount > 0 ? 999 : 0
            : Math.Round(currentCount / baselineAverageHourly, 2, MidpointRounding.AwayFromZero);
    }

    private static bool HasValue(string? value) => !string.IsNullOrWhiteSpace(value);

    private static string JoinKey(string? first, string? second)
    {
        return $"{FirstNonEmpty(first, "UNKNOWN")}->{FirstNonEmpty(second, "UNKNOWN")}";
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(HasValue) ?? "UNKNOWN";
    }

    private sealed class ComplianceTransaction
    {
        public long Id { get; init; }
        public DateTimeOffset? CreatedDate { get; init; }
        public string? BankFrom { get; init; }
        public string? BankFromName { get; init; }
        public string? BankTo { get; init; }
        public string? BankToName { get; init; }
        public string? TxId { get; init; }
        public string? Amount { get; init; }
        public string? Currency { get; init; }
        public string? DbtrAcctId { get; init; }
        public string? DbtrName { get; init; }
        public string? CdtrAcctId { get; init; }
        public string? CdtrName { get; init; }
        public string? Error { get; init; }
        public string? ErrorReason { get; init; }
        public bool IsFailed { get; init; }
    }

    private sealed record RiskScoreResult(
        int Score,
        FailedTransactionComplianceRiskContributionDto Contributions);
}
