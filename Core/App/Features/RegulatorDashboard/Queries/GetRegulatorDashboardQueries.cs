using System.Globalization;
using System.Text.Json;

namespace SPSS.App.Features.RegulatorDashboard.Queries;

[Authorize(Roles = QueryTransactions)]
public sealed record GetRegulatorMonitoringCardsQuery(string? Period, DateTimeOffset? From, DateTimeOffset? To)
    : IRequest<Response<List<RegulatorMonitoringCardDto>>>;

[Authorize(Roles = QueryTransactions)]
public sealed record GetRegulatorTransactionVolumeHourlyQuery(string? Period, DateTimeOffset? From, DateTimeOffset? To)
    : IRequest<Response<Dictionary<string, int>>>;

[Authorize(Roles = QueryTransactions)]
public sealed record GetRegulatorVelocityAlertsQuery(string? Period, DateTimeOffset? From, DateTimeOffset? To, int Limit = 20)
    : IRequest<Response<List<RegulatorVelocityAlertDto>>>;

[Authorize(Roles = QueryTransactions)]
public sealed record GetRegulatorVelocityAlertHeatmapQuery(string? Period, DateTimeOffset? From, DateTimeOffset? To)
    : IRequest<Response<RegulatorVelocityAlertHeatmapDto>>;

public sealed class GetRegulatorMonitoringCardsQueryHandler(IStorageBroker storage)
    : IRequestHandler<GetRegulatorMonitoringCardsQuery, Response<List<RegulatorMonitoringCardDto>>>
{
    private readonly IStorageBroker _storage = storage;

    public async ValueTask<Response<List<RegulatorMonitoringCardDto>>> Handle(
        GetRegulatorMonitoringCardsQuery request,
        CancellationToken cancellationToken)
    {
        var (from, to) = RegulatorDashboardQueryHelpers.ResolveWindow(request.Period, request.From, request.To);

        var successfulQuery = _storage.NetSettlementDetails
            .AsNoTracking()
            .Where(x => !x.IsFee && x.Date >= from && x.Date < to);

        var successfulCount = await successfulQuery.CountAsync(cancellationToken);
        var successfulValue = await successfulQuery.SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0;
        var report = await RegulatorDashboardQueryHelpers.GetLatestComplianceReportAsync(_storage, from, to, cancellationToken);

        var failedToSuccessfulBps = successfulCount == 0 || report is null
            ? 0
            : decimal.Round(report.Summary.TotalFailed * 10000m / successfulCount, 2);

        var cards = new List<RegulatorMonitoringCardDto>
        {
            new()
            {
                Id = 1,
                Title = "Failed Transaction Rate",
                Value = $"{failedToSuccessfulBps:0.##} bps",
                Description = "Failed attempts vs successful non-fee settlements"
            },
            new()
            {
                Id = 2,
                Title = "Active Velocity Alerts",
                Value = (report?.Findings.Count ?? 0).ToString(CultureInfo.InvariantCulture),
                Description = $"{report?.Summary.HighRiskFindings ?? 0} escalated to FIU"
            },
            new()
            {
                Id = 3,
                Title = "Successful Transactions",
                Value = successfulCount.ToString("N0", CultureInfo.InvariantCulture),
                Description = "Non-fee settlement transactions in the selected window"
            },
            new()
            {
                Id = 4,
                Title = "Successful Settlement Value",
                Value = successfulValue.ToString("N2", CultureInfo.InvariantCulture),
                Description = "Total value excluding fee transactions"
            }
        };

        return new Response<List<RegulatorMonitoringCardDto>>(cards);
    }
}

