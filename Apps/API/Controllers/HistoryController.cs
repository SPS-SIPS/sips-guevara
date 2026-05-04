using SPSS.App.Features.History.Commands;
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

    [HttpGet("uptime")]
    public async Task<List<InstitutionUptimeDto>> GetUptime([FromQuery] GetInstitutionsUptimeQuery query, CancellationToken ct)
        => await Mediator.Send(query, ct);

    [HttpPost("monit")]
    public async Task<Response<int>> Post([FromBody] AddMonitHistoryCommand command, CancellationToken ct)
        => await Mediator.Send(command, ct);
}
