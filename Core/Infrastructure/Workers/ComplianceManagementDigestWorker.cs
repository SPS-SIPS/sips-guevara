using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SPSS.Domain.Services;

namespace SPSS.Infrastructure.Workers;

public sealed class ComplianceManagementDigestConfiguration
{
    public bool Enabled { get; set; } = false;
    public int SendHour { get; set; } = 16;
    public int SendMinute { get; set; } = 0;
    public string TimeZoneId { get; set; } = "Africa/Mogadishu";
    public string SubjectPrefix { get; set; } = "Daily Compliance Intelligence Summary";
    public string PortalUrl { get; set; } = string.Empty;
    public string[] Recipients { get; set; } = [];
}

public sealed class ComplianceManagementDigestWorker(
    ILogger<ComplianceManagementDigestWorker> logger,
    IServiceScopeFactory serviceScopeFactory,
    IOptions<ComplianceManagementDigestConfiguration> options) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ILogger<ComplianceManagementDigestWorker> _logger = logger;
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;
    private readonly ComplianceManagementDigestConfiguration _config = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_config.Enabled)
        {
            _logger.LogInformation("Compliance management digest worker is disabled.");
            return;
        }

        if (_config.Recipients.Length == 0)
        {
            _logger.LogWarning("Compliance management digest worker is enabled but has no recipients.");
            return;
        }

        var timeZone = ResolveTimeZone(_config.TimeZoneId);
        _logger.LogInformation(
            "Compliance management digest worker started. SendTime={SendHour:D2}:{SendMinute:D2}, TimeZone={TimeZoneId}, Recipients={Recipients}",
            NormalizeHour(_config.SendHour),
            NormalizeMinute(_config.SendMinute),
            timeZone.Id,
            string.Join(",", _config.Recipients));

        while (!stoppingToken.IsCancellationRequested)
        {
            var nextRun = GetNextRun(DateTimeOffset.UtcNow, timeZone);
            var delay = nextRun - DateTimeOffset.UtcNow;
            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, stoppingToken);
            }

            if (!stoppingToken.IsCancellationRequested)
            {
                await SendDigestAsync(timeZone, stoppingToken);
            }
        }
    }

    private async Task SendDigestAsync(TimeZoneInfo timeZone, CancellationToken cancellationToken)
    {
        var nowLocal = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, timeZone);
        var targetDate = nowLocal.Date;
        var targetDateTime = new DateTime(
            targetDate.Year,
            targetDate.Month,
            targetDate.Day,
            NormalizeHour(_config.SendHour),
            NormalizeMinute(_config.SendMinute),
            0);
        var windowEndLocal = new DateTimeOffset(targetDateTime, timeZone.GetUtcOffset(targetDateTime));

        if (nowLocal.TimeOfDay < new TimeSpan(NormalizeHour(_config.SendHour), NormalizeMinute(_config.SendMinute), 0))
        {
            windowEndLocal = windowEndLocal.AddDays(-1);
        }

        var windowStartLocal = windowEndLocal.AddDays(-1);
        var windowStartUtc = windowStartLocal.ToUniversalTime();
        var windowEndUtc = windowEndLocal.ToUniversalTime();

        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var storage = scope.ServiceProvider.GetRequiredService<IStorageBroker>();
            var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
            var emailConfig = scope.ServiceProvider.GetRequiredService<EmailConfig>();
            var templateRenderer = scope.ServiceProvider.GetRequiredService<ITemplateRenderer>();

            var summary = await BuildSummaryAsync(storage, windowStartUtc, windowEndUtc, cancellationToken);
            var subject = $"{_config.SubjectPrefix} - {windowEndLocal:yyyy-MM-dd}";
            var body = await BuildHtmlBodyAsync(
                templateRenderer,
                emailConfig,
                summary,
                windowStartLocal,
                windowEndLocal,
                timeZone,
                cancellationToken);

            foreach (var recipient in _config.Recipients.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                await emailSender.SendEmailAsync(recipient.Trim(), subject, body, isHTML: true);
            }

            _logger.LogInformation(
                "Compliance management digest sent. Window={WindowStart} to {WindowEnd}, Recipients={Recipients}",
                windowStartUtc,
                windowEndUtc,
                string.Join(",", _config.Recipients));
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Compliance management digest worker is stopping.");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send compliance management digest. Window={WindowStart} to {WindowEnd}",
                windowStartUtc,
                windowEndUtc);
        }
    }

    private async Task<DigestSummary> BuildSummaryAsync(
        IStorageBroker storage,
        DateTimeOffset windowStartUtc,
        DateTimeOffset windowEndUtc,
        CancellationToken cancellationToken)
    {
        var failedTransactions = await storage.FailedTransactions
            .AsNoTracking()
            .Where(x => x.CreatedDate >= windowStartUtc && x.CreatedDate < windowEndUtc)
            .Select(x => new DigestTransaction
            {
                Id = x.Id,
                BankFrom = x.BankFrom,
                BankTo = x.BankTo,
                ErrorReason = x.ErrorReason,
                Amount = x.Amount
            })
            .ToListAsync(cancellationToken);

        var failedIds = failedTransactions.Select(x => x.Id).ToList();
        var processedCount = await storage.ProcessedTransactions
            .AsNoTracking()
            .CountAsync(x => x.CreatedDate >= windowStartUtc && x.CreatedDate < windowEndUtc, cancellationToken);

        var reports = await storage.FailedTransactionComplianceReports
            .AsNoTracking()
            .Where(x => x.WindowStart >= windowStartUtc && x.WindowEnd <= windowEndUtc)
            .OrderBy(x => x.WindowEnd)
            .Select(x => new
            {
                x.WindowEnd,
                x.HighRiskFindings,
                x.MediumRiskFindings,
                x.ReportJson
            })
            .ToListAsync(cancellationToken);

        var ruleAssignments = failedIds.Count == 0
            ? []
            : await storage.FailedTransactionRuleAssignments
                .AsNoTracking()
                .Where(x => failedIds.Contains(x.FailedTransactionId))
                .Select(x => new
                {
                    x.FailedTransactionId,
                    x.RuleIds
                })
                .ToListAsync(cancellationToken);

        var classifiedIds = ruleAssignments
            .Where(x => x.RuleIds.Any(HasValue))
            .Select(x => x.FailedTransactionId)
            .Distinct()
            .Count();

        var ingestionBatches = await storage.TransactionIngestionBatches
            .AsNoTracking()
            .Where(x => x.ReceivedAt >= windowStartUtc && x.ReceivedAt < windowEndUtc)
            .Select(x => new
            {
                x.Status,
                x.RejectedCount
            })
            .ToListAsync(cancellationToken);

        var latestReport = reports
            .OrderBy(x => x.WindowEnd)
            .Select(x => DeserializeReport(x.ReportJson))
            .Where(x => x is not null)
            .LastOrDefault();

        return new DigestSummary
        {
            TotalFailed = failedTransactions.Count,
            TotalProcessed = processedCount,
            HighRiskFindings = reports.Sum(x => x.HighRiskFindings),
            MediumRiskFindings = reports.Sum(x => x.MediumRiskFindings),
            ReportCount = reports.Count,
            ClassifiedFailedTransactions = classifiedIds,
            UnclassifiedFailedTransactions = Math.Max(0, failedTransactions.Count - classifiedIds),
            ClassificationCoveragePercent = failedTransactions.Count == 0 ? 100 : Divide(classifiedIds * 100m, failedTransactions.Count),
            IngestionSuccessBatches = ingestionBatches.Count(x => x.Status == "SUCCESS"),
            IngestionPartialBatches = ingestionBatches.Count(x => x.Status == "PARTIAL"),
            IngestionRejectedBatches = ingestionBatches.Count(x => x.Status == "REJECTED"),
            IngestionRejectedRows = ingestionBatches.Sum(x => x.RejectedCount),
            LatestNsiStatus = latestReport?.Summary.NsiReconciliation.Status ?? "NO_REPORT",
            TopErrorCodes = failedTransactions
                .Where(x => HasValue(x.ErrorReason))
                .GroupBy(x => x.ErrorReason!)
                .OrderByDescending(x => x.Count())
                .Take(5)
                .Select(x => new DigestDimension(x.Key, x.Count(), x.Sum(t => ParseAmount(t.Amount))))
                .ToList(),
            TopRuleIds = ruleAssignments
                .SelectMany(x => x.RuleIds.Where(HasValue))
                .GroupBy(x => x)
                .OrderByDescending(x => x.Count())
                .Take(5)
                .Select(x => new DigestDimension(x.Key, x.Count(), 0))
                .ToList(),
            TopRoutes = failedTransactions
                .GroupBy(x => $"{FirstNonEmpty(x.BankFrom, "UNKNOWN")} -> {FirstNonEmpty(x.BankTo, "UNKNOWN")}")
                .OrderByDescending(x => x.Count())
                .Take(5)
                .Select(x => new DigestDimension(x.Key, x.Count(), x.Sum(t => ParseAmount(t.Amount))))
                .ToList()
        };
    }

    private async Task<string> BuildHtmlBodyAsync(
        ITemplateRenderer templateRenderer,
        EmailConfig emailConfig,
        DigestSummary summary,
        DateTimeOffset windowStartLocal,
        DateTimeOffset windowEndLocal,
        TimeZoneInfo timeZone,
        CancellationToken cancellationToken)
    {
        var values = new Dictionary<string, string>
        {
            ["WindowStart"] = WebUtility.HtmlEncode(windowStartLocal.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)),
            ["WindowEnd"] = WebUtility.HtmlEncode(windowEndLocal.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)),
            ["TimeZone"] = WebUtility.HtmlEncode(timeZone.Id),
            ["TotalFailed"] = summary.TotalFailed.ToString("N0", CultureInfo.InvariantCulture),
            ["TotalProcessed"] = summary.TotalProcessed.ToString("N0", CultureInfo.InvariantCulture),
            ["ReportCount"] = summary.ReportCount.ToString("N0", CultureInfo.InvariantCulture),
            ["HighRiskFindings"] = summary.HighRiskFindings.ToString("N0", CultureInfo.InvariantCulture),
            ["MediumRiskFindings"] = summary.MediumRiskFindings.ToString("N0", CultureInfo.InvariantCulture),
            ["ClassificationCoveragePercent"] = summary.ClassificationCoveragePercent.ToString("0.##", CultureInfo.InvariantCulture),
            ["ClassifiedFailedTransactions"] = summary.ClassifiedFailedTransactions.ToString("N0", CultureInfo.InvariantCulture),
            ["UnclassifiedFailedTransactions"] = summary.UnclassifiedFailedTransactions.ToString("N0", CultureInfo.InvariantCulture),
            ["LatestNsiStatus"] = WebUtility.HtmlEncode(summary.LatestNsiStatus),
            ["IngestionSuccessBatches"] = summary.IngestionSuccessBatches.ToString("N0", CultureInfo.InvariantCulture),
            ["IngestionPartialBatches"] = summary.IngestionPartialBatches.ToString("N0", CultureInfo.InvariantCulture),
            ["IngestionRejectedBatches"] = summary.IngestionRejectedBatches.ToString("N0", CultureInfo.InvariantCulture),
            ["IngestionRejectedRows"] = summary.IngestionRejectedRows.ToString("N0", CultureInfo.InvariantCulture),
            ["TopErrorCodesRows"] = BuildDimensionRows(summary.TopErrorCodes),
            ["TopRuleIdsRows"] = BuildDimensionRows(summary.TopRuleIds),
            ["TopRoutesRows"] = BuildDimensionRows(summary.TopRoutes),
            ["PortalButton"] = BuildPortalButton()
        };

        return await templateRenderer.RenderAsync(
            emailConfig.TemplatesDirectory,
            "compliance-management-digest.html",
            values,
            cancellationToken);
    }

    private static string BuildDimensionRows(List<DigestDimension> rows)
    {
        if (rows.Count == 0)
        {
            return """<tr><td colspan="3" class="muted">No activity.</td></tr>""";
        }

        var builder = new StringBuilder();
        foreach (var row in rows)
        {
            builder.Append(
                CultureInfo.InvariantCulture,
                $"<tr><td>{WebUtility.HtmlEncode(row.Key)}</td><td>{row.Count:N0}</td><td>{row.TotalAmount:0.##}</td></tr>");
        }
        return builder.ToString();
    }

    private string BuildPortalButton()
    {
        if (string.IsNullOrWhiteSpace(_config.PortalUrl))
        {
            return string.Empty;
        }

        var url = WebUtility.HtmlEncode(_config.PortalUrl);
        return $"""<a class="button" href="{url}">Open Compliance Portal</a>""";
    }

    private DateTimeOffset GetNextRun(DateTimeOffset nowUtc, TimeZoneInfo timeZone)
    {
        var nowLocal = TimeZoneInfo.ConvertTime(nowUtc, timeZone);
        var targetDate = nowLocal.Date;
        var target = new DateTimeOffset(
            targetDate.Year,
            targetDate.Month,
            targetDate.Day,
            NormalizeHour(_config.SendHour),
            NormalizeMinute(_config.SendMinute),
            0,
            timeZone.GetUtcOffset(targetDate));

        if (target <= nowLocal)
        {
            target = target.AddDays(1);
        }

        return target.ToUniversalTime();
    }

    private static TimeZoneInfo ResolveTimeZone(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return TimeZoneInfo.Local;
        }

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.Local;
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.Local;
        }
    }

    private static FailedTransactionComplianceReportDto? DeserializeReport(string reportJson)
    {
        try
        {
            return JsonSerializer.Deserialize<FailedTransactionComplianceReportDto>(reportJson, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static int NormalizeHour(int hour) => Math.Clamp(hour, 0, 23);
    private static int NormalizeMinute(int minute) => Math.Clamp(minute, 0, 59);
    private static bool HasValue(string? value) => !string.IsNullOrWhiteSpace(value);
    private static string FirstNonEmpty(params string?[] values) => values.FirstOrDefault(HasValue) ?? "UNKNOWN";

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

    private sealed class DigestSummary
    {
        public int TotalFailed { get; init; }
        public int TotalProcessed { get; init; }
        public int HighRiskFindings { get; init; }
        public int MediumRiskFindings { get; init; }
        public int ReportCount { get; init; }
        public int ClassifiedFailedTransactions { get; init; }
        public int UnclassifiedFailedTransactions { get; init; }
        public decimal ClassificationCoveragePercent { get; init; }
        public int IngestionSuccessBatches { get; init; }
        public int IngestionPartialBatches { get; init; }
        public int IngestionRejectedBatches { get; init; }
        public int IngestionRejectedRows { get; init; }
        public string LatestNsiStatus { get; init; } = "NO_REPORT";
        public List<DigestDimension> TopErrorCodes { get; init; } = [];
        public List<DigestDimension> TopRuleIds { get; init; } = [];
        public List<DigestDimension> TopRoutes { get; init; } = [];
    }

    private sealed class DigestTransaction
    {
        public long Id { get; init; }
        public string? BankFrom { get; init; }
        public string? BankTo { get; init; }
        public string? ErrorReason { get; init; }
        public string? Amount { get; init; }
    }

    private sealed record DigestDimension(string Key, int Count, decimal TotalAmount);
}