public sealed class GetRegulatorTransactionVolumeHourlyQueryHandler(IStorageBroker storage)
    : IRequestHandler<GetRegulatorTransactionVolumeHourlyQuery, Response<Dictionary<string, int>>>
{
    private readonly IStorageBroker _storage = storage;

    public async ValueTask<Response<Dictionary<string, int>>> Handle(
        GetRegulatorTransactionVolumeHourlyQuery request,
        CancellationToken cancellationToken)
    {
        var (from, to) = RegulatorDashboardQueryHelpers.ResolveWindow(request.Period, request.From, request.To);

        var counts = await _storage.NetSettlementDetails
            .AsNoTracking()
            .Where(x => !x.IsFee && x.Date >= from && x.Date < to)
            .GroupBy(x => x.Date.Hour)
            .Select(x => new
            {
                Hour = x.Key,
                Count = x.Count()
            })
            .ToListAsync(cancellationToken);

        var byHour = Enumerable.Range(0, 24)
            .ToDictionary(
                x => $"{x:00}:00",
                x => counts.FirstOrDefault(c => c.Hour == x)?.Count ?? 0);

        return new Response<Dictionary<string, int>>(byHour);
    }
}

public sealed class GetRegulatorVelocityAlertsQueryHandler(IStorageBroker storage)
    : IRequestHandler<GetRegulatorVelocityAlertsQuery, Response<List<RegulatorVelocityAlertDto>>>
{
    private readonly IStorageBroker _storage = storage;

    public async ValueTask<Response<List<RegulatorVelocityAlertDto>>> Handle(
        GetRegulatorVelocityAlertsQuery request,
        CancellationToken cancellationToken)
    {
        var (from, to) = RegulatorDashboardQueryHelpers.ResolveWindow(request.Period, request.From, request.To);
        var report = await RegulatorDashboardQueryHelpers.GetLatestComplianceReportAsync(_storage, from, to, cancellationToken);
        if (report is null)
        {
            return new Response<List<RegulatorVelocityAlertDto>>([]);
        }

        var participantNames = await RegulatorDashboardQueryHelpers.LoadParticipantNamesAsync(_storage, cancellationToken);
        var timestamp = report.WindowEnd.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        var alerts = report.Findings
            .OrderByDescending(x => x.RiskScore)
            .ThenByDescending(x => x.CurrentCount)
            .Take(Math.Clamp(request.Limit, 1, 100))
            .Select((finding, index) => new RegulatorVelocityAlertDto
            {
                Id = (index + 1).ToString(CultureInfo.InvariantCulture),
                AlertId = $"VA-{report.WindowEnd:yyyy}-{report.Id:0000}-{index + 1:00}",
                Institution = RegulatorDashboardQueryHelpers.DisplayName(finding.ActorName, finding.ActorKey, participantNames),
                Pattern = RegulatorDashboardQueryHelpers.ToPatternName(finding.FindingType),
                TxCount = finding.CurrentCount,
                TimeWindow = RegulatorDashboardQueryHelpers.FormatWindow(report.WindowStart, report.WindowEnd),
                Value = finding.TotalAmount,
                Severity = finding.Severity,
                Status = string.Equals(finding.Severity, "HIGH", StringComparison.OrdinalIgnoreCase)
                    ? "Escalated to FIU"
                    : "Under Review",
                Timestamp = timestamp
            })
            .ToList();

        return new Response<List<RegulatorVelocityAlertDto>>(alerts);
    }
}

