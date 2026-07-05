using SPSS.App.Features.Balance.Queries;

namespace SPSS.API.Controllers;

[ApiController]
[Produces("application/json")]
[Route("api/v1/[controller]")]
public class FailedTransactionsController : BaseController
{
    [HttpGet]
    public async Task<PagedResponse<FailedTransactionRecordDto>> Get([FromQuery] GetFailedTransactionsQuery query, CancellationToken ct)
        => await Mediator.Send(query, ct);

    [HttpGet("ingestion-batches")]
    public async Task<PagedResponse<TransactionIngestionBatchDto>> GetIngestionBatches(
        [FromQuery] GetTransactionIngestionBatchesQuery query,
        CancellationToken ct)
        => await Mediator.Send(query, ct);
}
