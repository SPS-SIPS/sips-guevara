using SPSS.App.Features.Auth.Commands;
using Microsoft.AspNetCore.RateLimiting;
using SPSS.App.Features.Auth.Queries;

namespace SPSS.API.Controllers;
[ApiController]
[Produces("application/json")]
[Route("api/v1/[controller]")]
[EnableRateLimiting("authentication")]
public class AuthController : BaseController
{
    [HttpGet]
    public async Task<Response<UserDto>> Profile(CancellationToken ct)
    => await Mediator.Send(new GetMeQuery(), ct);

    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    [HttpPost("login")]
    public async Task<Response<LoginDto>> Login([FromBody] LoginCommand cmd, CancellationToken ct)
    => await Mediator.Send(cmd, ct);

    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<Response<LoginDto>> Refresh([FromBody] RefreshCommand cmd, CancellationToken ct)
    => await Mediator.Send(cmd, ct);

    [HttpPost("changeEmail")]
    public async Task<Response<string>> ChangeEmail([FromBody] ChangeEmailCommand cmd, CancellationToken ct)
        => await Mediator.Send(cmd, ct);

    [HttpPost("changePassword")]
    public async Task<Response<string>> ChangePassword([FromBody] ChangePasswordCommand cmd, CancellationToken ct)
        => await Mediator.Send(cmd, ct);

    [HttpPost("changePhone")]
    public async Task<Response<string>> ChangePhone([FromBody] ChangePhoneCommand cmd, CancellationToken ct)
        => await Mediator.Send(cmd, ct);

    [HttpPost("revokeToken")]
    public async Task<Response<string>> RevokeAccessToken([FromBody] RevokeAccessTokenCommand cmd, CancellationToken ct)
        => await Mediator.Send(cmd, ct);
}
