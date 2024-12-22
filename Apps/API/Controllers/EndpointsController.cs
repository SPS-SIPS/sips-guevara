using SPSS.App.Features.Endpoints.Commands;
using SPSS.App.Features.Endpoints.Queries;

namespace SPSS.API.Controllers;
[ApiController]
[Produces("application/json")]
[Route("api/v1/[controller]")]
public class EndpointsController : BaseController
{
    [HttpGet]
    public async Task<PagedResponse<EndpointDto>> GetList([FromQuery] GetEndpointsListQuery query, CancellationToken ct)
    => await Mediator.Send(query, ct);

    [HttpGet("lookup")]
    public async Task<Response<List<Lookup<int>>>> Get([FromQuery] GetEndpointsLookupListQuery query, CancellationToken ct)
        => await Mediator.Send(query, ct);

    [HttpGet("{Id}")]
    public async Task<Response<EndpointDto>> Get([FromRoute] GetEndpointQuery query, CancellationToken ct)
    => await Mediator.Send(query, ct);

    [HttpPost]
    public async Task<Response<int>> Post([FromBody] AddEndpointCommand query, CancellationToken ct)
        => await Mediator.Send(query, ct);

    [HttpPut]
    public async Task<Response<int>> Put([FromBody] EditEndpointCommand query, CancellationToken ct)
        => await Mediator.Send(query, ct);

    [HttpDelete("{Id}")]
    public async Task<Response<int>> Delete([FromRoute] DeleteEndpointCommand query, CancellationToken ct)
    => await Mediator.Send(query, ct);

}
