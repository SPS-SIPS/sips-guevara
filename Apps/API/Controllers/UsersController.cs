using NTran.App.Features.Users.Commands;
using SPSS.App.Features.Users.Commands;
using SPSS.App.Features.Users.Queries;

namespace SPSS.API.Controllers;
[ApiController]
[Produces("application/json")]
[Route("api/v1/[controller]")]
public class UsersController : BaseController
{
    [HttpGet]
    public async Task<PagedResponse<UserDto>> GetList([FromQuery] GetUsersListQuery query, CancellationToken ct)
    => await Mediator.Send(query, ct);


    [HttpGet("Tokens")]
    public async Task<PagedResponse<TokenDto>> GetTokensList([FromQuery] GetTokensListQuery query, CancellationToken ct)
        => await Mediator.Send(query, ct);

    [HttpGet("lookup")]
    public async Task<Response<List<Lookup<int>>>> Get([FromQuery] GetUsersLookupListQuery query, CancellationToken ct)
        => await Mediator.Send(query, ct);

    [HttpGet("{Id:int}")]
    public async Task<Response<UserDto>> Get([FromRoute] GetUserQuery query, CancellationToken ct)
    => await Mediator.Send(query, ct);

    [HttpPost]
    public async Task<Response<int>> Post([FromBody] AddUserCommand query, CancellationToken ct)
        => await Mediator.Send(query, ct);

    [HttpPut]
    public async Task<Response<int>> Put([FromBody] EditUserCommand query, CancellationToken ct)
        => await Mediator.Send(query, ct);

    [HttpDelete("{Id:int}")]
    public async Task<Response<int>> Delete([FromRoute] DeleteUserCommand query, CancellationToken ct)
    => await Mediator.Send(query, ct);

}
