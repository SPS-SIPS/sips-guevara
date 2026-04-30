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

        var ipsTransactionIds = transactions.Select(x => x.Id).ToList();
        var existingTransactions = ipsTransactionIds.Count == 0
            ? new Dictionary<long, ProcessedTransaction>()
            : await _storage.ProcessedTransactions
                .Where(x => ipsTransactionIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, cancellationToken);

        var insertedCount = 0;
        var updatedCount = 0;
        var receivedAt = DateTimeOffset.UtcNow;

        foreach (var transaction in transactions)
        {
            if (existingTransactions.TryGetValue(transaction.Id, out var entity))
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

        var summary = $"Processed {transactions.Count} processed transactions, inserted {insertedCount}, updated {updatedCount}";
        return new Response<string>(summary);
    }

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
