using System.Text.Json;
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

        var ipsTransactionIds = transactions.Select(x => x.Id).ToList();
        var existingTransactions = ipsTransactionIds.Count == 0
            ? new Dictionary<long, FailedTransaction>()
            : await _storage.FailedTransactions
                .Where(x => ipsTransactionIds.Contains(x.IpsTransactionId))
                .ToDictionaryAsync(x => x.IpsTransactionId, cancellationToken);

        var insertedCount = 0;
        var updatedCount = 0;

        foreach (var transaction in transactions)
        {
            if (existingTransactions.TryGetValue(transaction.Id, out var entity))
            {
                MapFailedTransaction(entity, transaction, data);
                _storage.FailedTransactions.Update(entity);
                updatedCount++;
            }
            else
            {
                await _storage.FailedTransactions.AddAsync(
                    MapFailedTransaction(new FailedTransaction(), transaction, data),
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

        await _storage.SaveChangesAsync(cancellationToken);

        var summary = $"Processed {transactions.Count} failed transactions, inserted {insertedCount}, updated {updatedCount}";
        return new Response<string>(summary);
    }

    private static FailedTransaction MapFailedTransaction(
        FailedTransaction entity,
        FailedTransactionDto transaction,
        FailedTransactionsWebhookDto snapshot)
    {
        entity.IpsTransactionId = transaction.Id;
        entity.SnapshotTimestamp = snapshot.Timestamp;
        entity.SnapshotFromDate = snapshot.FromDate;
        entity.SnapshotToDate = snapshot.ToDate;
        entity.ReceivedAt = DateTimeOffset.UtcNow;
        entity.RuleIds = transaction.RuleIds ?? [];
        entity.BankFrom = transaction.BankFrom;
        entity.BankFromName = transaction.BankFromName;
        entity.BankTo = transaction.BankTo;
        entity.BankToName = transaction.BankToName;
        entity.CreatedDate = transaction.CreatedDate;
        entity.ModifiedDate = transaction.ModifiedDate;
        entity.LastModifiedDate = transaction.LastModifiedDate;
        entity.AcceptanceDate = transaction.AcceptanceDate;
        entity.TxId = transaction.TxId;
        entity.Type = transaction.Type;
        entity.Amount = transaction.Amount;
        entity.Status = transaction.Status;
        entity.Description = transaction.Description;
        entity.SttlId = transaction.SttlId;
        entity.Currency = transaction.Currency;
        entity.IntrBkSttlmCurr = transaction.IntrBkSttlmCurr;
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
        entity.OldBalance = transaction.OldBalance;
        entity.NewBalance = transaction.NewBalance;
        entity.AliasFrom = transaction.AliasFrom;
        entity.AliasTo = transaction.AliasTo;
        entity.AliasTypeFrom = transaction.AliasTypeFrom;
        entity.AliasTypeTo = transaction.AliasTypeTo;
        entity.Error = transaction.Error;
        entity.ErrorReason = transaction.ErrorReason;
        entity.RawTransactionJson = transaction.Transaction is { ValueKind: not JsonValueKind.Undefined }
            ? transaction.Transaction.Value.GetRawText()
            : null;
        entity.TransactionsJson = JsonSerializer.Serialize(transaction.Transactions);
        entity.MessagesJson = JsonSerializer.Serialize(transaction.Messages);

        return entity;
    }
}
