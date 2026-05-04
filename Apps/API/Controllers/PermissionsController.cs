using SPSS.App.Features.Claims.Queries;

namespace SPSS.API.Controllers;
[ApiController]
[Produces("application/json")]
[Route("api/v1/[controller]")]
public class PermissionsController : BaseController
{

    [HttpGet("lookup")]
    public async Task<Response<List<Lookup<string>>>> Get([FromQuery] GetClaimsLookupQuery query, CancellationToken ct)
        => await Mediator.Send(query, ct);

}
