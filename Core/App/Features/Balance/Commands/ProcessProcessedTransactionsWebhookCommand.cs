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
        var receivedAt = DateTimeOffset.UtcNow;

        foreach (var transaction in uniqueTransactions)
        {
            ProcessedTransaction? entity = null;
            if (HasValue(transaction.TxId))
            {
                existingByTxId.TryGetValue(transaction.TxId!, out entity);
            }

            entity ??= existingTransactions.GetValueOrDefault(transaction.Id);

            if (entity is not null)
            {
                MapProcessedTransaction(entity, transaction, receivedAt);
                _storage.ProcessedTransactions.Update(entity);
                updatedCount++;
            }
            else
            {
                await _storage.ProcessedTransactions.AddAsync(
                    MapProcessedTransaction(new ProcessedTransaction(), transaction, receivedAt),
                    cancellationToken);
                insertedCount++;
            }
        }

        await _storage.SaveChangesAsync(cancellationToken);

        var summary = $"Processed {uniqueTransactions.Count} processed transactions, inserted {insertedCount}, updated {updatedCount}, ignored {duplicateCount} duplicate retries";
        return new Response<string>(summary);
    }

    private static string TransactionIdentity(ProcessedTransactionDto transaction)
        => HasValue(transaction.TxId)
            ? $"txid:{transaction.TxId}"
            : $"ips-id:{transaction.Id}";

    private static bool HasValue(string? value) => !string.IsNullOrWhiteSpace(value);

    private static ProcessedTransaction MapProcessedTransaction(
        ProcessedTransaction entity,
        ProcessedTransactionDto transaction,
        DateTimeOffset receivedAt)
    {
        entity.Id = transaction.Id;
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
}
