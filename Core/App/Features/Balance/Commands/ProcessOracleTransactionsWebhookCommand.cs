using System.Text.Json;
using SPSS.Domain.Models.DTOs;

namespace SPSS.App.Features.Balance.Commands;

[Authorize(Roles = CreateMonitHistory)]
public sealed record ProcessOracleTransactionsWebhookCommand(OracleTransactionsWebhookDto Data)
    : IRequest<Response<OracleTransactionBatchResponseDto>>;

public sealed class ProcessOracleTransactionsWebhookCommandHandler(
    IStorageBroker storage,
    ILogger<ProcessOracleTransactionsWebhookCommandHandler> logger)
    : IRequestHandler<ProcessOracleTransactionsWebhookCommand, Response<OracleTransactionBatchResponseDto>>
{
    private readonly IStorageBroker _storage = storage;
    private readonly ILogger<ProcessOracleTransactionsWebhookCommandHandler> _logger = logger;

    public async ValueTask<Response<OracleTransactionBatchResponseDto>> Handle(
        ProcessOracleTransactionsWebhookCommand request,
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
                "Oracle transactions batch rejected. BatchKey={BatchKey}, Reason={Reason}",
                batchKey,
                message);

            return Response<OracleTransactionBatchResponseDto>.BadRequest(
                message,
                new OracleTransactionBatchResponseDto
                {
                    Status = "error",
                    BatchKey = batchKey,
                    Received = transactions.Count
                });
        }

        var existingBatch = await _storage.OracleTransactionIngestionBatches
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Source == data.Source &&
                    x.FromId == data.FromId &&
                    x.ToId == data.ToId &&
                    x.HighWatermarkId == data.HighWatermarkId,
                cancellationToken);

        if (existingBatch is not null)
        {
            _logger.LogInformation(
                "Duplicate Oracle transactions batch received. BatchKey={BatchKey}, ExistingBatchId={BatchId}",
                batchKey,
                existingBatch.Id);

            return new Response<OracleTransactionBatchResponseDto>(
                new OracleTransactionBatchResponseDto
                {
                    Status = "already_processed",
                    BatchKey = batchKey,
                    Received = existingBatch.ReceivedCount,
                    Duplicates = existingBatch.DuplicateCount,
                    Inserted = existingBatch.InsertedCount,
                    Updated = existingBatch.UpdatedCount
                });
        }

        var source = data.Source.Trim();
        var rawPayload = JsonSerializer.Serialize(data);
        var receivedAt = DateTimeOffset.UtcNow;
        var duplicateCount = transactions.Count - transactions.Select(x => x.Id).Distinct().Count();
        var uniqueTransactions = transactions
            .GroupBy(x => x.Id)
            .Select(x => x.First())
            .ToList();
        var oracleIds = uniqueTransactions.Select(x => x.Id).ToList();

        var existingTransactions = await _storage.OracleTransactions
            .Where(x => x.Source == source && oracleIds.Contains(x.OracleId))
            .ToDictionaryAsync(x => x.OracleId, cancellationToken);

        var insertedCount = 0;
        var updatedCount = 0;

        foreach (var transaction in uniqueTransactions)
        {
            var transactionRawPayload = JsonSerializer.Serialize(transaction);
            if (existingTransactions.TryGetValue(transaction.Id, out var entity))
            {
                MapOracleTransaction(entity, source, transaction, receivedAt, transactionRawPayload);
                _storage.OracleTransactions.Update(entity);
                updatedCount++;
            }
            else
            {
                await _storage.OracleTransactions.AddAsync(
                    MapOracleTransaction(new OracleTransaction(), source, transaction, receivedAt, transactionRawPayload),
                    cancellationToken);
                insertedCount++;
            }
        }

        await _storage.OracleTransactionIngestionBatches.AddAsync(
            new OracleTransactionIngestionBatch
            {
                Source = source,
                FromId = data.FromId,
                ToId = data.ToId,
                HighWatermarkId = data.HighWatermarkId,
                ProducerTimestamp = data.Timestamp,
                Type = data.Type.Trim(),
                Statuses = data.Statuses ?? [],
                Status = "ACCEPTED",
                ReceivedAt = receivedAt,
                ProcessedAt = DateTimeOffset.UtcNow,
                TotalCount = data.TotalCount,
                ReceivedCount = transactions.Count,
                InsertedCount = insertedCount,
                UpdatedCount = updatedCount,
                DuplicateCount = duplicateCount,
                RejectedCount = 0,
                RawPayload = rawPayload
            },
            cancellationToken);

        await _storage.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Oracle transactions batch accepted. BatchKey={BatchKey}, Received={Received}, Inserted={Inserted}, Updated={Updated}, Duplicates={Duplicates}",
            batchKey,
            transactions.Count,
            insertedCount,
            updatedCount,
            duplicateCount);

        return new Response<OracleTransactionBatchResponseDto>(
            new OracleTransactionBatchResponseDto
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
        OracleTransactionsWebhookDto data,
        List<OracleTransactionWebhookRowDto> transactions)
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

        if (string.IsNullOrWhiteSpace(data.Type))
        {
            errors.Add("type is required");
        }

        if (data.ToId <= data.FromId)
        {
            errors.Add("to_id must be greater than from_id");
        }

        if (data.HighWatermarkId < data.ToId)
        {
            errors.Add("high_watermark_id must be greater than or equal to to_id");
        }

        if (data.TotalCount != transactions.Count)
        {
            errors.Add("total_count does not match transactions length");
        }

        foreach (var transaction in transactions)
        {
            AddTransactionValidationErrors(errors, transaction, data.FromId, data.ToId);
        }

        return errors;
    }

    private static void AddTransactionValidationErrors(
        List<string> errors,
        OracleTransactionWebhookRowDto transaction,
        long fromId,
        long toId)
    {
        if (transaction.Id <= 0)
        {
            errors.Add("transaction id is required");
        }

        if (transaction.Id <= fromId || transaction.Id > toId)
        {
            errors.Add($"transaction id {transaction.Id} is outside the batch boundaries");
        }

        if (string.IsNullOrWhiteSpace(transaction.BankFrom))
        {
            errors.Add($"transaction {transaction.Id} bankFrom is required");
        }

        if (string.IsNullOrWhiteSpace(transaction.BankTo))
        {
            errors.Add($"transaction {transaction.Id} bankTo is required");
        }

        if (string.IsNullOrWhiteSpace(transaction.TxId))
        {
            errors.Add($"transaction {transaction.Id} txId is required");
        }

        if (string.IsNullOrWhiteSpace(transaction.Type))
        {
            errors.Add($"transaction {transaction.Id} type is required");
        }

        if (string.IsNullOrWhiteSpace(transaction.Status))
        {
            errors.Add($"transaction {transaction.Id} status is required");
        }
    }

    private static OracleTransaction MapOracleTransaction(
        OracleTransaction entity,
        string source,
        OracleTransactionWebhookRowDto transaction,
        DateTimeOffset receivedAt,
        string rawPayload)
    {
        entity.Source = source;
        entity.OracleId = transaction.Id;
        entity.ParentId = transaction.ParentId;
        entity.BankFrom = transaction.BankFrom!.Trim();
        entity.BankTo = transaction.BankTo!.Trim();
        entity.CreatedDate = transaction.CreatedDate;
        entity.ModifiedDate = transaction.ModifiedDate;
        entity.LastModifiedDate = transaction.LastModifiedDate;
        entity.AcceptanceDate = transaction.AcceptanceDate;
        entity.TxId = transaction.TxId!.Trim();
        entity.Type = transaction.Type!.Trim();
        entity.TypeDescription = transaction.TypeDescription;
        entity.Amount = transaction.Amount;
        entity.AmountValue = transaction.AmountValue;
        entity.Status = transaction.Status!.Trim();
        entity.TxSts = transaction.TxSts;
        entity.Description = transaction.Description;
        entity.Currency = transaction.Currency;
        entity.IntrBkSttlmAtm = transaction.IntrBkSttlmAtm;
        entity.IntrBkSttlmAtmValue = transaction.IntrBkSttlmAtmValue;
        entity.IntrBkSttlmCurr = transaction.IntrBkSttlmCurr;
        entity.DbtrAcctId = transaction.DbtrAcctId;
        entity.DbtrAcctType = transaction.DbtrAcctType;
        entity.DbtrName = transaction.DbtrName;
        entity.DbtrAddress = transaction.DbtrAddress;
        entity.CdtrName = transaction.CdtrName;
        entity.CdtrAddress = transaction.CdtrAddress;
        entity.CdtrAcctId = transaction.CdtrAcctId;
        entity.CdtrAcctType = transaction.CdtrAcctType;
        entity.SttlId = transaction.SttlId;
        entity.Error = transaction.Error;
        entity.ErrDesc = transaction.ErrDesc;
        entity.ErrorReason = transaction.ErrorReason;
        entity.RequestId = transaction.RequestId;
        entity.TransCode = transaction.TransCode;
        entity.LclInstrm = transaction.LclInstrm;
        entity.CtgyPurp = transaction.CtgyPurp;
        entity.RtrId = transaction.RtrId;
        entity.OldBalance = transaction.OldBalance;
        entity.NewBalance = transaction.NewBalance;
        entity.BankFromSaPosition = transaction.BankFromSaPosition;
        entity.BankToSaPosition = transaction.BankToSaPosition;
        entity.BankFromWaPosition = transaction.BankFromWaPosition;
        entity.BankToWaPosition = transaction.BankToWaPosition;
        entity.EndToEndId = transaction.EndToEndId;
        entity.Iban = transaction.Iban;
        entity.RtpId = transaction.RtpId;
        entity.AttributesRaw = transaction.Attributes;
        entity.OrgTrxId = transaction.OrgTrxId;
        entity.StsRsnInfRsn = transaction.StsRsnInfRsn;
        entity.StsRsnInfAddtlInf = transaction.StsRsnInfAddtlInf;
        entity.MandateReqId = transaction.MandateReqId;
        entity.TransactionRaw = SerializeJsonElement(transaction.Transaction);
        entity.TransactionsRaw = SerializeJsonElement(transaction.Transactions);
        entity.MessagesRaw = SerializeJsonElement(transaction.Messages);
        entity.RawPayload = rawPayload;
        entity.ReceivedAt = receivedAt;

        return entity;
    }

    private static string BuildBatchKey(OracleTransactionsWebhookDto data)
        => $"{data.Source}:{data.FromId}:{data.ToId}:{data.HighWatermarkId}";

    private static string? SerializeJsonElement(JsonElement? value)
    {
        if (value is null || value.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        return value.Value.GetRawText();
    }
}
