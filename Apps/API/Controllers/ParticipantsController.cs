using SPSS.App.Features.Participants.Commands;
using SPSS.App.Features.Participants.Queries;

namespace SPSS.API.Controllers;
[ApiController]
[Produces("application/json")]
[Route("api/v1/[controller]")]
public class ParticipantsController : BaseController
{
    [HttpGet]
    public async Task<PagedResponse<ParticipantDto>> GetList([FromQuery] GetParticipantsListQuery query, CancellationToken ct)
    => await Mediator.Send(query, ct);

    [HttpGet("lookup")]
    public async Task<Response<List<Lookup<string>>>> Get([FromQuery] GetParticipantsLookupListQuery query, CancellationToken ct)
        => await Mediator.Send(query, ct);

    [HttpGet("live")]
    public async Task<Response<List<LiveParticipantDto>>> GetLive([FromQuery] GetLiveParticipantsQuery query, CancellationToken ct)
        => await Mediator.Send(query, ct);

    [HttpGet("{Id}")]
    public async Task<Response<ParticipantDto>> Get([FromRoute] GetParticipantQuery query, CancellationToken ct)
    => await Mediator.Send(query, ct);

    [HttpGet("balance-status")]
    public async Task<Response<ParticipantBalanceStatusDto>> GetBalanceStatus(CancellationToken ct)
        => await Mediator.Send(new GetParticipantBalanceStatusQuery(), ct);

    [HttpPost]
    public async Task<Response<string>> Post([FromBody] AddParticipantCommand query, CancellationToken ct)
        => await Mediator.Send(query, ct);

    [HttpPut]
    public async Task<Response<string>> Put([FromBody] EditParticipantCommand query, CancellationToken ct)
        => await Mediator.Send(query, ct);

    [HttpDelete("{Id}")]
    public async Task<Response<string>> Delete([FromRoute] DeleteParticipantCommand query, CancellationToken ct)
    => await Mediator.Send(query, ct);

}
