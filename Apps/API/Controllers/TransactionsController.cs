using SPSS.App.Features.Balance.Queries;
using SPSS.App.Features.Balance.Commands;
using Microsoft.AspNetCore.RateLimiting;

namespace SPSS.API.Controllers;

[ApiController]
[Produces("application/json")]
[Route("api/v1/[controller]")]
public class TransactionsController : BaseController
{
    [HttpGet]
    public async Task<PagedResponse<TransactionRecordDto>> Get(
        [FromQuery] GetTransactionsQuery query,
        CancellationToken ct)
        => await Mediator.Send(query, ct);

    [HttpGet("summary")]
    public async Task<Response<TransactionSummaryDto>> GetSummary(
        [FromQuery] GetTransactionsSummaryQuery query,
        CancellationToken ct)
        => await Mediator.Send(query, ct);

    [HttpGet("ingestion-batches")]
    public async Task<PagedResponse<TransactionIngestionBatchDto>> GetIngestionBatches(
        [FromQuery] GetOracleTransactionIngestionBatchesQuery query,
        CancellationToken ct)
        => await Mediator.Send(query, ct);

    [EnableRateLimiting("reports")]
    [HttpPost("failed-notifications/manual")]
    public async Task<Response<OracleFailedTransactionNotificationResult>> SendManualFailedTransactionNotification(
        [FromBody] ManualOracleFailedTransactionNotificationRequest request, CancellationToken ct)
        => await Mediator.Send(new SendManualFailedTransactionNotificationCommand(request.Bic, request.From, request.To), ct);
}
