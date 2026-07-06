using SPSS.Domain.Models.DTOs;

namespace SPSS.App.Features.Balance.Commands;

[Authorize(Roles = CreateMonitHistory)]
public sealed record ProcessOracleFraudTransactionsWebhookCommand(OracleFraudTransactionsWebhookDto Data)
    : IRequest<Response<OracleFraudTransactionBatchResponseDto>>;

public sealed class ProcessOracleFraudTransactionsWebhookCommandHandler(
    IStorageBroker storage,
    ILogger<ProcessOracleFraudTransactionsWebhookCommandHandler> logger)
    : IRequestHandler<ProcessOracleFraudTransactionsWebhookCommand, Response<OracleFraudTransactionBatchResponseDto>>
{
    private readonly IStorageBroker _storage = storage;
    private readonly ILogger<ProcessOracleFraudTransactionsWebhookCommandHandler> _logger = logger;

    public async ValueTask<Response<OracleFraudTransactionBatchResponseDto>> Handle(
        ProcessOracleFraudTransactionsWebhookCommand request,
        CancellationToken cancellationToken)
    {
        var data = request.Data;
        var transactions = data.Transactions ?? [];
        var batchKey = BuildBatchKey(data);

        var validationErrors = ValidateBatch(data, transactions);
        if (validationErrors.Count > 0)
        {
            var message = string.Join("; ", validationErrors);
            _logger.LogWarning(
                "Oracle fraud transactions batch rejected. BatchKey={BatchKey}, Reason={Reason}",
                batchKey,
                message);

            return Response<OracleFraudTransactionBatchResponseDto>.BadRequest(
                message,
                new OracleFraudTransactionBatchResponseDto
                {
                    Status = "error",
                    BatchKey = batchKey,
                    Received = transactions.Count
                });
        }

        var source = data.Source.Trim();

        var existingBatch = await _storage.OracleFraudTransactionIngestionBatches
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Source == source &&
                    x.FromUtrnno == data.FromUtrnno &&
                    x.ToUtrnno == data.ToUtrnno &&
                    x.HighWatermarkUtrnno == data.HighWatermarkUtrnno,
                cancellationToken);

        if (existingBatch is not null)
        {
            _logger.LogInformation(
                "Duplicate Oracle fraud transactions batch received. BatchKey={BatchKey}, ExistingBatchId={BatchId}",
                batchKey,
                existingBatch.Id);

            return new Response<OracleFraudTransactionBatchResponseDto>(
                new OracleFraudTransactionBatchResponseDto
                {
                    Status = "already_processed",
                    BatchKey = batchKey,
                    Received = existingBatch.ReceivedCount,
                    Duplicates = existingBatch.DuplicateCount,
                    Inserted = existingBatch.InsertedCount,
                    Updated = existingBatch.UpdatedCount
                });
        }

        var receivedAt = DateTimeOffset.UtcNow;
        var duplicateCount = transactions.Count - transactions.Select(x => x.Utrnno).Distinct().Count();
        var uniqueTransactions = transactions
            .GroupBy(x => x.Utrnno)
            .Select(x => x.First())
            .ToList();
        var utrnnos = uniqueTransactions.Select(x => x.Utrnno).ToList();

        var existingTransactions = await _storage.OracleFraudTransactions
            .Where(x => x.Source == source && utrnnos.Contains(x.Utrnno))
            .ToDictionaryAsync(x => x.Utrnno, cancellationToken);

        var insertedCount = 0;
        var updatedCount = 0;

        foreach (var transaction in uniqueTransactions)
        {
            if (existingTransactions.TryGetValue(transaction.Utrnno, out var entity))
            {
                MapOracleFraudTransaction(entity, source, transaction, receivedAt);
                _storage.OracleFraudTransactions.Update(entity);
                updatedCount++;
            }
            else
            {
                await _storage.OracleFraudTransactions.AddAsync(
                    MapOracleFraudTransaction(new OracleFraudTransaction(), source, transaction, receivedAt),
                    cancellationToken);
                insertedCount++;
            }
        }

        await _storage.OracleFraudTransactionIngestionBatches.AddAsync(
            new OracleFraudTransactionIngestionBatch
            {
                Source = source,
                FromUtrnno = data.FromUtrnno,
                ToUtrnno = data.ToUtrnno,
                HighWatermarkUtrnno = data.HighWatermarkUtrnno,
                ProducerTimestamp = data.Timestamp,
                Status = "ACCEPTED",
                ReceivedAt = receivedAt,
                ProcessedAt = DateTimeOffset.UtcNow,
                TotalCount = data.TotalCount,
                ReceivedCount = transactions.Count,
                InsertedCount = insertedCount,
                UpdatedCount = updatedCount,
                DuplicateCount = duplicateCount,
                RejectedCount = 0
            },
            cancellationToken);

        await _storage.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Oracle fraud transactions batch accepted. BatchKey={BatchKey}, Received={Received}, Inserted={Inserted}, Updated={Updated}, Duplicates={Duplicates}",
            batchKey,
            transactions.Count,
            insertedCount,
            updatedCount,
            duplicateCount);

        return new Response<OracleFraudTransactionBatchResponseDto>(
            new OracleFraudTransactionBatchResponseDto
            {
                Status = "accepted",
                BatchKey = batchKey,
                Received = transactions.Count,
                Duplicates = duplicateCount,
                Inserted = insertedCount,
                Updated = updatedCount
            })
        {
            StatusCode = 202
        };
    }

    private static List<string> ValidateBatch(
        OracleFraudTransactionsWebhookDto data,
        List<OracleFraudTransactionWebhookRowDto> transactions)
    {
        var errors = new List<string>();

        if (data.Timestamp == default)
        {
            errors.Add("timestamp is required");
        }

        if (string.IsNullOrWhiteSpace(data.Source))
        {
            errors.Add("source is required");
        }

        if (data.ToUtrnno <= data.FromUtrnno)
        {
            errors.Add("to_utrnno must be greater than from_utrnno");
        }

        if (data.HighWatermarkUtrnno < data.ToUtrnno)
        {
            errors.Add("high_watermark_utrnno must be greater than or equal to to_utrnno");
        }

        if (data.TotalCount != transactions.Count)
        {
            errors.Add("total_count does not match transactions length");
        }

        foreach (var transaction in transactions)
        {
            AddTransactionValidationErrors(errors, transaction, data.FromUtrnno, data.ToUtrnno);
        }

        return errors;
    }

    private static void AddTransactionValidationErrors(
        List<string> errors,
        OracleFraudTransactionWebhookRowDto transaction,
        long fromUtrnno,
        long toUtrnno)
    {
        if (transaction.Utrnno <= 0)
        {
            errors.Add("transaction utrnno is required");
        }

        if (transaction.Utrnno <= fromUtrnno || transaction.Utrnno > toUtrnno)
        {
            errors.Add($"transaction utrnno {transaction.Utrnno} is outside the batch boundaries");
        }

        if (string.IsNullOrWhiteSpace(transaction.RespCode))
        {
            errors.Add($"transaction {transaction.Utrnno} respCode is required");
        }

        if (string.IsNullOrWhiteSpace(transaction.FraudZone))
        {
            errors.Add($"transaction {transaction.Utrnno} fraudZone is required");
        }

        if (string.IsNullOrWhiteSpace(transaction.Refnum))
        {
            errors.Add($"transaction {transaction.Utrnno} refnum is required");
        }
    }

    private static OracleFraudTransaction MapOracleFraudTransaction(
        OracleFraudTransaction entity,
        string source,
        OracleFraudTransactionWebhookRowDto transaction,
        DateTimeOffset receivedAt)
    {
        entity.Source = source;
        entity.Utrnno = transaction.Utrnno;
        entity.RespCode = transaction.RespCode!.Trim();
        entity.FraudZone = transaction.FraudZone!.Trim();
        entity.RulesList = string.IsNullOrWhiteSpace(transaction.RulesList)
            ? null
            : transaction.RulesList.Trim();
        entity.Rules = ParseRulesList(transaction.RulesList);
        entity.Refnum = transaction.Refnum!.Trim();
        entity.RiskValue = string.IsNullOrWhiteSpace(transaction.RiskValue)
            ? null
            : transaction.RiskValue.Trim();
        entity.ReceivedAt = receivedAt;

        return entity;
    }

    private static List<string> ParseRulesList(string? rulesList) =>
        string.IsNullOrWhiteSpace(rulesList)
            ? []
            : rulesList
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

    private static string BuildBatchKey(OracleFraudTransactionsWebhookDto data)
        => $"{data.Source}:{data.FromUtrnno}:{data.ToUtrnno}:{data.HighWatermarkUtrnno}";
}
