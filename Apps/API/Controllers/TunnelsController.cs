using SPSS.App.Features.Tunnels.Commands;
using SPSS.App.Features.Tunnels.Queries;

namespace SPSS.API.Controllers;
[ApiController]
[Produces("application/json")]
[Route("api/v1/[controller]")]
public class TunnelsController : BaseController
{
    [HttpGet]
    public async Task<PagedResponse<TunnelDto>> GetList([FromQuery] GetTunnelsListQuery query, CancellationToken ct)
    => await Mediator.Send(query, ct);

    [HttpGet("lookup")]
    public async Task<Response<List<Lookup<int>>>> Get([FromQuery] GetTunnelsLookupListQuery query, CancellationToken ct)
        => await Mediator.Send(query, ct);

    [HttpGet("{Id}")]
    public async Task<Response<TunnelDto>> Get([FromRoute] GetTunnelQuery query, CancellationToken ct)
    => await Mediator.Send(query, ct);

    [HttpPost]
    public async Task<Response<int>> Post([FromBody] AddTunnelCommand query, CancellationToken ct)
        => await Mediator.Send(query, ct);

    [HttpPut]
    public async Task<Response<int>> Put([FromBody] EditTunnelCommand query, CancellationToken ct)
        => await Mediator.Send(query, ct);

    [HttpDelete("{Id}")]
    public async Task<Response<int>> Delete([FromRoute] DeleteTunnelCommand query, CancellationToken ct)
    => await Mediator.Send(query, ct);

}
