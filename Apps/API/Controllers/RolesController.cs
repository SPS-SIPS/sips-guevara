using SPSS.App.Features.Roles.Commands;
using SPSS.App.Features.Roles.Queries;

namespace SPSS.API.Controllers;
[ApiController]
[Produces("application/json")]
[Route("api/v1/[controller]")]
public class RolesController : BaseController
{
    [HttpGet]
    public async Task<PagedResponse<RoleDto>> GetList([FromQuery] GetRolesListQuery query, CancellationToken ct)
    => await Mediator.Send(query, ct);

    [HttpGet("lookup")]
    public async Task<Response<List<Lookup<Guid>>>> Get([FromQuery] GetRoleLookupListQuery query, CancellationToken ct)
        => await Mediator.Send(query, ct);

    [HttpGet("{Id:Guid}")]
    public async Task<Response<RoleDto>> Get([FromRoute] GetRoleQuery query, CancellationToken ct)
        => await Mediator.Send(query, ct);

    [HttpGet("{Id:Guid}/Claims")]
    public async Task<PagedResponse<ClaimDto>> Get([FromRoute] GetRoleClaimsListQuery query, CancellationToken ct)
        => await Mediator.Send(query, ct);

    [HttpGet("{Id:int}/Users")]
    public async Task<PagedResponse<UserDto>> Get([FromRoute] GetRoleUsersListQuery query, CancellationToken ct)
        => await Mediator.Send(query, ct);

    [HttpPost]
    public async Task<Response<Guid>> Post([FromBody] AddRoleCommand query, CancellationToken ct)
        => await Mediator.Send(query, ct);

    [HttpPut]
    public async Task<Response<Guid>> Put([FromBody] EditRoleCommand query, CancellationToken ct)
     => await Mediator.Send(query, ct);

    [HttpDelete("{Id:Guid}")]
    public async Task<Response<Guid>> Delete([FromRoute] RemoveRoleCommand query, CancellationToken ct)
    => await Mediator.Send(query, ct);

}
