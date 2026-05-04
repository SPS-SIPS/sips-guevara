using SPSS.Domain.Models.DTOs;

namespace SPSS.App.Features.Balance.Commands;

public sealed record ProcessProcessedTransactionsWebhookCommand(ProcessedTransactionsWebhookDto Data) : IRequest<Response<string>>;

public sealed class ProcessProcessedTransactionsWebhookCommandHandler(
    IStorageBroker storage,
    ILogger<ProcessProcessedTransactionsWebhookCommandHandler> logger)
    : IRequestHandler<ProcessProcessedTransactionsWebhookCommand, Response<string>>
{
    private readonly IStorageBroker _storage = storage;
    private readonly ILogger<ProcessProcessedTransactionsWebhookCommandHandler> _logger = logger;

    public async ValueTask<Response<string>> Handle(ProcessProcessedTransactionsWebhookCommand request, CancellationToken cancellationToken)
    {
        var data = request.Data;
        var transactions = data.Transactions ?? [];

        if (data.FromDate >= data.ToDate)
        {
            _logger.LogWarning(
                "Processed transactions webhook received an invalid window. FromDate={FromDate}, ToDate={ToDate}",
                data.FromDate,
                data.ToDate);

            return Response<string>.BadRequest("Invalid processed transaction snapshot window");
        }

        _logger.LogInformation(
            "Processed transactions webhook received at {Timestamp}. Window={FromDate} to {ToDate}, Type={Type}, Status={Status}, TotalCount={TotalCount}, ReceivedCount={ReceivedCount}",
            data.Timestamp,
            data.FromDate,
            data.ToDate,
            data.Type,
            data.Status,
            data.TotalCount,
            transactions.Count);

        var uniqueTransactions = transactions
            .GroupBy(TransactionIdentity)
            .Select(x => x.OrderByDescending(t => t.CreatedDate).First())
            .ToList();
        var duplicateCount = transactions.Count - uniqueTransactions.Count;
        var txIds = uniqueTransactions.Select(x => x.TxId).Where(HasValue).Distinct().ToList();
        var ipsTransactionIds = uniqueTransactions.Select(x => x.Id).ToList();
        var existingTransactions = uniqueTransactions.Count == 0
            ? new Dictionary<long, ProcessedTransaction>()
            : await _storage.ProcessedTransactions
                .Where(x => ipsTransactionIds.Contains(x.Id) || (x.TxId != null && txIds.Contains(x.TxId)))
                .ToDictionaryAsync(x => x.Id, cancellationToken);
        var existingByTxId = existingTransactions.Values
            .Where(x => HasValue(x.TxId))
            .GroupBy(x => x.TxId!)
            .ToDictionary(x => x.Key, x => x.First());

        var insertedCount = 0;
        var updatedCount = 0;
        var invalidCount = 0;
        var invalidRows = new List<string>();
        var receivedAt = DateTimeOffset.UtcNow;

        foreach (var transaction in uniqueTransactions)
        {
            var validationErrors = ValidateTransaction(transaction);
            if (validationErrors.Count > 0)
            {
                invalidCount++;
                var reason = string.Join("; ", validationErrors);
                invalidRows.Add($"Id={transaction.Id}, TxId={transaction.TxId ?? "-"}: {reason}");
                _logger.LogError(
                    "Processed transaction webhook row rejected before persistence. Id={Id}, TxId={TxId}, Reason={Reason}",
                    transaction.Id,
                    transaction.TxId,
                    reason);
                continue;
            }

            ProcessedTransaction? entity = null;
            if (HasValue(transaction.TxId))
            {
                existingByTxId.TryGetValue(transaction.TxId!, out entity);
            }

            entity ??= existingTransactions.GetValueOrDefault(transaction.Id);

            if (entity is not null)
            {
                MapProcessedTransaction(entity, transaction, receivedAt, setIdentity: false);
                _storage.ProcessedTransactions.Update(entity);
                updatedCount++;
            }
            else
            {
                await _storage.ProcessedTransactions.AddAsync(
                    MapProcessedTransaction(new ProcessedTransaction(), transaction, receivedAt, setIdentity: true),
                    cancellationToken);
                insertedCount++;
            }
        }

        var batchStatus = BuildBatchStatus(insertedCount + updatedCount, invalidCount);
        await _storage.TransactionIngestionBatches.AddAsync(
            new TransactionIngestionBatch
            {
                BatchType = "PROCESSED_TRANSACTIONS",
                Status = batchStatus,
                FromDate = data.FromDate,
                ToDate = data.ToDate,
                ReceivedAt = data.Timestamp == default ? DateTimeOffset.UtcNow : data.Timestamp,
                ProcessedAt = DateTimeOffset.UtcNow,
                SnapshotType = data.Type,
                SnapshotStatus = data.Status,
                TotalCount = data.TotalCount,
                ReceivedCount = transactions.Count,
                UniqueCount = uniqueTransactions.Count,
                InsertedCount = insertedCount,
                UpdatedCount = updatedCount,
                DuplicateCount = duplicateCount,
                RejectedCount = invalidCount,
                ErrorMessage = invalidCount > 0 ? "One or more rows were rejected before persistence" : null,
                SampleErrors = invalidRows.Take(10).ToList()
            },
            cancellationToken);

        await _storage.SaveChangesAsync(cancellationToken);

        var summary = $"Processed {uniqueTransactions.Count} processed transactions, inserted {insertedCount}, updated {updatedCount}, rejected {invalidCount} invalid rows, ignored {duplicateCount} duplicate retries";
        if (invalidCount > 0)
        {
            _logger.LogWarning(
                "Processed transactions webhook accepted with rejected rows. Inserted={Inserted}, Updated={Updated}, Rejected={Rejected}, SampleErrors={SampleErrors}",
                insertedCount,
                updatedCount,
                invalidCount,
                string.Join(" | ", invalidRows.Take(5)));
        }

        return new Response<string>(summary);
    }

    private static string BuildBatchStatus(int persistedCount, int rejectedCount)
    {
        if (rejectedCount == 0)
        {
            return "SUCCESS";
        }

        return persistedCount == 0 ? "REJECTED" : "PARTIAL";
    }

    private static string TransactionIdentity(ProcessedTransactionDto transaction)
        => HasValue(transaction.TxId)
            ? $"txid:{transaction.TxId}"
            : $"ips-id:{transaction.Id}";

    private static bool HasValue(string? value) => !string.IsNullOrWhiteSpace(value);

    private static ProcessedTransaction MapProcessedTransaction(
        ProcessedTransaction entity,
        ProcessedTransactionDto transaction,
        DateTimeOffset receivedAt,
        bool setIdentity)
    {
        if (setIdentity)
        {
            entity.Id = transaction.Id;
        }

        entity.BankFrom = transaction.BankFrom;
        entity.BankFromName = transaction.BankFromName;
        entity.BankTo = transaction.BankTo;
        entity.BankToName = transaction.BankToName;
        entity.CreatedDate = transaction.CreatedDate;
        entity.ModifiedDate = transaction.ModifiedDate;
        entity.AcceptanceDate = transaction.AcceptanceDate;
        entity.TxId = transaction.TxId;
        entity.Type = transaction.Type;
        entity.Amount = transaction.Amount;
        entity.Status = transaction.Status;
        entity.Description = transaction.Description;
        entity.SttlId = transaction.SttlId;
        entity.Currency = transaction.Currency;
        entity.IntrBkSttlmAtm = transaction.IntrBkSttlmAtm;
        entity.DbtrAcctId = transaction.DbtrAcctId;
        entity.DbtrAcctType = transaction.DbtrAcctType;
        entity.DbtrName = transaction.DbtrName;
        entity.DbtrAddress = transaction.DbtrAddress;
        entity.CdtrAcctId = transaction.CdtrAcctId;
        entity.CdtrName = transaction.CdtrName;
        entity.CdtrAddress = transaction.CdtrAddress;
        entity.RequestId = transaction.RequestId;
        entity.TransCode = transaction.TransCode;
        entity.ReceivedAt = receivedAt;

        return entity;
    }

    private static List<string> ValidateTransaction(ProcessedTransactionDto transaction)
    {
        var errors = new List<string>();

        if (transaction.Id <= 0)
        {
            errors.Add("IPS transaction id must be greater than zero");
        }

        AddMaxLengthError(errors, transaction.BankFrom, 20, nameof(transaction.BankFrom));
        AddMaxLengthError(errors, transaction.BankTo, 20, nameof(transaction.BankTo));
        AddMaxLengthError(errors, transaction.TxId, 100, nameof(transaction.TxId));
        AddMaxLengthError(errors, transaction.RequestId, 100, nameof(transaction.RequestId));
        AddMaxLengthError(errors, transaction.Type, 50, nameof(transaction.Type));
        AddMaxLengthError(errors, transaction.Status, 50, nameof(transaction.Status));
        AddMaxLengthError(errors, transaction.Currency, 10, nameof(transaction.Currency));

        return errors;
    }

    private static void AddMaxLengthError(List<string> errors, string? value, int maxLength, string fieldName)
    {
        if (value?.Length > maxLength)
        {
            errors.Add($"{fieldName} exceeds {maxLength} characters");
        }
    }
}
