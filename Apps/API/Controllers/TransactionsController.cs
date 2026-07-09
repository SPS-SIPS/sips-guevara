using SPSS.App.Features.Balance.Queries;

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
}
