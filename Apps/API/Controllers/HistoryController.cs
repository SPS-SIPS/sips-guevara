using SPSS.App.Features.History.Queries;

namespace SPSS.API.Controllers;
[ApiController]
[Produces("application/json")]
[Route("api/v1/[controller]")]
public class HistoryController : BaseController
{
    [HttpGet]
    public async Task<PagedResponse<HistoryDto>> Get([FromQuery] GetHistoryQuery query, CancellationToken ct)
        => await Mediator.Send(query, ct);
}
