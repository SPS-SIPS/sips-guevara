using SPSS.App.Features.Balance.Commands;
using SPSS.Domain.Models.DTOs;

namespace SPSS.API.Controllers;

[ApiController]
[Produces("application/json")]
[Route("api/v1/[controller]")]
public class BalanceController : BaseController
{
    [HttpPost("webhook")]
    public async Task<Response<string>> PostWebhook([FromBody] BalanceWebhookDto data, CancellationToken ct)
        => await Mediator.Send(new ProcessBalanceWebhookCommand(data), ct);

    [HttpPost("failed-transactions")]
    public async Task<Response<string>> PostFailedTransactions([FromBody] FailedTransactionsWebhookDto data, CancellationToken ct)
        => await Mediator.Send(new ProcessFailedTransactionsWebhookCommand(data), ct);

    [HttpPost("processed-transactions")]
    public async Task<Response<string>> PostProcessedTransactions([FromBody] ProcessedTransactionsWebhookDto data, CancellationToken ct)
        => await Mediator.Send(new ProcessProcessedTransactionsWebhookCommand(data), ct);

    [HttpPost("transaction-summary")]
    public async Task<Response<string>> PostTransactionSummary([FromBody] TransactionSummaryWebhookDto data, CancellationToken ct)
        => await Mediator.Send(new ProcessTransactionSummaryWebhookCommand(data), ct);
}
