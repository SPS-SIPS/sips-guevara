using SPSS.Domain.Models.DTOs;

namespace SPSS.App.Features.Balance.Commands;

public sealed record ProcessTransactionSummaryWebhookCommand(TransactionSummaryWebhookDto Data) : IRequest<Response<string>>;

public sealed class ProcessTransactionSummaryWebhookCommandHandler(
    IStorageBroker storage,
    ILogger<ProcessTransactionSummaryWebhookCommandHandler> logger)
    : IRequestHandler<ProcessTransactionSummaryWebhookCommand, Response<string>>
{
    private const string ExpectedTransactionType = "FP_PAYMENT";
    private const string ExpectedFailedStatus = "DECLINED";
    private const string ExpectedProcessedStatus = "PROCESSED";

    private readonly IStorageBroker _storage = storage;
    private readonly ILogger<ProcessTransactionSummaryWebhookCommandHandler> _logger = logger;

    public async ValueTask<Response<string>> Handle(
        ProcessTransactionSummaryWebhookCommand request,
        CancellationToken cancellationToken)
    {
        var data = request.Data;
        var errors = Validate(data);
        if (errors.Count > 0)
        {
            var message = string.Join("; ", errors);
            _logger.LogWarning("Transaction summary webhook rejected. Reason={Reason}", message);
            return Response<string>.BadRequest(message);
        }

        var receivedAt = data.Timestamp == default ? DateTimeOffset.UtcNow : data.Timestamp;
        var processedAt = DateTimeOffset.UtcNow;

        await _storage.TransactionIngestionBatches.AddRangeAsync(
            [
                new TransactionIngestionBatch
                {
                    BatchType = "FAILED_TRANSACTIONS_SUMMARY",
                    Status = "SUCCESS",
                    FromDate = data.FailedFromDate,
                    ToDate = data.FailedToDate,
                    ReceivedAt = receivedAt,
                    ProcessedAt = processedAt,
                    SnapshotType = data.TransactionType,
                    SnapshotStatus = data.FailedStatus,
                    TotalCount = data.TotalFailed,
                    ReceivedCount = 0,
                    UniqueCount = 0,
                    InsertedCount = 0,
                    UpdatedCount = 0,
                    DuplicateCount = 0,
                    RejectedCount = 0
                },
                new TransactionIngestionBatch
                {
                    BatchType = "PROCESSED_TRANSACTIONS_SUMMARY",
                    Status = "SUCCESS",
                    FromDate = data.ProcessedFromDate,
                    ToDate = data.ProcessedToDate,
                    ReceivedAt = receivedAt,
                    ProcessedAt = processedAt,
                    SnapshotType = data.TransactionType,
                    SnapshotStatus = data.ProcessedStatus,
                    TotalCount = data.TotalProcessed,
                    ReceivedCount = 0,
                    UniqueCount = 0,
                    InsertedCount = 0,
                    UpdatedCount = 0,
                    DuplicateCount = 0,
                    RejectedCount = 0
                }
            ],
            cancellationToken);

        await _storage.SaveChangesAsync(cancellationToken);

        var summary = $"Accepted transaction summary: failed={data.TotalFailed}, processed={data.TotalProcessed}, type={data.TransactionType}";
        _logger.LogInformation(
            "Transaction summary webhook accepted. Failed={Failed}, Processed={Processed}, FailedWindow={FailedFrom} to {FailedTo}, ProcessedWindow={ProcessedFrom} to {ProcessedTo}, Type={Type}",
            data.TotalFailed,
            data.TotalProcessed,
            data.FailedFromDate,
            data.FailedToDate,
            data.ProcessedFromDate,
            data.ProcessedToDate,
            data.TransactionType);

        return new Response<string>(summary);
    }

    private static List<string> Validate(TransactionSummaryWebhookDto data)
    {
        var errors = new List<string>();

        if (data.TotalFailed < 0)
        {
            errors.Add("total_failed cannot be negative");
        }

        if (data.TotalProcessed < 0)
        {
            errors.Add("total_processed cannot be negative");
        }

        if (data.FailedFromDate >= data.FailedToDate)
        {
            errors.Add("failed transaction window is invalid");
        }

        if (data.ProcessedFromDate >= data.ProcessedToDate)
        {
            errors.Add("processed transaction window is invalid");
        }

        if (!string.Equals(data.TransactionType, ExpectedTransactionType, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add($"transaction_type must be {ExpectedTransactionType}");
        }

        if (!string.Equals(data.FailedStatus, ExpectedFailedStatus, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add($"failed_status must be {ExpectedFailedStatus}");
        }

        if (!string.Equals(data.ProcessedStatus, ExpectedProcessedStatus, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add($"processed_status must be {ExpectedProcessedStatus}");
        }

        return errors;
    }
}
