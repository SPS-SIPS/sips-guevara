using SPSS.Domain.Models.BL;

namespace SPSS.Infrastructure.Services;

public sealed class OracleFailedTransactionAnalyticsService(
    IStorageBroker storage,
    OracleFailedTransactionAnalyticsConfig config,
    ILogger<OracleFailedTransactionAnalyticsService> logger) : IOracleFailedTransactionAnalyticsService
{
    private readonly IStorageBroker _storage = storage;
    private readonly OracleFailedTransactionAnalyticsConfig _config = config;
    private readonly ILogger<OracleFailedTransactionAnalyticsService> _logger = logger;

    public async Task<long> GenerateAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var failureStatuses = _config.FailureStatuses
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim().ToUpperInvariant())
            .Distinct()
            .ToList();

        if (failureStatuses.Count == 0)
        {
            var failedRun = new OracleFailedTransactionAnalyticsRun
            {
                WindowStart = _config.FirstWindowStartAt,
                WindowEnd = _config.FirstWindowStartAt.AddDays(1),
                GeneratedAt = now,
                Status = "FAILED",
                ErrorMessage = "No failure statuses configured."
            };

            await _storage.OracleFailedTransactionAnalyticsRuns.AddAsync(failedRun, cancellationToken);
            await _storage.SaveChangesAsync(cancellationToken);
            return failedRun.Id;
        }

        var firstWindowStart = _config.FirstWindowStartAt;
        var refreshStart = AlignToWindow(firstWindowStart, now.AddDays(-Math.Max(1, _config.LookbackDays)));
        var nextWindowStart = refreshStart < firstWindowStart ? firstWindowStart : refreshStart;
        long lastRunId = 0;

        while (nextWindowStart.AddDays(1) <= now)
        {
            lastRunId = await GenerateWindowAsync(nextWindowStart, nextWindowStart.AddDays(1), failureStatuses, now, cancellationToken);
            nextWindowStart = nextWindowStart.AddDays(1);
        }

        await DeleteExpiredRunsAsync(now, cancellationToken);
        await _storage.SaveChangesAsync(cancellationToken);

        if (lastRunId == 0)
        {
            lastRunId = await _storage.OracleFailedTransactionAnalyticsRuns
                .Where(x => x.Status == "SUCCESS")
                .OrderByDescending(x => x.WindowEnd)
                .Select(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return lastRunId;
    }

    private async Task<long> GenerateWindowAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        List<string> failureStatuses,
        DateTimeOffset generatedAt,
        CancellationToken cancellationToken)
    {
        await DeleteWindowAsync(from, to, cancellationToken);

        var run = new OracleFailedTransactionAnalyticsRun
        {
            WindowStart = from,
            WindowEnd = to,
            GeneratedAt = generatedAt,
            Status = "RUNNING"
        };

        await _storage.OracleFailedTransactionAnalyticsRuns.AddAsync(run, cancellationToken);
        await _storage.SaveChangesAsync(cancellationToken);

        try
        {
            var rows = await _storage.OracleTransactions
                .AsNoTracking()
                .Where(x => (x.CreatedAt ?? x.ReceivedAt) >= from && (x.CreatedAt ?? x.ReceivedAt) < to)
                .Where(x =>
                    failureStatuses.Contains(x.Status.ToUpper()) ||
                    (x.TxSts != null && failureStatuses.Contains(x.TxSts.ToUpper())))
                .Select(x => new AnalyticsSourceRow
                {
                    SendingInstitution = x.BankFrom,
                    ReceivingInstitution = x.BankTo,
                    TransactionAt = x.CreatedAt ?? x.ReceivedAt,
                    Amount = x.AmountValue ?? x.IntrBkSttlmAtmValue ?? 0,
                    DebtorAccount = x.DbtrAcctId,
                    FailureCode = x.Error ?? x.StsRsnInfRsn ?? x.TxSts ?? x.Status,
                    FailureReason = x.ErrorReason ?? x.ErrDesc ?? x.StsRsnInfAddtlInf ?? x.Description ?? x.Error ?? x.Status
                })
                .ToListAsync(cancellationToken);

            run.TotalTransactions = rows.Count;
            run.TotalAmount = rows.Sum(x => x.Amount);
            run.UniqueSendingInstitutions = rows.Select(x => x.SendingInstitution).Where(HasValue).Distinct().Count();
            run.UniqueReceivingInstitutions = rows.Select(x => x.ReceivingInstitution).Where(HasValue).Distinct().Count();
            run.UniqueAccounts = rows.Select(x => x.DebtorAccount).Where(HasValue).Distinct().Count();

            if (rows.Count > 0)
            {
                await _storage.OracleFailedInstitutionAnalytics.AddRangeAsync(BuildInstitutionAnalytics(run.Id, rows), cancellationToken);
                await _storage.OracleFailedAccountAnalytics.AddRangeAsync(BuildAccountAnalytics(run.Id, rows), cancellationToken);
                await _storage.OracleFailedRejectionTypeAnalytics.AddRangeAsync(BuildRejectionAnalytics(run.Id, rows), cancellationToken);
                await _storage.OracleFailedTrendAnalytics.AddRangeAsync(BuildTrendAnalytics(run.Id, rows), cancellationToken);
            }

            run.Status = "SUCCESS";
            await _storage.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Oracle failed transaction analytics generated. RunId={RunId}, Window={From} to {To}, Rows={Rows}, Amount={Amount}",
                run.Id,
                from,
                to,
                run.TotalTransactions,
                run.TotalAmount);

            return run.Id;
        }
        catch (Exception ex)
        {
            run.Status = "FAILED";
            run.ErrorMessage = ex.Message;
            await _storage.SaveChangesAsync(cancellationToken);
            _logger.LogError(ex, "Oracle failed transaction analytics generation failed. RunId={RunId}", run.Id);
            return run.Id;
        }
    }

    private async Task DeleteWindowAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken)
    {
        var existingRunIds = await _storage.OracleFailedTransactionAnalyticsRuns
            .Where(x => x.WindowStart == from && x.WindowEnd == to)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (existingRunIds.Count == 0)
        {
            return;
        }

        await DeleteRunsAsync(existingRunIds, cancellationToken);
    }

    private async Task DeleteExpiredRunsAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var cutoff = now.AddDays(-Math.Max(1, _config.RetentionDays));
        var expiredRunIds = await _storage.OracleFailedTransactionAnalyticsRuns
            .Where(x => x.GeneratedAt < cutoff)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        await DeleteRunsAsync(expiredRunIds, cancellationToken);
    }

    private async Task DeleteRunsAsync(List<long> runIds, CancellationToken cancellationToken)
    {
        if (runIds.Count == 0)
        {
            return;
        }

        await _storage.OracleFailedInstitutionAnalytics.Where(x => runIds.Contains(x.RunId)).ExecuteDeleteAsync(cancellationToken);
        await _storage.OracleFailedAccountAnalytics.Where(x => runIds.Contains(x.RunId)).ExecuteDeleteAsync(cancellationToken);
        await _storage.OracleFailedRejectionTypeAnalytics.Where(x => runIds.Contains(x.RunId)).ExecuteDeleteAsync(cancellationToken);
        await _storage.OracleFailedTrendAnalytics.Where(x => runIds.Contains(x.RunId)).ExecuteDeleteAsync(cancellationToken);
        await _storage.OracleFailedTransactionAnalyticsRuns.Where(x => runIds.Contains(x.Id)).ExecuteDeleteAsync(cancellationToken);
    }

    private static DateTimeOffset AlignToWindow(DateTimeOffset firstWindowStart, DateTimeOffset candidate)
    {
        if (candidate <= firstWindowStart)
        {
            return firstWindowStart;
        }

        var elapsedDays = Math.Floor((candidate - firstWindowStart).TotalDays);
        return firstWindowStart.AddDays(elapsedDays);
    }

    private static IEnumerable<OracleFailedInstitutionAnalytic> BuildInstitutionAnalytics(long runId, List<AnalyticsSourceRow> rows)
        => rows
            .GroupBy(x => new { x.SendingInstitution, x.ReceivingInstitution })
            .Select(group =>
            {
                var topFailure = TopFailure(group);
                return new OracleFailedInstitutionAnalytic
                {
                    RunId = runId,
                    SendingInstitution = group.Key.SendingInstitution,
                    ReceivingInstitution = group.Key.ReceivingInstitution,
                    TransactionCount = group.Count(),
                    TotalAmount = group.Sum(x => x.Amount),
                    DistinctDebtorAccounts = group.Select(x => x.DebtorAccount).Where(HasValue).Distinct().Count(),
                    TopFailureCode = topFailure.Code,
                    TopFailureReason = topFailure.Reason,
                    FirstTransactionAt = group.Min(x => x.TransactionAt),
                    LastTransactionAt = group.Max(x => x.TransactionAt)
                };
            });

    private static IEnumerable<OracleFailedAccountAnalytic> BuildAccountAnalytics(long runId, List<AnalyticsSourceRow> rows)
        => rows
            .Where(x => HasValue(x.DebtorAccount))
            .GroupBy(x => new { x.SendingInstitution, x.ReceivingInstitution, AccountId = x.DebtorAccount! })
            .Select(group =>
            {
                var topFailure = TopFailure(group);
                return new OracleFailedAccountAnalytic
                {
                    RunId = runId,
                    InstitutionBic = group.Key.SendingInstitution,
                    ReceivingInstitution = group.Key.ReceivingInstitution,
                    AccountId = group.Key.AccountId,
                    TransactionCount = group.Count(),
                    TotalAmount = group.Sum(x => x.Amount),
                    TopFailureCode = topFailure.Code,
                    TopFailureReason = topFailure.Reason,
                    FirstTransactionAt = group.Min(x => x.TransactionAt),
                    LastTransactionAt = group.Max(x => x.TransactionAt)
                };
            });

    private static IEnumerable<OracleFailedRejectionTypeAnalytic> BuildRejectionAnalytics(long runId, List<AnalyticsSourceRow> rows)
        => rows
            .GroupBy(x => new { x.SendingInstitution, x.ReceivingInstitution, x.FailureCode, x.FailureReason })
            .Select(group => new OracleFailedRejectionTypeAnalytic
            {
                RunId = runId,
                SendingInstitution = group.Key.SendingInstitution,
                ReceivingInstitution = group.Key.ReceivingInstitution,
                FailureCode = group.Key.FailureCode,
                FailureReason = group.Key.FailureReason,
                TransactionCount = group.Count(),
                TotalAmount = group.Sum(x => x.Amount)
            });

    private static IEnumerable<OracleFailedTrendAnalytic> BuildTrendAnalytics(long runId, List<AnalyticsSourceRow> rows)
        => rows
            .GroupBy(x => new
            {
                BucketStart = new DateTimeOffset(x.TransactionAt.UtcDateTime.Date, TimeSpan.Zero),
                x.SendingInstitution,
                x.ReceivingInstitution,
                x.FailureCode
            })
            .Select(group => new OracleFailedTrendAnalytic
            {
                RunId = runId,
                BucketStart = group.Key.BucketStart,
                BucketType = "DAY",
                SendingInstitution = group.Key.SendingInstitution,
                ReceivingInstitution = group.Key.ReceivingInstitution,
                FailureCode = group.Key.FailureCode,
                TransactionCount = group.Count(),
                TotalAmount = group.Sum(x => x.Amount)
            });

    private static (string Code, string Reason) TopFailure(IEnumerable<AnalyticsSourceRow> rows)
        => rows
            .GroupBy(x => new { x.FailureCode, x.FailureReason })
            .OrderByDescending(x => x.Count())
            .Select(x => (x.Key.FailureCode, x.Key.FailureReason))
            .FirstOrDefault();

    private static bool HasValue(string? value) => !string.IsNullOrWhiteSpace(value);

    private sealed class AnalyticsSourceRow
    {
        public string SendingInstitution { get; set; } = string.Empty;
        public string ReceivingInstitution { get; set; } = string.Empty;
        public DateTimeOffset TransactionAt { get; set; }
        public decimal Amount { get; set; }
        public string? DebtorAccount { get; set; }
        public string FailureCode { get; set; } = string.Empty;
        public string FailureReason { get; set; } = string.Empty;
    }
}
