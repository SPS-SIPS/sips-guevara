using System.Text.Json;

namespace SPSS.App.Features.Balance.Services;

public interface IFailedTransactionComplianceClassificationRefresher
{
    Task RefreshByFailedTransactionIdsAsync(IEnumerable<long> failedTransactionIds, CancellationToken cancellationToken);
}

public sealed class FailedTransactionComplianceClassificationRefresher(IStorageBroker storage)
    : IFailedTransactionComplianceClassificationRefresher
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IStorageBroker _storage = storage;

    public async Task RefreshByFailedTransactionIdsAsync(
        IEnumerable<long> failedTransactionIds,
        CancellationToken cancellationToken)
    {
        var ids = failedTransactionIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return;
        }

        var transactionDates = await _storage.FailedTransactions
            .AsNoTracking()
            .Where(x => ids.Contains(x.Id) && x.CreatedDate.HasValue)
            .Select(x => x.CreatedDate!.Value)
            .ToListAsync(cancellationToken);

        if (transactionDates.Count == 0)
        {
            return;
        }

        var minDate = transactionDates.Min();
        var maxDate = transactionDates.Max();
        var reports = await _storage.FailedTransactionComplianceReports
            .Where(x => x.WindowStart <= maxDate && x.WindowEnd > minDate)
            .ToListAsync(cancellationToken);

        foreach (var reportEntity in reports)
        {
            var report = JsonSerializer.Deserialize<FailedTransactionComplianceReportDto>(
                reportEntity.ReportJson,
                JsonOptions);

            if (report is null)
            {
                continue;
            }

            await RefreshReportClassificationAsync(report, cancellationToken);
            report.Summary.ClassificationUpdatedAt = DateTimeOffset.UtcNow;
            reportEntity.ReportJson = JsonSerializer.Serialize(report, JsonOptions);
        }

        if (reports.Count > 0)
        {
            await _storage.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task RefreshReportClassificationAsync(
        FailedTransactionComplianceReportDto report,
        CancellationToken cancellationToken)
    {
        var transactions = await _storage.FailedTransactions
            .AsNoTracking()
            .Where(x => x.CreatedDate >= report.WindowStart && x.CreatedDate < report.WindowEnd)
            .Select(x => new
            {
                x.Id,
                x.Amount
            })
            .ToListAsync(cancellationToken);

        var transactionIds = transactions.Select(x => x.Id).ToList();
        var assignments = transactionIds.Count == 0
            ? new Dictionary<long, List<string>>()
            : await _storage.FailedTransactionRuleAssignments
                .AsNoTracking()
                .Where(x => transactionIds.Contains(x.FailedTransactionId))
                .ToDictionaryAsync(x => x.FailedTransactionId, x => NormalizeRuleIds(x.RuleIds), cancellationToken);

        var classifiedIds = assignments
            .Where(x => x.Value.Count > 0)
            .Select(x => x.Key)
            .ToHashSet();

        report.Summary.ClassifiedFailedTransactions = classifiedIds.Count;
        report.Summary.UnclassifiedFailedTransactions = Math.Max(0, transactions.Count - classifiedIds.Count);
        report.Summary.ClassificationCoveragePercent = transactions.Count == 0
            ? 100
            : Divide(classifiedIds.Count * 100m, transactions.Count);
        report.Summary.TopRuleIds = transactions
            .SelectMany(x => assignments.TryGetValue(x.Id, out var ruleIds)
                ? ruleIds.Select(ruleId => new { RuleId = ruleId, x.Amount })
                : [])
            .GroupBy(x => x.RuleId)
            .OrderByDescending(x => x.Count())
            .ThenByDescending(x => x.Sum(t => ParseAmount(t.Amount)))
            .Take(10)
            .Select(x => new FailedTransactionComplianceDimensionDto
            {
                Key = x.Key,
                Name = x.Key,
                CurrentCount = x.Count(),
                TotalAmount = x.Sum(t => ParseAmount(t.Amount))
            })
            .ToList();

        foreach (var finding in report.Findings)
        {
            var ruleIds = finding.SampleTransactionIds
                .Where(assignments.ContainsKey)
                .SelectMany(x => assignments[x]);

            finding.RuleIds = NormalizeRuleIds(ruleIds);
        }
    }

    private static List<string> NormalizeRuleIds(IEnumerable<string>? ruleIds)
    {
        return ruleIds?
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order()
            .ToList() ?? [];
    }

    private static decimal ParseAmount(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }

        var clean = value.Replace(",", string.Empty, StringComparison.Ordinal).Trim();
        return decimal.TryParse(clean, System.Globalization.NumberStyles.Number | System.Globalization.NumberStyles.AllowLeadingSign, System.Globalization.CultureInfo.InvariantCulture, out var amount)
            ? Math.Abs(amount)
            : 0;
    }

    private static decimal Divide(decimal value, decimal divisor)
    {
        return divisor == 0 ? 0 : Math.Round(value / divisor, 2, MidpointRounding.AwayFromZero);
    }
}