public sealed class GetRegulatorVelocityAlertHeatmapQueryHandler(IStorageBroker storage)
    : IRequestHandler<GetRegulatorVelocityAlertHeatmapQuery, Response<RegulatorVelocityAlertHeatmapDto>>
{
    private readonly IStorageBroker _storage = storage;

    public async ValueTask<Response<RegulatorVelocityAlertHeatmapDto>> Handle(
        GetRegulatorVelocityAlertHeatmapQuery request,
        CancellationToken cancellationToken)
    {
        var (from, to) = RegulatorDashboardQueryHelpers.ResolveWindow(request.Period, request.From, request.To);
        var report = await RegulatorDashboardQueryHelpers.GetLatestComplianceReportAsync(_storage, from, to, cancellationToken);
        if (report is null)
        {
            return new Response<RegulatorVelocityAlertHeatmapDto>(new RegulatorVelocityAlertHeatmapDto());
        }

        var participantNames = await RegulatorDashboardQueryHelpers.LoadParticipantNamesAsync(_storage, cancellationToken);
        var institutions = report.Findings
            .GroupBy(x => RegulatorDashboardQueryHelpers.DisplayName(x.ActorName, x.ActorKey, participantNames))
            .Select(group =>
            {
                var patterns = new RegulatorVelocityAlertPatternsDto();
                foreach (var finding in group)
                {
                    RegulatorDashboardQueryHelpers.AddPatternCount(patterns, finding.FindingType, finding.CurrentCount);
                }

                return new RegulatorVelocityAlertInstitutionDto
                {
                    Name = group.Key,
                    Patterns = patterns,
                    TotalAlerts = group.Sum(x => x.CurrentCount),
                    RiskScore = group.Max(x => x.RiskScore),
                    PeakTime = RegulatorDashboardQueryHelpers.FormatWindow(report.WindowStart, report.WindowEnd)
                };
            })
            .OrderByDescending(x => x.RiskScore)
            .ThenByDescending(x => x.TotalAlerts)
            .ToList();

        var heatmap = new RegulatorVelocityAlertHeatmapDto
        {
            MaxAlerts = institutions.Count == 0 ? 0 : institutions.Max(x => x.TotalAlerts),
            Institutions = institutions
        };

        return new Response<RegulatorVelocityAlertHeatmapDto>(heatmap);
    }
}

