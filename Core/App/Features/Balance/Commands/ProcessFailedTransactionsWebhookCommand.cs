using SPSS.Domain.Models.DTOs;

namespace SPSS.App.Features.Balance.Commands;

public sealed record ProcessFailedTransactionsWebhookCommand(FailedTransactionsWebhookDto Data) : IRequest<Response<string>>;

public sealed class ProcessFailedTransactionsWebhookCommandHandler(
    IStorageBroker storage,
    ILogger<ProcessFailedTransactionsWebhookCommandHandler> logger)
    : IRequestHandler<ProcessFailedTransactionsWebhookCommand, Response<string>>
{
    private readonly IStorageBroker _storage = storage;
    private readonly ILogger<ProcessFailedTransactionsWebhookCommandHandler> _logger = logger;

    public async ValueTask<Response<string>> Handle(ProcessFailedTransactionsWebhookCommand request, CancellationToken cancellationToken)
    {
        var data = request.Data;
        var transactions = data.Transactions ?? [];

        if (data.FromDate >= data.ToDate)
        {
            _logger.LogWarning(
                "Failed transactions webhook received an invalid window. FromDate={FromDate}, ToDate={ToDate}",
                data.FromDate,
                data.ToDate);

            return Response<string>.BadRequest("Invalid failed transaction snapshot window");
        }

        if (!string.Equals(data.Type, "FP_PAYMENT", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(data.Status, "DECLINED", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "Failed transactions webhook received unexpected filters. Type={Type}, Status={Status}",
                data.Type,
                data.Status);

            return Response<string>.BadRequest("Unexpected failed transaction snapshot filters");
        }

        _logger.LogInformation(
            "Failed transactions webhook received at {Timestamp}. Window={FromDate} to {ToDate}, TotalCount={TotalCount}, ReceivedCount={ReceivedCount}",
            data.Timestamp,
            data.FromDate,
            data.ToDate,
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
            ? new Dictionary<long, FailedTransaction>()
            : await _storage.FailedTransactions
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

        foreach (var transaction in uniqueTransactions)
        {
            var validationErrors = ValidateTransaction(transaction);
            if (validationErrors.Count > 0)
            {
                invalidCount++;
                var reason = string.Join("; ", validationErrors);
                invalidRows.Add($"Id={transaction.Id}, TxId={transaction.TxId ?? "-"}: {reason}");
                _logger.LogError(
                    "Failed transaction webhook row rejected before persistence. Id={Id}, TxId={TxId}, Reason={Reason}",
                    transaction.Id,
                    transaction.TxId,
                    reason);
                continue;
            }

            FailedTransaction? entity = null;
            if (HasValue(transaction.TxId))
            {
                existingByTxId.TryGetValue(transaction.TxId!, out entity);
            }

            entity ??= existingTransactions.GetValueOrDefault(transaction.Id);

            if (entity is not null)
            {
                MapFailedTransaction(entity, transaction, setIdentity: false);
                _storage.FailedTransactions.Update(entity);
                updatedCount++;
            }
            else
            {
                await _storage.FailedTransactions.AddAsync(
                    MapFailedTransaction(new FailedTransaction(), transaction, setIdentity: true),
                    cancellationToken);
                insertedCount++;
            }

            _logger.LogWarning(
                "Declined FP_PAYMENT: Id={Id}, TxId={TxId}, RequestId={RequestId}, From={BankFrom}, To={BankTo}, Amount={Amount} {Currency}, Error={Error}, ErrorReason={ErrorReason}, CreatedDate={CreatedDate}",
                transaction.Id,
                transaction.TxId,
                transaction.RequestId,
                transaction.BankFrom,
                transaction.BankTo,
                transaction.Amount,
                transaction.Currency,
                transaction.Error,
                transaction.ErrorReason,
                transaction.CreatedDate);
        }

        if (insertedCount + updatedCount == 0 && uniqueTransactions.Count > 0)
        {
            return Response<string>.BadRequest(
                $"Failed transactions webhook contained {uniqueTransactions.Count} rows, but none were valid for persistence. First errors: {string.Join(" | ", invalidRows.Take(5))}");
        }

        await _storage.SaveChangesAsync(cancellationToken);

        var summary = $"Processed {uniqueTransactions.Count} failed transactions, inserted {insertedCount}, updated {updatedCount}, rejected {invalidCount} invalid rows, ignored {duplicateCount} duplicate retries";
        if (invalidCount > 0)
        {
            _logger.LogWarning(
                "Failed transactions webhook accepted with rejected rows. Inserted={Inserted}, Updated={Updated}, Rejected={Rejected}, SampleErrors={SampleErrors}",
                insertedCount,
                updatedCount,
                invalidCount,
                string.Join(" | ", invalidRows.Take(5)));
        }

        return new Response<string>(summary);
    }

    private static string TransactionIdentity(FailedTransactionDto transaction)
        => HasValue(transaction.TxId)
            ? $"txid:{transaction.TxId}"
            : $"ips-id:{transaction.Id}";

    private static bool HasValue(string? value) => !string.IsNullOrWhiteSpace(value);

    private static FailedTransaction MapFailedTransaction(
        FailedTransaction entity,
        FailedTransactionDto transaction,
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
        entity.Error = transaction.Error;
        entity.ErrorReason = transaction.ErrorReason;

        return entity;
    }

    private static List<string> ValidateTransaction(FailedTransactionDto transaction)
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
        AddMaxLengthError(errors, transaction.ErrorReason, 100, nameof(transaction.ErrorReason));

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
