using CsvHelper.Configuration.Attributes;
using SPSS.Domain.Models.BL;
using SPSS.Infrastructure.Helpers;

namespace SPSS.Infrastructure.Services;

public sealed class OracleFailedTransactionNotificationService(
    IStorageBroker storage,
    IEmailSender emailSender,
    EmailConfig emailConfig,
    OracleFailedTransactionNotificationConfig config,
    ILogger<OracleFailedTransactionNotificationService> logger) : IOracleFailedTransactionNotificationService
{
    private const string LedgerBatchType = "ORACLE_FAILED_TRANSACTION_NOTIFICATIONS";
    private const string TemplateFileName = "oracle-failed-transactions.html";

    private readonly IStorageBroker _storage = storage;
    private readonly IEmailSender _emailSender = emailSender;
    private readonly EmailConfig _emailConfig = emailConfig;
    private readonly OracleFailedTransactionNotificationConfig _config = config;
    private readonly ILogger<OracleFailedTransactionNotificationService> _logger = logger;

    public async Task NotifyNextWindowAsync(DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var lastCompletedWindowEnd = await _storage.TransactionIngestionBatches
            .Where(x => x.BatchType == LedgerBatchType)
            .Where(x => x.Status == "SUCCESS" || x.Status == "EMPTY" || x.Status == "PARTIAL")
            .OrderByDescending(x => x.ToDate)
            .Select(x => (DateTimeOffset?)x.ToDate)
            .FirstOrDefaultAsync(cancellationToken);

        var from = lastCompletedWindowEnd ?? _config.FirstWindowStartAt;
        var to = from.AddHours(Math.Max(1, _config.LookbackHours));

        if (now < to)
        {
            _logger.LogInformation(
                "Oracle failed transaction notification skipped because next report window is not complete. Window={From} to {To}, Now={Now}",
                from,
                to,
                now);
            return;
        }

        var alreadyRecorded = await _storage.TransactionIngestionBatches
            .AnyAsync(x => x.BatchType == LedgerBatchType && x.FromDate == from && x.ToDate == to, cancellationToken);

        if (alreadyRecorded)
        {
            _logger.LogInformation(
                "Oracle failed transaction notification window already recorded. Window={From} to {To}",
                from,
                to);
            return;
        }

        await NotifyAsync(from, to, cancellationToken);
    }

    public async Task NotifyAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
        => await NotifyAsync(from, to, participantBic: null, recordWindow: true, applyFirstWindowGuard: true, cancellationToken);

    public async Task<OracleFailedTransactionNotificationResult> NotifyInstitutionAsync(string bic, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
        => await NotifyAsync(from, to, bic, recordWindow: false, applyFirstWindowGuard: false, cancellationToken);

    private async Task<OracleFailedTransactionNotificationResult> NotifyAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        string? participantBic,
        bool recordWindow,
        bool applyFirstWindowGuard,
        CancellationToken cancellationToken = default)
    {
        var result = new OracleFailedTransactionNotificationResult
        {
            Bic = participantBic ?? string.Empty,
            From = from,
            To = to
        };

        if (from >= to)
        {
            throw new ArgumentException("Report window start must be before end.");
        }

        var failureStatuses = _config.FailureStatuses
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim().ToUpperInvariant())
            .Distinct()
            .ToList();

        if (failureStatuses.Count == 0)
        {
            _logger.LogWarning("Oracle failed transaction notification skipped because no failure statuses are configured.");
            result.Message = "No failure statuses are configured.";
            return result;
        }

        if (applyFirstWindowGuard && from < _config.FirstWindowStartAt)
        {
            from = _config.FirstWindowStartAt;
            result.From = from;
        }

        var transactions = await _storage.OracleTransactions
            .Where(t => (t.CreatedAt ?? t.ReceivedAt) >= from && (t.CreatedAt ?? t.ReceivedAt) < to)
            .Where(t =>
                failureStatuses.Contains(t.Status.ToUpper()) ||
                (t.TxSts != null && failureStatuses.Contains(t.TxSts.ToUpper())))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        if (transactions.Count == 0)
        {
            _logger.LogInformation(
                "No Oracle failed transactions found for notification window {From} to {To}.",
                from,
                to);
            if (recordWindow)
            {
                await RecordWindowAsync(from, to, "EMPTY", 0, 0, 0, null, cancellationToken);
            }
            result.Message = "No failed Oracle transactions found for the selected window.";
            return result;
        }

        result.SourceRows = transactions.Count;

        var firstTransactionTime = transactions.Min(TransactionTime);
        var lastTransactionTime = transactions.Max(TransactionTime);

        var participantBics = transactions
            .SelectMany(t => new[] { t.BankFrom, t.BankTo })
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(NormalizeBic)
            .Distinct()
            .ToList();

        if (!string.IsNullOrWhiteSpace(participantBic))
        {
            var selectedBic = NormalizeBic(participantBic.Trim());
            result.Bic = selectedBic;
            participantBics = participantBics
                .Where(x => string.Equals(x, selectedBic, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (participantBics.Count == 0)
            {
                _logger.LogInformation(
                    "No Oracle failed transactions found for selected BIC {BIC} in notification window {From} to {To}.",
                    selectedBic,
                    from,
                    to);
                result.Bic = selectedBic;
                result.SourceRows = 0;
                result.Message = $"No failed Oracle transactions found for selected BIC {selectedBic} in the selected window.";
                return result;
            }
        }

        var participants = await _storage.Participants
            .Where(p => p.Active && p.DeletedOn == null && p.Email != null)
            .Select(p => new { p.Name, p.Email, p.BicNo })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var sentCount = 0;
        var skipped = new List<string>();

        foreach (var bic in participantBics)
        {
            var participant = participants.FirstOrDefault(p => NormalizeBic(p.BicNo) == bic);
            if (participant == null || string.IsNullOrWhiteSpace(participant.Email))
            {
                skipped.Add(bic);
                _logger.LogInformation("Skipping Oracle failed transaction report for BIC {BIC} due to missing participant/email.", bic);
                continue;
            }

            var rows = transactions
                .Where(t => MatchesBic(t.BankFrom, bic) || MatchesBic(t.BankTo, bic))
                .OrderBy(t => t.CreatedAt ?? t.AcceptanceAt ?? t.ReceivedAt)
                .ThenBy(t => t.TxId)
                .Select(ToCsvRow)
                .ToList();

            if (rows.Count == 0)
            {
                continue;
            }

            var fileName = $"Failed_Transactions_{from:yyyyMMdd}_{to:yyyyMMdd}_{bic}.csv";
            var tempPath = Path.Combine(Path.GetTempPath(), fileName);

            try
            {
                await using (var memoryStream = await SPSS.Domain.Common.CSVHelper.WriteRecordsToCsv(rows))
                await using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    memoryStream.Position = 0;
                    await memoryStream.CopyToAsync(fs, cancellationToken);
                }

                var subject = $"Daily Failed Transactions Report for {bic} - {to:yyyy-MM-dd}";
                var htmlBody = await TemplateRenderer.RenderAsync(_emailConfig.TemplatesDirectory, TemplateFileName, new Dictionary<string, string>
                {
                    ["ParticipantName"] = participant.Name,
                    ["BIC"] = bic,
                    ["From"] = from.ToString("yyyy-MM-dd HH:mm:ss 'UTC'"),
                    ["To"] = to.ToString("yyyy-MM-dd HH:mm:ss 'UTC'"),
                    ["WindowDescription"] = $"24-hour transaction window from {from:yyyy-MM-dd HH:mm:ss 'UTC'} to {to:yyyy-MM-dd HH:mm:ss 'UTC'}",
                    ["FirstTransactionTime"] = firstTransactionTime.ToString("yyyy-MM-dd HH:mm:ss 'UTC'"),
                    ["LastTransactionTime"] = lastTransactionTime.ToString("yyyy-MM-dd HH:mm:ss 'UTC'"),
                    ["ReportDate"] = to.ToString("yyyy-MM-dd"),
                    ["TransactionCount"] = rows.Count.ToString(),
                    ["TotalRejectedAmount"] = rows.Sum(r => r.Amount).ToString("0.##"),
                    ["AttachmentName"] = fileName
                }, cancellationToken);

                await _emailSender.SendEmailAsync(participant.Email!, subject, htmlBody, tempPath, isHTML: true);
                sentCount++;
                result.SentCount++;

                _logger.LogInformation(
                    "Sent Oracle failed transaction report to {Email} for BIC {BIC}. Rows={Rows}",
                    participant.Email,
                    bic,
                    rows.Count);
            }
            catch (Exception ex)
            {
                skipped.Add(bic);
                result.SkippedCount++;
                _logger.LogError(ex, "Failed to send Oracle failed transaction report for BIC {BIC}.", bic);
            }
            finally
            {
                TryDelete(tempPath);
            }
        }

        _logger.LogInformation(
            "Oracle failed transaction notification summary: window={From} to {To}, sourceRows={Rows}, sent={Sent}, skipped={SkippedCount}, skippedBICs=[{Skipped}]",
            from,
            to,
            transactions.Count,
            sentCount,
            skipped.Count,
            string.Join(",", skipped));

        result.SourceRows = participantBic is null
            ? transactions.Count
            : transactions.Count(t => MatchesBic(t.BankFrom, result.Bic) || MatchesBic(t.BankTo, result.Bic));
        result.SkippedCount = Math.Max(result.SkippedCount, skipped.Count);
        result.Message = result.Sent
            ? $"Sent failed transaction notification for {result.Bic} with {result.SourceRows} transactions."
            : skipped.Count > 0
                ? $"Failed transaction notification was not sent for {result.Bic}. Skipped BICs: {string.Join(",", skipped)}"
                : $"No failed transaction notification was sent for {result.Bic}.";

        if (recordWindow)
        {
            await RecordWindowAsync(
                from,
                to,
                skipped.Count == 0 ? "SUCCESS" : "PARTIAL",
                transactions.Count,
                sentCount,
                skipped.Count,
                skipped.Count > 0 ? $"Skipped BICs: {string.Join(",", skipped)}" : null,
                cancellationToken);
        }

        return result;
    }

    private static OracleFailedTransactionCsvRow ToCsvRow(OracleTransaction transaction)
        => new()
        {
            TransactionId = transaction.TxId,
            DateTime = TransactionTime(transaction).ToString("yyyy-MM-dd HH:mm:ss 'UTC'"),
            SendingInstitution = transaction.BankFrom,
            ReceivingInstitution = transaction.BankTo,
            SenderAccount = transaction.DbtrAcctId ?? string.Empty,
            ReceiverAccount = transaction.CdtrAcctId ?? string.Empty,
            Amount = transaction.AmountValue ?? transaction.IntrBkSttlmAtmValue ?? 0,
            FailureCode = FirstValue(transaction.Error, transaction.StsRsnInfRsn, transaction.TxSts, transaction.Status),
            FailureReason = FirstValue(transaction.ErrorReason, transaction.ErrDesc, transaction.StsRsnInfAddtlInf, transaction.Description)
        };

    private static string FirstValue(params string?[] values)
        => values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? string.Empty;

    private async Task RecordWindowAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        string status,
        int sourceRows,
        int sentCount,
        int skippedCount,
        string? errorMessage,
        CancellationToken cancellationToken)
    {
        await _storage.TransactionIngestionBatches.AddAsync(
            new TransactionIngestionBatch
            {
                BatchType = LedgerBatchType,
                Status = status,
                FromDate = from,
                ToDate = to,
                ReceivedAt = DateTimeOffset.UtcNow,
                ProcessedAt = DateTimeOffset.UtcNow,
                SnapshotType = "ORACLE_TRANSACTIONS",
                SnapshotStatus = string.Join(",", _config.FailureStatuses),
                TotalCount = sourceRows,
                ReceivedCount = sourceRows,
                UniqueCount = sourceRows,
                InsertedCount = sentCount,
                UpdatedCount = 0,
                DuplicateCount = 0,
                RejectedCount = skippedCount,
                ErrorMessage = errorMessage,
                SampleErrors = errorMessage is null ? [] : [errorMessage]
            },
            cancellationToken);

        await _storage.SaveChangesAsync(cancellationToken);
    }

    private static DateTimeOffset TransactionTime(OracleTransaction transaction)
        => transaction.CreatedAt ?? transaction.AcceptanceAt ?? transaction.ReceivedAt;

    private static string NormalizeBic(string bic)
        => bic.Length >= 8 ? bic[..8] : bic;

    private static bool MatchesBic(string bicValue, string bic8)
        => !string.IsNullOrWhiteSpace(bicValue) && bicValue.StartsWith(bic8, StringComparison.OrdinalIgnoreCase);

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Best effort cleanup only.
        }
    }
}

sealed class OracleFailedTransactionCsvRow
{
    [Name("Transactions Id")]
    public string TransactionId { get; set; } = string.Empty;

    [Name("Date time")]
    public string DateTime { get; set; } = string.Empty;

    [Name("Sending Institution")]
    public string SendingInstitution { get; set; } = string.Empty;

    [Name("Receiving Institution")]
    public string ReceivingInstitution { get; set; } = string.Empty;

    [Name("Sender Account")]
    public string SenderAccount { get; set; } = string.Empty;

    [Name("Receiver Account")]
    public string ReceiverAccount { get; set; } = string.Empty;

    [Name("Amount")]
    public decimal Amount { get; set; }

    [Name("Failure Code")]
    public string FailureCode { get; set; } = string.Empty;

    [Name("Failure Reason")]
    public string FailureReason { get; set; } = string.Empty;
}
