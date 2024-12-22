using SPSS.App.Features.VPNs.Commands;
using SPSS.App.Features.VPNs.Queries;

namespace SPSS.API.Controllers;
[ApiController]
[Produces("application/json")]
[Route("api/v1/[controller]")]
public class VPNsController : BaseController
{
    [HttpGet]
    public async Task<PagedResponse<VPNDto>> GetList([FromQuery] GetVPNsListQuery query, CancellationToken ct)
    => await Mediator.Send(query, ct);

    [HttpGet("lookup")]
    public async Task<Response<List<Lookup<int>>>> Get([FromQuery] GetVPNsLookupListQuery query, CancellationToken ct)
        => await Mediator.Send(query, ct);

    [HttpGet("{Id}")]
    public async Task<Response<VPNDto>> Get([FromRoute] GetVPNQuery query, CancellationToken ct)
    => await Mediator.Send(query, ct);

    [HttpPost]
    public async Task<Response<int>> Post([FromBody] AddVPNCommand query, CancellationToken ct)
        => await Mediator.Send(query, ct);

    [HttpPut]
    public async Task<Response<int>> Put([FromBody] EditVPNCommand query, CancellationToken ct)
        => await Mediator.Send(query, ct);

    [HttpDelete("{Id}")]
    public async Task<Response<int>> Delete([FromRoute] DeleteVPNCommand query, CancellationToken ct)
    => await Mediator.Send(query, ct);

}