internal static class RegulatorDashboardQueryHelpers
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static (DateTimeOffset From, DateTimeOffset To) ResolveWindow(
        string? period,
        DateTimeOffset? from,
        DateTimeOffset? to)
    {
        var resolvedTo = to ?? DateTimeOffset.UtcNow;
        var resolvedFrom = from ?? ResolvePeriodStart(period, resolvedTo);

        return resolvedFrom <= resolvedTo
            ? (resolvedFrom, resolvedTo)
            : (resolvedTo, resolvedFrom);
    }

    public static async Task<FailedTransactionComplianceReportDto?> GetLatestComplianceReportAsync(
        IStorageBroker storage,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken)
    {
        var report = await storage.FailedTransactionComplianceReports
            .AsNoTracking()
            .Where(x => x.WindowEnd > from && x.WindowStart < to)
            .OrderByDescending(x => x.WindowStart)
            .Select(x => new
            {
                x.Id,
                x.ReportJson
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (report is null)
        {
            return null;
        }

        var dto = JsonSerializer.Deserialize<FailedTransactionComplianceReportDto>(report.ReportJson, JsonOptions)
                  ?? new FailedTransactionComplianceReportDto();
        dto.Id = report.Id;

        return dto;
    }

    public static async Task<Dictionary<string, string>> LoadParticipantNamesAsync(
        IStorageBroker storage,
        CancellationToken cancellationToken)
        => await storage.Participants
            .AsNoTracking()
            .Where(x => x.BicNo != string.Empty)
            .Select(x => new
            {
                x.BicNo,
                x.Name
            })
            .ToDictionaryAsync(x => x.BicNo, x => x.Name, StringComparer.OrdinalIgnoreCase, cancellationToken);

    public static string DisplayName(string? name, string key, IReadOnlyDictionary<string, string> participantNames)
    {
        if (!string.IsNullOrWhiteSpace(name) && !string.Equals(name, key, StringComparison.OrdinalIgnoreCase))
        {
            return name;
        }

        if (participantNames.TryGetValue(key, out var participantName))
        {
            return participantName;
        }

        if (key.Contains("->", StringComparison.Ordinal))
        {
            var parts = key.Split("->", StringSplitOptions.TrimEntries);
            return string.Join("->", parts.Select(part => participantNames.TryGetValue(part, out var mapped) ? mapped : part));
        }

        return key;
    }

    public static string FormatWindow(DateTimeOffset from, DateTimeOffset to)
    {
        var totalMinutes = Math.Max(1, (int)Math.Round((to - from).TotalMinutes));
        return totalMinutes < 60
            ? $"< {totalMinutes}m"
            : $"{from:HH:mm} - {to:HH:mm}";
    }

    public static string ToPatternName(string findingType)
        => ToPatternBucket(findingType) switch
        {
            DashboardPattern.Smurfing => "Smurfing",
            DashboardPattern.RoundTrip => "Round Trip",
            DashboardPattern.Structuring => "Structuring",
            DashboardPattern.BurstP2M => "Burst P2M",
            DashboardPattern.BurstP2P => "Burst P2P",
            _ => "Velocity"
        };

    public static void AddPatternCount(RegulatorVelocityAlertPatternsDto patterns, string findingType, int count)
    {
        switch (ToPatternBucket(findingType))
        {
            case DashboardPattern.BurstP2P:
                patterns.BurstP2P += count;
                break;
            case DashboardPattern.Smurfing:
                patterns.Smurfing += count;
                break;
            case DashboardPattern.RoundTrip:
                patterns.RoundTrip += count;
                break;
            case DashboardPattern.Structuring:
                patterns.Structuring += count;
                break;
            case DashboardPattern.BurstP2M:
                patterns.BurstP2M += count;
                break;
            default:
                patterns.Velocity += count;
                break;
        }
    }

    private static DashboardPattern ToPatternBucket(string findingType)
        => findingType switch
        {
            "BANK_ROUTE_SPIKE" or "PARTICIPANT_LEVEL_STORM" or "RECEIVER_BANK_STORM" => DashboardPattern.BurstP2P,
            "DEBTOR_MANY_COUNTERPARTIES" => DashboardPattern.Smurfing,
            "DEBTOR_CREDITOR_REPEAT" => DashboardPattern.RoundTrip,
            "HIGH_VALUE_FAILED_ATTEMPT" or "SENSITIVE_ERROR_REASON" => DashboardPattern.Structuring,
            "FAILED_AND_PROCESSED_SAME_DEBTOR" => DashboardPattern.BurstP2M,
            _ => DashboardPattern.Velocity
        };

    private static DateTimeOffset ResolvePeriodStart(string? period, DateTimeOffset to)
    {
        var normalized = NormalizePeriod(period);
        return normalized switch
        {
            "last-hour" => to.AddHours(-1),
            "last-24-hours" => to.AddHours(-24),
            "today" => new DateTimeOffset(to.Date, to.Offset),
            "yesterday" => new DateTimeOffset(to.Date, to.Offset).AddDays(-1),
            "week-to-date" => new DateTimeOffset(to.Date, to.Offset).AddDays(-DaysFromMonday(to.DayOfWeek)),
            "month-to-date" => new DateTimeOffset(to.Year, to.Month, 1, 0, 0, 0, to.Offset),
            "last-week" => to.AddDays(-7),
            "last-month" => to.AddMonths(-1),
            "last-quarter" => to.AddMonths(-3),
            "last-year" => to.AddYears(-1),
            _ => new DateTimeOffset(to.Date, to.Offset)
        };
    }

    private static string NormalizePeriod(string? period)
        => string.IsNullOrWhiteSpace(period)
            ? "today"
            : period.Trim().ToLowerInvariant().Replace("_", "-").Replace(" ", "-");

    private static int DaysFromMonday(DayOfWeek dayOfWeek)
        => dayOfWeek == DayOfWeek.Sunday ? 6 : (int)dayOfWeek - (int)DayOfWeek.Monday;

    private enum DashboardPattern
    {
        Velocity,
        BurstP2P,
        Smurfing,
        RoundTrip,
        Structuring,
        BurstP2M
    }
}
