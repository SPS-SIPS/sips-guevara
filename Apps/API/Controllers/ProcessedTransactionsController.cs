using SPSS.App.Features.Balance.Queries;

namespace SPSS.API.Controllers;

[ApiController]
[Produces("application/json")]
[Route("api/v1/[controller]")]
public class ProcessedTransactionsController : BaseController
{
    [HttpGet]
    public async Task<PagedResponse<ProcessedTransactionRecordDto>> Get([FromQuery] GetProcessedTransactionsQuery query, CancellationToken ct)
        => await Mediator.Send(query, ct);
}
