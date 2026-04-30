using SPSS.Domain.Models.DTOs;

namespace SPSS.App.Features.Balance.Commands;

public sealed record ProcessFailedTransactionsWebhookCommand(FailedTransactionsWebhookDto Data) : IRequest<Response<string>>;

public sealed class ProcessFailedTransactionsWebhookCommandHandler(
    ILogger<ProcessFailedTransactionsWebhookCommandHandler> logger)
    : IRequestHandler<ProcessFailedTransactionsWebhookCommand, Response<string>>
{
    private readonly ILogger<ProcessFailedTransactionsWebhookCommandHandler> _logger = logger;

    public ValueTask<Response<string>> Handle(ProcessFailedTransactionsWebhookCommand request, CancellationToken cancellationToken)
    {
        var data = request.Data;
        var transactions = data.Transactions ?? [];

        if (data.FromDate >= data.ToDate)
        {
            _logger.LogWarning(
                "Failed transactions webhook received an invalid window. FromDate={FromDate}, ToDate={ToDate}",
                data.FromDate,
                data.ToDate);

            return ValueTask.FromResult(Response<string>.BadRequest("Invalid failed transaction snapshot window"));
        }

        if (!string.Equals(data.Type, "FP_PAYMENT", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(data.Status, "DECLINED", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "Failed transactions webhook received unexpected filters. Type={Type}, Status={Status}",
                data.Type,
                data.Status);

            return ValueTask.FromResult(Response<string>.BadRequest("Unexpected failed transaction snapshot filters"));
        }

        _logger.LogInformation(
            "Failed transactions webhook received at {Timestamp}. Window={FromDate} to {ToDate}, TotalCount={TotalCount}, ReceivedCount={ReceivedCount}",
            data.Timestamp,
            data.FromDate,
            data.ToDate,
            data.TotalCount,
            transactions.Count);

        foreach (var transaction in transactions)
        {
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

        var summary = $"Processed {transactions.Count} failed transactions";
        return ValueTask.FromResult(new Response<string>(summary));
    }
}
